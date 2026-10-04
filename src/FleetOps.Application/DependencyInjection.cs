using FleetOps.Domain;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace FleetOps.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddFleetOpsApplication(this IServiceCollection services)
    {
        services.AddSingleton(TimeProvider.System);
        services.AddSingleton<IVehicleRepository, InMemoryVehicleRepository>();
        services.AddSingleton<IMaintenancePolicy>(_ => new AnyPolicy(new IMaintenancePolicy[]
        {
            new MileageIntervalPolicy(10_000),
            new TimeIntervalPolicy(180),
        }));

        services.AddScoped<ICommandHandler<RegisterVehicleCommand, VehicleId>>(sp =>
            new LoggingCommandHandler<RegisterVehicleCommand, VehicleId>(
                new RegisterVehicleHandler(sp.GetRequiredService<IVehicleRepository>(), sp.GetRequiredService<IEventPublisher>(), sp.GetRequiredService<TimeProvider>()),
                sp.GetRequiredService<ILogger<LoggingCommandHandler<RegisterVehicleCommand, VehicleId>>>()));
        services.AddScoped<ICommandHandler<LogMaintenanceCommand, bool>>(sp =>
            new LoggingCommandHandler<LogMaintenanceCommand, bool>(
                new LogMaintenanceHandler(sp.GetRequiredService<IVehicleRepository>(), sp.GetRequiredService<IEventPublisher>(), sp.GetRequiredService<TimeProvider>()),
                sp.GetRequiredService<ILogger<LoggingCommandHandler<LogMaintenanceCommand, bool>>>()));
        services.AddScoped<IQueryHandler<VehiclesDueQuery, IReadOnlyList<VehicleDto>>, VehiclesDueForServiceHandler>();
        return services;
    }
}
