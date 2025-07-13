using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Porticle.Grpc.GuidMapper;


public class ClassVisitor : CSharpSyntaxRewriter
{
    public override SyntaxNode? VisitClassDeclaration(ClassDeclarationSyntax node)
    {
        Console.WriteLine("Visit Class "+node.Identifier.ValueText);
        var propertyVisitor = new PropertyVisitor();
        node = (ClassDeclarationSyntax)propertyVisitor.Visit(node);

        
        Console.WriteLine("Visit Methods for "+propertyVisitor.ReplaceProps.Count+" props");
        var methodVisitor = new MethodVisitor(propertyVisitor.ReplaceProps);
        node = (ClassDeclarationSyntax)methodVisitor.Visit(node);

        return node;
    }
}

public class PropertyVisitor : CSharpSyntaxRewriter
{
    public HashSet<PropertyToField> ReplaceProps = new();

    public override SyntaxNode? VisitPropertyDeclaration(PropertyDeclarationSyntax node)
    {
        var newProperty = CheckProperty(node);

        return newProperty ?? node;
    }

    private PropertyDeclarationSyntax? CheckProperty(PropertyDeclarationSyntax property)
    {
        if (property.Type.ToString() != "string")
            // dont change anything
            return null;

        if (property.GetLeadingTrivia().ToFullString().Contains("[GrpcGuid]"))
        {
            return ConvertToGuidProperty(property);
        }

        if (property.GetLeadingTrivia().ToFullString().Contains("[NullableString]"))
        {
            return ConvertToNullableStringProperty(property);
        }

        return null;
    }

    private PropertyDeclarationSyntax? ConvertToNullableStringProperty(PropertyDeclarationSyntax property)
    {
        var setter = property.AccessorList?.Accessors.FirstOrDefault(a => a.IsKind(SyntaxKind.SetAccessorDeclaration));

        if (setter?.Body == null)
        {
            Console.WriteLine($"[Error] No setter found in property {property.Identifier}");
            return null;
        }

        var isNullable = !setter.Body.ToFullString().Contains("ProtoPreconditions.CheckNotNull");

        if (!isNullable)
        {
            Console.WriteLine($"[Error] String property {property.Identifier} ist not nullable");
            return null;
        }
        
        var enableDirective = SyntaxFactory.Trivia(SyntaxFactory.NullableDirectiveTrivia(SyntaxFactory.Token(SyntaxKind.EnableKeyword).WithLeadingTrivia(SyntaxFactory.Space), true));
        var disableDirective = SyntaxFactory.Trivia(SyntaxFactory.NullableDirectiveTrivia(SyntaxFactory.Token(SyntaxKind.DisableKeyword).WithLeadingTrivia(SyntaxFactory.Space), true));

        var leadingTrivia = SyntaxFactory.TriviaList(enableDirective, SyntaxFactory.ElasticCarriageReturnLineFeed);
        var trailingTrivia = SyntaxFactory.TriviaList(SyntaxFactory.ElasticCarriageReturnLineFeed, disableDirective);

        return property
            .WithType(SyntaxFactory.ParseTypeName("string?").WithTrailingTrivia(SyntaxFactory.ElasticSpace))
            .WithLeadingTrivia(leadingTrivia.AddRange(property.GetLeadingTrivia()))
            .WithTrailingTrivia(property.GetTrailingTrivia().AddRange(trailingTrivia));        
    }

