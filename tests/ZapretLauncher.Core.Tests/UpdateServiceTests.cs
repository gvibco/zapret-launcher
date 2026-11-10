using System.IO.Compression;
using System.Net;
using System.Text;
using ZapretLauncher.Core.Engine;
using ZapretLauncher.Core.Runtime;
using ZapretLauncher.Core.Updates;

namespace ZapretLauncher.Core.Tests;

public class UpdateServiceTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "zl-" + Guid.NewGuid().ToString("N"));

    [Fact]
    public async Task GetLatest_ParsesVersionAndZipAsset()
    {
        var json = """
        {
          "tag_name": "v1.10.5",
          "assets": [
            { "name": "notes.txt", "browser_download_url": "https://example.test/notes.txt" },
            { "name": "zapret-discord-youtube-1.10.5.zip", "browser_download_url": "https://example.test/engine.zip" }
          ]
        }
        """;
        var service = CreateService(json, BuildEngineZip());

        var info = await service.GetLatestAsync();

        Assert.NotNull(info);
        Assert.Equal("1.10.5", info!.Version);
        Assert.Equal("https://example.test/engine.zip", info.DownloadUrl);
    }

    [Fact]
    public async Task DownloadAndInstall_SwitchesEngineVersion()
    {
        var engine = new EngineStore(_root);
        engine.EnsureEngine();
        var service = new UpdateService(
            new HttpClient(new StubHandler("{}", BuildEngineZip())), engine,
            new LogService(_root));
        var info = new UpdateInfo("1.10.5", "https://example.test/engine.zip");

        var path = await service.DownloadAndInstallAsync(info);

        Assert.Equal("1.10.5", engine.CurrentVersion);
        Assert.True(File.Exists(Path.Combine(path, "winws.exe")));
    }

    private UpdateService CreateService(string json, byte[] zip) =>
        new(new HttpClient(new StubHandler(json, zip)), new EngineStore(_root), new LogService(_root));

    private static byte[] BuildEngineZip()
    {
        using var buffer = new MemoryStream();
        using (var zip = new ZipArchive(buffer, ZipArchiveMode.Create, leaveOpen: true))
        {
            foreach (var name in new[] { "winws.exe", "WinDivert.dll", "WinDivert64.sys", "cygwin1.dll" })
                Add(zip, name);
            Add(zip, "bin/quic_initial_www_google_com.bin");
        }
        return buffer.ToArray();
    }

    private static void Add(ZipArchive zip, string name)
    {
        using var writer = new StreamWriter(zip.CreateEntry(name).Open());
        writer.Write("x");
    }

    private sealed class StubHandler : HttpMessageHandler
    {
        private readonly string _json;
        private readonly byte[] _zip;
        public StubHandler(string json, byte[] zip) { _json = json; _zip = zip; }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken ct)
        {
            if (request.RequestUri!.AbsoluteUri.Contains("api.github.com"))
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(_json, Encoding.UTF8, "application/json")
                });
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new ByteArrayContent(_zip)
            });
        }
    }

    public void Dispose()
    {
        if (Directory.Exists(_root)) Directory.Delete(_root, true);
    }
}
