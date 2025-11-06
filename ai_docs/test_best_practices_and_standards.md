# Testing Best Practices and Standards

## Overview

This document outlines the testing standards, patterns, and best practices used in the Fin system. It covers test organization, naming conventions, mocking strategies, and common patterns for different types of tests.

---

## Test Organization

### 1. Test Structure

Tests are organized following the same structure as the application:

```
Fin.Test/
├── Entities/
│   └── EntityNameTest.cs
├── Services/
│   ├── ServiceNameTest.cs
│   └── ValidationServiceNameTest.cs
└── Controllers/
    └── ControllerNameTest.cs
```

### 2. Test Class Naming

**Pattern**: `{ClassName}Test`

**Examples**:
- `TitleEntityTest` - Tests for Title entity
- `WalletServiceTest` - Tests for WalletService
- `WalletValidationServiceTest` - Tests for WalletValidationService
- `WalletControllerTest` - Tests for WalletController

### 3. Test Method Naming

**Pattern**: `{MethodName}_Should{ExpectedBehavior}_When{Condition}`

**Examples**:
```csharp
Get_ShouldReturnWallet_WhenExists
Create_ShouldReturnSuccess_WhenInputIsValid
Update_ShouldReturnFailure_WhenValidationFails
ValidateInput_ShouldReturnFailure_WhenNameIsRequired
```

**Alternative for simple scenarios**:
```csharp
Constructor_ShouldInitialize
ResultingBalance_ShouldBePositive_ForIncome
```

---

## Test Patterns by Layer

### Entity Tests

Entity tests verify domain logic without database access. Use plain test classes without base classes.

**Characteristics**:
- No database required
- No base class inheritance (unless using BaseTest for AmbientData)
- Focus on business logic and property calculations
- Test constructors, methods, and computed properties

**Example**:
```csharp
public class TitleEntityTest
{
    [Fact]
    public void Constructor_ShouldInitializeWithInputAndPreviousBalance()
    {
        // Arrange
        var input = new TitleInput
        {
            Value = TestUtils.Decimals[0],
            Type = TitleType.Income,
            Description = TestUtils.Strings[0],
            Date = TestUtils.UtcDateTimes[0],
            WalletId = TestUtils.Guids[0],
            TitleCategoriesIds = new List<Guid> { TestUtils.Guids[1] }
        };

        // Act
        var title = new Title(input, 50m);

        // Assert
        title.Should().NotBeNull();
        title.Value.Should().Be(TestUtils.Decimals[0]);
        title.PreviousBalance.Should().Be(50m);
    }

    [Fact]
    public void ResultingBalance_ShouldCalculateCorrectly_ForIncome()
    {
        // Arrange
        var input = new TitleInput
        {
            Value = 100m,
            Type = TitleType.Income,
            Description = TestUtils.Strings[0],
            Date = TestUtils.UtcDateTimes[0],
            WalletId = TestUtils.Guids[0],
            TitleCategoriesIds = new List<Guid>()
        };

        // Act
        var title = new Title(input, 50m);

        // Assert
        title.ResultingBalance.Should().Be(150m);
    }
}
```

---

### Service Tests

Service tests verify business logic with database interaction. Use `BaseTestWithContext` for database access.

**Characteristics**:
- Inherit from `BaseTestWithContext`
- Use Resources pattern for dependency organization
- Mock external service dependencies
- Test both success and failure scenarios
- Verify database state after operations

**Pattern Structure**:
```csharp
public class ServiceNameTest : TestUtils.BaseTestWithContext
{
    private readonly Mock<IDependencyService> _dependencyMock;

    public ServiceNameTest()
    {
        _dependencyMock = new Mock<IDependencyService>();
    }

    #region MethodName

    [Fact]
    public async Task MethodName_ShouldReturnSuccess_WhenValid()
    {
        // Arrange
        var resources = GetResources();
        var service = GetService(resources);
        // Setup test data...

        // Act
        var result = await service.MethodName(input);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        // Verify database state...
    }

    #endregion

    private ServiceName GetService(Resources resources)
    {
        return new ServiceName(
            resources.Repository,
            _dependencyMock.Object
        );
    }

    private Resources GetResources()
    {
        return new Resources
        {
            Repository = GetRepository<Entity>()
        };
    }

    private class Resources
    {
        public IRepository<Entity> Repository { get; set; }
    }
}
```

