using FleetOps.Application;
using FleetOps.Domain;
using FleetOps.Infrastructure.Azure;
using Serilog;

var builder = WebApplication.CreateBuilder(args);
builder.Host.UseSerilog((ctx, cfg) => cfg.ReadFrom.Configuration(ctx.Configuration).WriteTo.Console());

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddFleetOpsApplication();

if (!string.IsNullOrEmpty(builder.Configuration["Azure:ServiceBusNamespace"]))
    builder.Services.AddFleetOpsAzure(o => builder.Configuration.GetSection(AzureOptions.Section).Bind(o));
else
    builder.Services.AddSingleton<IEventPublisher, NullEventPublisher>();

var app = builder.Build();
app.UseSwagger();
app.UseSwaggerUI();

app.MapPost("/vehicles", async (RegisterVehicleRequest r, ICommandHandler<RegisterVehicleCommand, VehicleId> h) =>
    Results.Created($"/vehicles/{await h.HandleAsync(new RegisterVehicleCommand(r.Plate, r.Kilometres))}", null));

app.MapPost("/vehicles/{id:guid}/maintenance", async (Guid id, MaintenanceRequest r, ICommandHandler<LogMaintenanceCommand, bool> h) =>
    await h.HandleAsync(new LogMaintenanceCommand(new VehicleId(id), r.Description)) ? Results.NoContent() : Results.NotFound());

app.MapGet("/vehicles/due", async (IQueryHandler<VehiclesDueQuery, IReadOnlyList<VehicleDto>> h, int? minKm) =>
    Results.Ok(await h.HandleAsync(new VehiclesDueQuery(minKm ?? 0))));

app.Run();

public sealed record RegisterVehicleRequest(string Plate, int Kilometres);
public sealed record MaintenanceRequest(string Description);

public sealed class NullEventPublisher : IEventPublisher
{
    public Task PublishAsync(IDomainEvent evt, CancellationToken ct = default) => Task.CompletedTask;
}
