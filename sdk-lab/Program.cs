using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using Teracrafts.Huefy.Sdk;
using Teracrafts.Huefy.Sdk.Errors;
using Teracrafts.Huefy.Sdk.Models;

const string Green = "\u001B[32m";
const string Red = "\u001B[31m";
const string Reset = "\u001B[0m";

int passed = 0;
int failed = 0;

void Pass(string label)
{
    Console.WriteLine($"{Green}[PASS]{Reset} {label}");
    passed++;
}

void Fail(string label, string reason)
{
    Console.WriteLine($"{Red}[FAIL]{Reset} {label}: {reason}");
    failed++;
}

Console.WriteLine("=== Huefy .NET SDK Lab ===");
Console.WriteLine();

await using var stub = new StubServer();

HuefyEmailClient? client = null;

try
{
    client = new HuefyEmailClient(new HuefyConfig
    {
        ApiKey = "sdk_lab_test_key",
        BaseUrl = stub.BaseUrl,
        Timeout = 2_000,
    });
    Pass("Initialization");
}
catch (Exception ex)
{
    Fail("Initialization", ex.Message);
}

if (client is not null)
{
    try
    {
        var response = await client.SendEmailAsync(new SendEmailRequest
        {
            TemplateKey = "  welcome-email  ",
            Data = new Dictionary<string, object?> { ["name"] = "Jane" },
            Recipient = new SendEmailRecipient
            {
                Email = "  user@example.com  ",
                Type = " CC ",
                Data = new Dictionary<string, object?> { ["segment"] = "vip" },
            },
            ProviderType = EmailProvider.Ses,
        });

        var request = stub.RequestAt(0);
        var recipient = request?.Body?.RootElement.GetProperty("recipient");

        if (!response.Success)
        {
            Fail("Single-send contract shaping", "expected successful stub response");
        }
        else if (request is null || recipient is null)
        {
            Fail("Single-send contract shaping", "expected captured single-send request");
        }
        else if (request.Method != "POST" || request.Path != "/emails/send")
        {
            Fail("Single-send contract shaping", $"expected POST /emails/send, got {request.Method} {request.Path}");
        }
        else if (request.ApiKey != "sdk_lab_test_key")
        {
            Fail("Single-send contract shaping", "missing X-API-Key header");
        }
        else if (request.Body!.RootElement.GetProperty("templateKey").GetString() != "welcome-email")
        {
            Fail("Single-send contract shaping", "templateKey was not trimmed");
        }
        else if (recipient.Value.GetProperty("email").GetString() != "user@example.com")
        {
            Fail("Single-send contract shaping", "recipient email was not trimmed");
        }
        else if (recipient.Value.GetProperty("type").GetString() != "cc")
        {
            Fail("Single-send contract shaping", "recipient type was not normalized");
        }
        else
        {
            Pass("Single-send contract shaping");
        }
    }
    catch (Exception ex)
    {
        Fail("Single-send contract shaping", ex.Message);
    }

    try
    {
        var response = await client.SendBulkEmailsAsync(new SendBulkEmailsRequest
        {
            TemplateKey = "  welcome-email  ",
            Recipients =
            [
                new BulkRecipient
                {
                    Email = "  first@example.com  ",
                    Type = " TO ",
                    Data = new Dictionary<string, object?> { ["tier"] = "gold" },
                },
                new BulkRecipient
                {
                    Email = " second@example.com ",
                    Type = " BCC ",
                },
            ],
            Provider = EmailProvider.Ses,
        });

        var request = stub.RequestAt(1);
        if (!response.Success)
        {
            Fail("Bulk-send contract shaping", "expected successful stub response");
        }
        else if (request?.Body is null)
        {
            Fail("Bulk-send contract shaping", "expected captured bulk request");
        }
        else
        {
            var root = request.Body.RootElement;
            var recipients = root.GetProperty("recipients");
            var first = recipients[0];
            var second = recipients[1];

            if (request.Method != "POST" || request.Path != "/emails/send-bulk")
            {
                Fail("Bulk-send contract shaping", $"expected POST /emails/send-bulk, got {request.Method} {request.Path}");
            }
            else if (root.GetProperty("templateKey").GetString() != "welcome-email")
            {
                Fail("Bulk-send contract shaping", "templateKey was not trimmed");
            }
            else if (first.GetProperty("email").GetString() != "first@example.com")
            {
                Fail("Bulk-send contract shaping", "first bulk recipient email was not trimmed");
            }
            else if (first.GetProperty("type").GetString() != "to")
            {
                Fail("Bulk-send contract shaping", "first bulk recipient type was not normalized");
            }
            else if (second.GetProperty("email").GetString() != "second@example.com")
            {
                Fail("Bulk-send contract shaping", "second bulk recipient email was not trimmed");
            }
            else if (second.GetProperty("type").GetString() != "bcc")
            {
                Fail("Bulk-send contract shaping", "second bulk recipient type was not normalized");
            }
            else
            {
                Pass("Bulk-send contract shaping");
            }
        }
    }
    catch (Exception ex)
    {
        Fail("Bulk-send contract shaping", ex.Message);
    }

    var beforeSingle = stub.RequestCount;
    try
    {
        await client.SendEmailAsync(new SendEmailRequest
        {
            TemplateKey = "",
            Data = null!,
            Recipient = "not-an-email",
        });
        Fail("Validation rejection for invalid single input", "expected validation error");
    }
    catch (HuefyException)
    {
        if (stub.RequestCount != beforeSingle)
        {
            Fail("Validation rejection for invalid single input", "invalid request reached the transport");
        }
        else
        {
            Pass("Validation rejection for invalid single input");
        }
    }
    catch (Exception ex)
    {
        Fail("Validation rejection for invalid single input", ex.Message);
    }

    var beforeBulk = stub.RequestCount;
    try
    {
        await client.SendBulkEmailsAsync(new SendBulkEmailsRequest
        {
            TemplateKey = "welcome-email",
            Recipients =
            [
                new BulkRecipient
                {
                    Email = "bad-email",
                    Type = "reply-to",
                },
            ],
        });
        Fail("Validation rejection for invalid bulk input", "expected validation error");
    }
    catch (HuefyException)
    {
        if (stub.RequestCount != beforeBulk)
        {
            Fail("Validation rejection for invalid bulk input", "invalid bulk request reached the transport");
        }
        else
        {
            Pass("Validation rejection for invalid bulk input");
        }
    }
    catch (Exception ex)
    {
        Fail("Validation rejection for invalid bulk input", ex.Message);
    }

    try
    {
        var health = await client.HealthCheckAsync();
        var request = stub.RequestAt(2);

        if (health.Data.Status != "healthy")
        {
            Fail("SDK health path behavior", "expected decoded healthy response");
        }
        else if (request is null)
        {
            Fail("SDK health path behavior", "expected captured health request");
        }
        else if (request.Method != "GET" || request.Path != "/health")
        {
            Fail("SDK health path behavior", $"expected GET /health, got {request.Method} {request.Path}");
        }
        else
        {
            Pass("SDK health path behavior");
        }
    }
    catch (Exception ex)
    {
        Fail("SDK health path behavior", ex.Message);
    }

    client.Dispose();
    Pass("Cleanup");
}

