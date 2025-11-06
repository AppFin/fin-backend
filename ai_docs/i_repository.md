# IRepository Documentation

## Overview

`IRepository<T>` is the generic repository interface that provides data access operations for entities in the Fin system. It abstracts Entity Framework Core operations and implements the Repository Pattern, offering a consistent API for CRUD operations across all entities.

## Interface Definition

```csharp
public interface IRepository<T> : IQueryable<T> where T : class
```

The repository implements `IQueryable<T>`, allowing it to be used directly in LINQ queries while providing additional data access methods.

---

## Properties

### Context

```csharp
FinDbContext Context { get; }
```

**Description**: Provides access to the underlying Entity Framework DbContext.

**Usage**: Use when you need direct access to the context for advanced scenarios.

**Example**:
```csharp
var repository = GetRepository<Wallet>();
var context = repository.Context;
```

**Warning**: Avoid using Context directly unless absolutely necessary. Prefer repository methods.

---

## Methods

### Query Operations

#### Direct Query on Repository (Recommended)

Since `IRepository<T>` implements `IQueryable<T>`, you can query directly on the repository without calling any method.

**How it works**: The repository itself is queryable with tracking enabled by default.

**Examples**:

```csharp
// Query directly on repository (with tracking by default)
var wallet = await repository
    .FirstOrDefaultAsync(w => w.Id == walletId);

// Modify and save
wallet.Name = "New Name";
await repository.UpdateAsync(wallet, true);

// Complex query with tracking
var activeWallets = await repository
    .Include(w => w.FinancialInstitution)
    .Where(w => !w.Inactivated)
    .Where(w => w.InitialBalance > 0)
    .OrderByDescending(w => w.CreatedAt)
    .Skip(10)
    .Take(20)
    .ToListAsync();
```

**For read-only queries, use AsNoTracking()**:

```csharp
// Read-only query (better performance)
var wallets = await repository.AsNoTracking()
    .Where(w => w.Inactivated == false)
    .OrderBy(w => w.Name)
    .ToListAsync();

// Read-only with includes
var wallet = await repository.AsNoTracking()
    .Include(w => w.FinancialInstitution)
    .FirstOrDefaultAsync(w => w.Id == walletId);
```

**Best Practices**:
- Query directly on repository for operations that modify entities
- Use `AsNoTracking()` for read-only queries (better performance)
- Prefer specific queries over loading all data

#### Query(bool tracking = true) - OBSOLETE

```csharp
[Obsolete("Unnecessary, now you can query direct on repository")]
IQueryable<T> Query(bool tracking = true);
```

**Status**: This method is obsolete and will be removed in future versions.

**Migration**:
```csharp
// OLD (obsolete)
var wallets = await repository.Query(false)
    .Where(w => !w.Inactivated)
    .ToListAsync();

// NEW (recommended)
var wallets = await repository.AsNoTracking()
    .Where(w => !w.Inactivated)
    .ToListAsync();

// OLD (obsolete)
var wallet = await repository.Query()
    .FirstOrDefaultAsync(w => w.Id == walletId);

// NEW (recommended)
var wallet = await repository
    .FirstOrDefaultAsync(w => w.Id == walletId);
```

**Do not use this method in new code.**

#### AsNoTracking()

```csharp
IQueryable<T> AsNoTracking();
```

**Description**: Returns a no-tracking query for read-only operations.

**Returns**: `IQueryable<T>` configured for no-tracking (better performance)

**When to use**: For all read-only queries where you won't modify the entities

**Examples**:
```csharp
// Simple read-only query
var wallets = await repository.AsNoTracking()
    .Where(w => !w.Inactivated)
    .ToListAsync();

// Count query
var count = await repository.AsNoTracking()
    .Where(w => !w.Inactivated)
    .CountAsync();

// Complex read-only query
var walletNames = await repository.AsNoTracking()
    .Where(w => !w.Inactivated)
    .Select(w => w.Name)
    .ToListAsync();
```

**Best Practice**: Always use `AsNoTracking()` for read-only queries to improve performance.

---

### Find Operations

#### FindAsync(object keyValue)

```csharp
Task<T?> FindAsync(object keyValue, CancellationToken cancellationToken = default);
```

**Description**: Finds an entity by its primary key value.

**Parameters**:
- `keyValue`: The primary key value (usually a Guid)
- `cancellationToken`: Optional cancellation token

**Returns**: The entity if found, `null` otherwise

**When to use**: When you know the primary key and need to fetch a single entity

