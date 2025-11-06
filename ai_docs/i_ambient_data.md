# IAmbientData Documentation

## Overview

`IAmbientData` provides access to the current user's context information throughout the application. It stores and exposes data about the authenticated user, including tenant isolation, user identity, and authorization level. This is the single source of truth for "who is making the request" in the system.

## Purpose

- Store current user's context (tenant, user ID, display name, admin status)
- Enable multi-tenancy through tenant isolation
- Provide authorization information
- Support audit trails (who created/updated entities)
- Integrate with Entity Framework interceptors for automatic tenant filtering

---

## Interface Definition

```csharp
public interface IAmbientData
{
    public Guid? TenantId { get; }
    public Guid? UserId { get; }
    public string? DisplayName { get; }
    public bool IsAdmin { get; }
    public bool IsLogged { get; }

    public void SetData(Guid tenantId, Guid userId, string displayName, bool isAdmin);
    public void SetNotLogged();
}
```

---

## Properties

### TenantId

```csharp
public Guid? TenantId { get; }
```

**Description**: The unique identifier of the current user's tenant (organization).

**Type**: `Guid?` (nullable)

**Value**:
- `Guid`: When user is logged in
- `null`: When no user is authenticated

**Usage**:
- Multi-tenancy data isolation
- Automatic tenant filtering in queries
- Tenant assignment on entity creation

**Example**:
```csharp
var currentTenantId = _ambientData.TenantId;
if (currentTenantId.HasValue)
{
    // User is logged in with a tenant
    var wallets = await repository.AsNoTracking()
        .Where(w => w.TenantId == currentTenantId.Value)
        .ToListAsync();
}
```

**Important**: All tenant-scoped entities automatically have their `TenantId` set by the `TenantEntityInterceptor`.

---

### UserId

```csharp
public Guid? UserId { get; }
```

**Description**: The unique identifier of the currently authenticated user.

**Type**: `Guid?` (nullable)

**Value**:
- `Guid`: When user is logged in
- `null`: When no user is authenticated

**Usage**:
- Identify who is performing the action
- Audit trails (CreatedBy, UpdatedBy)
- User-specific filtering

**Example**:
```csharp
var currentUserId = _ambientData.UserId;
if (currentUserId.HasValue)
{
    var myWallets = await repository.AsNoTracking()
        .Where(w => w.CreatedBy == currentUserId.Value)
        .ToListAsync();
}
```

**Important**: All audited entities automatically have `CreatedBy` and `UpdatedBy` set by the `AuditedEntityInterceptor`.

---

### DisplayName

```csharp
public string? DisplayName { get; }
```

**Description**: The display name of the currently authenticated user.

**Type**: `string?` (nullable)

**Value**:
- `string`: User's display name when logged in
- `null`: When no user is authenticated

**Usage**:
- Display current user information in UI
- Logging and error messages
- User-friendly audit information

**Example**:
```csharp
var userName = _ambientData.DisplayName ?? "Anonymous";
_logger.LogInformation($"Action performed by {userName}");
```

---

### IsAdmin

```csharp
public bool IsAdmin { get; }
```

**Description**: Indicates whether the current user has administrator privileges.

**Type**: `bool`

**Value**:
- `true`: User is an administrator
- `false`: User is a regular user or not logged in

**Usage**:
- Authorization checks
- Feature access control
- Admin-only operations

**Example**:
```csharp
if (_ambientData.IsAdmin)
{
    // Allow admin-only operation
    await adminService.PerformAdminAction();
}
else
{
    return Forbidden();
}
```

**Best Practice**: Use this for coarse-grained authorization. For fine-grained permissions, implement a proper permission system.

---

### IsLogged

```csharp
public bool IsLogged { get; }
```

**Description**: Indicates whether a user is currently authenticated.

**Type**: `bool`

**Value**:
- `true`: User is authenticated (TenantId and UserId are not null)
- `false`: No user is authenticated

**Usage**:
- Check authentication status
- Guard clauses for authenticated-only operations
- Conditional logic based on authentication

**Example**:
```csharp
if (!_ambientData.IsLogged)
{
    return Unauthorized();
}

// Proceed with authenticated operation
var userId = _ambientData.UserId.Value; // Safe because IsLogged is true
```

**Implementation Note**: Typically returns `TenantId.HasValue && UserId.HasValue`.

---

## Methods

### SetData

```csharp
public void SetData(Guid tenantId, Guid userId, string displayName, bool isAdmin);
```

**Description**: Sets the ambient data for an authenticated user.

**Parameters**:
- `tenantId`: The tenant (organization) identifier
- `userId`: The user identifier
- `displayName`: The user's display name
- `isAdmin`: Whether the user is an administrator

**When to use**:
- After successful authentication
- In middleware after validating JWT token
- In tests to simulate logged-in users

