using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Porticle.Grpc.GuidMapper;

/// <summary>
///     Ein spezialisierter Rewriter, der alle Vorkommen eines bestimmten
///     Identifier-Namens durch einen anderen ersetzt.
/// </summary>
public class PropertyToFieldRewriter : CSharpSyntaxRewriter
{
    public PropertyToFieldRewriter(HashSet<PropertyToField> replaceNames)
    {
        ReplaceNames = replaceNames;
    }

    public HashSet<PropertyToField> ReplaceNames { get; }

    public override SyntaxNode? VisitIdentifierName(IdentifierNameSyntax node)
    {
        var mapping = ReplaceNames.SingleOrDefault(field => field.PropertyName == node.Identifier.Text);

        if (mapping != null) return SyntaxFactory.IdentifierName(mapping.FieldName).WithTriviaFrom(node);

        return base.VisitIdentifierName(node);
    }
}