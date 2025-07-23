namespace Porticle.Grpc.GuidMapper;

public class TypeMapperException : Exception
{
    public TypeMapperException()
    {
    }

    public TypeMapperException(string message) : base(message)
    {
    }

    public TypeMapperException(string message, Exception inner) : base(message, inner)
    {
    }
}