**Example**:
```csharp
var wallet = await repository.FindAsync(walletId);
if (wallet == null)
{
    return NotFound();
}
```

#### FindAsync(object[] keyValues)

```csharp
Task<T?> FindAsync(object[] keyValues, CancellationToken cancellationToken = default);
```

**Description**: Finds an entity by composite primary key values.

**Parameters**:
- `keyValues`: Array of primary key values for composite keys
- `cancellationToken`: Optional cancellation token

**Returns**: The entity if found, `null` otherwise

**When to use**: When the entity has a composite primary key

**Example**:
```csharp
// For entities with composite keys (e.g., TitleTitleCategory)
var titleCategory = await repository.FindAsync(new object[] { titleId, categoryId });
```

**Note**: Most entities in Fin use single Guid keys, so the single-parameter version is more common.

---

### Create Operations

#### AddAsync(T entity, bool autoSave)

```csharp
Task AddAsync(T entity, bool autoSave = false, CancellationToken cancellationToken = default);
```

**Description**: Adds a new entity to the database.

**Parameters**:
- `entity`: The entity to add
- `autoSave` (default: `false`): Whether to automatically save changes
    - `true`: Calls SaveChanges immediately
    - `false`: Changes are staged but not saved (requires manual SaveChanges)
- `cancellationToken`: Optional cancellation token

**When to use**:
- Adding a single entity
- Use `autoSave: true` in tests or simple operations
- Use `autoSave: false` when adding multiple entities or within a transaction

**Examples**:

```csharp
// Simple add with auto-save (common in tests)
var wallet = new Wallet(input);
await repository.AddAsync(wallet, autoSave: true);

// Add without auto-save (requires manual save)
var wallet = new Wallet(input);
await repository.AddAsync(wallet, autoSave: false);
await repository.SaveChangesAsync();

// Multiple operations in a transaction
var wallet1 = new Wallet(input1);
var wallet2 = new Wallet(input2);
await repository.AddAsync(wallet1, autoSave: false);
await repository.AddAsync(wallet2, autoSave: false);
await repository.SaveChangesAsync(); // Save all at once
```

#### AddAsync(T entity)

```csharp
Task AddAsync(T entity, CancellationToken cancellationToken);
```

**Description**: Adds entity without auto-save (requires manual SaveChanges).

**Parameters**:
- `entity`: The entity to add
- `cancellationToken`: Cancellation token

**Example**:
```csharp
await repository.AddAsync(wallet, cancellationToken);
await repository.SaveChangesAsync(cancellationToken);
```

#### AddRangeAsync(IEnumerable<T> entities, bool autoSave)

```csharp
Task AddRangeAsync(IEnumerable<T> entities, bool autoSave = false, CancellationToken cancellationToken = default);
```

**Description**: Adds multiple entities in a single operation.

**Parameters**:
- `entities`: Collection of entities to add
- `autoSave` (default: `false`): Whether to automatically save changes
- `cancellationToken`: Optional cancellation token

**When to use**: When adding multiple entities at once (more efficient than multiple AddAsync calls)

**Example**:
```csharp
var wallets = new List<Wallet>
{
    new Wallet(input1),
    new Wallet(input2),
    new Wallet(input3)
};

await repository.AddRangeAsync(wallets, autoSave: true);
```

**Performance Note**: AddRangeAsync is more efficient than calling AddAsync multiple times.

#### AddRangeAsync(IEnumerable<T> entities)

```csharp
Task AddRangeAsync(IEnumerable<T> entities, CancellationToken cancellationToken);
```

**Description**: Adds multiple entities without auto-save.

**Example**:
```csharp
await repository.AddRangeAsync(wallets, cancellationToken);
await repository.SaveChangesAsync(cancellationToken);
```

---

### Update Operations

#### UpdateAsync(T entity, bool autoSave)

```csharp
Task UpdateAsync(T entity, bool autoSave = false, CancellationToken cancellationToken = default);
```

**Description**: Updates an existing entity in the database.

**Parameters**:
- `entity`: The entity with modified values
- `autoSave` (default: `false`): Whether to automatically save changes
- `cancellationToken`: Optional cancellation token

**When to use**:
- Updating entity properties
- Entity must be tracked by the context or explicitly marked as modified

**Examples**:

