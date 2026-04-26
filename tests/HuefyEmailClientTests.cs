using Huefy.Sdk.Errors;
using Huefy.Sdk.Models;
using Xunit;

namespace Huefy.Sdk.Tests;

public class HuefyEmailClientTests
{
    private static HuefyEmailClient MakeClient() =>
        new(new HuefyConfig { ApiKey = "sdk_test_key" });

    // -------------------------------------------------------------------------
    // SendEmailAsync — validation
    // -------------------------------------------------------------------------

    [Fact]
    public async Task SendEmailAsync_EmptyTemplateKey_ThrowsValidation()
    {
        using var client = MakeClient();
        await Assert.ThrowsAsync<HuefyException>(() =>
            client.SendEmailAsync(new SendEmailRequest
            {
                TemplateKey = "",
                Data = new Dictionary<string, object?> { ["name"] = "John" },
                Recipient = "john@example.com",
            }));
    }

    [Fact]
    public async Task SendEmailAsync_InvalidRecipient_ThrowsValidation()
    {
        using var client = MakeClient();
        var ex = await Assert.ThrowsAsync<HuefyException>(() =>
            client.SendEmailAsync(new SendEmailRequest
            {
                TemplateKey = "welcome",
                Data = new Dictionary<string, object?> { ["name"] = "John" },
                Recipient = "not-an-email",
            }));
        Assert.Contains("Validation", ex.Message);
    }

    [Fact]
    public async Task SendEmailAsync_DisposedClient_ThrowsObjectDisposed()
    {
        var client = MakeClient();
        client.Dispose();
        await Assert.ThrowsAsync<ObjectDisposedException>(() =>
            client.SendEmailAsync(new SendEmailRequest
            {
                TemplateKey = "welcome",
                Data = new Dictionary<string, object?>(),
                Recipient = "john@example.com",
            }));
    }

    // -------------------------------------------------------------------------
    // SendBulkEmailsAsync — validation
    // -------------------------------------------------------------------------

    [Fact]
    public async Task SendBulkEmailsAsync_EmptyRecipients_ThrowsValidation()
    {
        using var client = MakeClient();
        await Assert.ThrowsAsync<HuefyException>(() =>
            client.SendBulkEmailsAsync(new SendBulkEmailsRequest
            {
                TemplateKey = "welcome",
                Recipients = new List<BulkRecipient>(),
            }));
    }

    [Fact]
    public async Task SendBulkEmailsAsync_InvalidRecipientEmail_ThrowsWithIndex()
    {
        using var client = MakeClient();
        var ex = await Assert.ThrowsAsync<HuefyException>(() =>
            client.SendBulkEmailsAsync(new SendBulkEmailsRequest
            {
                TemplateKey = "welcome",
                Recipients = new List<BulkRecipient>
                {
                    new() { Email = "not-an-email" },
                },
            }));
        Assert.Contains("recipients[0]", ex.Message);
    }

    [Fact]
    public async Task SendBulkEmailsAsync_DisposedClient_ThrowsObjectDisposed()
    {
        var client = MakeClient();
        client.Dispose();
        await Assert.ThrowsAsync<ObjectDisposedException>(() =>
            client.SendBulkEmailsAsync(new SendBulkEmailsRequest
            {
                TemplateKey = "welcome",
                Recipients = new List<BulkRecipient> { new() { Email = "a@b.com" } },
            }));
    }

    // -------------------------------------------------------------------------
    // SendEmailRequest — model construction
    // -------------------------------------------------------------------------

    [Fact]
    public void SendEmailRequest_InitializesCorrectly()
    {
        var req = new SendEmailRequest
        {
            TemplateKey = "welcome",
            Data = new Dictionary<string, object?> { ["name"] = "John" },
            Recipient = "john@example.com",
        };
        Assert.Equal("welcome", req.TemplateKey);
        Assert.Equal("john@example.com", req.Recipient);
        Assert.Equal("John", req.Data["name"]);
        Assert.Null(req.ProviderType);
    }

    [Fact]
    public void SendBulkEmailsRequest_InitializesCorrectly()
    {
        var recipients = new List<BulkRecipient> { new() { Email = "a@b.com" } };
        var req = new SendBulkEmailsRequest
        {
            TemplateKey = "welcome",
            Recipients = recipients,
        };
        Assert.Equal("welcome", req.TemplateKey);
        Assert.Single(req.Recipients);
        Assert.Null(req.Provider);
    }
}