**Example**:
```csharp
public class WalletServiceTest : TestUtils.BaseTestWithContext
{
    private readonly Mock<IWalletValidationService> _validationServiceMock;

    public WalletServiceTest()
    {
        _validationServiceMock = new Mock<IWalletValidationService>();
    }

    #region Create

    [Fact]
    public async Task Create_ShouldReturnSuccessAndWallet_WhenInputIsValid()
    {
        // Arrange
        var resources = GetResources();
        var service = GetService(resources);
        var input = new WalletInput 
        { 
            Name = TestUtils.Strings[0], 
            Color = TestUtils.Strings[1], 
            Icon = TestUtils.Strings[2], 
            InitialBalance = 50.5m 
        };
        
        var successValidation = new ValidationResultDto<WalletOutput, WalletCreateOrUpdateErrorCode> 
        { 
            Success = true 
        };
        _validationServiceMock
            .Setup(v => v.ValidateInput<WalletOutput>(input, null))
            .ReturnsAsync(successValidation);

        // Act
        var result = await service.Create(input, true);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        
        var dbWallet = await resources.WalletRepository
            .Query(false)
            .FirstOrDefaultAsync(a => a.Id == result.Data.Id);
        
        dbWallet.Should().NotBeNull();
        dbWallet.Name.Should().Be(input.Name);
        dbWallet.InitialBalance.Should().Be(input.InitialBalance);
    }

    [Fact]
    public async Task Create_ShouldReturnFailure_WhenValidationFails()
    {
        // Arrange
        var resources = GetResources();
        var service = GetService(resources);
        var input = new WalletInput 
        { 
            Name = TestUtils.Strings[0], 
            Color = TestUtils.Strings[1], 
            Icon = TestUtils.Strings[2], 
            InitialBalance = 50.5m 
        };
        
        var failureValidation = new ValidationResultDto<WalletOutput, WalletCreateOrUpdateErrorCode>
        { 
            Success = false, 
            ErrorCode = WalletCreateOrUpdateErrorCode.NameIsRequired,
            Message = "Name is required."
        };
        _validationServiceMock
            .Setup(v => v.ValidateInput<WalletOutput>(input, null))
            .ReturnsAsync(failureValidation);

        // Act
        var result = await service.Create(input, true);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
        result.ErrorCode.Should().Be(WalletCreateOrUpdateErrorCode.NameIsRequired);
        result.Data.Should().BeNull();
        
        var count = await resources.WalletRepository.Query(false).CountAsync();
        count.Should().Be(0);
    }

    #endregion

    private WalletService GetService(Resources resources)
    {
        return new WalletService(
            resources.WalletRepository, 
            _validationServiceMock.Object, 
            DateTimeProvider.Object
        );
    }

    private Resources GetResources()
    {
        return new Resources
        {
            WalletRepository = GetRepository<Wallet>()
        };
    }

    private class Resources
    {
        public IRepository<Wallet> WalletRepository { get; set; }
    }
}
```

---

### Validation Service Tests

Validation services contain complex business rules. Tests focus on validation scenarios.

**Characteristics**:
- Inherit from `BaseTestWithContext`
- Test all validation rules independently
- Use Theory tests for multiple similar cases
- Mock external service dependencies
- Focus on error codes and messages

