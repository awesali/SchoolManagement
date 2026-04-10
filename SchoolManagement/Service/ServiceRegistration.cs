using System.Reflection;

namespace SchoolManagement.Service
{
    public static class ServiceRegistration
    {
        public static void RegisterAppServices(this IServiceCollection services)
        {
            var assembly = Assembly.GetExecutingAssembly();

            var types = assembly.GetTypes()
                .Where(t => t.IsClass && !t.IsAbstract);

            foreach (var implementation in types)
            {
                var interfaces = implementation.GetInterfaces();

                foreach (var service in interfaces)
                {
                    // Register only Repository & Service
                    if (service.Name.EndsWith("Repository") || service.Name.EndsWith("Service"))
                    {
                        services.AddScoped(service, implementation);
                    }
                }
            }
        }
    }
}
