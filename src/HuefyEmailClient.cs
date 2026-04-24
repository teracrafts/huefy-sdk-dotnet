using Huefy.Sdk.Errors;
using Huefy.Sdk.Http;
using Huefy.Sdk.Models;
using Huefy.Sdk.Security;
using Huefy.Sdk.Validators;
using Huefy.Utils;

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
///     Recipient = "john@example.com",
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
            if (SecurityUtils.ContainsPii(kvp.Value))
            {
                _logger.Log(HuefyLogLevel.Warning,
                    $"Potential PII detected in template data field '{kvp.Key}'. Consider removing or encrypting sensitive fields.");
            }
        }

        var normalized = request with
        {
            TemplateKey = request.TemplateKey.Trim(),
            Recipient = request.Recipient.Trim(),
        };

        return await _httpClient.PostAsync<SendEmailResponse>(EmailsSendPath, normalized, ct)
            .ConfigureAwait(false);
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