**Example**:
```csharp
public class WalletValidationServiceTest : TestUtils.BaseTestWithContext
{
    #region ValidateInput

    private WalletInput GetValidInput() => new()
    {
        Name = "New Wallet",
        Color = "#FFFFFF",
        Icon = "fa-icon",
        InitialBalance = 0m,
        FinancialInstitutionId = null
    };

    [Fact]
    public async Task ValidateInput_Create_ShouldReturnSuccess_WhenValid()
    {
        // Arrange
        var resources = GetResources();
        var service = GetService(resources);
        var input = GetValidInput();

        // Act
        var result = await service.ValidateInput<bool>(input);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public async Task ValidateInput_ShouldReturnFailure_WhenNameIsRequired(string name)
    {
        // Arrange
        var resources = GetResources();
        var service = GetService(resources);
        var input = GetValidInput();
        input.Name = name;

        // Act
        var result = await service.ValidateInput<bool>(input);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
        result.ErrorCode.Should().Be(WalletCreateOrUpdateErrorCode.NameIsRequired);
        result.Message.Should().Be("Name is required.");
    }

    [Fact]
    public async Task ValidateInput_ShouldReturnFailure_WhenNameTooLong()
    {
        // Arrange
        var resources = GetResources();
        var service = GetService(resources);
        var input = GetValidInput();
        input.Name = new string('A', 101);

        // Act
        var result = await service.ValidateInput<bool>(input);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
        result.ErrorCode.Should().Be(WalletCreateOrUpdateErrorCode.NameTooLong);
        result.Message.Should().Be("Name is too long. Max 100 characters.");
    }

    [Fact]
    public async Task ValidateInput_ShouldReturnFailure_WhenNameAlreadyInUseOnCreate()
    {
        // Arrange
        var resources = GetResources();
        var service = GetService(resources);
        var existingName = TestUtils.Strings[0];
        
        await resources.WalletRepository.AddAsync(
            new Wallet(new WalletInput 
            { 
                Name = existingName, 
                Color = TestUtils.Strings[1], 
                Icon = TestUtils.Strings[2], 
                InitialBalance = 0m 
            }), 
            true
        );

        var input = GetValidInput();
        input.Name = existingName;

        // Act
        var result = await service.ValidateInput<bool>(input);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
        result.ErrorCode.Should().Be(WalletCreateOrUpdateErrorCode.NameAlreadyInUse);
        result.Message.Should().Be("Name is already in use.");
    }

    #endregion

    private WalletValidationService GetService(Resources resources)
    {
        return new WalletValidationService(
            resources.WalletRepository,
            resources.CreditCardRepository,
            resources.TitleRepository,
            resources.FakeFinancialInstitution.Object
        );
    }

    private Resources GetResources()
    {
        return new Resources
        {
            WalletRepository = GetRepository<Wallet>(),
            CreditCardRepository = GetRepository<CreditCard>(),
            TitleRepository = GetRepository<Title>(),
            FakeFinancialInstitution = new Mock<IFinancialInstitutionService>()
        };
    }

    private class Resources
    {
        public IRepository<Wallet> WalletRepository { get; set; }
        public IRepository<CreditCard> CreditCardRepository { get; set; }
        public IRepository<Title> TitleRepository { get; set; }
        public Mock<IFinancialInstitutionService> FakeFinancialInstitution { get; set; }
    }
}
```

---

### Controller Tests

Controller tests verify HTTP response mapping and routing. Use `BaseTest` and mock all dependencies.

**Characteristics**:
- Inherit from `BaseTest` (no database needed)
- Mock all service dependencies
- Test HTTP status codes and response types
- Verify correct method calls to services
- Test all endpoint scenarios

**Pattern Structure**:
```csharp
public class ControllerNameTest : TestUtils.BaseTest
{
    private readonly Mock<IService> _serviceMock;
    private readonly ControllerName _controller;

    public ControllerNameTest()
    {
        _serviceMock = new Mock<IService>();
        _controller = new ControllerName(_serviceMock.Object);
    }

    #region MethodName

    [Fact]
    public async Task MethodName_ShouldReturnOk_WhenSuccess()
    {
        // Test implementation...
    }

    [Fact]
    public async Task MethodName_ShouldReturnNotFound_WhenNotExists()
    {
        // Test implementation...
    }

    #endregion
}
```

