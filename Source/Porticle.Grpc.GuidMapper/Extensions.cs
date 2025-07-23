using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Porticle.Grpc.GuidMapper;

public static class Extensions
{
    public static T CheckNotNull<T>(this T? value, string valueDescription) where T : class
    {
        if (value == null) throw new TypeMapperException(valueDescription);

        return value;
    }

    public static AccessorDeclarationSyntax GetGetter(this PropertyDeclarationSyntax property)
    {
        return property.AccessorList.CheckNotNull("Accessors not found").Accessors.FirstOrDefault(a => a.IsKind(SyntaxKind.GetAccessorDeclaration)).CheckNotNull("Getter not found");
    }

    public static AccessorDeclarationSyntax GetSetter(this PropertyDeclarationSyntax property)
    {
        return property.AccessorList.CheckNotNull("Accessors not found").Accessors.FirstOrDefault(a => a.IsKind(SyntaxKind.SetAccessorDeclaration)).CheckNotNull("Setter not found");
    }
}