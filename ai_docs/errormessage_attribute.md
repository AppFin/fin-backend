## Error Message Utility Documentation

This document describes a simple utility pattern designed to **associate human-readable messages directly with enumeration members** using C\# attributes, and provides an extension method to retrieve these messages dynamically.

### I. Attribute Definition: `ErrorMessageAttribute`

This attribute acts as the container for the descriptive error message.

| Component | Description |
| :--- | :--- |
| **Name** | `ErrorMessageAttribute` |
| **Usage** | Applicable only to **Enum Fields** (`AttributeTargets.Field`). |
| **Constructor** | Takes a single `string message` argument, which is stored in the read-only `Message` property. |
| **Purpose** | Decouples the presentation layer's error string from the internal error code definition, allowing for localized or descriptive messages without modifying the enumeration structure. |

#### Usage Example (C\#)

```csharp
using Fin.Infrastructure.Errors;

public enum WalletDeleteErrorCode
{
    // The message is attached directly to the enum field
    [ErrorMessage("Wallet not found to delete.")]
    WalletNotFound = 0, 

    [ErrorMessage("Wallet is currently in use.")]
    WalletInUse = 1,
}
```

-----

### II. Retrieval Mechanism: `ErrorMessageExtension`

This static class provides the reflection-based method to access the message defined by the attribute.

#### `GetErrorMessage<TEnum>(this TEnum enumValue, bool throwIfNotFoundMessage = true)`

| Parameter | Type | Description |
| :--- | :--- | :--- |
| `enumValue` | `TEnum` | The enumeration value (the error code) for which to retrieve the message. |
| `throwIfNotFoundMessage` | `bool` | If `true` (default), throws an `ArgumentException` if the attribute is missing. If `false`, returns an empty string. |

#### Mechanism Details

1.  **Reflection:** Uses `GetType().GetMember(enumValue.ToString())` to retrieve the `MemberInfo` for the specific enum field.
2.  **Attribute Search:** Calls `GetCustomAttribute<ErrorMessageAttribute>()` on the member.
3.  **Result:** Returns the `attribute.Message` if found.

#### Usage Example (C\#)

```csharp
WalletDeleteErrorCode code = WalletDeleteErrorCode.WalletNotFound;

// Retrieves the string "Wallet not found to delete."
string message = code.GetErrorMessage(); 

// Example of integrating with logging or response handling:
// Console.WriteLine(message); 
```