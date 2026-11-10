using System.IO.Compression;
using ZapretLauncher.Core.Engine;

namespace ZapretLauncher.Core.Tests;

public class EngineStoreTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "zl-" + Guid.NewGuid().ToString("N"));

    [Fact]
    public void EnsureEngine_ExtractsBundledEngine()
    {
        var store = new EngineStore(_root);

        var path = store.EnsureEngine();

        Assert.True(File.Exists(Path.Combine(path, "winws.exe")));
        Assert.True(File.Exists(Path.Combine(path, "WinDivert64.sys")));
        Assert.True(Directory.Exists(Path.Combine(path, "bin")));
        Assert.Equal("1.10.4", store.CurrentVersion);
    }

    [Fact]
    public void EnsureEngine_IsIdempotent()
    {
        var store = new EngineStore(_root);
        var first = store.EnsureEngine();
        var second = store.EnsureEngine();
        Assert.Equal(first, second);
    }

    [Fact]
    public void InstallUpdate_SwitchesVersion()
    {
        var store = new EngineStore(_root);
        store.EnsureEngine();

        using var buffer = new MemoryStream();
        using (var zip = new ZipArchive(buffer, ZipArchiveMode.Create, leaveOpen: true))
        {
            foreach (var name in new[] { "winws.exe", "WinDivert.dll", "WinDivert64.sys", "cygwin1.dll" })
                AddEntry(zip, name, "x");
            AddEntry(zip, "bin/quic_initial_www_google_com.bin", "x");
        }
        buffer.Position = 0;

        var path = store.InstallUpdate(buffer, "1.10.5");

        Assert.EndsWith(Path.Combine("engine", "1.10.5"), path);
        Assert.Equal("1.10.5", store.CurrentVersion);
    }

    private static void AddEntry(ZipArchive zip, string name, string content)
    {
        var entry = zip.CreateEntry(name);
        using var writer = new StreamWriter(entry.Open());
        writer.Write(content);
    }

    public void Dispose()
    {
        if (Directory.Exists(_root)) Directory.Delete(_root, true);
    }
}
