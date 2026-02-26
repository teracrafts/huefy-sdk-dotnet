using Huefy.Sdk.Errors;
using Huefy.Sdk.Http;
using Huefy.Sdk.Models;
using Huefy.Sdk.Security;
using Huefy.Sdk.Validators;

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
    private const string EmailsBulkPath = "/emails/bulk";

    private readonly SdkHttpClient _httpClient;
    private bool _disposed;

    /// <summary>
    /// Creates a new email client with the given configuration.
    /// </summary>
    /// <param name="config">The SDK configuration.</param>
    public HuefyEmailClient(HuefyConfig config)
    {
        ArgumentNullException.ThrowIfNull(config);
        _httpClient = new SdkHttpClient(config);
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

        // Check template data values for potential PII before sending.
        foreach (var kvp in data)
        {
            if (SecurityUtils.ContainsPii(kvp.Value))
            {
                throw HuefyException.ValidationError(
                    $"Potential PII detected in template data field '{kvp.Key}'");
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
    /// Sends multiple emails in bulk.
    /// </summary>
    /// <remarks>
    /// Each request is sent independently. Failures for individual emails
    /// do not prevent remaining emails from being sent.
    /// </remarks>
    /// <param name="requests">The list of email requests to send.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A list of results for each email.</returns>
    public async Task<List<BulkEmailResult>> SendBulkEmailsAsync(
        List<SendEmailRequest> requests,
        CancellationToken ct = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentNullException.ThrowIfNull(requests);

        var countErr = EmailValidators.ValidateBulkCount(requests.Count);
        if (countErr is not null)
        {
            throw HuefyException.ValidationError(countErr);
        }

        var results = new List<BulkEmailResult>(requests.Count);

        foreach (var request in requests)
        {
            try
            {
                var response = await SendEmailAsync(
                    request.TemplateKey,
                    request.Data,
                    request.Recipient,
                    request.ProviderType,
                    ct).ConfigureAwait(false);

                results.Add(new BulkEmailResult
                {
                    Email = request.Recipient,
                    Success = true,
                    Result = response
                });
            }
            catch (HuefyException ex)
            {
                results.Add(new BulkEmailResult
                {
                    Email = request.Recipient,
                    Success = false,
                    Error = new BulkEmailError
                    {
                        Message = ex.Message,
                        Code = ex.Code.ToString()
                    }
                });
            }
        }

        return results;
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
