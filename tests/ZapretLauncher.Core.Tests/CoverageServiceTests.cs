using System.Net;
using ZapretLauncher.Core.Coverage;
using ZapretLauncher.Core.Runtime;
using ZapretLauncher.Core.Settings;

namespace ZapretLauncher.Core.Tests;

public class CoverageServiceTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "zl-" + Guid.NewGuid().ToString("N"));
    private readonly LogService _log;

    public CoverageServiceTests()
    {
        Directory.CreateDirectory(_root);
        _log = new LogService(_root);
    }

    [Theory]
    [InlineData("disabled", "12", "12")]
    [InlineData("tcp", "2000-3000", "12")]
    [InlineData("udp", "12", "4000-5000")]
    [InlineData("all", "2000-3000", "4000-5000")]
    public void GameValues_MapsModes(string mode, string tcp, string udp)
    {
        var settings = new GameFilterSettings
        {
            Mode = mode,
            TcpRange = "2000-3000",
            UdpRange = "4000-5000"
        };
        Assert.Equal((tcp, udp), CoverageService.GameValues(settings));
    }

    [Fact]
    public void Apply_NoneWritesStub()
    {
        var service = new CoverageService(_root, new HttpClient(), _log);
        service.EnsureDefaults(Path.Combine(_root, "engine"));
        var settings = new AppSettings { IpsetMode = "none" };
        service.Apply(settings);

        var text = File.ReadAllText(Path.Combine(service.ListsDir, "ipset-all.txt"));
        Assert.Equal("203.0.113.113/32", text.Trim());
    }

    [Fact]
    public void Apply_AnyWritesEmptyFile()
    {
        var service = new CoverageService(_root, new HttpClient(), _log);
        service.EnsureDefaults(Path.Combine(_root, "engine"));
        service.Apply(new AppSettings { IpsetMode = "any" });

        var text = File.ReadAllText(Path.Combine(service.ListsDir, "ipset-all.txt"));
        Assert.Equal("", text.Trim());
    }

    [Fact]
    public void Apply_WritesCustomDomainsAndExact()
    {
        var service = new CoverageService(_root, new HttpClient(), _log);
        service.EnsureDefaults(Path.Combine(_root, "engine"));
        var settings = new AppSettings();
        settings.CustomDomains.Add(new CustomDomain { Domain = "example.com" });
        settings.CustomDomains.Add(new CustomDomain { Domain = "exact.com", Exact = true });
        settings.CustomExclusions.Add("exclude.com");
        service.Apply(settings);

        var general = File.ReadAllLines(Path.Combine(service.ListsDir, "list-general-user.txt"));
        Assert.Contains("example.com", general);
        Assert.Contains("^exact.com", general);
        Assert.Contains("exclude.com",
            File.ReadAllLines(Path.Combine(service.ListsDir, "list-exclude-user.txt")));
    }

    [Fact]
    public async Task UpdateLists_InvalidHtmlKeepsOldFile()
    {
        var service = new CoverageService(_root, new HttpClient(new StubHandler("<html>error</html>")), _log);
        service.EnsureDefaults(Path.Combine(_root, "engine"));
        var path = Path.Combine(service.ListsDir, "list-general.txt");
        File.WriteAllText(path, "old.example\n");

        await service.UpdateListsAsync("https://example.test/");

        Assert.Contains("old.example", File.ReadAllText(path));
    }

    private sealed class StubHandler : HttpMessageHandler
    {
        private readonly string _body;
        public StubHandler(string body) => _body = body;

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken ct) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(_body)
            });
    }

    public void Dispose()
    {
        if (Directory.Exists(_root)) Directory.Delete(_root, true);
    }
}
