using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SurplusLink.Api.Auth;

namespace SurplusLink.Tests;

public sealed class BrevoAuthEmailSenderTests
{
    private const string ApiKey = "brevo-test-api-key";
    private const string VerificationCode = "123456";
    private const string ResetCode = "654321";

    [Fact]
    public async Task Verification_email_posts_expected_Brevo_request()
    {
        var handler = new CapturingHandler(_ => new HttpResponseMessage(HttpStatusCode.Created));
        var sender = CreateSender(handler);

        await sender.SendVerificationAsync("buyer@example.com", VerificationCode, default);

        Assert.Equal(HttpMethod.Post, handler.Method);
        Assert.Equal("https://api.brevo.com/v3/smtp/email", handler.Uri!.ToString());
        Assert.Equal(ApiKey, Assert.Single(handler.Headers["api-key"]));
        using var payload = JsonDocument.Parse(handler.Body!);
        Assert.Equal("SurplusLink Mail", payload.RootElement.GetProperty("sender").GetProperty("name").GetString());
        Assert.Equal("no-reply@surpluslink.test", payload.RootElement.GetProperty("sender").GetProperty("email").GetString());
        Assert.Equal("buyer@example.com", payload.RootElement.GetProperty("to")[0].GetProperty("email").GetString());
        Assert.Equal("Verify your SurplusLink email", payload.RootElement.GetProperty("subject").GetString());
        Assert.Contains(VerificationCode, payload.RootElement.GetProperty("htmlContent").GetString());
        Assert.Contains("expires in 10 minutes", payload.RootElement.GetProperty("textContent").GetString());
    }

    [Fact]
    public async Task Password_reset_email_posts_expected_Brevo_request()
    {
        var handler = new CapturingHandler(_ => new HttpResponseMessage(HttpStatusCode.Accepted));
        var sender = CreateSender(handler);

        await sender.SendPasswordResetAsync("buyer@example.com", ResetCode, default);

        using var payload = JsonDocument.Parse(handler.Body!);
        Assert.Equal("Reset your SurplusLink password", payload.RootElement.GetProperty("subject").GetString());
        Assert.Contains(ResetCode, payload.RootElement.GetProperty("htmlContent").GetString());
        Assert.Contains("expires in 10 minutes", payload.RootElement.GetProperty("textContent").GetString());
    }

    [Fact]
    public async Task Non_successful_Brevo_response_is_a_delivery_exception_without_secret_logging()
    {
        var logger = new CapturingLogger<BrevoAuthEmailSender>();
        var handler = new CapturingHandler(_ => new HttpResponseMessage(HttpStatusCode.BadRequest)
        {
            Content = new StringContent("provider response containing no secrets")
        });
        var sender = CreateSender(handler, logger: logger);

        await Assert.ThrowsAsync<EmailDeliveryException>(() => sender.SendVerificationAsync("buyer@example.com", VerificationCode, default));

        Assert.All(logger.Messages, message =>
        {
            Assert.DoesNotContain(ApiKey, message);
            Assert.DoesNotContain(VerificationCode, message);
        });
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Network_and_timeout_failures_are_delivery_exceptions(bool timeout)
    {
        var handler = new CapturingHandler(_ =>
        {
            if (timeout) throw new TaskCanceledException("transport timeout");
            throw new HttpRequestException("network unavailable");
        });
        var sender = CreateSender(handler);

        await Assert.ThrowsAsync<EmailDeliveryException>(() => sender.SendVerificationAsync("buyer@example.com", VerificationCode, default));
    }

    [Theory]
    [InlineData(true, false)]
    [InlineData(false, true)]
    public async Task Missing_required_Brevo_configuration_is_a_delivery_exception(bool missingApiKey, bool missingSenderEmail)
    {
        var sender = CreateSender(new CapturingHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)), new BrevoEmailOptions
        {
            ApiKey = missingApiKey ? "" : ApiKey,
            FromEmail = missingSenderEmail ? "" : "no-reply@surpluslink.test",
            FromName = "SurplusLink Mail"
        });

        await Assert.ThrowsAsync<EmailDeliveryException>(() => sender.SendVerificationAsync("buyer@example.com", VerificationCode, default));
    }

    private static BrevoAuthEmailSender CreateSender(CapturingHandler handler, BrevoEmailOptions? options = null, CapturingLogger<BrevoAuthEmailSender>? logger = null)
    {
        var client = new HttpClient(handler) { BaseAddress = new Uri("https://api.brevo.com/"), Timeout = TimeSpan.FromSeconds(10) };
        return new BrevoAuthEmailSender(new SingleClientFactory(client), Options.Create(options ?? new BrevoEmailOptions
        {
            ApiKey = ApiKey,
            FromEmail = "no-reply@surpluslink.test",
            FromName = "SurplusLink Mail"
        }), logger ?? new CapturingLogger<BrevoAuthEmailSender>());
    }

    private sealed class SingleClientFactory(HttpClient client) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name)
        {
            Assert.Equal(BrevoAuthEmailSender.HttpClientName, name);
            return client;
        }
    }

    private sealed class CapturingHandler(Func<HttpRequestMessage, HttpResponseMessage> response) : HttpMessageHandler
    {
        public HttpMethod? Method { get; private set; }
        public Uri? Uri { get; private set; }
        public Dictionary<string, IEnumerable<string>> Headers { get; } = new(StringComparer.OrdinalIgnoreCase);
        public string? Body { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Method = request.Method;
            Uri = request.RequestUri;
            foreach (var header in request.Headers)
                Headers[header.Key] = header.Value.ToArray();
            Body = request.Content is null ? null : await request.Content.ReadAsStringAsync(cancellationToken);
            return response(request);
        }
    }

    private sealed class CapturingLogger<T> : ILogger<T>
    {
        public List<string> Messages { get; } = [];
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel logLevel) => true;
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) =>
            Messages.Add(formatter(state, exception));
    }
}
