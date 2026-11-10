using System.IO.Compression;
using System.Reflection;

namespace ZapretLauncher.Core.Engine;

public sealed class EngineStore
{
    public const string BundledVersion = "1.10.4";
    private const string ResourceName = "ZapretLauncher.Core.engine.zip";
    private static readonly string[] RequiredFiles =
    {
        "winws.exe", "WinDivert.dll", "WinDivert64.sys", "cygwin1.dll"
    };

    private readonly string _engineRoot;

    public EngineStore(string dataRoot)
    {
        _engineRoot = Path.Combine(dataRoot, "engine");
    }

    public string CurrentVersion =>
        File.Exists(CurrentVersionFile) ? File.ReadAllText(CurrentVersionFile).Trim() : "";

    private string CurrentVersionFile => Path.Combine(_engineRoot, "current.txt");

    public string EnsureEngine()
    {
        Directory.CreateDirectory(_engineRoot);
        var version = CurrentVersion;
        if (!string.IsNullOrWhiteSpace(version))
        {
            var existing = Path.Combine(_engineRoot, version);
            if (IsValid(existing)) return existing;
        }

        var bundled = Path.Combine(_engineRoot, BundledVersion);
        if (Directory.Exists(bundled)) Directory.Delete(bundled, true);
        ExtractEmbedded(bundled);
        WriteCurrentVersion(BundledVersion);
        return bundled;
    }

    public string InstallUpdate(Stream zipStream, string version)
    {
        var target = Path.Combine(_engineRoot, version);
        var temp = target + ".tmp-" + Guid.NewGuid().ToString("N");

        try
        {
            Directory.CreateDirectory(_engineRoot);
            ExtractZip(zipStream, temp);
            if (!IsValid(temp)) throw new InvalidOperationException("Update archive is incomplete.");
            if (Directory.Exists(target)) Directory.Delete(target, true);
            Directory.Move(temp, target);
            WriteCurrentVersion(version);
            return target;
        }
        finally
        {
            if (Directory.Exists(temp)) Directory.Delete(temp, true);
        }
    }

    private void ExtractEmbedded(string destination)
    {
        var assembly = Assembly.GetExecutingAssembly();
        using var stream = assembly.GetManifestResourceStream(ResourceName)
            ?? throw new InvalidOperationException($"Embedded resource {ResourceName} not found.");
        ExtractZip(stream, destination);
    }

    private static void ExtractZip(Stream stream, string destination)
    {
        Directory.CreateDirectory(destination);
        using var archive = new ZipArchive(stream, ZipArchiveMode.Read, leaveOpen: true);
        foreach (var entry in archive.Entries)
        {
            if (string.IsNullOrEmpty(entry.Name)) continue;
            var path = Path.GetFullPath(Path.Combine(destination, entry.FullName));
            if (!path.StartsWith(Path.GetFullPath(destination), StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Zip entry escapes destination.");
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            entry.ExtractToFile(path, overwrite: true);
        }
    }

    private static bool IsValid(string directory)
    {
        if (!Directory.Exists(directory)) return false;
        if (RequiredFiles.Any(f => !File.Exists(Path.Combine(directory, f)))) return false;
        var bin = Path.Combine(directory, "bin");
        return Directory.Exists(bin) && Directory.EnumerateFiles(bin, "*.bin").Any();
    }

    private void WriteCurrentVersion(string version)
    {
        Directory.CreateDirectory(_engineRoot);
        File.WriteAllText(CurrentVersionFile, version);
    }
}
