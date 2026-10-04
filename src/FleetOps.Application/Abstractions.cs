using FleetOps.Domain;

namespace FleetOps.Application;

// CQRS contracts
public interface ICommandHandler<in TCommand, TResult> { Task<TResult> HandleAsync(TCommand command, CancellationToken ct = default); }
public interface IQueryHandler<in TQuery, TResult> { Task<TResult> HandleAsync(TQuery query, CancellationToken ct = default); }

// Repository
public interface IVehicleRepository
{
    Task AddAsync(Vehicle vehicle, CancellationToken ct = default);
    Task<Vehicle?> GetAsync(VehicleId id, CancellationToken ct = default);
    Task<IReadOnlyList<Vehicle>> FindAsync(Specification<Vehicle> spec, CancellationToken ct = default);
}

// Ports implemented by the Azure adapter layer
public interface IBlobStore
{
    Task<Uri> UploadAsync(string name, Stream content, string contentType, CancellationToken ct = default);
    Task<bool> ExistsAsync(string name, CancellationToken ct = default);
}

public interface IEventPublisher { Task PublishAsync(IDomainEvent evt, CancellationToken ct = default); }

public interface ISecretProvider { Task<string?> GetSecretAsync(string name, CancellationToken ct = default); }
