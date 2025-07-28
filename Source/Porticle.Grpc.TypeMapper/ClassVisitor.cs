using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Porticle.Grpc.TypeMapper;

public class ClassVisitor : CSharpSyntaxRewriter
{
    public override SyntaxNode? VisitClassDeclaration(ClassDeclarationSyntax node)
    {
        var marker = "[Porticle.Grpc.TypeMapper]";

        if (node.GetLeadingTrivia().ToFullString().Contains(marker))
            // Skip if marker exists - class alreqady patched
            return node;

        // Add marker
        var trivia = node.GetLeadingTrivia().Add(SyntaxFactory.Comment("/// <remark>" + marker + "</remark>")).Add(SyntaxFactory.CarriageReturnLineFeed);
        node = node.WithLeadingTrivia(trivia);

        var propertyVisitor = new PropertyVisitor();
        node = (ClassDeclarationSyntax)propertyVisitor.Visit(node);

        if (propertyVisitor.NeedGuidConverter) node = node.AddMembers(ClassFromSource(ListWrappers.RepeatedFieldGuidWrapper));

        var methodVisitor = new MethodVisitor(propertyVisitor.ReplaceProps);
        node = (ClassDeclarationSyntax)methodVisitor.Visit(node);

        return node;
    }

    private static ClassDeclarationSyntax ClassFromSource(string classCode)
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(classCode);
        var root = syntaxTree.GetRoot();
        var nestedClass = root.DescendantNodes()
            .OfType<ClassDeclarationSyntax>()
            .Single();
        return nestedClass
            .WithLeadingTrivia(SyntaxFactory.CarriageReturnLineFeed)
            .WithTrailingTrivia(SyntaxFactory.CarriageReturnLineFeed);
    }
}