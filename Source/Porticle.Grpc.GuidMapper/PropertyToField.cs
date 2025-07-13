namespace Porticle.Grpc.GuidMapper;

/// <summary>
/// Represents a mapping between a property name and its corresponding field name.
/// </summary>
public record PropertyToField(string PropertyName, string FieldName);