**Example**:
```csharp
public class WalletControllerTest : TestUtils.BaseTest
{
    private readonly Mock<IWalletService> _serviceMock;
    private readonly WalletController _controller;

    public WalletControllerTest()
    {
        _serviceMock = new Mock<IWalletService>();
        _controller = new WalletController(_serviceMock.Object);
    }

    #region Get

    [Fact]
    public async Task Get_ShouldReturnOk_WhenWalletExists()
    {
        // Arrange
        var walletId = TestUtils.Guids[0];
        var expectedWallet = new WalletOutput 
        { 
            Id = walletId, 
            Name = TestUtils.Strings[1] 
        };
        _serviceMock
            .Setup(s => s.Get(walletId))
            .ReturnsAsync(expectedWallet);

        // Act
        var result = await _controller.Get(walletId);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>()
            .Which.Value.Should().Be(expectedWallet);
    }

    [Fact]
    public async Task Get_ShouldReturnNotFound_WhenWalletDoesNotExist()
    {
        // Arrange
        var walletId = TestUtils.Guids[0];
        _serviceMock
            .Setup(s => s.Get(walletId))
            .ReturnsAsync((WalletOutput)null);

        // Act
        var result = await _controller.Get(walletId);

        // Assert
        result.Result.Should().BeOfType<NotFoundResult>();
    }

    #endregion

    #region Create

    [Fact]
    public async Task Create_ShouldReturnCreated_WhenInputIsValid()
    {
        // Arrange
        var input = new WalletInput 
        { 
            Name = TestUtils.Strings[1], 
            Color = TestUtils.Strings[2], 
            Icon = TestUtils.Strings[3], 
            InitialBalance = 100m 
        };
        var createdWallet = new WalletOutput 
        { 
            Id = TestUtils.Guids[0], 
            Name = TestUtils.Strings[1] 
        };
        var successResult = new ValidationResultDto<WalletOutput, WalletCreateOrUpdateErrorCode>
        {
            Success = true,
            Data = createdWallet
        };
        _serviceMock
            .Setup(s => s.Create(input, true))
            .ReturnsAsync(successResult);

        // Act
        var result = await _controller.Create(input);

        // Assert
        result.Result.Should().BeOfType<CreatedResult>()
            .Which.Value.Should().Be(createdWallet);

        var createdResult = result.Result as CreatedResult;
        createdResult.Location.Should().Be($"categories/{createdWallet.Id}");
    }

    [Fact]
    public async Task Create_ShouldReturnUnprocessableEntity_WhenValidationFails()
    {
        // Arrange
        var input = new WalletInput();
        var failureResult = new ValidationResultDto<WalletOutput, WalletCreateOrUpdateErrorCode>
        {
            Success = false,
            ErrorCode = WalletCreateOrUpdateErrorCode.NameIsRequired,
            Message = "Name is required."
        };
        _serviceMock
            .Setup(s => s.Create(input, true))
            .ReturnsAsync(failureResult);

        // Act
        var result = await _controller.Create(input);

        // Assert
        var unprocessableResult = result.Result
            .Should().BeOfType<UnprocessableEntityObjectResult>()
            .Subject;
        unprocessableResult.Value.Should().BeEquivalentTo(failureResult);
    }

    #endregion
}
```

---

## Common Testing Patterns

### 1. Arrange-Act-Assert (AAA)

Always use the AAA pattern for test organization:

```csharp
[Fact]
public async Task MethodName_ShouldExpectedBehavior_WhenCondition()
{
    // Arrange - Setup test data and dependencies
    var resources = GetResources();
    var service = GetService(resources);
    var input = CreateTestInput();

    // Act - Execute the method being tested
    var result = await service.MethodName(input);

    // Assert - Verify the results
    result.Should().NotBeNull();
    result.Success.Should().BeTrue();
}
```

### 2. Resources Pattern

Use a Resources class to organize dependencies:

```csharp
private Resources GetResources()
{
    return new Resources
    {
        Repository = GetRepository<Entity>(),
        AnotherRepository = GetRepository<AnotherEntity>(),
        ServiceMock = new Mock<IService>()
    };
}

private class Resources
{
    public IRepository<Entity> Repository { get; set; }
    public IRepository<AnotherEntity> AnotherRepository { get; set; }
    public Mock<IService> ServiceMock { get; set; }
}
```

### 3. Helper Methods

Create helper methods for common setup:

