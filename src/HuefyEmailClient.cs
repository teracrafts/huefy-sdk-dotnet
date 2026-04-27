using Huefy.Sdk.Errors;
using Huefy.Sdk.Http;
using Huefy.Sdk.Models;
using Huefy.Sdk.Security;
using Huefy.Sdk.Validators;
using Huefy.Utils;
using System.Linq;

namespace Huefy.Sdk;

/// <summary>
/// Email-focused client for the Huefy SDK.
/// </summary>
/// <remarks>
/// Extends the base HTTP capabilities with email-specific operations
/// including single and bulk email sending with input validation.
/// <code>
/// using var client = new HuefyEmailClient(new HuefyConfig { ApiKey = "your-api-key" });
///
/// var response = await client.SendEmailAsync(new SendEmailRequest
/// {
///     TemplateKey = "welcome",
///     Data = new Dictionary&lt;string, string&gt; { ["name"] = "John" },
///     Recipient = new SendEmailRecipient { Email = "john@example.com", Type = "cc" },
/// });
/// </code>
/// </remarks>
public sealed class HuefyEmailClient : IDisposable
{
    private const string EmailsSendPath = "/emails/send";
    private const string EmailsBulkPath = "/emails/send-bulk";

    private readonly SdkHttpClient _httpClient;
    private readonly IHuefyLogger _logger;
    private bool _disposed;

    /// <summary>
    /// Creates a new email client with the given configuration.
    /// </summary>
    /// <param name="config">The SDK configuration.</param>
    public HuefyEmailClient(HuefyConfig config)
    {
        ArgumentNullException.ThrowIfNull(config);
        _httpClient = new SdkHttpClient(config);
        _logger = config.Logger ?? new NullLogger();
    }

    /// <summary>
    /// Sends a single email using a template.
    /// </summary>
    /// <param name="request">The email request containing TemplateKey, Data, Recipient, and optional ProviderType.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The send email response.</returns>
    public async Task<SendEmailResponse> SendEmailAsync(
        SendEmailRequest request,
        CancellationToken ct = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var errors = EmailValidators.ValidateSendEmailInput(request.TemplateKey, request.Data, request.Recipient);
        if (errors.Count > 0)
        {
            throw HuefyException.ValidationError(
                $"Validation failed: {string.Join("; ", errors)}");
        }

        // Warn if template data values contain potential PII (advisory only — never blocks the send).
        foreach (var kvp in request.Data)
        {
            if (kvp.Value is not null && SecurityUtils.ContainsPii(kvp.Value.ToString() ?? string.Empty))
            {
                _logger.Log(HuefyLogLevel.Warning,
                    $"Potential PII detected in template data field '{kvp.Key}'. Consider removing or encrypting sensitive fields.");
            }
        }

        WarnIfPotentialRecipientPii(request.Recipient);

        var normalized = request with
        {
            TemplateKey = request.TemplateKey.Trim(),
            Recipient = NormalizeRecipient(request.Recipient),
        };

        return await _httpClient.PostAsync<SendEmailResponse>(EmailsSendPath, normalized, ct)
            .ConfigureAwait(false);
    }

    private static object NormalizeRecipient(object recipient) => recipient switch
    {
        string email => email.Trim(),
        SendEmailRecipient sendEmailRecipient => sendEmailRecipient with
        {
            Email = sendEmailRecipient.Email.Trim(),
            Type = sendEmailRecipient.Type?.Trim().ToLowerInvariant(),
        },
        IDictionary<string, object?> map => NormalizeRecipientMap(map),
        IDictionary<string, string> map => NormalizeRecipientMap(map.ToDictionary(
            pair => pair.Key,
            pair => (object?)pair.Value)),
        _ => recipient,
    };

    private static Dictionary<string, object?> NormalizeRecipientMap(IDictionary<string, object?> map)
    {
        var normalized = new Dictionary<string, object?>(map);
        if (normalized.TryGetValue("email", out var email) && email is string emailText)
        {
            normalized["email"] = emailText.Trim();
        }

        if (normalized.TryGetValue("type", out var recipientType) && recipientType is string typeText)
        {
            normalized["type"] = typeText.Trim().ToLowerInvariant();
        }

        return normalized;
    }

    private void WarnIfPotentialRecipientPii(object recipient)
    {
        switch (recipient)
        {
            case SendEmailRecipient sendEmailRecipient when sendEmailRecipient.Data is not null:
                WarnIfPotentialPii(sendEmailRecipient.Data, "recipient data");
                break;
            case IDictionary<string, object?> map
                when map.TryGetValue("data", out var recipientData)
                     && recipientData is IDictionary<string, object?> data:
                WarnIfPotentialPii(data, "recipient data");
                break;
            case IDictionary<string, string> map
                when map.TryGetValue("data", out var rawRecipientData):
                WarnIfPotentialPii(new Dictionary<string, object?> { ["data"] = rawRecipientData }, "recipient data");
                break;
        }
    }

    private void WarnIfPotentialPii(IDictionary<string, object?> data, string dataType)
    {
        foreach (var kvp in data)
        {
            if (kvp.Value is not null && SecurityUtils.ContainsPii(kvp.Value.ToString() ?? string.Empty))
            {
                _logger.Log(HuefyLogLevel.Warning,
                    $"Potential PII detected in {dataType} field '{kvp.Key}'. Consider removing or encrypting sensitive fields.");
            }
        }
    }

    /// <summary>
    /// Sends multiple emails in bulk using a shared template.
    /// </summary>
    /// <param name="request">The bulk email request containing TemplateKey, Recipients, and optional Provider.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The bulk send response.</returns>
    public async Task<SendBulkEmailsResponse> SendBulkEmailsAsync(
        SendBulkEmailsRequest request,
        CancellationToken ct = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentNullException.ThrowIfNull(request);

        var countErr = EmailValidators.ValidateBulkCount(request.Recipients.Count);
        if (countErr is not null)
        {
            throw HuefyException.ValidationError(countErr);
        }

        for (var i = 0; i < request.Recipients.Count; i++)
        {
            var emailErr = EmailValidators.ValidateEmail(request.Recipients[i].Email);
            if (emailErr is not null)
            {
                throw HuefyException.ValidationError($"recipients[{i}]: {emailErr}");
            }
        }

        var normalized = request with { TemplateKey = request.TemplateKey.Trim() };

        return await _httpClient.PostAsync<SendBulkEmailsResponse>(EmailsBulkPath, normalized, ct)
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Performs a health check against the API.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Health check response with status and version.</returns>
    public async Task<EmailHealthResponse> HealthCheckAsync(CancellationToken ct = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        return await _httpClient.GetAsync<EmailHealthResponse>("/health", ct)
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Disposes the client and releases all managed resources.
    /// </summary>
    public void Dispose()
    {
        if (!_disposed)
        {
            _httpClient.Dispose();
            _disposed = true;
        }
    }
}
