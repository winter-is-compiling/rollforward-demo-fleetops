namespace FleetOps.Domain;

public readonly record struct VehicleId(Guid Value)
{
    public static VehicleId New() => new(Guid.NewGuid());
    public override string ToString() => Value.ToString("N");
}

public readonly record struct Mileage
{
    public int Kilometres { get; }

    public Mileage(int kilometres)
    {
        if (kilometres < 0) throw new ArgumentOutOfRangeException(nameof(kilometres), "Mileage cannot be negative.");
        Kilometres = kilometres;
    }

    public Mileage Add(int kilometres) => new(Kilometres + kilometres);
}

public enum VehicleStatus { Active, InMaintenance, Retired }

public sealed record MaintenanceRecord(DateOnly Date, Mileage MileageAtService, string Description);

public interface IDomainEvent { DateTimeOffset OccurredAt { get; } }

public sealed record VehicleRegistered(VehicleId Id, string Plate, DateTimeOffset OccurredAt) : IDomainEvent;
public sealed record MaintenanceLogged(VehicleId Id, string Description, DateTimeOffset OccurredAt) : IDomainEvent;

public sealed class Vehicle
{
    private readonly List<MaintenanceRecord> _history = new();
    private readonly List<IDomainEvent> _events = new();

    public VehicleId Id { get; }
    public string Plate { get; }
    public Mileage Mileage { get; private set; }
    public VehicleStatus Status { get; private set; } = VehicleStatus.Active;
    public IReadOnlyList<MaintenanceRecord> History => _history;
    public IReadOnlyList<IDomainEvent> Events => _events;

    private Vehicle(VehicleId id, string plate, Mileage mileage)
    {
        Id = id;
        Plate = plate;
        Mileage = mileage;
    }

    public static Vehicle Register(string plate, Mileage initial, TimeProvider clock)
    {
        if (string.IsNullOrWhiteSpace(plate)) throw new ArgumentException("Plate is required.", nameof(plate));
        var v = new Vehicle(VehicleId.New(), plate.Trim().ToUpperInvariant(), initial);
        v._events.Add(new VehicleRegistered(v.Id, v.Plate, clock.GetUtcNow()));
        return v;
    }

    public void RecordDrive(int kilometres)
    {
        if (Status == VehicleStatus.Retired) throw new InvalidOperationException("Retired vehicles cannot be driven.");
        Mileage = Mileage.Add(kilometres);
    }

    public void LogMaintenance(string description, TimeProvider clock)
    {
        if (Status == VehicleStatus.Retired) throw new InvalidOperationException("Retired vehicles cannot be serviced.");
        _history.Add(new MaintenanceRecord(DateOnly.FromDateTime(clock.GetUtcNow().UtcDateTime), Mileage, description));
        _events.Add(new MaintenanceLogged(Id, description, clock.GetUtcNow()));
    }

    public void Retire() => Status = VehicleStatus.Retired;
    public void ClearEvents() => _events.Clear();
}
