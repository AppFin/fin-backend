# TestUtils Documentation

## Overview

TestUtils is a utility class that provides testing infrastructure and pre-configured test data for unit and integration tests in the Fin system. It offers base classes, in-memory database contexts, and collections of reusable test data.

## Purpose

- Standardize test creation across the project
- Simplify test scenario configuration
- Provide consistent and reusable test data
- Manage isolated database contexts for each test
- Automatically configure ambient data and required providers

---

## Base Classes

### 1. BaseTest

Simple base class for tests that do not require database access.

#### Properties

```csharp
protected Mock<IDateTimeProvider> DateTimeProvider  // Mock for date/time control
protected AmbientData AmbientData                   // Application context data
```

#### Methods

```csharp
protected async Task ConfigureLoggedAmbientAsync(bool isAdmin = true)
```

**Description**: Configures AmbientData simulating an authenticated user.

**Parameters**:
- `isAdmin` (default: `true`): Defines if the user is an administrator

**When to use**: In tests that need to simulate a logged-in user but do not need to persist data in the database.

**Usage example**:
```csharp
public class MyServiceTest : TestUtils.BaseTest
{
    [Fact]
    public async Task MyTest()
    {
        // Arrange
        await ConfigureLoggedAmbientAsync(isAdmin: true);
        var service = new MyService(AmbientData);
        
        // Act & Assert...
    }
}
```

---

### 2. BaseTestWithContext

Base class for tests that require database access. Automatically creates a SQLite context in memory or file.

#### Properties

```csharp
protected readonly FinDbContext Context     // Entity Framework context
protected readonly UnitOfWork UnitOfWork    // Unit of Work for transactions
```

#### Characteristics

- Implements `IDisposable` for automatic cleanup
- Creates isolated SQLite database for each test
- Automatically configures interceptors (Audit and Tenant)
- Cleans up resources at test completion

#### Methods

##### `ConfigureLoggedAmbientAsync(bool isAdmin = true)`

Overridden version that persists the user in the database before configuring AmbientData.

**Difference from BaseTest**:
- `BaseTest`: Only simulates the user in AmbientData
- `BaseTestWithContext`: Creates the user in the database AND configures AmbientData

**Usage example**:
```csharp
public class TitleServiceTest : TestUtils.BaseTestWithContext
{
    [Fact]
    public async Task Create_ShouldSucceed()
    {
        // Arrange
        await ConfigureLoggedAmbientAsync(isAdmin: true);
        var service = GetService();
        
        // Act
        var result = await service.Create(input);
        
        // Assert
        result.Should().NotBeNull();
    }
}
```

##### `GetRepository<T>()`

Creates an instance of `IRepository<T>` connected to the test context.

**Usage example**:
```csharp
var titleCategoryRepository = GetRepository<TitleCategory>();
await titleCategoryRepository.AddAsync(titleCategory, true);
```

#### Resources Pattern

Common pattern to organize repositories using an internal Resources class:

```csharp
public class TitleCategoryServiceTest : TestUtils.BaseTestWithContext
{
    private TitleCategoryService GetService(Resources resources)
    {
        return new TitleCategoryService(resources.TitleCategoryRepository);
    }

    private Resources GetResources()
    {
        return new Resources
        {
            TitleCategoryRepository = GetRepository<TitleCategory>()
        };
    }

    private class Resources
    {
        public IRepository<TitleCategory> TitleCategoryRepository { get; set; }
    }
}
```

---

## Pre-configured Test Data

TestUtils provides static lists of ready-to-use test data. This data is consistent and reusable across all tests.

### 1. Guids (List<Guid>)

10 unique pre-defined GUIDs.

**Common usage**:
- Entity IDs
- Foreign keys
- Test identifiers

**Example**:
```csharp
var titleCategory = new TitleCategory(new TitleCategoryInput 
{ 
    Name = TestUtils.Strings[0],
    Color = TestUtils.Strings[1],
    Icon = TestUtils.Strings[3]
});

var result = await service.Get(TestUtils.Guids[9]); // Non-existent ID
```

### 2. Strings (List<string>)

10 varied strings for different purposes.

**Content**:
- `[0]`: "alpha-923" (identifier)
- `[1]`: "John Doe" (name)
- `[2]`: "sample@test.com" (email)
- `[3]`: "lorem ipsum" (text)
- `[4]`: "token_ABC123" (token)
- `[5]`: "password123!" (password)
- `[6]`: "Order#987654" (order)
- `[7]`: "Hello, World!" (message)
- `[8]`: "A1B2C3D4" (code)
- `[9]`: "Zebra@Night" (unique name)

**Example**:
```csharp
var input = new TitleCategoryInput
{
    Name = TestUtils.Strings[0],   // "alpha-923"
    Color = TestUtils.Strings[1],  // "John Doe"
    Icon = TestUtils.Strings[2]    // "sample@test.com"
};
```

