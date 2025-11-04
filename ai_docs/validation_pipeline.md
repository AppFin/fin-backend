## Validation Pipeline System Documentation

This documentation covers the core components for implementing **complex, sequential validation logic** using a Pipeline pattern based on Dependency Injection (DI) and C\# generics.

-----

### I. Core Data Contract: `ValidationPipelineOutput`

This class hierarchy is the standardized result structure returned by all validation rules and the orchestrator. It ensures the validation outcome is clear and consistent.

#### `ValidationPipelineOutput<TErrorCode, TErrorData>`

This is the fully parameterized output, including an error code and optional detailed error data.

| Generic | Constraint | Description |
| :--- | :--- | :--- |
| `TErrorCode` | `struct` | The enumeration type defining specific error codes. |
| `TErrorData` | None | Type for detailed data about the error (e.g., field names, validation failure counts). |

| Property | Type | Description |
| :--- | :--- | :--- |
| **`Success`** | `bool` | Returns `true` if `Code` is `null` (no error found). |
| `Code` | `TErrorCode?` | The specific error code if validation failed. |
| `Data` | `TErrorData?` | Optional detailed error payload. |

**Fluent Methods:**

* `AddError(TErrorCode code, TErrorData? data)`: Sets the error code and error data.
* `AddError(TErrorCode code)`: Sets only the error code.

#### `ValidationPipelineOutput<TErrorCode>`

The simplified output used when the validation rule does not need to return additional error data.

* Inherits `Success` logic and includes the `Code` property.

-----

### II. The Rules: `IValidationRule`

These interfaces define the contract for individual validation steps that will be executed by the orchestrator. Rules are automatically discovered via DI based on the input type (`TInput`).

#### `IValidationRule<TInput, TErrorCode, TErrorData>`

* **Usage:** For complex rules that may return **detailed error data** (`TErrorData`).
* **Method:** `Task<ValidationPipelineOutput<TErrorCode, TErrorData>> ValidateAsync(TInput input, Guid? editingId = null, CancellationToken cancellationToken = default)`

#### `IValidationRule<TInput, TErrorCode>`

* **Usage:** For simple rules that only need to return an **error code**.
* **Method:** `Task<ValidationPipelineOutput<TErrorCode>> ValidateAsync(TInput titleId, Guid? editingId = null, CancellationToken cancellationToken = default)`

-----

### III. The Execution Engine: `ValidationPipelineOrchestrator`

The orchestrator is responsible for discovering all registered validation rules for a given input type (`TInput`) and executing them sequentially. Execution stops immediately upon the first failure (`Success == false`).

#### `IValidationPipelineOrchestrator`

Defines the service contract for triggering the pipeline validation.

#### `ValidationPipelineOrchestrator` Implementation

* **Dependency:** Requires `IServiceProvider` (injected via constructor) to dynamically fetch all rules registered in DI.
* **DI Registration:** Implements `IAutoTransient`, suggesting it is registered as a transient service.

**Execution Flow (in `Validate<TInput, TErrorCode, TErrorData>`):**

1.  **Iterates** through all registered `IValidationRule<TInput, TErrorCode>` (Rules without data).
2.  **If any rule fails**, it immediately wraps the result into the full `TErrorData` output and returns.
3.  **If all simple rules pass**, it iterates through all registered `IValidationRule<TInput, TErrorCode, TErrorData>` (Rules with data).
4.  **If any rule fails**, it immediately returns the specific `TErrorData` output.
5.  **If all rules pass**, it returns a successful `ValidationPipelineOutput`.

-----

###  Usage Example (C\#)

This illustrates how a system service would trigger the pipeline and how a rule is implemented.

#### 1\. Example Error Enumeration

```csharp
public enum UserValidationError {
    EmailFormatInvalid = 1,
    UserAlreadyExists = 2,
    PasswordTooWeak = 3
}

public class UserErrorDetails { public string Field { get; set; } }
```

#### 2\. Example Rule Implementation

```csharp
// The rule validates email format and returns specific error details
public class EmailFormatRule : IValidationRule<CreateUserCommand, UserValidationError, UserErrorDetails>
{
    public async Task<ValidationPipelineOutput<UserValidationError, UserErrorDetails>> ValidateAsync(
        CreateUserCommand input, Guid? editingId = null, CancellationToken cancellationToken = default)
    {
        if (!IsValidEmail(input.Email))
        {
            var errorData = new UserErrorDetails { Field = "Email" };
            return new ValidationPipelineOutput<UserValidationError, UserErrorDetails>()
                .AddError(UserValidationError.EmailFormatInvalid, errorData);
        }
        return new ValidationPipelineOutput<UserValidationError, UserErrorDetails>();
    }
    // ... validation logic
}
```

#### 3\. Execution in a Service

```csharp
public async Task<ValidationResultDto<Guid, UserErrorDetails, UserValidationError>> ProcessUserCreation(
    CreateUserCommand input, IValidationPipelineOrchestrator orchestrator)
{
    // Executes the entire pipeline, stopping on the first failure
    var validationResult = await orchestrator.Validate<CreateUserCommand, UserValidationError, UserErrorDetails>(input);
    
    if (!validationResult.Success)
    {
        // Converts pipeline output to the standard DTO format for API return
        return new ValidationResultDto<Guid, UserErrorDetails, UserValidationError>()
            .WithError(validationResult.Code.Value, validationResult.Data);
    }
    
    // Logic for successful creation...
    return new ValidationResultDto<Guid, UserErrorDetails, UserValidationError>()
        .WithSuccess(Guid.NewGuid());
}
```