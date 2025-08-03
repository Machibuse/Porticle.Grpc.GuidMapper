using JetBrains.Annotations;
using Microsoft.Build.Framework;
using Microsoft.CodeAnalysis.CSharp;
using Task = Microsoft.Build.Utilities.Task;

namespace Porticle.Grpc.TypeMapper;

/// <inheritdoc />
[UsedImplicitly]
public class ProtoPostProcessor : Task
{
    [Required] public ITaskItem[] FilesToPostProcess { get; set; }

    public bool WrapAllNonNullableStrings { get; set; }
    public bool WrapAllNullableStringValues { get; set; }

    public override bool Execute()
    {
        Log.LogMessage(MessageImportance.High,
            $"ProtoPostProcessor: WrapAllNonNullableStrings:{WrapAllNonNullableStrings} WrapAllNullableStringValues:{WrapAllNullableStringValues} FileCount:{FilesToPostProcess.Length}");
        foreach (var item in FilesToPostProcess)
        {
            var file = item.ItemSpec;
            Log.LogMessage(MessageImportance.High, $"Verarbeite: {file}");

            var basename = Path.GetFileNameWithoutExtension(file);

            if (string.IsNullOrWhiteSpace(basename))
            {
                Log.LogError("Nothing to preprocess - no filename given");
                return false;
            }

            var directory = Path.GetDirectoryName(file)!;

            string[] filenames = [StringUtils.LowerUnderscoreToUpperCamelProtocWay(basename) + ".cs", StringUtils.LowerUnderscoreToUpperCamelGrpcWay(basename) + "Grpc.cs"];

            foreach (var filename in filenames)
            {
                var filePath = Path.Combine(directory, filename);

                var originalCode = File.ReadAllText(filePath);
                var tree = CSharpSyntaxTree.ParseText(originalCode);
                var root = tree.GetRoot();
                File.WriteAllText(filePath + "_", root.ToFullString());

                var classVisitor = new ClassVisitor(Log);
                root = classVisitor.Visit(root);

                File.WriteAllText(filePath, root.ToFullString());
                Log.LogMessage(MessageImportance.High, "Grpc-Post-processing complete.");
            }
        }

        // return false, when errors was logged
        return !Log.HasLoggedErrors;
    }
}