```csharp
// Update with auto-save
var wallet = await repository.FindAsync(walletId);
wallet.Name = "Updated Name";
await repository.UpdateAsync(wallet, autoSave: true);

// Update without auto-save
var wallet = await repository.Query().FirstAsync(w => w.Id == walletId);
wallet.Name = "Updated Name";
await repository.UpdateAsync(wallet, autoSave: false);
await repository.SaveChangesAsync();

// Multiple updates
var wallet1 = await repository.FindAsync(id1);
var wallet2 = await repository.FindAsync(id2);
wallet1.Name = "New Name 1";
wallet2.Name = "New Name 2";
await repository.UpdateAsync(wallet1, autoSave: false);
await repository.UpdateAsync(wallet2, autoSave: false);
await repository.SaveChangesAsync(); // Save all at once
```

**Important**: The entity must be tracked by the context. If you query with `tracking: false`, you must manually attach the entity before updating.

#### UpdateAsync(T entity)

```csharp
Task UpdateAsync(T entity, CancellationToken cancellationToken);
```

**Description**: Updates entity without auto-save.

**Example**:
```csharp
await repository.UpdateAsync(wallet, cancellationToken);
await repository.SaveChangesAsync(cancellationToken);
```

---

### Delete Operations

#### DeleteAsync(T entity, bool autoSave)

```csharp
Task DeleteAsync(T entity, bool autoSave = false, CancellationToken cancellationToken = default);
```

**Description**: Deletes an entity from the database.

**Parameters**:
- `entity`: The entity to delete
- `autoSave` (default: `false`): Whether to automatically save changes
- `cancellationToken`: Optional cancellation token

**When to use**: When you need to remove an entity from the database

**Examples**:

```csharp
// Delete with auto-save
var wallet = await repository.FindAsync(walletId);
if (wallet != null)
{
    await repository.DeleteAsync(wallet, autoSave: true);
}

// Delete without auto-save
var wallet = await repository.Query().FirstOrDefaultAsync(w => w.Id == walletId);
if (wallet != null)
{
    await repository.DeleteAsync(wallet, autoSave: false);
    await repository.SaveChangesAsync();
}

// Delete multiple entities
var walletsToDelete = await repository.Query()
    .Where(w => w.Inactivated)
    .ToListAsync();

foreach (var wallet in walletsToDelete)
{
    await repository.DeleteAsync(wallet, autoSave: false);
}
await repository.SaveChangesAsync(); // Delete all at once
```

#### DeleteAsync(T entity)

```csharp
Task DeleteAsync(T entity, CancellationToken cancellationToken);
```

**Description**: Deletes entity without auto-save.

**Example**:
```csharp
await repository.DeleteAsync(wallet, cancellationToken);
await repository.SaveChangesAsync(cancellationToken);
```

---

### Save Operations

#### SaveChangesAsync()

```csharp
Task SaveChangesAsync(CancellationToken cancellationToken = default);
```

**Description**: Persists all pending changes to the database.

**Parameters**:
- `cancellationToken`: Optional cancellation token

**When to use**: When you use `autoSave: false` on Add, Update, or Delete operations

**Example**:
```csharp
// Multiple operations in a single transaction
await repository.AddAsync(wallet1, autoSave: false);
await repository.AddAsync(wallet2, autoSave: false);
await repository.UpdateAsync(wallet3, autoSave: false);
await repository.DeleteAsync(wallet4, autoSave: false);

// All changes are saved together (atomic operation)
await repository.SaveChangesAsync();
```

**Important**: All changes since the last SaveChanges are saved together. If any operation fails, all changes are rolled back.

---

## Usage Patterns

### Pattern 1: Simple CRUD Operations

```csharp
// Create
var wallet = new Wallet(input);
await repository.AddAsync(wallet, autoSave: true);

// Read
var wallet = await repository.FindAsync(walletId);

// Update
wallet.Name = "New Name";
await repository.UpdateAsync(wallet, autoSave: true);

// Delete
await repository.DeleteAsync(wallet, autoSave: true);
```

### Pattern 2: Query with Filters

```csharp
// With tracking (for entities you will modify)
var wallet = await repository
    .FirstOrDefaultAsync(w => w.Id == walletId);
wallet.Name = "Updated";
await repository.UpdateAsync(wallet, autoSave: true);

// Read-only (better performance)
var activeWallets = await repository.AsNoTracking()
    .Where(w => !w.Inactivated)
    .OrderBy(w => w.Name)
    .ToListAsync();
```

### Pattern 3: Complex Query with Includes

```csharp
// With tracking
var wallet = await repository
    .Include(w => w.FinancialInstitution)
    .Include(w => w.Titles)
    .FirstOrDefaultAsync(w => w.Id == walletId);

// Read-only
var wallets = await repository.AsNoTracking()
    .Include(w => w.FinancialInstitution)
    .Where(w => !w.Inactivated)
    .ToListAsync();
```

