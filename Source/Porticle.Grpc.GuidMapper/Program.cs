using Microsoft.CodeAnalysis.CSharp;
using Porticle.Grpc.GuidMapper;

// Akzeptiert den Dateipfad als Argument
if (args.Length != 1)
{
    Console.WriteLine("Error: Extected exactly 1 arg but got " + args.Length);
    return;
}

var basename = Path.GetFileNameWithoutExtension(args[0]);
var directory = Path.GetDirectoryName(args[0])!;
string[] filenames = [StringUtils.LowerUnderscoreToUpperCamelProtocWay(basename) + ".cs", StringUtils.LowerUnderscoreToUpperCamelGrpcWay(basename) + "Grpc.cs"];


foreach (var filename in filenames)
{
    var filePath = Path.Combine(directory, filename);
    Console.WriteLine($"Post-processing file: {filePath}");

    var originalCode = File.ReadAllText(filePath);
    var tree = CSharpSyntaxTree.ParseText(originalCode);
    var root = tree.GetRoot();
    File.WriteAllText(filePath + "_", root.ToFullString());

    var propertyVisitor = new PropertyVisitor();
    root = propertyVisitor.Visit(root);

    var methodVisitor = new MethodVisitor(propertyVisitor.ReplaceProps);
    root = methodVisitor.Visit(root);

    // das ganze noch für GOOGLE_PROTOBUF_REFSTRUCT_COMPATIBILITY_MODE
    var options = new CSharpParseOptions();
    options = options.WithPreprocessorSymbols("GOOGLE_PROTOBUF_REFSTRUCT_COMPATIBILITY_MODE");
    root = CSharpSyntaxTree.ParseText(root.ToFullString(), options).GetRoot();
    root = methodVisitor.Visit(root);

    File.WriteAllText(filePath, root.ToFullString());
    Console.WriteLine("Post-processing complete.");
}