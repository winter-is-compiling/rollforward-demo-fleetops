using FleetOps.Domain;

namespace FleetOps.Application;

/// <summary>Strategy pattern: interchangeable rules for deciding when a vehicle is due for service.</summary>
public interface IMaintenancePolicy
{
    string Name { get; }
    bool IsDue(Vehicle vehicle, DateOnly today);
}

public sealed class MileageIntervalPolicy(int intervalKm) : IMaintenancePolicy
{
    public string Name => $"every-{intervalKm}km";

    public bool IsDue(Vehicle vehicle, DateOnly today)
    {
        var last = vehicle.History.Count == 0 ? 0 : vehicle.History[^1].MileageAtService.Kilometres;
        return vehicle.Mileage.Kilometres - last >= intervalKm;
    }
}

public sealed class TimeIntervalPolicy(int intervalDays) : IMaintenancePolicy
{
    public string Name => $"every-{intervalDays}d";

    public bool IsDue(Vehicle vehicle, DateOnly today) =>
        vehicle.History.Count > 0 && today.DayNumber - vehicle.History[^1].Date.DayNumber >= intervalDays;
}

/// <summary>Composite: due when any child policy says so.</summary>
public sealed class AnyPolicy(IEnumerable<IMaintenancePolicy> policies) : IMaintenancePolicy
{
    private readonly IReadOnlyList<IMaintenancePolicy> _policies = policies.ToList();
    public string Name => string.Join("|", _policies.Select(p => p.Name));
    public bool IsDue(Vehicle vehicle, DateOnly today) => _policies.Any(p => p.IsDue(vehicle, today));
}
