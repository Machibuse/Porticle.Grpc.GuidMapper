using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Porticle.Grpc.TypeMapper;

/// <summary>
///     Represents a mapping between a property name and its corresponding field name.
/// </summary>
public record PropertyToField(string PropertyName, string FieldName);

public record ClearFunction(string FunctionName, ExpressionStatementSyntax Expression);