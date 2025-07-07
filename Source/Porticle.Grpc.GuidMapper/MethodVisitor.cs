using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Porticle.Grpc.GuidMapper;

public class MethodVisitor : CSharpSyntaxRewriter
{
    public MethodVisitor(HashSet<PropertyToField> replaceProps)
    {
        ReplaceProps = replaceProps;
    }

    public HashSet<PropertyToField> ReplaceProps { get; }

    public override SyntaxNode? VisitMethodDeclaration(MethodDeclarationSyntax node)
    {
        var propertyToFieldRewriter = new PropertyToFieldRewriter(ReplaceProps);

        var newBody = (BlockSyntax?)propertyToFieldRewriter.Visit(node.Body);

        if (newBody == null) return base.VisitMethodDeclaration(node);

        return node.WithBody(newBody);
    }
}