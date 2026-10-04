using System.Text.Json;
using Azure.Messaging.ServiceBus;
using Azure.Security.KeyVault.Secrets;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using FleetOps.Application;
using FleetOps.Domain;
using Microsoft.Extensions.Logging;

namespace FleetOps.Infrastructure.Azure;

public sealed class AzureBlobStore(BlobContainerClient container) : IBlobStore
{
    public async Task<Uri> UploadAsync(string name, Stream content, string contentType, CancellationToken ct = default)
    {
        var blob = container.GetBlobClient(name);
        await blob.UploadAsync(content, new BlobHttpHeaders { ContentType = contentType }, cancellationToken: ct);
        return blob.Uri;
    }

    public async Task<bool> ExistsAsync(string name, CancellationToken ct = default) =>
        (await container.GetBlobClient(name).ExistsAsync(ct)).Value;
}

public sealed class ServiceBusEventPublisher(ServiceBusSender sender, ILogger<ServiceBusEventPublisher> log) : IEventPublisher
{
    public async Task PublishAsync(IDomainEvent evt, CancellationToken ct = default)
    {
        var body = JsonSerializer.Serialize(evt, evt.GetType());
        var message = new ServiceBusMessage(body) { Subject = evt.GetType().Name, ContentType = "application/json" };
        await sender.SendMessageAsync(message, ct);
        log.LogDebug("Published {Event} to Service Bus", message.Subject);
    }
}

public sealed class KeyVaultSecretProvider(SecretClient client) : ISecretProvider
{
    public async Task<string?> GetSecretAsync(string name, CancellationToken ct = default)
    {
        try { return (await client.GetSecretAsync(name, null, ct)).Value.Value; }
        catch (global::Azure.RequestFailedException ex) when (ex.Status == 404) { return null; }
    }
}
