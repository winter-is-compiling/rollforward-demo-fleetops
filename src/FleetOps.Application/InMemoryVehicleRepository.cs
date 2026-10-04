using System.Collections.Concurrent;
using FleetOps.Domain;

namespace FleetOps.Application;

public sealed class InMemoryVehicleRepository : IVehicleRepository
{
    private readonly ConcurrentDictionary<VehicleId, Vehicle> _store = new();

    public Task AddAsync(Vehicle vehicle, CancellationToken ct = default)
    {
        _store[vehicle.Id] = vehicle;
        return Task.CompletedTask;
    }

    public Task<Vehicle?> GetAsync(VehicleId id, CancellationToken ct = default) =>
        Task.FromResult(_store.TryGetValue(id, out var v) ? v : null);

    public Task<IReadOnlyList<Vehicle>> FindAsync(Specification<Vehicle> spec, CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<Vehicle>>(_store.Values.Where(spec.IsSatisfiedBy).ToList());
}
