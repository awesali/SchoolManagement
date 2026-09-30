// Backend section: application services and shared rules.
using System.Reflection;

namespace SchoolManagement.Service
{
    // Implements service registration application behavior.
    public static class ServiceRegistration
    {
        public static void RegisterAppServices(this IServiceCollection services)
        {
            var assembly = Assembly.GetExecutingAssembly();

            var types = assembly
                .GetTypes()
                .Where(t =>
                    t.IsClass && !t.IsAbstract && !typeof(IHostedService).IsAssignableFrom(t)
                );

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
