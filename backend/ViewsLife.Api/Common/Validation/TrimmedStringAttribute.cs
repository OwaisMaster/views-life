using System.ComponentModel.DataAnnotations;

namespace ViewsLife.Api.Common.Validation;

/// <summary>
/// Validates that a string value is trimmed (no leading/trailing whitespace)
/// and optionally not empty after trimming.
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Parameter)]
public sealed class TrimmedStringAttribute : ValidationAttribute
{
    private readonly bool _allowEmpty;

    /// <summary>
    /// Creates a new TrimmedString validation attribute.
    /// </summary>
    /// <param name="allowEmpty">Whether to allow empty strings after trimming.</param>
    public TrimmedStringAttribute(bool allowEmpty = false)
    {
        _allowEmpty = allowEmpty;
        ErrorMessage = allowEmpty
            ? "The field must not contain only whitespace."
            : "The field is required and must not contain only whitespace.";
    }

    public override bool IsValid(object? value)
    {
        if (value is not string stringValue)
        {
            return true;
        }

        // Check if the string is only whitespace
        if (string.IsNullOrWhiteSpace(stringValue))
        {
            return _allowEmpty is false;
        }

        // Check if the string has leading or trailing whitespace
        return stringValue == stringValue.Trim();
    }
}
