## Consolidated Code Patterns and Best Practices Documentation

This document synthesizes the architectural patterns and best practices observed across the `Wallet` and `Title` feature implementations. It serves as an extensive template for maintaining and extending the codebase, optimized for technical comprehension and AI processing.

-----

### I. Domain Layer: Encapsulation and Integrity

The Domain layer (`Entities` and related DTOs) enforces business rules and maintains data integrity through strict encapsulation.

#### 1\. Entity Encapsulation (State Protection)

* **Principle:** Entity properties must use `private set` (`Wallet.Name`, `Title.Value`).
* **Practice:** State mutation must occur exclusively through public methods defined on the entity itself (e.g., `Wallet.Update(input)`, `Title.ToggleInactivated()`). This ensures that business invariants are checked and maintained during any state change.

#### 2\. Entity Behavioral Logic

* **Principle:** Complex logic that relies solely on the entity's internal state belongs to the entity.
* **Practice:** Methods like `Wallet.CalculateBalanceAt(DateTime)` and calculated properties like `Title.EffectiveValue` and `Title.ResultingBalance` reside within the entity, guaranteeing data derivation is consistent with the current state.

#### 3\. Many-to-Many Synchronization

* **Principle:** Relationship maintenance logic should be contained within the owning entity.
* **Practice:** The `Title.SyncCategories` and `UpdateAndReturnCategoriesToRemove` methods abstract the complexity of managing the relational table (`TitleTitleCategory`), handling additions and deletions based on the input DTO list.

-----

### II. Application Layer: Flow Control and Standardization

The Application layer defines the boundaries and standardizes the input/output of feature operations.

#### 1\. Service Boundary and SRP

* **Interface Definition:** All services must implement a dedicated interface (e.g., `IWalletService`, `ITitleService`) to facilitate mocking, testing, and dependency inversion.
* **Delegation of Concerns:** Application Services strictly focus on **orchestration** (Transaction, Persistence, Flow Control) and delegate specialized concerns:
    * **Validation:** Delegated to the `IWalletValidationService` or `IValidationPipelineOrchestrator`.
    * **Complex Logic:** Delegated to specialized helper services (e.g., `ITitleUpdateHelpService` for balance reprocessing).

#### 2\. Standardized Result Handling

* **Pattern:** All mutating service operations must return a generic `ValidationResultDto<TSuccess, TErrorCode>`.
* **Benefit:** Provides a consistent contract for API controllers to map success data or specific business error codes to HTTP responses (200/201 vs. 422/404).

#### 3\. Optimized Query Projection

* **Principle:** Minimize memory consumption and processing time by projecting data early.
* **Practice:** For list operations, use `.Select(n => new OutputDTO(n))` directly on the `IQueryable<T>` before materialization (`.ToListAsync()`). This ensures only necessary fields are retrieved from the database and mapped efficiently.

-----

### III. Advanced Validation Pipeline Pattern

The system leverages a sophisticated, extensible validation pipeline for complex and cross-cutting checks.

#### 1\. Rule Modularity and Discovery

* **Contract:** Individual rules implement `IValidationRule<TInput, TErrorCode>` (or the fully parameterized version).
* **Discovery:** Rules are registered in DI and automatically discovered and executed by the `ValidationPipelineOrchestrator` based on the `TInput` type.

#### 2\. Fail-Fast Execution

* **Principle:** Minimize resource consumption by stopping validation immediately upon the first discovered error.
* **Practice:** The `ValidationPipelineOrchestrator` iterates through rules and returns as soon as a validation returns `!Success`.

#### 3\. Granular Error Reporting

* **Detailed Output:** Rules that require returning auxiliary data (e.g., lists of IDs that failed) use `ValidationPipelineOutput<TErrorCode, TErrorData>`.
    * *Example:* `TitleInputCategoriesValidation` returns a `List<Guid>` of categories not found or inactive.
* **Error Enums:** Error codes are defined using specific enumerations (e.g., `WalletCreateOrUpdateErrorCode`), increasing clarity and allowing for precise client-side error handling.

#### 4\. Validation Query Efficiency

* **Práctica:** Validation queries must be minimal and non-tracking (e.g., `repository.Query(tracking: false).AnyAsync(...)` or `FirstOrDefaultAsync(tracking: false)`). This avoids unnecessary data materialization and EF Core overhead.

-----

### IV. Complex Business Flow: Reprocessing and Transactions (Title Feature)

The `Title` update/delete operations illustrate best practices for managing complex state dependencies and transactional integrity.

#### 1\. Explicit Transaction Control

* **Pattern:** All critical financial operations that involve multiple database steps (`Update`, `Create`, `Delete`) are wrapped in an explicit Unit of Work scope.
  ```csharp
  await using (var scope = await unitOfWork.BeginTransactionAsync(cancellationToken)) { /* ... logic ... */ }
  ```
* **Benefit:** Guarantees atomicity (ACID), ensuring that if the balance re-calculation fails, the core entity change is rolled back.

#### 2\. State Context Management

* **Pattern:** Use an **immutable C\# `record`** (`UpdateTitleContext`) to capture the essential **previous state** of the entity *before* modification.
* **Benefit:** This historical context (`PreviousWalletId`, `PreviousDate`, etc.) is vital for correcting balances in dependent aggregates *after* the primary entity update is committed.

#### 3\. Delegated Reprocessing Logic

* **Specialized Service:** The complex logic for balance correction is isolated in `ITitleUpdateHelpService`.
* **Conditional Execution:** The entity method `Title.MustReprocess(input)` provides a quick check to execute the heavy reprocessing logic *only* when financial-critical fields (Value, Type, Date, WalletId) have changed.
* **Two-Wallet Correction:** The service explicitly manages the complex case of a Wallet change:
    1.  `ReprocessPreviousWallet`: Corrects the balance stream on the **old wallet** starting from the old date/title.
    2.  `ReprocessCurrentWallet`: Corrects the balance stream on the **new wallet** (or the current wallet) starting from the appropriate effective date.

-----

### V. Infrastructure and Data Access

#### 1\. EF Core Configuration (Integrity)

* **Precision:** Financial fields must enforce high precision via configuration: `.HasColumnType("numeric(19,4)").HasPrecision(19, 4)`.
* **Uniqueness:** Composite unique indexes are defined for tenant isolation (e.g., `builder.HasIndex(x => new {x.Name, x.TenantId}).IsUnique()`).
* **Relationship Restriction:** Foreign key constraints are set to `OnDelete(DeleteBehavior.Restrict)` for related financial entities (e.g., `Title` to `Wallet`), preventing accidental cascade deletions and enforcing application-level deletion checks.

#### 2\. Repository Tracking Control

* **Read Operations:** Use `repository.Query(false)` to disable change tracking for display and reading (performance).
* **Write Operations:** Use `repository.Query()` (default tracking enabled) before fetching the entity to be updated, minimizing database hits.

#### 3\. API Controller Mapping

* **Minimalism:** Controllers are thin wrappers.
* **Status Code Mapping:** Standardized mapping from the service result (`ValidationResultDto`) to HTTP status codes:
    * **Success:** `Created(201)` / `Ok(200)`.
    * **Business Error (Generic):** `UnprocessableEntity(422)`.
    * **Business Error (Specific):** Check for `*NotFound` error codes and return `NotFound(404)`.