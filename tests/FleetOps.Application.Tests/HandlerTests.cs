using FleetOps.Application;
using FleetOps.Domain;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;

namespace FleetOps.Application.Tests;

public class HandlerTests
{
    private sealed class RecordingPublisher : IEventPublisher
    {
        public List<IDomainEvent> Published { get; } = new();
        public Task PublishAsync(IDomainEvent evt, CancellationToken ct = default) { Published.Add(evt); return Task.CompletedTask; }
    }

    private sealed class FixedClock(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }

    [Fact]
    public async Task Register_StoresVehicle_AndPublishesEvent()
    {
        var repo = new InMemoryVehicleRepository();
        var pub = new RecordingPublisher();
        var id = await new RegisterVehicleHandler(repo, pub, TimeProvider.System).HandleAsync(new RegisterVehicleCommand("ab1", 10));

        (await repo.GetAsync(id)).Should().NotBeNull();
        pub.Published.Should().ContainSingle(e => e is VehicleRegistered);
    }

    [Fact]
    public async Task LogMaintenance_UnknownVehicle_ReturnsFalse()
    {
        var h = new LogMaintenanceHandler(new InMemoryVehicleRepository(), new RecordingPublisher(), TimeProvider.System);
        (await h.HandleAsync(new LogMaintenanceCommand(VehicleId.New(), "x"))).Should().BeFalse();
    }

    [Fact]
    public async Task LogMaintenance_KnownVehicle_Publishes()
    {
        var repo = new InMemoryVehicleRepository();
        var pub = new RecordingPublisher();
        var id = await new RegisterVehicleHandler(repo, pub, TimeProvider.System).HandleAsync(new RegisterVehicleCommand("ab1", 0));
        (await new LogMaintenanceHandler(repo, pub, TimeProvider.System).HandleAsync(new LogMaintenanceCommand(id, "tyres"))).Should().BeTrue();
        pub.Published.Should().Contain(e => e is MaintenanceLogged);
    }

    [Fact]
    public async Task Due_ReturnsOnlyVehiclesOverMileageInterval()
    {
        var repo = new InMemoryVehicleRepository();
        var pub = new RecordingPublisher();
        var reg = new RegisterVehicleHandler(repo, pub, TimeProvider.System);
        await reg.HandleAsync(new RegisterVehicleCommand("OLD", 12_000));
        await reg.HandleAsync(new RegisterVehicleCommand("NEW", 500));

        var q = new VehiclesDueForServiceHandler(repo, new MileageIntervalPolicy(10_000), TimeProvider.System);
        var due = await q.HandleAsync(new VehiclesDueQuery());
        due.Should().ContainSingle().Which.Plate.Should().Be("OLD");
    }

    [Fact]
    public void TimePolicy_DueAfterInterval_AndCompositeCombines()
    {
        var clock = new FixedClock(new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero));
        var v = Vehicle.Register("T1", new Mileage(0), clock);
        v.LogMaintenance("service", clock);

        var later = new DateOnly(2026, 8, 1);
        new TimeIntervalPolicy(180).IsDue(v, later).Should().BeTrue();
        new TimeIntervalPolicy(400).IsDue(v, later).Should().BeFalse();
        new AnyPolicy(new IMaintenancePolicy[] { new MileageIntervalPolicy(99_999), new TimeIntervalPolicy(180) }).IsDue(v, later).Should().BeTrue();
    }

    [Fact]
    public async Task LoggingDecorator_DelegatesAndRethrows()
    {
        var ok = new LoggingCommandHandler<int, int>(new Doubler(), NullLogger<LoggingCommandHandler<int, int>>.Instance);
        (await ok.HandleAsync(21)).Should().Be(42);

        var bad = new LoggingCommandHandler<int, int>(new Thrower(), NullLogger<LoggingCommandHandler<int, int>>.Instance);
        await bad.Invoking(h => h.HandleAsync(1)).Should().ThrowAsync<InvalidOperationException>();
    }

    private sealed class Doubler : ICommandHandler<int, int> { public Task<int> HandleAsync(int c, CancellationToken ct = default) => Task.FromResult(c * 2); }
    private sealed class Thrower : ICommandHandler<int, int> { public Task<int> HandleAsync(int c, CancellationToken ct = default) => throw new InvalidOperationException(); }
}
