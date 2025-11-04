##  ValidationResultDto C\# Documentation for AI

This document describes the C\# class hierarchy for handling **operation results**, specifically focusing on **validation and business logic outcomes**. It is designed to be concise, effective, and assertive for AI processing.

###  Purpose

The `ValidationResultDto` classes provide a **standardized, immutable-like container** for the result of an operation, clearly distinguishing between **success** (with data) and **failure** (with an error code and optional error data/message).

It's primarily used as the **return type** for application service methods or API controllers, ensuring a consistent contract for result handling.

###  Core Class: `ValidationResultDto<TDSuccess, TDError, TErroCode>`

| Generics | Description | Constraint |
| :--- | :--- | :--- |
| `TDSuccess` | Type of the **success data** (e.g., an entity, DTO). | `class?` or `struct?` |
| `TDError` | Type of the **error data** (e.g., a validation details DTO). | `class?` or `struct?` |
| `TErroCode` | Type of the **error enumeration** (e.g., `UserErrorCodes`). | `struct, Enum` |

#### Key Properties

| Property | Type | Description |
| :--- | :--- | :--- |
| `Success` | `bool` | **True** if the result is a success (no `ErrorCode`). |
| `Data` | `TDSuccess?` | The **successful result payload**. Non-null on success. |
| `ErrorCode` | `TErroCode?` | The **specific error code** on failure. Null on success. |
| `ErrorData` | `TDError?` | **Optional detailed error information**. |
| `Message` | `string` | Human-readable message. Defaults to "Success" or the error message derived from `ErrorCode`. |

#### Builders (Fluent API)

| Method | Purpose | Usage Example (C\#) |
| :--- | :--- | :--- |
| `WithSuccess(TDSuccess)` | Creates a **success result**. | `new Result().WithSuccess(userDto);` |
| `WithError(TErroCode, message?)` | Creates an **error result** (no `ErrorData`). | `new Result().WithError(Code.NotFound);` |
| `WithError(TErroCode, TDError, message?)` | Creates an **error result** with `ErrorData`. | `new Result().WithError(Code.Invalid, details);` |

#### Static Factory

* `FromPipeline(ValidationPipelineOutput<TErroCode, TDError>)`: Converts a result from a validation pipeline (likely a failure) into the DTO.

###  Specialized Subclasses (Reduced Complexity)

The base class is extended for common scenarios, simplifying usage by defaulting generic types.

1.  **`ValidationResultDto<TDSuccess, TErroCode>`**:

    * **Defaults `TDError` to `object`**.
    * Used when the error context is primarily defined by the `TErroCode` alone.

2.  **`ValidationResultDto<TDSuccess>`**:

    * **Defaults `TDError` to `object` and `TErroCode` to `NoErrorCode`**.
    * Simplest form, suitable for operations where errors are *not* handled via this DTO or the only possible error is generic/implicit (e.g., exceptions). Primarily for success results.

### 🛠️ Good Practices & Usage

* **Immutability:** While properties have setters, the **fluent builder methods** (`WithSuccess`, `WithError`) are the preferred way to instantiate and configure the DTO, promoting a functional, immutable-like pattern.
* **Result Checking:** Always check the **`Success`** property first before accessing `Data` or `ErrorCode`.
* **Decoupling:** Use the `TErroCode` generic parameter to keep business logic error codes separate from HTTP status codes or infrastructure errors.
* **Extension Methods:** The `ValidationResultDtoExtensions.ToValidationResult` methods simplify converting `ValidationPipelineOutput` types directly into the required DTO flavor.

#### C\# Usage Example

```csharp
// 1. Define types
public enum UserError { NotFound, InvalidEmail }
public class UserDto { /* ... */ }
public class UserErrorDetails { public string FieldName { get; set; } }

// 2. Class instance creation (The "Fluent" Way)
var successResult = new ValidationResultDto<UserDto, UserErrorDetails, UserError>()
    .WithSuccess(new UserDto { Name = "Alice" });

var errorWithData = new ValidationResultDto<UserDto, UserErrorDetails, UserError>()
    .WithError(
        UserError.InvalidEmail, 
        new UserErrorDetails { FieldName = "Email" }, 
        "Invalid email format provided"
    );

// 3. Consumption in Application Logic
if (successResult.Success)
{
    Console.WriteLine($"User: {successResult.Data.Name}"); // Access Data
}
else
{
    Console.WriteLine($"Error: {successResult.ErrorCode}"); // Access ErrorCode
    // Console.WriteLine(errorWithData.ErrorData.FieldName); // Access ErrorData
}
```