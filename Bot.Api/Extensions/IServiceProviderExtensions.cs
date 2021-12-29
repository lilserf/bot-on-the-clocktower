namespace System
{
    public static class IServiceProviderExtensions
    {
        public static T GetService<T>(this IServiceProvider @this) where T : class
        {
            if (@this.GetService(typeof(T)) is not T t) throw new ServiceNotFoundException(typeof(T));
            return t;
        }
    }

    public class ServiceNotFoundException : Exception
    {
        public Type TypeNotFound { get; }

        public ServiceNotFoundException(Type t)
        {
            TypeNotFound = t;
        }
    }
}