### 3. Decimals (List<decimal>)

10 decimal values for financial tests.

**Content**:
- Positive, negative, and zero values
- Different scales (small, medium, large)
- Values with decimal places

**Example**:
```csharp
var walletInput = new WalletInput
{
    Name = TestUtils.Strings[0],
    InitialBalance = TestUtils.Decimals[0] // 100.00m
};
```

### 4. UtcDateTimes (List<DateTime>)

10 UTC dates/times for temporal tests.

**Characteristics**:
- All with `DateTimeKind.Utc`
- Cover different years (2023-2030)
- Different times of day

**Example**:
```csharp
var input = new TitleInput
{
    Date = TestUtils.UtcDateTimes[0], // 2023-01-01 00:00:00 UTC
    Value = 100m
};
```

### 5. TimeSpans (List<TimeSpan>)

10 time intervals for tests.

**Common usage**:
- Durations
- Intervals between events
- Times of day

### 6. CardBrands (List<CardBrand>)

5 pre-configured card brands.

**Example**:
```csharp
var cardBrand = TestUtils.CardBrands[0];
await repository.AddAsync(cardBrand, true);
```

### 7. FinancialInstitutions (List<FinancialInstitution>)

5 financial institutions with different types.

**Types included**:
- Bank
- DigitalBank
- FoodCard

### 8. WalletsInputs (List<WalletInput>)

5 ready-to-use inputs for creating wallets.

### 9. Wallets (List<Wallet>)

5 already instantiated Wallet entities.

**Example**:
```csharp
var wallet = TestUtils.Wallets[0];
await Context.Wallets.AddAsync(wallet);
await Context.SaveChangesAsync();
```

---

## TestDbContextFactory

Factory class responsible for creating and destroying database contexts for tests.

### `Create()`

Creates an isolated SQLite context for tests.

**Parameters**:
- `out SqliteConnection connection`: Returns the created connection
- `out string dbFilePath`: Returns the file path (if `useFile = true`)
- `IAmbientData ambientData`: Environment data
- `IDateTimeProvider dateTimeProvider`: Date/time provider
- `bool useFile` (default: `false`): Whether to use physical file or memory

**Operation modes**:

1. **Memory** (`useFile = false`):
    - Faster
    - Does not leave files on disk
    - Data is lost when connection closes

2. **File** (`useFile = true`):
    - Useful for debugging
    - Can inspect database after test
    - Slower
    - Requires file cleanup

**Automatic configuration**:
- Audit Interceptor (CreatedAt, UpdatedAt, CreatedBy, UpdatedBy)
- Tenant Interceptor (filtering by TenantId)
- Database schema created automatically (`EnsureCreated`)

**Internal usage**: Called automatically by `BaseTestWithContext` constructor.

### `Destroy()`

Cleans up resources and deletes database file if necessary.

**Parameters**:
- `SqliteConnection connection`: Connection to be closed
- `string dbFilePath`: File to be deleted (if exists)

**Internal usage**: Called automatically by `BaseTestWithContext.Dispose()`.

---

## Usage Patterns

### Pattern 1: Simple entity test (no database)

```csharp
public class TitleEntityTest
{
    [Fact]
    public void Constructor_ShouldInitialize()
    {
        // Arrange
        var input = new TitleInput
        {
            Value = TestUtils.Decimals[0],
            Type = TitleType.Income,
            Description = TestUtils.Strings[0],
            Date = TestUtils.UtcDateTimes[0],
            WalletId = TestUtils.Guids[0],
            TitleCategoriesIds = new List<Guid> 
            { 
                TestUtils.Guids[1], 
                TestUtils.Guids[2] 
            }
        };

        // Act
        var title = new Title(input, 50m);

        // Assert
        title.Should().NotBeNull();
        title.Value.Should().Be(TestUtils.Decimals[0]);
    }
}
```

### Pattern 2: Service test (with database)

```csharp
public class TitleCategoryServiceTest : TestUtils.BaseTestWithContext
{
    [Fact]
    public async Task Create_ShouldReturnSuccess_WhenInputIsValid()
    {
        // Arrange
        await ConfigureLoggedAmbientAsync();
        var resources = GetResources();
        var service = GetService(resources);

        var input = new TitleCategoryInput
        {
            Name = TestUtils.Strings[0],
            Color = TestUtils.Strings[1],
            Icon = TestUtils.Strings[2]
        };

        // Act
        var result = await service.Create(input, true);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        
        var dbEntity = await resources.TitleCategoryRepository
            .Query(false)
            .FirstOrDefaultAsync(a => a.Id == result.Data.Id);
        
        dbEntity.Should().NotBeNull();
        dbEntity.Name.Should().Be(input.Name);
    }

    private TitleCategoryService GetService(Resources resources)
    {
        return new TitleCategoryService(resources.TitleCategoryRepository);
    }

    private Resources GetResources()
    {
        return new Resources
        {
            TitleCategoryRepository = GetRepository<TitleCategory>()
        };
    }

    private class Resources
    {
        public IRepository<TitleCategory> TitleCategoryRepository { get; set; }
    }
}
```