### Pattern 4: Batch Operations

```csharp
var wallets = new List<Wallet>
{
    new Wallet(input1),
    new Wallet(input2),
    new Wallet(input3)
};

// All added in a single database round-trip
await repository.AddRangeAsync(wallets, autoSave: true);
```

### Pattern 5: Transaction with Multiple Operations

```csharp
// Start transaction (all or nothing)
var wallet = new Wallet(input);
await repository.AddAsync(wallet, autoSave: false);

var title = new Title(titleInput);
await titleRepository.AddAsync(title, autoSave: false);

// Save all changes atomically
await repository.SaveChangesAsync();
```

### Pattern 6: Pagination

```csharp
// Query with pagination
var pagedWallets = await repository.AsNoTracking()
    .Where(w => !w.Inactivated)
    .OrderBy(w => w.Name)
    .Skip(input.SkipCount)
    .Take(input.MaxResultCount)
    .ToListAsync();

// Count total (also read-only)
var totalCount = await repository.AsNoTracking()
    .Where(w => !w.Inactivated)
    .CountAsync();
```

---

## AutoSave Parameter Decision Guide

### Use `autoSave: true` when:
- Performing a single, simple operation
- In tests (for simplicity)
- Operation does not need to be part of a larger transaction
- No related entities need to be modified together

```csharp
// Single operation - use autoSave: true
var wallet = new Wallet(input);
await repository.AddAsync(wallet, autoSave: true);
```

### Use `autoSave: false` when:
- Performing multiple related operations
- Operations need to be atomic (all succeed or all fail)
- Working with related entities
- Performance optimization (batch saves)

```csharp
// Multiple operations - use autoSave: false
var wallet = new Wallet(input);
await repository.AddAsync(wallet, autoSave: false);

var title = new Title(titleInput);
await titleRepository.AddAsync(title, autoSave: false);

// Save all together (atomic)
await repository.SaveChangesAsync();
```

---

## Tracking vs No-Tracking

### Use Tracking (Direct Query on Repository):
- When you will modify the entity
- When you need change tracking
- For Update operations

```csharp
// Will modify - use tracking (query directly on repository)
var wallet = await repository
    .FirstOrDefaultAsync(w => w.Id == walletId);
wallet.Name = "New Name";
await repository.UpdateAsync(wallet, autoSave: true);
```

### Use No-Tracking (`AsNoTracking()`):
- Read-only operations
- Better performance (no change tracking overhead)
- When you won't modify the entity
- For queries that return many entities

```csharp
// Read-only - use no-tracking
var wallets = await repository.AsNoTracking()
    .Where(w => !w.Inactivated)
    .ToListAsync();
```

**Performance Impact**: No-tracking queries are significantly faster and use less memory, especially for large result sets.

---

## Common Patterns in Tests

### Test Pattern 1: Add and Verify

```csharp
[Fact]
public async Task Create_ShouldPersistToDatabase()
{
    // Arrange
    var repository = GetRepository<Wallet>();
    var wallet = new Wallet(input);

    // Act
    await repository.AddAsync(wallet, autoSave: true);

    // Assert - Verify in database
    var dbWallet = await repository.AsNoTracking()
        .FirstOrDefaultAsync(w => w.Id == wallet.Id);
    
    dbWallet.Should().NotBeNull();
    dbWallet.Name.Should().Be(wallet.Name);
}
```

### Test Pattern 2: Query with Filter

```csharp
[Fact]
public async Task GetList_ShouldFilterByInactivated()
{
    // Arrange
    var repository = GetRepository<Wallet>();
    await repository.AddAsync(new Wallet(activeInput), autoSave: true);
    
    var inactiveWallet = new Wallet(inactiveInput);
    inactiveWallet.ToggleInactivated();
    await repository.AddAsync(inactiveWallet, autoSave: true);

    // Act
    var inactiveWallets = await repository.AsNoTracking()
        .Where(w => w.Inactivated)
        .ToListAsync();

    // Assert
    inactiveWallets.Should().HaveCount(1);
}
```

### Test Pattern 3: Update and Verify

```csharp
[Fact]
public async Task Update_ShouldModifyEntity()
{
    // Arrange
    var repository = GetRepository<Wallet>();
    var wallet = new Wallet(input);
    await repository.AddAsync(wallet, autoSave: true);

    // Act
    wallet.Name = "Updated Name";
    await repository.UpdateAsync(wallet, autoSave: true);

    // Assert
    var dbWallet = await repository.AsNoTracking()
        .FirstAsync(w => w.Id == wallet.Id);
    
    dbWallet.Name.Should().Be("Updated Name");
}
```

