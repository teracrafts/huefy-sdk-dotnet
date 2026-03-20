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
/// var response = await client.SendEmailAsync(
///     "welcome",
///     new Dictionary&lt;string, string&gt; { ["name"] = "John" },
///     "john@example.com");
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
    /// Sends a single email using the default provider (SES).
    /// </summary>
    /// <param name="templateKey">The template key identifying the email template.</param>
    /// <param name="data">Template data variables to merge into the email.</param>
    /// <param name="recipient">The recipient email address.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The send email response.</returns>
    public Task<SendEmailResponse> SendEmailAsync(
        string templateKey,
        Dictionary<string, string> data,
        string recipient,
        CancellationToken ct = default)
    {
        return SendEmailAsync(templateKey, data, recipient, provider: null, ct);
    }

    /// <summary>
    /// Sends a single email using the specified provider.
    /// </summary>
    /// <param name="templateKey">The template key identifying the email template.</param>
    /// <param name="data">Template data variables to merge into the email.</param>
    /// <param name="recipient">The recipient email address.</param>
    /// <param name="provider">The email provider to use. Pass <c>null</c> for the default (SES).</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The send email response.</returns>
    public async Task<SendEmailResponse> SendEmailAsync(
        string templateKey,
        Dictionary<string, string> data,
        string recipient,
        EmailProvider? provider,
        CancellationToken ct = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var errors = EmailValidators.ValidateSendEmailInput(templateKey, data, recipient);
        if (errors.Count > 0)
        {
            throw HuefyException.ValidationError(
                $"Validation failed: {string.Join("; ", errors)}");
        }

        // Warn if template data values contain potential PII (advisory only — never blocks the send).
        foreach (var kvp in data)
        {
            if (SecurityUtils.ContainsPii(kvp.Value))
            {
                _logger.Log(HuefyLogLevel.Warning,
                    $"Potential PII detected in template data field '{kvp.Key}'. Consider removing or encrypting sensitive fields.");
            }
        }

        var request = new SendEmailRequest
        {
            TemplateKey = templateKey.Trim(),
            Recipient = recipient.Trim(),
            Data = data,
            ProviderType = provider
        };

        return await _httpClient.PostAsync<SendEmailResponse>(EmailsSendPath, request, ct)
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Sends multiple emails in bulk using a shared template.
    /// </summary>
    /// <param name="templateKey">The template key to use for all recipients.</param>
    /// <param name="recipients">The list of bulk recipients.</param>
    /// <param name="fromEmail">Optional sender email address.</param>
    /// <param name="fromName">Optional sender name.</param>
    /// <param name="providerType">Optional email provider type.</param>
    /// <param name="batchSize">Optional batch size.</param>
    /// <param name="correlationId">Optional correlation ID.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The bulk send response.</returns>
    public async Task<SendBulkEmailsResponse> SendBulkEmailsAsync(
        string templateKey,
        List<BulkRecipient> recipients,
        string? fromEmail = null,
        string? fromName = null,
        string? providerType = null,
        int? batchSize = null,
        string? correlationId = null,
        CancellationToken ct = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentNullException.ThrowIfNull(templateKey);
        ArgumentNullException.ThrowIfNull(recipients);

        var countErr = EmailValidators.ValidateBulkCount(recipients.Count);
        if (countErr is not null)
        {
            throw HuefyException.ValidationError(countErr);
        }

        var request = new SendBulkEmailsRequest
        {
            TemplateKey = templateKey.Trim(),
            Recipients = recipients,
            FromEmail = fromEmail,
            FromName = fromName,
            ProviderType = providerType,
            BatchSize = batchSize,
            CorrelationId = correlationId,
        };

        return await _httpClient.PostAsync<SendBulkEmailsResponse>(EmailsBulkPath, request, ct)
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
