namespace Porticle.Grpc.GuidMapper;

public static class Extensions
{
    public static T CheckNotNull<T>(this T? value, string valueDescription) where T : class
    {
        if (value == null)
        {
            throw new TypeMapperException(valueDescription);
        }

        return value;
    }
    

    
    
}