---

## Best Practices

### DO

1. **Use AsNoTracking for read-only queries**:
```csharp
var wallets = await repository.AsNoTracking() // GOOD
    .Where(w => !w.Inactivated)
    .ToListAsync();
```

2. **Query directly on repository for tracking**:
```csharp
var wallet = await repository // GOOD - tracking enabled
    .FirstOrDefaultAsync(w => w.Id == walletId);
wallet.Name = "New Name";
await repository.UpdateAsync(wallet, autoSave: true);
```

3. **Use autoSave: true in tests**:
```csharp
await repository.AddAsync(wallet, autoSave: true); // GOOD in tests
```

4. **Use autoSave: false for multiple operations**:
```csharp
await repository.AddAsync(entity1, autoSave: false);
await repository.AddAsync(entity2, autoSave: false);
await repository.SaveChangesAsync(); // GOOD
```

5. **Use FindAsync for single entity by ID**:
```csharp
var wallet = await repository.FindAsync(walletId); // GOOD
```

6. **Check for null after queries**:
```csharp
var wallet = await repository.FindAsync(walletId);
if (wallet == null)
{
    return NotFound(); // GOOD
}
```

### DO NOT

1. **Do not use Query() method (obsolete)**:
```csharp
var wallets = await repository.Query(false) // BAD - obsolete
    .ToListAsync();

// Use this instead
var wallets = await repository.AsNoTracking() // GOOD
    .ToListAsync();
```

2. **Do not use tracking for read-only operations**:
```csharp
var wallets = await repository // BAD - unnecessary tracking
    .ToListAsync();

// Use this instead
var wallets = await repository.AsNoTracking() // GOOD
    .ToListAsync();
```

3. **Do not mix autoSave: true with transactions**:
```csharp
await repository.AddAsync(entity1, autoSave: true); // BAD
await repository.AddAsync(entity2, autoSave: true); // BAD
// These are separate transactions, not atomic

// Use this instead
await repository.AddAsync(entity1, autoSave: false); // GOOD
await repository.AddAsync(entity2, autoSave: false); // GOOD
await repository.SaveChangesAsync(); // Atomic
```

4. **Do not forget SaveChanges with autoSave: false**:
```csharp
await repository.AddAsync(wallet, autoSave: false); // BAD - changes not saved
// Add this
await repository.SaveChangesAsync(); // GOOD
```

5. **Do not load unnecessary data**:
```csharp
var wallets = await repository.AsNoTracking()
    .Include(w => w.Titles) // BAD - loading all titles when not needed
    .ToListAsync();
```

6. **Do not use direct query when FindAsync is sufficient**:
```csharp
var wallet = await repository
    .FirstOrDefaultAsync(w => w.Id == walletId); // BAD

// Use this instead
var wallet = await repository.FindAsync(walletId); // GOOD
```

---

## Performance Considerations

### Query Performance

**Efficient**:
```csharp
// Specific columns only
var names = await repository.AsNoTracking()
    .Select(w => new { w.Id, w.Name })
    .ToListAsync();

// Filtered before loading
var activeWallets = await repository.AsNoTracking()
    .Where(w => !w.Inactivated)
    .ToListAsync();
```

**Inefficient**:
```csharp
// Loading all then filtering in memory
var allWallets = await repository.AsNoTracking().ToListAsync();
var activeWallets = allWallets.Where(w => !w.Inactivated).ToList(); // BAD
```

### Batch Operations

**Efficient**:
```csharp
// Single database call
await repository.AddRangeAsync(wallets, autoSave: true);
```

**Inefficient**:
```csharp
// Multiple database calls
foreach (var wallet in wallets)
{
    await repository.AddAsync(wallet, autoSave: true); // BAD - N database calls
}
```

### Tracking Overhead

**Efficient**:
```csharp
// No tracking for read-only
var count = await repository.AsNoTracking().CountAsync();
```

**Inefficient**:
```csharp
// Unnecessary tracking overhead
var count = await repository.CountAsync(); // BAD - uses tracking by default
```

---

## Summary

`IRepository<T>` provides:

- **Abstraction** over Entity Framework Core
- **Consistent API** for all entities
- **Flexible querying** with IQueryable
- **Transaction support** with autoSave parameter
- **Performance optimization** with tracking control
- **Simplicity** in tests and production code

Always consider tracking requirements and transaction boundaries when using the repository to ensure optimal performance and data consistency.