using System.Text.RegularExpressions;
using Huefy.Sdk.Models;

namespace Huefy.Sdk.Validators;

/// <summary>
/// Validation utilities for email-related inputs.
/// </summary>
public static partial class EmailValidators
{
    private static readonly HashSet<string> ValidRecipientTypes =
    [
        "to",
        "cc",
        "bcc",
    ];

    /// <summary>Maximum allowed email address length.</summary>
    public const int MaxEmailLength = 254;

    /// <summary>Maximum allowed template key length.</summary>
    public const int MaxTemplateKeyLength = 100;

    /// <summary>Maximum number of emails in a single bulk request.</summary>
    public const int MaxBulkEmails = 1000;

    private static readonly Regex EmailRegex = new(
        @"^[^\s@]+@[^\s@]+\.[^\s@]+$",
        RegexOptions.Compiled);

    /// <summary>
    /// Validates a recipient email address.
    /// </summary>
    /// <param name="email">The email address to validate.</param>
    /// <returns>An error message string, or <c>null</c> if valid.</returns>
    public static string? ValidateEmail(string email)
    {
        if (string.IsNullOrEmpty(email))
            return "recipient email is required";

        var trimmed = email.Trim();

        if (trimmed.Length > MaxEmailLength)
            return $"email exceeds maximum length of {MaxEmailLength} characters";

        if (!EmailRegex.IsMatch(trimmed))
            return $"invalid email address: {trimmed}";

        return null;
    }

    /// <summary>
    /// Validates a template key.
    /// </summary>
    /// <param name="key">The template key to validate.</param>
    /// <returns>An error message string, or <c>null</c> if valid.</returns>
    public static string? ValidateTemplateKey(string key)
    {
        if (string.IsNullOrEmpty(key))
            return "template key is required";

        var trimmed = key.Trim();

        if (trimmed.Length == 0)
            return "template key cannot be empty";

        if (trimmed.Length > MaxTemplateKeyLength)
            return $"template key exceeds maximum length of {MaxTemplateKeyLength} characters";

        return null;
    }

    /// <summary>
    /// Validates template data.
    /// </summary>
    /// <param name="data">The template data dictionary.</param>
    /// <returns>An error message string, or <c>null</c> if valid.</returns>
    public static string? ValidateEmailData(Dictionary<string, object?>? data)
    {
        if (data is null)
            return "template data is required";

        return null;
    }

    /// <summary>
    /// Validates the count of emails in a bulk request.
    /// </summary>
    /// <param name="count">The number of emails.</param>
    /// <returns>An error message string, or <c>null</c> if valid.</returns>
    public static string? ValidateBulkCount(int count)
    {
        if (count <= 0)
            return "at least one email is required";

        if (count > MaxBulkEmails)
            return $"maximum of {MaxBulkEmails} emails per bulk request";

        return null;
    }

    /// <summary>
    /// Validates all inputs for sending a single email.
    /// </summary>
    /// <param name="templateKey">The template key.</param>
    /// <param name="data">The template data.</param>
    /// <param name="recipient">The recipient email.</param>
    /// <returns>A list of error message strings. Empty if all inputs are valid.</returns>
    public static List<string> ValidateSendEmailInput(
        string templateKey,
        Dictionary<string, object?>? data,
        object recipient)
    {
        var errors = new List<string>();

        var templateErr = ValidateTemplateKey(templateKey);
        if (templateErr is not null) errors.Add(templateErr);

        var dataErr = ValidateEmailData(data);
        if (dataErr is not null) errors.Add(dataErr);

        var emailErr = ValidateRecipient(recipient);
        if (emailErr is not null) errors.Add(emailErr);

        return errors;
    }

    /// <summary>
    /// Validates a send-email recipient value.
    /// </summary>
    /// <param name="recipient">A recipient email string or recipient object.</param>
    /// <returns>An error message string, or <c>null</c> if valid.</returns>
    public static string? ValidateRecipient(object? recipient)
    {
        switch (recipient)
        {
            case null:
                return "recipient email is required";
            case string email:
                return ValidateEmail(email);
            case SendEmailRecipient sendEmailRecipient:
            {
                var emailError = ValidateEmail(sendEmailRecipient.Email);
                if (emailError is not null)
                {
                    return emailError;
                }

                var typeError = ValidateRecipientType(sendEmailRecipient.Type);
                if (typeError is not null)
                {
                    return typeError;
                }

                return ValidateRecipientData(sendEmailRecipient.Data);
            }
            case IDictionary<string, object?> map:
            {
                if (!map.TryGetValue("email", out var emailValue) || emailValue is not string emailValueText)
                {
                    return "recipient email is required";
                }

                var emailError = ValidateEmail(emailValueText);
                if (emailError is not null)
                {
                    return emailError;
                }

                map.TryGetValue("type", out var recipientTypeValue);
                var typeError = ValidateRecipientType(recipientTypeValue);
                if (typeError is not null)
                {
                    return typeError;
                }

                map.TryGetValue("data", out var recipientDataValue);
                return ValidateRecipientData(recipientDataValue);
            }
            case IDictionary<string, string> map:
            {
                if (!map.TryGetValue("email", out var email))
                {
                    return "recipient email is required";
                }

                var emailError = ValidateEmail(email);
                if (emailError is not null)
                {
                    return emailError;
                }

                map.TryGetValue("type", out var recipientType);
                return ValidateRecipientType(recipientType);
            }
            default:
                return "recipient must be a string or recipient object";
        }
    }

    private static string? ValidateRecipientType(object? recipientType) => recipientType switch
    {
        null => null,
        string recipientTypeText when string.IsNullOrWhiteSpace(recipientTypeText) => null,
        string recipientTypeText when ValidRecipientTypes.Contains(recipientTypeText.Trim().ToLowerInvariant()) => null,
        string => "recipient type must be one of: to, cc, bcc",
        _ => "recipient type must be one of: to, cc, bcc",
    };

    private static string? ValidateRecipientData(object? recipientData) => recipientData switch
    {
        null => null,
        IDictionary<string, object?> => null,
        IDictionary<string, string> => null,
        _ => "recipient data must be an object",
    };
}