Console.WriteLine();
Console.WriteLine("========================================");
Console.WriteLine($"Results: {passed} passed, {failed} failed");
Console.WriteLine("========================================");

if (failed == 0)
{
    Console.WriteLine();
    Console.WriteLine("All verifications passed!");
}

return failed > 0 ? 1 : 0;

sealed class CapturedRequest
{
    public required string Method { get; init; }
    public required string Path { get; init; }
    public required string? ApiKey { get; init; }
    public JsonDocument? Body { get; init; }
}

sealed class StubServer : IAsyncDisposable
{
    private readonly HttpListener _listener;
    private readonly CancellationTokenSource _cts = new();
    private readonly Task _loop;
    private readonly List<CapturedRequest> _requests = [];
    private readonly object _lock = new();

    public StubServer()
    {
        var port = GetFreePort();
        BaseUrl = $"http://127.0.0.1:{port}";
        _listener = new HttpListener();
        _listener.Prefixes.Add($"{BaseUrl}/");
        _listener.Start();
        _loop = Task.Run(() => RunAsync(_cts.Token));
    }

    public string BaseUrl { get; }

    public int RequestCount
    {
        get
        {
            lock (_lock)
            {
                return _requests.Count;
            }
        }
    }

    public CapturedRequest? RequestAt(int index)
    {
        lock (_lock)
        {
            return index >= 0 && index < _requests.Count ? _requests[index] : null;
        }
    }