### Pattern 3: Test with pre-populated data

```csharp
[Fact]
public async Task GetList_ShouldReturnOrderedResults()
{
    // Arrange
    await ConfigureLoggedAmbientAsync();
    var resources = GetResources();
    var service = GetService(resources);

    // Populate database with test data
    await resources.Repository.AddAsync(
        new TitleCategory(new TitleCategoryInput 
        { 
            Name = "C", 
            Color = TestUtils.Strings[1], 
            Icon = TestUtils.Strings[3] 
        }), true);
    
    await resources.Repository.AddAsync(
        new TitleCategory(new TitleCategoryInput 
        { 
            Name = "A", 
            Color = TestUtils.Strings[1], 
            Icon = TestUtils.Strings[3] 
        }), true);

    var input = new TitleCategoryGetListInput 
    { 
        MaxResultCount = 10, 
        SkipCount = 0 
    };

    // Act
    var result = await service.GetList(input);

    // Assert
    result.Items.First().Name.Should().Be("A");
    result.Items.Last().Name.Should().Be("C");
}
```

---

## Best Practices

### DO

1. **Use TestUtils data** for consistency:
   ```csharp
   Name = TestUtils.Strings[0]  // GOOD
   ```

2. **Always call `ConfigureLoggedAmbientAsync()`** in tests with `BaseTestWithContext`:
   ```csharp
   await ConfigureLoggedAmbientAsync();  // GOOD
   ```

3. **Use Resources pattern** to organize dependencies:
   ```csharp
   private Resources GetResources() { ... }  // GOOD
   ```

4. **Validate both return value and database state**:
   ```csharp
   result.Success.Should().BeTrue();
   var dbEntity = await repository.Query(false).FirstAsync(...);
   dbEntity.Should().NotBeNull();  // GOOD
   ```

### DO NOT

1. **Do not create hardcoded data**:
   ```csharp
   Name = "Test Category"  // BAD
   Name = TestUtils.Strings[0]  // GOOD
   ```

2. **Do not reuse the same index for different fields**:
   ```csharp
   Name = TestUtils.Strings[0],
   Color = TestUtils.Strings[0],  // BAD
   Icon = TestUtils.Strings[0]
   ```

   Better:
   ```csharp
   Name = TestUtils.Strings[0],
   Color = TestUtils.Strings[1],  // GOOD
   Icon = TestUtils.Strings[2]
   ```

3. **Do not forget to Dispose** (but `BaseTestWithContext` handles this automatically):
   ```csharp
   public class MyTest : BaseTestWithContext  // GOOD - Automatic Dispose
   ```

4. **Do not use BaseTestWithContext if you do not need the database**:
   ```csharp
   // Pure entity test? Use normal class or BaseTest
   public class TitleEntityTest  // GOOD
   
   // Service test? Use BaseTestWithContext
   public class TitleServiceTest : BaseTestWithContext  // GOOD
   ```

---

## Advantages of TestUtils

1. **Isolation**: Each test has its own database
2. **Automatic cleanup**: Dispose handles resource cleanup
3. **Consistency**: Standardized test data across the project
4. **Simplicity**: Less boilerplate code in each test
5. **Reusability**: Base classes prevent duplication
6. **Realism**: Interceptors work as in production
7. **Flexibility**: Supports in-memory and file-based tests

---

## Troubleshooting

### Problem: "Table X not found"

**Cause**: Context was not created correctly.

**Solution**: Verify you are inheriting from `BaseTestWithContext` and the database was created:
```csharp
public class MyTest : TestUtils.BaseTestWithContext  // CORRECT
```

### Problem: "TenantId null"

**Cause**: `ConfigureLoggedAmbientAsync()` was not called.

**Solution**:
```csharp
[Fact]
public async Task MyTest()
{
    await ConfigureLoggedAmbientAsync();  // ADD THIS
    // rest of test...
}
```

### Problem: "Guid already exists"

**Cause**: Attempting to insert entity with duplicate ID.

**Solution**: Use different IDs from `TestUtils.Guids` or let the constructor generate:
```csharp
var entity1 = new Entity { Id = TestUtils.Guids[0] };
var entity2 = new Entity { Id = TestUtils.Guids[1] };  // Different ID
```

### Problem: Test fails with "Database locked"

**Cause**: Attempting to access database with `useFile = true` concurrently.

**Solution**: Use `useFile = false` (default) or ensure tests do not execute in parallel.

---

## Summary

TestUtils is the foundation for all system tests. It:

- Provides testing infrastructure (database, ambient data)
- Offers ready-to-use test data
- Manages automatic resource cleanup
- Standardizes test structure
- Accelerates new test creation

Always use base classes and pre-configured data to maintain consistent and maintainable tests.