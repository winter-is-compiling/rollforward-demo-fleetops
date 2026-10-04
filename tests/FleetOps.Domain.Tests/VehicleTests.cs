using FleetOps.Domain;

namespace FleetOps.Domain.Tests;

public class VehicleTests
{
    private static readonly TimeProvider Clock = TimeProvider.System;

    [Fact]
    public void Register_NormalisesPlate_AndRaisesEvent()
    {
        var v = Vehicle.Register("  ab-123 ", new Mileage(0), Clock);
        Assert.Equal("AB-123", v.Plate);
        Assert.Single(v.Events.OfType<VehicleRegistered>());
    }

    [Fact]
    public void Register_RejectsBlankPlate() =>
        Assert.Throws<ArgumentException>(() => Vehicle.Register(" ", new Mileage(0), Clock));

    [Fact]
    public void Mileage_RejectsNegative() =>
        Assert.Throws<ArgumentOutOfRangeException>(() => new Mileage(-1));

    [Fact]
    public void RecordDrive_AddsKilometres()
    {
        var v = Vehicle.Register("X1", new Mileage(100), Clock);
        v.RecordDrive(50);
        Assert.Equal(150, v.Mileage.Kilometres);
    }

    [Fact]
    public void RetiredVehicle_CannotBeDrivenOrServiced()
    {
        var v = Vehicle.Register("X1", new Mileage(0), Clock);
        v.Retire();
        Assert.Throws<InvalidOperationException>(() => v.RecordDrive(1));
        Assert.Throws<InvalidOperationException>(() => v.LogMaintenance("oil", Clock));
    }

    [Fact]
    public void LogMaintenance_RecordsHistoryAtCurrentMileage()
    {
        var v = Vehicle.Register("X1", new Mileage(500), Clock);
        v.LogMaintenance("oil change", Clock);
        Assert.Equal(500, v.History.Single().MileageAtService.Kilometres);
    }

    [Fact]
    public void Specifications_Compose()
    {
        var v = Vehicle.Register("X1", new Mileage(2000), Clock);
        var spec = new ActiveVehicleSpecification().And(new MileageAtLeastSpecification(1000));
        Assert.True(spec.IsSatisfiedBy(v));
        v.Retire();
        Assert.False(spec.IsSatisfiedBy(v));
    }
}
