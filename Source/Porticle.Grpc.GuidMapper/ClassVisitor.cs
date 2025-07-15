using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Porticle.Grpc.GuidMapper;

public class ClassVisitor : CSharpSyntaxRewriter
{
    public override SyntaxNode? VisitClassDeclaration(ClassDeclarationSyntax node)
    {
        Console.WriteLine("Visit Class "+node.Identifier.ValueText);
        var marker = "[Porticle.Grpc.GuidMapper]";
        
        // Skip if marker exists
        if (node.GetLeadingTrivia().ToFullString().Contains(marker))
        {
            Console.WriteLine("Class Already Patched");
            return node;
        }
        
        // Add marker
        var trivia = SyntaxFactory.TriviaList(SyntaxFactory.Comment("/// <remark>" + marker + "</remark>"), SyntaxFactory.LineFeed).AddRange(node.GetLeadingTrivia());
        node = node.WithLeadingTrivia(trivia);        
        
        var propertyVisitor = new PropertyVisitor();
        node = (ClassDeclarationSyntax)propertyVisitor.Visit(node);

        node = node.AddMembers(ClassFromSource(ListWrappers.RepeatedFieldGuidWrapper));
        node = node.AddMembers(ClassFromSource(ListWrappers.RepeatedFieldNullableGuidWrapper));
        node = node.AddMembers(ClassFromSource(ListWrappers.RepeatedFieldNullableStringWrapper));
        
        Console.WriteLine("Visit Methods for "+propertyVisitor.ReplaceProps.Count+" props");
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