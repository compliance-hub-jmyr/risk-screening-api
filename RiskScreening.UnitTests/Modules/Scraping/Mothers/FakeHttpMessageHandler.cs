using System.Net;
using System.Text;

namespace RiskScreening.UnitTests.Modules.Scraping.Mothers;

/// <summary>
///     Test double for <see cref="HttpMessageHandler"/> that simulates a two-step
///     HTTP flow (GET → POST) without making real network calls.
///     <para>
///         <c>HttpMessageHandler.SendAsync</c> is <c>protected</c>, so NSubstitute
///         cannot mock it directly — this manual fake is the standard .NET approach.
///     </para>
///     <list type="bullet">
///         <item>First call → returns <c>initialResponse</c> (simulates GET for ViewState)</item>
///         <item>Second call → returns <c>resultsResponse</c> (simulates POST with search results)</item>
///     </list>
/// </summary>
public sealed class FakeHttpMessageHandler : HttpMessageHandler
{
    private readonly HttpResponseMessage? _initialResponse;
    private readonly HttpResponseMessage? _resultsResponse;
    private readonly bool _throwOnSend;
    private int _callCount;

    /// <summary>Creates a handler that returns HTML content for both steps.</summary>
    public FakeHttpMessageHandler(string initialHtml, string resultsHtml)
    {
        _initialResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(initialHtml, Encoding.UTF8, "text/html")
        };
        _resultsResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(resultsHtml, Encoding.UTF8, "text/html")
        };
    }

    /// <summary>Creates a handler with raw <see cref="HttpResponseMessage"/> instances for error scenarios.</summary>
    public FakeHttpMessageHandler(
        HttpResponseMessage initialResponse,
        HttpResponseMessage resultsResponse,
        bool throwOnSend = false)
    {
        _initialResponse = initialResponse;
        _resultsResponse = resultsResponse;
        _throwOnSend = throwOnSend;
    }

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (_throwOnSend)
            throw new TaskCanceledException("Request timed out.");

        _callCount++;

        if (_callCount == 1)
            return Task.FromResult(_initialResponse ?? new HttpResponseMessage(HttpStatusCode.OK));

        return Task.FromResult(_resultsResponse ?? new HttpResponseMessage(HttpStatusCode.OK));
    }
}