```csharp
private WalletInput GetValidInput() => new()
{
    Name = "Valid Name",
    Color = "#FFFFFF",
    Icon = "fa-icon",
    InitialBalance = 0m
};

private async Task<Wallet> CreateWalletInDatabase(Resources resources)
{
    var wallet = new Wallet(GetValidInput());
    await resources.WalletRepository.AddAsync(wallet, true);
    return wallet;
}
```

### 4. Theory Tests

Use Theory tests for multiple similar scenarios:

```csharp
[Theory]
[InlineData(null)]
[InlineData("")]
[InlineData(" ")]
public async Task Validate_ShouldReturnFailure_WhenNameIsInvalid(string name)
{
    // Arrange
    var input = GetValidInput();
    input.Name = name;

    // Act
    var result = await service.ValidateInput(input);

    // Assert
    result.Success.Should().BeFalse();
    result.ErrorCode.Should().Be(ErrorCode.NameIsRequired);
}
```

### 5. Database State Verification

Always verify database state after operations:

```csharp
[Fact]
public async Task Create_ShouldPersistToDatabase()
{
    // Arrange & Act
    var result = await service.Create(input, true);

    // Assert - Verify return value
    result.Success.Should().BeTrue();
    
    // Assert - Verify database state
    var dbEntity = await repository.Query(false)
        .FirstOrDefaultAsync(e => e.Id == result.Data.Id);
    
    dbEntity.Should().NotBeNull();
    dbEntity.Name.Should().Be(input.Name);
}
```

### 6. Mock Setup Pattern

Setup mocks clearly with expected behavior:

```csharp
// Success scenario
var successResult = new ValidationResultDto<Output, ErrorCode> 
{ 
    Success = true,
    Data = expectedData
};
_mockService
    .Setup(s => s.Method(input))
    .ReturnsAsync(successResult);

// Failure scenario
var failureResult = new ValidationResultDto<Output, ErrorCode>
{
    Success = false,
    ErrorCode = ErrorCode.SomeError,
    Message = "Error message."
};
_mockService
    .Setup(s => s.Method(input))
    .ReturnsAsync(failureResult);
```

---

## Test Organization by Method

### Region Organization

Use regions to group tests by method:

```csharp
public class ServiceTest : TestUtils.BaseTestWithContext
{
    #region Get

    [Fact]
    public async Task Get_ShouldReturnEntity_WhenExists()
    {
        // Test implementation...
    }

    [Fact]
    public async Task Get_ShouldReturnNull_WhenNotExists()
    {
        // Test implementation...
    }

    #endregion

    #region GetList

    [Fact]
    public async Task GetList_ShouldReturnPagedResult()
    {
        // Test implementation...
    }

    #endregion

    #region Create

    [Fact]
    public async Task Create_ShouldReturnSuccess_WhenValid()
    {
        // Test implementation...
    }

    [Fact]
    public async Task Create_ShouldReturnFailure_WhenInvalid()
    {
        // Test implementation...
    }

    #endregion
}
```

---

## Testing Tools and Libraries

### 1. xUnit

Main testing framework.

**Usage**:
```csharp
[Fact] // Single test
public void TestMethod() { }

[Theory] // Parameterized test
[InlineData(value1)]
[InlineData(value2)]
public void TestMethod(string value) { }
```

### 2. FluentAssertions

Readable assertion library.

**Common assertions**:
```csharp
// Null checks
result.Should().NotBeNull();
result.Should().BeNull();

// Boolean
result.Success.Should().BeTrue();
result.Success.Should().BeFalse();

// Equality
result.Name.Should().Be("Expected");
result.Id.Should().Be(expectedId);

// Collections
list.Should().HaveCount(3);
list.Should().BeEmpty();
list.Should().Contain(item);
list.First().Name.Should().Be("First");

// Types
result.Should().BeOfType<OkObjectResult>();
result.Result.Should().BeOfType<NotFoundResult>();

// Object comparison
result.Should().BeEquivalentTo(expected);

// Chaining
result.Should().NotBeNull()
    .And.BeOfType<ValidationResultDto>()
    .Which.Success.Should().BeTrue();
```

### 3. Moq