    private PropertyDeclarationSyntax? ConvertToGuidProperty(PropertyDeclarationSyntax property)
    {
        var setter = property.AccessorList?.Accessors.FirstOrDefault(a => a.IsKind(SyntaxKind.SetAccessorDeclaration));

        if (setter?.Body == null)
        {
            Console.WriteLine($"[Error] No setter found in property {property.Identifier}");
            return null;
        }

        var isNullable = !setter.Body.ToFullString().Contains("ProtoPreconditions.CheckNotNull");

        // Manipulate setter 
        var assignment = setter.Body.Statements
            .OfType<ExpressionStatementSyntax>()
            .Select(s => s.Expression as AssignmentExpressionSyntax)
            .FirstOrDefault();

        if (assignment == null)
        {
            Console.WriteLine($"[Error] Setter has no valid assignment expression in property {property.Identifier}");
            return null;
        }

        var originalRightHandSide = assignment.Right;

        if (!isNullable)
        {
            if (originalRightHandSide is not InvocationExpressionSyntax invocationExpr || !invocationExpr.Expression.ToString().EndsWith("CheckNotNull"))
            {
                Console.WriteLine($"[Error] Can't find CheckNotNull call in setter od property {property.Identifier}");
                return null;
            }

            originalRightHandSide = invocationExpr.ArgumentList.Arguments.First().Expression;
        }

        var toStringMethodName = SyntaxFactory.IdentifierName("ToString");
        var toStringArgument = SyntaxFactory.Argument(SyntaxFactory.LiteralExpression(SyntaxKind.StringLiteralExpression, SyntaxFactory.Literal("D")));
        var toStringArgumentList = SyntaxFactory.ArgumentList(SyntaxFactory.SingletonSeparatedList(toStringArgument));
        var parenthesizedoriginalRightHandSide = SyntaxFactory.ParenthesizedExpression(originalRightHandSide);

        ExpressionSyntax newRightHandSide = isNullable
            ? SyntaxFactory.ConditionalAccessExpression(parenthesizedoriginalRightHandSide,
                SyntaxFactory.InvocationExpression(SyntaxFactory.MemberBindingExpression(toStringMethodName), toStringArgumentList))
            : SyntaxFactory.InvocationExpression(
                SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, parenthesizedoriginalRightHandSide, toStringMethodName), toStringArgumentList);
        var newAssignment = assignment.WithRight(newRightHandSide);
        var newSetter = setter.ReplaceNode(assignment, newAssignment);
        property = property.ReplaceNode(setter, newSetter);

        // Manipulate getter 
        var getter = property.AccessorList?.Accessors.FirstOrDefault(a => a.IsKind(SyntaxKind.GetAccessorDeclaration));

        if (getter?.Body == null)
        {
            Console.WriteLine($"[Error] No getter found in property {property.Identifier}");
            return null;
        }

        var returnStatement = getter.Body?.Statements.OfType<ReturnStatementSyntax>().FirstOrDefault();

        if (returnStatement?.Expression == null)
        {
            Console.WriteLine($"[Error] Getter has no valid return statement in property {property.Identifier}");
            return null;
        }

        var originalReturnExpression = returnStatement.Expression;

        if (originalReturnExpression is not IdentifierNameSyntax identifierNameSyntax)
        {
            Console.WriteLine($"[Error] Getter return statement should be a simple identifier in property {property.Identifier}");
            return null;
        }

        ReplaceProps.Add(new PropertyToField(property.Identifier.ValueText, identifierNameSyntax.Identifier.ValueText));

        var newReturnExpression = SyntaxFactory.InvocationExpression(
            SyntaxFactory.ParseExpression("global::System.Guid.Parse"), // Die Methode
            SyntaxFactory.ArgumentList(SyntaxFactory.SingletonSeparatedList(SyntaxFactory.Argument(originalReturnExpression))));

        var newReturnStatement = returnStatement.WithExpression(newReturnExpression).WithTrailingTrivia(SyntaxFactory.Space);

        BlockSyntax newGetterBody;
        if (isNullable)
        {
            // Statement: if (variable == null) return null;
            var ifStatement = SyntaxFactory.IfStatement(
                SyntaxFactory.BinaryExpression(SyntaxKind.EqualsExpression, originalReturnExpression, SyntaxFactory.LiteralExpression(SyntaxKind.NullLiteralExpression)),
                SyntaxFactory.ReturnStatement(SyntaxFactory.LiteralExpression(SyntaxKind.DefaultLiteralExpression).WithLeadingTrivia(SyntaxFactory.Space))
            ).WithTrailingTrivia(SyntaxFactory.Space);

            newGetterBody = SyntaxFactory.Block(ifStatement, newReturnStatement);
        }
        else
        {
            newGetterBody = SyntaxFactory.Block(newReturnStatement);
        }

        var newGetter = getter.WithBody(newGetterBody.WithTrailingTrivia(SyntaxFactory.CarriageReturnLineFeed));

        property = property.ReplaceNode(getter, newGetter);

        // change the type of the property
        if (isNullable)
            property = property.WithType(SyntaxFactory.ParseTypeName("global::System.Guid?").WithTrailingTrivia(SyntaxFactory.Space));
        else
            property = property.WithType(SyntaxFactory.ParseTypeName("global::System.Guid").WithTrailingTrivia(SyntaxFactory.Space));

        return property;
    }
}