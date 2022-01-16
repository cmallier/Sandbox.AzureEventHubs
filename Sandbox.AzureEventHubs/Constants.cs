namespace Sandbox.AzureEventHubs;

internal static class Constants
{
    public const string AzureEventHubConnectionString = "Endpoint=sb://{{HUB}}.servicebus.windows.net/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey={{KEY}}";
    public const string AzureEventHubName = "{{NAME}}";

    public const string AzureBlobStorageConnectionString = "DefaultEndpointsProtocol=https;EndpointSuffix=core.windows.net;AccountName={{NAME}};AccountKey={{KEY}}";
    public const string AzureBlobContainerName = "{{NAME}}";
}
