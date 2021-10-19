using System;
using System.Threading.Tasks;
using Azure;
using Azure.Identity;
using Azure.ResourceManager.Resources;
using Azure.ResourceManager.Resources.Models;
using Azure.Security.KeyVault.Secrets;

namespace ConsoleAppAzureServices
{
    class Program
    {
        static async Task Main()
        {
            DefaultAzureCredential credential = new DefaultAzureCredential();

            await GetSecretFromKeyVault(credential);

            await GetResourceGroups(credential);

            Console.WriteLine($"{Environment.NewLine}Principal used:{credential}");
            Console.ReadLine();
        }

        private static async Task GetSecretFromKeyVault(DefaultAzureCredential credential)
        {
            Console.WriteLine("Please enter the Key Vault name");
            string keyVaultName = Console.ReadLine();

            SecretClient secretClient = new SecretClient(new Uri($"https://{keyVaultName}.vault.azure.net/"), credential);

            try
            {
                var secret = await secretClient
                    .GetSecretAsync("Secretname")
                    .ConfigureAwait(false);

                Console.WriteLine($"Secret: {secret.Value.Value}");

            }
            catch (RequestFailedException exp)
            {
                Console.WriteLine($"Something went wrong: {exp.Message}");
            }
        }

        private static async Task GetResourceGroups(DefaultAzureCredential credential)
        {
            Console.WriteLine($"{Environment.NewLine}{Environment.NewLine}Please enter the subscription Id");

            var subscriptionId = Console.ReadLine();

            try
            {
                var resourceClient = new ResourcesManagementClient(subscriptionId, credential);

                var resourceGroupsClient = resourceClient.ResourceGroups;

                AsyncPageable<ResourceGroup> response = resourceGroupsClient.ListAsync();

                await foreach (var resourceGroup in response)
                {
                    Console.WriteLine($"Resource group {resourceGroup.Name}");
                }

            }
            catch (RequestFailedException exp)
            {
                Console.WriteLine($"Something went wrong: {exp.Message}");
            }
        }
    }
}