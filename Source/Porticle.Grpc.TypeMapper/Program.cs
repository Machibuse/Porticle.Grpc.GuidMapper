using Microsoft.CodeAnalysis.CSharp;
using Porticle.Grpc.TypeMapper;

// filter out "--" arg 
args = args.Where(s => s != "--").ToArray();

// Akzeptiert den Dateipfad als Argument
if (args.Length != 1)
{
    Console.WriteLine("Error: Expected exactly 1 arg but got " + args.Length);
    foreach (var arg in args) Console.WriteLine("Error: arg[..] '" + arg + "'");
    return;
}

var basename = Path.GetFileNameWithoutExtension(args[0]);

if (string.IsNullOrWhiteSpace(basename))
{
    Console.WriteLine("Warning: Nothing to preprocess - no filename given");
    return;
}

var directory = Path.GetDirectoryName(args[0])!;
string[] filenames = [StringUtils.LowerUnderscoreToUpperCamelProtocWay(basename) + ".cs", StringUtils.LowerUnderscoreToUpperCamelGrpcWay(basename) + "Grpc.cs"];

Console.WriteLine($"GRPC Post-processing for: {string.Join(", ", filenames)}");

foreach (var filename in filenames)
{
    var filePath = Path.Combine(directory, filename);

    var originalCode = File.ReadAllText(filePath);
    var tree = CSharpSyntaxTree.ParseText(originalCode);
    var root = tree.GetRoot();
    File.WriteAllText(filePath + "_", root.ToFullString());

    var classVisitor = new ClassVisitor();
    root = classVisitor.Visit(root);

    File.WriteAllText(filePath, root.ToFullString());
    Console.WriteLine("Post-processing complete.");
}