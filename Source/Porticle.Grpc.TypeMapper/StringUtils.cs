using System.Text;

namespace Porticle.Grpc.TypeMapper;

internal static class StringUtils
{
    // This is how the protoc codegen constructs its output filename.
// See protobuf/compiler/csharp/csharp_helpers.cc:137.
// Note that protoc explicitly discards non-ASCII letters.
    public static string LowerUnderscoreToUpperCamelProtocWay(string str)
    {
        var result = new StringBuilder(str.Length, str.Length);
        var cap = true;
        foreach (var c in str)
        {
            var upperC = char.ToUpperInvariant(c);
            var isAsciiLetter = 'A' <= upperC && upperC <= 'Z';
            if (isAsciiLetter || ('0' <= c && c <= '9')) result.Append(cap ? upperC : c);
            cap = !isAsciiLetter;
        }

        return result.ToString();
    }


// This is how the gRPC codegen currently construct its output filename.
// See src/compiler/generator_helpers.h:118.
    public static string LowerUnderscoreToUpperCamelGrpcWay(string str)
    {
        var result = new StringBuilder(str.Length, str.Length);
        var cap = true;
        foreach (var c in str)
            if (c == '_')
            {
                cap = true;
            }
            else if (cap)
            {
                result.Append(char.ToUpperInvariant(c));
                cap = false;
            }
            else
            {
                result.Append(c);
            }

        return result.ToString();
    }


// --- Beispiel für einen einfachen Roslyn Rewriter ---
}