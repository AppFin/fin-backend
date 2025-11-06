## QueryableExtensions C\# Documentation

This static class provides **fluent extension methods** for `IQueryable<T>` (Entity Framework Core) and `IEnumerable<T>`, enabling **dynamic, standardized data retrieval** through filtering, sorting, and pagination.

-----

### Core Functionality and Contracts Overview

The extensions facilitate building complex database queries using standardized **input contracts** derived from API requests.

| Function | Method | Input Contract | Key Mechanism |
| :--- | :--- | :--- | :--- |
| **Unified Retrieval** | `ApplyFilterAndSorter` (Preferred) | `IFilteredAndSortedInput` | Chained dynamic query building. |
| **Dynamic Sorting** | `ApplySorter` | `ISortedInput` | Expression Trees (`OrderBy`/`ThenBy`). |
| **Dynamic Filtering** | `ApplyFilter` | `IFilteredInput` | Database-optimized `LIKE` or `ILike` text search. |
| **Pagination** | `ToPagedResult` | `IPagedInput` | `CountAsync`, `Skip`, `Take`, `ToListAsync`. |
| **Conditional Where** | `WhereIf` | N/A | Conditional LINQ predicate application. |

-----

### Input Contracts and Models

These structures define the required format for client requests to interact with the extension methods.

#### Interfaces

| Interface | Purpose | Required Properties |
| :--- | :--- | :--- |
| `IPagedInput` | Defines pagination limits. | `SkipCount` (`int`), `MaxResultCount` (`int`) |
| `ISortedInput` | Defines a list of dynamic sort criteria. | `Sorts` (`List<SortedProperty>`) |
| `IFilteredInput` | Defines a single partial search/filter. | `Filter` (`FilteredProperty`) |
| `IFilteredAndSortedInput` | **Composite Contract.** Combines filtering and sorting. | Inherits `IFilteredInput` and `ISortedInput`. |

#### Data Models

| Model | Purpose | Key Fields | Notes |
| :--- | :--- | :--- | :--- |
| `SortedProperty` | Single sort instruction. | `Property` (`string`), `Desc` (`bool`) | Used by `ApplySorter`. Property name is matched **case-insensitively**. |
| `FilteredProperty` | Single text search criterion. | `Property` (`string`), `Filter` (`string`) | Used by `ApplyFilter`. Property must be a **string**. |
| `PagedOutput<T>` | Standard query output container. | `Items` (`List<T>`), `TotalCount` (`int`) | Returned by `ToPagedResult`. |
| `PagedFilteredAndSortedInput` | Concrete Request DTO. | Implements all input interfaces. | Provides defaults: `SkipCount = 0`, `MaxResultCount = 25`. |

-----

### Preferred Usage Pattern (Unified Retrieval)

**Always prefer `ApplyFilterAndSorter`** for applying dynamic criteria, as it ensures the correct sequence of operations (Filter then Sort) and maximizes code clarity.

```csharp
// Input DTO implements IFilteredAndSortedInput and IPagedInput
public async Task<PagedOutput<UserDto>> GetPagedUsers(UserQueryInput input, CancellationToken ct)
{
    IQueryable<User> query = _dbContext.Users;

    // 1. Apply unified criteria (Filter + Sort)
    query = query.ApplyFilterAndSorter(input); 

    // 2. Execute query and paginate
    return await query.ToPagedResult<UserDto>(input, ct);
}
```

-----

### Implementation Details (For Maintenance/Extensibility)

#### Dynamic Sorting (`ApplySorter`)

* Uses **Reflection** to locate the property and **Expression Trees** to dynamically construct the lambda expression for `OrderBy`/`ThenBy`.
* Relies on the internal `GetMethodName` helper to choose `OrderBy` (first sort) vs. `ThenBy` (subsequent sorts).

#### Dynamic Filtering (`ApplyFilter`)

* Applies a `LIKE '%value%'` pattern.
* **Database-Agnostic Handling:**
    * **PostgreSQL:** Uses `NpgsqlDbFunctionsExtensions.ILike` for native case-insensitivity.
    * **Others (e.g., SQL Server):** Uses `DbFunctionsExtensions.Like` combined with explicit `string.ToLower()` calls on both sides of the comparison for simulated case-insensitivity.
