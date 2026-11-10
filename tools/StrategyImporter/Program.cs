using ZapretLauncher.Core.Strategies;

if (args.Length != 2)
{
    Console.Error.WriteLine("Usage: StrategyImporter <bat-directory> <output-json>");
    return 1;
}

var sourceDir = args[0];
var output = args[1];
var files = Directory.GetFiles(sourceDir, "general*.bat")
    .OrderBy(f => f, StringComparer.OrdinalIgnoreCase)
    .ToList();

var strategies = files
    .Select(file => BatStrategyParser.Parse(File.ReadAllText(file), Path.GetFileName(file)))
    .ToList();

Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(output))!);
File.WriteAllText(output, StrategyJson.Serialize(strategies));
Console.WriteLine($"Wrote {strategies.Count} strategies to {output}");
return 0;