Mocking framework for dependencies.

**Common patterns**:
```csharp
// Create mock
var mock = new Mock<IService>();

// Setup method return
mock.Setup(s => s.Method(param))
    .ReturnsAsync(result);

// Setup with any parameter
mock.Setup(s => s.Method(It.IsAny<Type>()))
    .ReturnsAsync(result);

// Verify method was called
mock.Verify(s => s.Method(param), Times.Once);

// Setup property
mock.Setup(s => s.Property).Returns(value);
```

### 4. Entity Framework Core InMemory

SQLite in-memory database for testing.

**Usage** (handled by TestUtils):
```csharp
// Automatically configured in BaseTestWithContext
public class MyTest : TestUtils.BaseTestWithContext
{
    [Fact]
    public async Task MyTest()
    {
        // Context and repositories are ready to use
        var repository = GetRepository<Entity>();
    }
}
```

---

## Best Practices

### DO

1. **Use TestUtils data** for consistency:
```csharp
Name = TestUtils.Strings[0]
Id = TestUtils.Guids[0]
Date = TestUtils.UtcDateTimes[0]
```

2. **Test both success and failure paths**:
```csharp
MethodName_ShouldReturnSuccess_WhenValid
MethodName_ShouldReturnFailure_WhenInvalid
```

3. **Verify database state** after operations:
```csharp
var dbEntity = await repository.Query(false)
    .FirstOrDefaultAsync(e => e.Id == id);
dbEntity.Should().NotBeNull();
```

4. **Use descriptive test names**:
```csharp
Create_ShouldReturnFailure_WhenNameAlreadyInUse // GOOD
TestCreate1 // BAD
```

5. **Group tests** by method using regions

6. **Setup mocks clearly** with expected behavior

7. **Use Theory tests** for multiple similar scenarios

8. **Isolate tests** - each test should be independent

### DO NOT

1. **Do not share state** between tests:
```csharp
// BAD - Shared field
private Wallet _sharedWallet;

// GOOD - Create in each test
var wallet = new Wallet(input);
```

2. **Do not use magic strings**:
```csharp
Name = "Test Wallet" // BAD
Name = TestUtils.Strings[0] // GOOD
```

3. **Do not test multiple concerns** in one test:
```csharp
// BAD - Tests creation AND update
Create_ShouldCreateAndAllowUpdate

// GOOD - Separate tests
Create_ShouldReturnSuccess
Update_ShouldReturnSuccess
```

4. **Do not ignore Arrange section**:
```csharp
// BAD - Setup in Act
var result = await service.Create(new Input { Name = "Test" });

// GOOD - Clear Arrange
var input = new Input { Name = TestUtils.Strings[0] };
var result = await service.Create(input);
```

5. **Do not use inheritance** for test classes unless necessary (BaseTest, BaseTestWithContext)

---

## Testing Checklist

### Entity Tests
- [ ] Constructor with valid input
- [ ] Constructor with invalid input
- [ ] All public methods
- [ ] Computed properties/getters
- [ ] Business logic edge cases

### Service Tests
- [ ] Get - exists and not exists
- [ ] GetList - with and without filters
- [ ] GetList - pagination
- [ ] Create - success and all failure scenarios
- [ ] Update - success and all failure scenarios
- [ ] Delete - success and failure
- [ ] Toggle operations
- [ ] Database state verification

### Validation Service Tests
- [ ] All required field validations
- [ ] All length validations
- [ ] Unique constraint validations
- [ ] Foreign key validations
- [ ] Business rule validations
- [ ] Create vs Update scenarios

### Controller Tests
- [ ] All HTTP methods (GET, POST, PUT, DELETE)
- [ ] Success responses (Ok, Created)
- [ ] Error responses (NotFound, UnprocessableEntity)
- [ ] Response body verification
- [ ] Location header verification (for Created)

---

## Summary

The testing standards ensure:

- **Consistency** across all tests
- **Maintainability** through clear patterns
- **Coverage** of all scenarios
- **Isolation** of test cases
- **Readability** for future developers

Always follow these patterns and use TestUtils for consistent, maintainable tests.