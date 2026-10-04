using FleetOps.Domain;
using Microsoft.Extensions.Logging;

namespace FleetOps.Application;

public sealed record RegisterVehicleCommand(string Plate, int InitialKilometres);
public sealed record LogMaintenanceCommand(VehicleId VehicleId, string Description);
public sealed record VehiclesDueQuery(int MinKilometres = 0);

public sealed record VehicleDto(Guid Id, string Plate, int Kilometres, string Status);

public sealed class RegisterVehicleHandler(IVehicleRepository repo, IEventPublisher events, TimeProvider clock)
    : ICommandHandler<RegisterVehicleCommand, VehicleId>
{
    public async Task<VehicleId> HandleAsync(RegisterVehicleCommand command, CancellationToken ct = default)
    {
        var vehicle = Vehicle.Register(command.Plate, new Mileage(command.InitialKilometres), clock);
        await repo.AddAsync(vehicle, ct);
        foreach (var e in vehicle.Events) await events.PublishAsync(e, ct);
        vehicle.ClearEvents();
        return vehicle.Id;
    }
}

public sealed class LogMaintenanceHandler(IVehicleRepository repo, IEventPublisher events, TimeProvider clock)
    : ICommandHandler<LogMaintenanceCommand, bool>
{
    public async Task<bool> HandleAsync(LogMaintenanceCommand command, CancellationToken ct = default)
    {
        var vehicle = await repo.GetAsync(command.VehicleId, ct);
        if (vehicle is null) return false;
        vehicle.LogMaintenance(command.Description, clock);
        foreach (var e in vehicle.Events) await events.PublishAsync(e, ct);
        vehicle.ClearEvents();
        return true;
    }
}

public sealed class VehiclesDueForServiceHandler(IVehicleRepository repo, IMaintenancePolicy policy, TimeProvider clock)
    : IQueryHandler<VehiclesDueQuery, IReadOnlyList<VehicleDto>>
{
    public async Task<IReadOnlyList<VehicleDto>> HandleAsync(VehiclesDueQuery query, CancellationToken ct = default)
    {
        var spec = new ActiveVehicleSpecification().And(new MileageAtLeastSpecification(query.MinKilometres));
        var today = DateOnly.FromDateTime(clock.GetUtcNow().UtcDateTime);
        var found = await repo.FindAsync(spec, ct);
        return found.Where(v => policy.IsDue(v, today))
                    .Select(v => new VehicleDto(v.Id.Value, v.Plate, v.Mileage.Kilometres, v.Status.ToString()))
                    .ToList();
    }
}

/// <summary>Decorator: adds logging around any command handler without touching it.</summary>
public sealed class LoggingCommandHandler<TCommand, TResult>(
    ICommandHandler<TCommand, TResult> inner, ILogger<LoggingCommandHandler<TCommand, TResult>> log)
    : ICommandHandler<TCommand, TResult>
{
    public async Task<TResult> HandleAsync(TCommand command, CancellationToken ct = default)
    {
        log.LogInformation("Handling {Command}", typeof(TCommand).Name);
        try { return await inner.HandleAsync(command, ct); }
        catch (Exception ex) { log.LogError(ex, "{Command} failed", typeof(TCommand).Name); throw; }
    }
}
