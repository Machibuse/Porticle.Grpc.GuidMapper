using System.Text.RegularExpressions;
using JetBrains.Annotations;
using Microsoft.Build.Framework;
using Microsoft.CodeAnalysis.CSharp;
using Task = Microsoft.Build.Utilities.Task;

namespace Porticle.Grpc.TypeMapper;

/// <inheritdoc />
[UsedImplicitly]
public class ProtoPostProcessor : Task
{
    [Required] public ITaskItem[] FilesToPostProcess { get; set; } = null!;

    public bool WrapAllNonNullableStrings { get; set; }
    public bool WrapAllNullableStringValues { get; set; }

    public override bool Execute()
    {
        Log.LogMessage(MessageImportance.High,
            $"ProtoPostProcessor: WrapAllNonNullableStrings:{WrapAllNonNullableStrings} WrapAllNullableStringValues:{WrapAllNullableStringValues} ProtoFileCount:{FilesToPostProcess.Length}");

        foreach (var item in FilesToPostProcess)
        {
            // Change to platform-specific DirectorySeparator and fix duplicate DirectorySeparatorChars 
            var file = Regex.Replace(item.ItemSpec, "[" + Regex.Escape(Path.AltDirectorySeparatorChar.ToString()) + Regex.Escape(Path.DirectorySeparatorChar.ToString()) + "]+",
                Path.DirectorySeparatorChar.ToString());

            var basename = Path.GetFileNameWithoutExtension(file);

            if (string.IsNullOrWhiteSpace(basename))
            {
                Log.LogError($"Nothing to preprocess - no filename given {file}");
                return false;
            }

            var directory = Path.GetDirectoryName(file)!;

            string[] filenames = [StringUtils.LowerUnderscoreToUpperCamelProtocWay(basename) + ".cs", StringUtils.LowerUnderscoreToUpperCamelGrpcWay(basename) + "Grpc.cs"];

            foreach (var filename in filenames)
            {
                var filePath = Path.Combine(directory, filename);
                Log.LogMessage(MessageImportance.High, $"ProtoPostProcessor: protoc output file: {filePath}");

                var originalCode = File.ReadAllText(filePath);
                var tree = CSharpSyntaxTree.ParseText(originalCode);
                var root = tree.GetRoot();
                File.WriteAllText(filePath + "_", root.ToFullString());

                var classVisitor = new ClassVisitor(Log, WrapAllNonNullableStrings, WrapAllNullableStringValues);
                root = classVisitor.Visit(root);

                File.WriteAllText(filePath, root.ToFullString());
            }
        }

        Log.LogMessage(MessageImportance.High, "ProtoPostProcessor: completed");

        // return false when errors were logged
        return !Log.HasLoggedErrors;
    }
}