    private async Task RunAsync(CancellationToken cancellationToken)
    {
        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                var context = await _listener.GetContextAsync().WaitAsync(cancellationToken);
                _ = Task.Run(() => HandleAsync(context), cancellationToken);
            }
        }
        catch (OperationCanceledException)
        {
        }
        catch (HttpListenerException)
        {
        }
        catch (ObjectDisposedException)
        {
        }
    }

    private async Task HandleAsync(HttpListenerContext context)
    {
        string bodyText = string.Empty;
        using (var reader = new StreamReader(context.Request.InputStream, context.Request.ContentEncoding))
        {
            bodyText = await reader.ReadToEndAsync();
        }

        JsonDocument? body = null;
        if (!string.IsNullOrWhiteSpace(bodyText))
        {
            body = JsonDocument.Parse(bodyText);
        }

        lock (_lock)
        {
            _requests.Add(new CapturedRequest
            {
                Method = context.Request.HttpMethod,
                Path = context.Request.Url?.AbsolutePath ?? string.Empty,
                ApiKey = context.Request.Headers["X-API-Key"],
                Body = body,
            });
        }

        var responseJson = context.Request.Url?.AbsolutePath switch
        {
            "/emails/send" => """
                {
                  "success": true,
                  "data": {
                    "emailId": "email_123",
                    "status": "queued",
                    "recipients": [
                      { "email": "user@example.com", "status": "queued", "messageId": "msg_123" }
                    ]
                  },
                  "correlationId": "corr_send"
                }
                """,
            "/emails/send-bulk" => """
                {
                  "success": true,
                  "data": {
                    "batchId": "batch_123",
                    "status": "queued",
                    "templateKey": "welcome-email",
                    "templateVersion": 1,
                    "senderUsed": "ses",
                    "senderVerified": true,
                    "totalRecipients": 2,
                    "processedCount": 2,
                    "successCount": 2,
                    "failureCount": 0,
                    "suppressedCount": 0,
                    "startedAt": "2026-01-01T00:00:00Z",
                    "recipients": [
                      { "email": "first@example.com", "status": "queued" },
                      { "email": "second@example.com", "status": "queued" }
                    ]
                  },
                  "correlationId": "corr_bulk"
                }
                """,
            "/health" => """
                {
                  "success": true,
                  "data": {
                    "status": "healthy",
                    "timestamp": "2026-01-01T00:00:00Z",
                    "version": "test"
                  },
                  "correlationId": "corr_health"
                }
                """,
            _ => """{ "message": "not found" }""",
        };

        var statusCode = context.Request.Url?.AbsolutePath switch
        {
            "/emails/send" or "/emails/send-bulk" or "/health" => HttpStatusCode.OK,
            _ => HttpStatusCode.NotFound,
        };

        var buffer = Encoding.UTF8.GetBytes(responseJson);
        context.Response.StatusCode = (int)statusCode;
        context.Response.ContentType = "application/json";
        context.Response.ContentLength64 = buffer.Length;
        await context.Response.OutputStream.WriteAsync(buffer);
        context.Response.Close();
    }

    public async ValueTask DisposeAsync()
    {
        _cts.Cancel();
        _listener.Stop();
        _listener.Close();

        try
        {
            await _loop;
        }
        catch
        {
        }

        lock (_lock)
        {
            foreach (var request in _requests)
            {
                request.Body?.Dispose();
            }
        }

        _cts.Dispose();
    }

    private static int GetFreePort()
    {
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        return ((IPEndPoint)listener.LocalEndpoint).Port;
    }
}
