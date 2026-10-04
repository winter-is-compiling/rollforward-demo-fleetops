using Azure;
using Azure.Messaging.ServiceBus;
using Azure.Security.KeyVault.Secrets;
using Azure.Storage.Blobs;
using FleetOps.Domain;
using FleetOps.Infrastructure.Azure;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace FleetOps.Infrastructure.Azure.Tests;

public class AdapterTests
{
    [Fact]
    public async Task EventPublisher_SendsJsonMessageWithSubject()
    {
        var sender = new Mock<ServiceBusSender>();
        ServiceBusMessage? sent = null;
        sender.Setup(s => s.SendMessageAsync(It.IsAny<ServiceBusMessage>(), It.IsAny<CancellationToken>()))
              .Callback<ServiceBusMessage, CancellationToken>((m, _) => sent = m)
              .Returns(Task.CompletedTask);

        var pub = new ServiceBusEventPublisher(sender.Object, NullLogger<ServiceBusEventPublisher>.Instance);
        await pub.PublishAsync(new VehicleRegistered(VehicleId.New(), "AB1", DateTimeOffset.UtcNow));

        Assert.NotNull(sent);
        Assert.Equal("VehicleRegistered", sent!.Subject);
        Assert.Equal("application/json", sent.ContentType);
    }

    [Fact]
    public async Task SecretProvider_ReturnsNull_WhenSecretMissing()
    {
        var client = new Mock<SecretClient>();
        client.Setup(c => c.GetSecretAsync("nope", It.IsAny<string>(), It.IsAny<CancellationToken>()))
              .ThrowsAsync(new RequestFailedException(404, "not found"));

        Assert.Null(await new KeyVaultSecretProvider(client.Object).GetSecretAsync("nope"));
    }

    [Fact]
    public async Task SecretProvider_ReturnsValue_WhenPresent()
    {
        var client = new Mock<SecretClient>();
        var response = Response.FromValue(SecretModelFactory.KeyVaultSecret(new SecretProperties("k"), "v"), Mock.Of<Response>());
        client.Setup(c => c.GetSecretAsync("k", It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(response);

        Assert.Equal("v", await new KeyVaultSecretProvider(client.Object).GetSecretAsync("k"));
    }

    [Fact]
    public async Task BlobStore_ExistsAsync_ReflectsContainerAnswer()
    {
        var blob = new Mock<BlobClient>();
        blob.Setup(b => b.ExistsAsync(It.IsAny<CancellationToken>())).ReturnsAsync(Response.FromValue(true, Mock.Of<Response>()));
        var container = new Mock<BlobContainerClient>();
        container.Setup(c => c.GetBlobClient("a.pdf")).Returns(blob.Object);

        Assert.True(await new AzureBlobStore(container.Object).ExistsAsync("a.pdf"));
    }
}
