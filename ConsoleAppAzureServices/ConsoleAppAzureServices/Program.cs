using Azure;
using Azure.Identity;
using Azure.ResourceManager.Resources;
using Azure.ResourceManager.Resources.Models;
using Azure.Security.KeyVault.Secrets;
using System;
using System.Threading.Tasks;

namespace ConsoleAppAzureServices
{
    class Program
    {
        static void Main()
        {
            InteractiveBrowserCredential credential = new InteractiveBrowserCredential();

            AuthenticationRecord authRecord = credential.Authenticate();

            GetSecretFromKeyVault(credential).Wait();

            GetResourceGroups(credential).Wait();

            if (credential != null)
            {
                Console.WriteLine($"{Environment.NewLine}Principal used:{authRecord.Authority} TenantId:{authRecord.TenantId} UserPrincipalName:{authRecord.Username}");
            }

            Console.ReadLine();
        }

        private static async Task GetSecretFromKeyVault(InteractiveBrowserCredential credential)
        {
            SecretClient secretClient =
                new SecretClient(new Uri("https://{keyVaultName}.vault.azure.net/"), credential);

            Console.WriteLine("Please enter the key vault name");

            Console.ReadLine();

            try
            {
                var secret = await secretClient
                    .GetSecretAsync("Secretname")
                    .ConfigureAwait(false);

                Console.WriteLine($"Secret: {secret.Value.Value}");

            }
            catch (Exception exp)
            {
                Console.WriteLine($"Something went wrong: {exp.Message}");
            }
        }

        private static async Task GetResourceGroups(InteractiveBrowserCredential credential)
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
            catch (Exception exp)
            {
                Console.WriteLine($"Something went wrong: {exp.Message}");
            }
        }
    }
}