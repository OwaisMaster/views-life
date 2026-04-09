using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace ViewsLife.Api.Common.Validation;

/// <summary>
/// Validates that an email address is normalized (lowercase, trimmed).
/// Inherently validates email format as per RFC 5321.
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Parameter)]
public sealed class NormalizedEmailAttribute : ValidationAttribute
{
    // Simplified RFC 5321 email validation pattern
    private static readonly Regex EmailPattern =
        new(@"^[a-zA-Z0-9.!#$%&'*+/=?^_`{|}~-]+@[a-zA-Z0-9](?:[a-zA-Z0-9-]{0,61}[a-zA-Z0-9])?(?:\.[a-zA-Z0-9](?:[a-zA-Z0-9-]{0,61}[a-zA-Z0-9])?)*$",
            RegexOptions.Compiled);

    public NormalizedEmailAttribute()
    {
        ErrorMessage = "The email address is not valid or not properly normalized. Email must be lowercase and trimmed.";
    }

    public override bool IsValid(object? value)
    {
        if (value is not string stringValue)
        {
            return true;
        }

        if (string.IsNullOrWhiteSpace(stringValue))
        {
            return false;
        }

        // Must be trimmed
        if (stringValue != stringValue.Trim())
        {
            return false;
        }

        // Must be lowercase
        if (stringValue != stringValue.ToLowerInvariant())
        {
            return false;
        }

        // Must match email format
        return EmailPattern.IsMatch(stringValue) && stringValue.Length <= 320;
    }
}