**Example (Authentication Middleware)**:
```csharp
public class AuthenticationMiddleware
{
    private readonly IAmbientData _ambientData;

    public async Task InvokeAsync(HttpContext context)
    {
        var token = context.Request.Headers["Authorization"].ToString();
        
        if (!string.IsNullOrEmpty(token))
        {
            var claims = ValidateToken(token);
            
            _ambientData.SetData(
                tenantId: Guid.Parse(claims.TenantId),
                userId: Guid.Parse(claims.UserId),
                displayName: claims.DisplayName,
                isAdmin: claims.IsAdmin
            );
        }
        
        await _next(context);
    }
}
```

**Example (Tests)**:
```csharp
[Fact]
public async Task Create_ShouldSetCreatedBy_WhenUserIsLogged()
{
    // Arrange
    await ConfigureLoggedAmbientAsync(isAdmin: false);
    // This sets TenantId, UserId, DisplayName, and IsAdmin
    
    var service = GetService();
    
    // Act
    var result = await service.Create(input, true);
    
    // Assert
    result.Data.CreatedBy.Should().Be(_ambientData.UserId);
}
```

**Important**: This method should only be called by authentication/authorization infrastructure, not by business logic.

---

### SetNotLogged

```csharp
public void SetNotLogged();
```

**Description**: Clears the ambient data, indicating no user is authenticated.

**When to use**:
- After logout
- When authentication fails
- In middleware when no valid token is present
- In tests for anonymous scenarios

**Example (Logout)**:
```csharp
public class AuthenticationService
{
    private readonly IAmbientData _ambientData;
    
    public void Logout()
    {
        _ambientData.SetNotLogged();
        // Clear session, cookies, etc.
    }
}
```

**Example (Tests)**:
```csharp
[Fact]
public async Task Get_ShouldReturnUnauthorized_WhenNotLogged()
{
    // Arrange
    _ambientData.SetNotLogged(); // Simulate anonymous user
    var controller = new WalletController(_service);
    
    // Act
    var result = await controller.GetList();
    
    // Assert
    result.Should().BeOfType<UnauthorizedResult>();
}
```

**Effect**: After calling this method:
- `TenantId` becomes `null`
- `UserId` becomes `null`
- `DisplayName` becomes `null`
- `IsAdmin` becomes `false`
- `IsLogged` becomes `false`

---

## Usage Patterns

### Pattern 1: Service with User Context

```csharp
public class WalletService
{
    private readonly IRepository<Wallet> _repository;
    private readonly IAmbientData _ambientData;
    
    public WalletService(
        IRepository<Wallet> repository,
        IAmbientData ambientData)
    {
        _repository = repository;
        _ambientData = ambientData;
    }
    
    public async Task<WalletOutput> Get(Guid id)
    {
        if (!_ambientData.IsLogged)
        {
            throw new UnauthorizedException();
        }
        
        // TenantId automatically filtered by interceptor
        var wallet = await _repository.FindAsync(id);
        
        if (wallet == null)
        {
            return null;
        }
        
        return MapToOutput(wallet);
    }
}
```

### Pattern 2: Admin-Only Operation

```csharp
public class AdminService
{
    private readonly IAmbientData _ambientData;
    
    public async Task<Result> PerformAdminOperation()
    {
        if (!_ambientData.IsLogged)
        {
            return Result.Unauthorized();
        }
        
        if (!_ambientData.IsAdmin)
        {
            return Result.Forbidden();
        }
        
        // Perform admin-only operation
        await DoAdminStuff();
        
        return Result.Success();
    }
}
```

### Pattern 3: Audit Logging

```csharp
public class AuditLogger
{
    private readonly IAmbientData _ambientData;
    private readonly ILogger _logger;
    
    public void LogAction(string action, string entityType, Guid entityId)
    {
        var userName = _ambientData.DisplayName ?? "Anonymous";
        var userId = _ambientData.UserId?.ToString() ?? "N/A";
        var tenantId = _ambientData.TenantId?.ToString() ?? "N/A";
        
        _logger.LogInformation(
            "Action: {Action}, EntityType: {EntityType}, EntityId: {EntityId}, " +
            "User: {UserName} ({UserId}), Tenant: {TenantId}",
            action, entityType, entityId, userName, userId, tenantId
        );
    }
}
```

### Pattern 4: User-Specific Query

```csharp
public class MyItemsService
{
    private readonly IRepository<Item> _repository;
    private readonly IAmbientData _ambientData;
    
    public async Task<List<ItemOutput>> GetMyItems()
    {
        if (!_ambientData.IsLogged)
        {
            return new List<ItemOutput>();
        }
        
        var currentUserId = _ambientData.UserId.Value;
        
        var items = await _repository.AsNoTracking()
            .Where(i => i.CreatedBy == currentUserId)
            .OrderByDescending(i => i.CreatedAt)
            .ToListAsync();
        
        return items.Select(MapToOutput).ToList();
    }
}
```

