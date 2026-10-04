using Azure.Identity;
using Azure.Messaging.ServiceBus;
using Azure.Security.KeyVault.Secrets;
using Azure.Storage.Blobs;
using FleetOps.Application;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace FleetOps.Infrastructure.Azure;

public sealed class AzureOptions
{
    public const string Section = "Azure";
    public string StorageAccountUrl { get; set; } = "";
    public string ContainerName { get; set; } = "fleet-documents";
    public string ServiceBusNamespace { get; set; } = "";
    public string TopicName { get; set; } = "fleet-events";
    public string KeyVaultUrl { get; set; } = "";
}

public static class AzureServiceCollectionExtensions
{
    /// <summary>Registers Azure adapters using DefaultAzureCredential (managed identity in Azure, dev login locally).</summary>
    public static IServiceCollection AddFleetOpsAzure(this IServiceCollection services, Action<AzureOptions> configure)
    {
        services.Configure(configure);
        services.AddSingleton(new DefaultAzureCredential());
        services.AddSingleton(sp =>
        {
            var o = sp.GetRequiredService<IOptions<AzureOptions>>().Value;
            return new BlobContainerClient(new Uri($"{o.StorageAccountUrl.TrimEnd('/')}/{o.ContainerName}"), sp.GetRequiredService<DefaultAzureCredential>());
        });
        services.AddSingleton(sp =>
        {
            var o = sp.GetRequiredService<IOptions<AzureOptions>>().Value;
            return new ServiceBusClient(o.ServiceBusNamespace, sp.GetRequiredService<DefaultAzureCredential>());
        });
        services.AddSingleton(sp => sp.GetRequiredService<ServiceBusClient>().CreateSender(sp.GetRequiredService<IOptions<AzureOptions>>().Value.TopicName));
        services.AddSingleton(sp => new SecretClient(new Uri(sp.GetRequiredService<IOptions<AzureOptions>>().Value.KeyVaultUrl), sp.GetRequiredService<DefaultAzureCredential>()));
        services.AddSingleton<IBlobStore, AzureBlobStore>();
        services.AddSingleton<IEventPublisher, ServiceBusEventPublisher>();
        services.AddSingleton<ISecretProvider, KeyVaultSecretProvider>();
        return services;
    }
}
