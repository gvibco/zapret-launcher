using System.Net;
using System.Net.Sockets;
using System.Security.Authentication;
using ZapretLauncher.Core.Probing;

namespace ZapretLauncher.Core.Tests;

public class SiteProbeTests
{
    [Fact]
    public void Classify_DnsFailure()
    {
        var ex = new HttpRequestException("dns", new SocketException((int)SocketError.HostNotFound));
        Assert.Equal(FailureKind.Dns, SiteProbe.Classify(ex));
    }

    [Fact]
    public void Classify_TlsFailure()
    {
        var ex = new HttpRequestException("tls", new AuthenticationException("bad cert"));
        Assert.Equal(FailureKind.Tls, SiteProbe.Classify(ex));
    }

    [Fact]
    public void Classify_Reset()
    {
        var ex = new HttpRequestException("reset", new SocketException((int)SocketError.ConnectionReset));
        Assert.Equal(FailureKind.Reset, SiteProbe.Classify(ex));
    }

    [Fact]
    public async Task Probe_AnyHttpResponseIsSuccess()
    {
        var handler = new StubHandler(HttpStatusCode.NotFound);
        var probe = new SiteProbe(new HttpClient(handler));
        var result = await probe.ProbeAsync(new ProbeTarget("YouTube", "https://www.youtube.com", true));

        Assert.True(result.Success);
        Assert.Equal(404, result.StatusCode);
        Assert.Equal(FailureKind.None, result.Failure);
    }

    private sealed class StubHandler : HttpMessageHandler
    {
        private readonly HttpStatusCode _status;
        public StubHandler(HttpStatusCode status) => _status = status;

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(new HttpResponseMessage(_status));
    }
}