### Pattern 5: Conditional Authorization

```csharp
public class WalletService
{
    private readonly IAmbientData _ambientData;
    
    public async Task<Result> Delete(Guid walletId)
    {
        if (!_ambientData.IsLogged)
        {
            return Result.Unauthorized();
        }
        
        var wallet = await _repository.FindAsync(walletId);
        
        if (wallet == null)
        {
            return Result.NotFound();
        }
        
        // Allow if admin OR if user owns the wallet
        if (!_ambientData.IsAdmin && wallet.CreatedBy != _ambientData.UserId)
        {
            return Result.Forbidden();
        }
        
        await _repository.DeleteAsync(wallet, autoSave: true);
        
        return Result.Success();
    }
}
```

---

## Integration with Entity Framework

### Automatic Tenant Filtering

The `TenantEntityInterceptor` automatically filters queries by `TenantId`:

```csharp
// This query
var wallets = await repository.AsNoTracking().ToListAsync();

// Automatically becomes
var wallets = await repository.AsNoTracking()
    .Where(w => w.TenantId == _ambientData.TenantId)
    .ToListAsync();
```

**Entities affected**: All entities implementing `ITenantEntity` interface.

### Automatic Audit Fields

The `AuditedEntityInterceptor` automatically sets audit fields:

```csharp
// On Create
entity.CreatedBy = _ambientData.UserId;
entity.CreatedAt = DateTime.UtcNow;

// On Update
entity.UpdatedBy = _ambientData.UserId;
entity.UpdatedAt = DateTime.UtcNow;
```

**Entities affected**: All entities implementing `IAuditedEntity` or `IAuditedTenantEntity` interfaces.

---

## Usage in Tests

### Test Pattern 1: Simulating Logged User

```csharp
public class WalletServiceTest : TestUtils.BaseTestWithContext
{
    [Fact]
    public async Task Create_ShouldSucceed_WhenUserIsLogged()
    {
        // Arrange
        await ConfigureLoggedAmbientAsync(isAdmin: false);
        // Sets TenantId, UserId, DisplayName, IsAdmin
        
        var service = GetService();
        var input = new WalletInput { /* ... */ };
        
        // Act
        var result = await service.Create(input, true);
        
        // Assert
        result.Success.Should().BeTrue();
        
        // Verify ambient data was used
        var dbWallet = await repository.FindAsync(result.Data.Id);
        dbWallet.CreatedBy.Should().Be(AmbientData.UserId);
        dbWallet.TenantId.Should().Be(AmbientData.TenantId);
    }
}
```

### Test Pattern 2: Simulating Admin User

```csharp
[Fact]
public async Task DeleteAll_ShouldSucceed_WhenUserIsAdmin()
{
    // Arrange
    await ConfigureLoggedAmbientAsync(isAdmin: true);
    
    var service = GetService();
    
    // Act
    var result = await service.DeleteAll();
    
    // Assert
    result.Success.Should().BeTrue();
}
```

### Test Pattern 3: Simulating Anonymous User

```csharp
[Fact]
public async Task Get_ShouldReturnUnauthorized_WhenNotLogged()
{
    // Arrange
    AmbientData.SetNotLogged();
    var service = GetService();
    
    // Act & Assert
    await Assert.ThrowsAsync<UnauthorizedException>(
        async () => await service.Get(Guid.NewGuid())
    );
}
```

### Test Pattern 4: Multiple Users (Advanced)

```csharp
[Fact]
public async Task User_ShouldOnlySeeOwnTenantData()
{
    // Arrange - User 1
    await ConfigureLoggedAmbientAsync(isAdmin: false);
    var tenant1Id = AmbientData.TenantId.Value;
    
    var wallet1 = new Wallet(input1);
    await repository.AddAsync(wallet1, autoSave: true);
    
    // Switch to User 2 (different tenant)
    AmbientData.SetData(
        tenantId: TestUtils.Guids[5], // Different tenant
        userId: TestUtils.Guids[6],
        displayName: "User 2",
        isAdmin: false
    );
    
    var wallet2 = new Wallet(input2);
    await repository.AddAsync(wallet2, autoSave: true);
    
    // Act - Query as User 1
    AmbientData.SetData(tenant1Id, TestUtils.Guids[0], "User 1", false);
    
    var walletsForUser1 = await repository.AsNoTracking().ToListAsync();
    
    // Assert - Should only see User 1's tenant data
    walletsForUser1.Should().HaveCount(1);
    walletsForUser1.First().Id.Should().Be(wallet1.Id);
}
```

---

## Best Practices

### DO

1. **Always check IsLogged before accessing user data**:
```csharp
if (!_ambientData.IsLogged)
{
    throw new UnauthorizedException(); // GOOD
}

var userId = _ambientData.UserId.Value; // Safe
```

2. **Use IsAdmin for coarse-grained authorization**:
```csharp
if (_ambientData.IsAdmin)
{
    // Allow admin operation
} // GOOD
```

3. **Inject IAmbientData in services that need user context**:
```csharp
public class MyService
{
    private readonly IAmbientData _ambientData;
    
    public MyService(IAmbientData ambientData) // GOOD
    {
        _ambientData = ambientData;
    }
}
```

4. **Use ConfigureLoggedAmbientAsync in tests**:
```csharp
await ConfigureLoggedAmbientAsync(isAdmin: true); // GOOD
```

5. **Provide default values for nullable properties**:
```csharp
var userName = _ambientData.DisplayName ?? "Anonymous"; // GOOD
```

### DO NOT

1. **Do not access UserId or TenantId without checking IsLogged**:
```csharp
var userId = _ambientData.UserId.Value; // BAD - might be null

// Do this instead
if (!_ambientData.IsLogged)
{
    throw new UnauthorizedException();
}
var userId = _ambientData.UserId.Value; // GOOD
```

2. **Do not call SetData from business logic**:
```csharp
public class WalletService
{
    public void SomeMethod()
    {
        _ambientData.SetData(...); // BAD - only authentication should set this
    }
}
```

3. **Do not use ambient data for fine-grained permissions**:
```csharp
if (_ambientData.IsAdmin)
{
    // Only check: can user delete wallet X?
    // For complex permissions, use a proper permission system
} // Consider using a permission service instead
```

4. **Do not store mutable state in ambient data**:
```csharp
// AmbientData is for read-only user context
// Do not try to modify user state through it
```

5. **Do not bypass tenant filtering**:
```csharp
// The interceptor handles this automatically
// Do not try to query cross-tenant unless you're building admin tools
var wallets = await repository.AsNoTracking()
    .Where(w => w.TenantId != _ambientData.TenantId) // BAD - bypassing isolation
    .ToListAsync();
```

---

## Common Scenarios

### Scenario 1: Checking if User Can Modify Entity

```csharp
public async Task<bool> CanModify(Guid entityId)
{
    if (!_ambientData.IsLogged)
        return false;
    
    // Admins can modify anything
    if (_ambientData.IsAdmin)
        return true;
    
    var entity = await repository.FindAsync(entityId);
    
    // User can modify if they created it
    return entity?.CreatedBy == _ambientData.UserId;
}
```

### Scenario 2: Filtering by Current User

```csharp
public async Task<List<Wallet>> GetMyWallets()
{
    if (!_ambientData.IsLogged)
        return new List<Wallet>();
    
    return await repository.AsNoTracking()
        .Where(w => w.CreatedBy == _ambientData.UserId.Value)
        .ToListAsync();
}
```

### Scenario 3: Admin Override

```csharp
public async Task<List<Wallet>> GetWallets(Guid? specificUserId = null)
{
    if (!_ambientData.IsLogged)
        throw new UnauthorizedException();
    
    // Admin can query any user's wallets
    if (_ambientData.IsAdmin && specificUserId.HasValue)
    {
        return await repository.AsNoTracking()
            .Where(w => w.CreatedBy == specificUserId.Value)
            .ToListAsync();
    }
    
    // Regular users only see their own
    return await repository.AsNoTracking()
        .Where(w => w.CreatedBy == _ambientData.UserId.Value)
        .ToListAsync();
}
```

---

## Troubleshooting

### Problem: TenantId is null in service

**Cause**: User is not authenticated or SetData was not called.

**Solution**:
```csharp
if (!_ambientData.IsLogged)
{
    throw new UnauthorizedException("User must be logged in");
}
```

### Problem: Entity has wrong TenantId

**Cause**: TenantEntityInterceptor not configured or ambient data set incorrectly.

**Solution**: Ensure interceptor is registered in DbContext and ambient data is set correctly in middleware.

### Problem: Cannot access data from other tenant (even as admin)

**Cause**: TenantEntityInterceptor filters by current tenant automatically.

**Solution**: For admin cross-tenant access, you may need to bypass the interceptor or use Context directly (not recommended for most cases).

### Problem: CreatedBy/UpdatedBy not set

**Cause**: AuditedEntityInterceptor not configured or user not logged in.

**Solution**: Ensure interceptor is registered and user is authenticated.

---

## Summary

`IAmbientData` provides:

- **User Context**: Access to current user's identity and tenant
- **Multi-Tenancy**: Automatic tenant isolation through interceptors
- **Authorization**: Basic admin/user role checking
- **Audit Trail**: Automatic tracking of who created/updated entities
- **Testability**: Easy simulation of different user contexts in tests

Always check `IsLogged` before accessing user-specific properties and use the ambient data as a read-only source of truth for the current user's context.