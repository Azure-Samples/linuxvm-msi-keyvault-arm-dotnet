using System;
using System.Threading.Tasks;
using Microsoft.Azure.KeyVault;
using Microsoft.Azure.Management.ResourceManager;
using Microsoft.Azure.Services.AppAuthentication;
using Microsoft.Rest;

namespace ConsoleAppAzureServices
{
    class Program
    {
        static void Main()
        {
            AzureServiceTokenProvider azureServiceTokenProvider = new AzureServiceTokenProvider();

            GetSecretFromKeyVault(azureServiceTokenProvider).Wait();

            GetResourceGroups(azureServiceTokenProvider).Wait();

            if (azureServiceTokenProvider.PrincipalUsed != null)
            {
                Console.WriteLine($"{Environment.NewLine}Principal used: {azureServiceTokenProvider.PrincipalUsed}");
            }

            Console.ReadLine();
        }

        private static async Task GetSecretFromKeyVault(AzureServiceTokenProvider azureServiceTokenProvider)
        {
            KeyVaultClient keyVaultClient =
                new KeyVaultClient(
                    new KeyVaultClient.AuthenticationCallback(azureServiceTokenProvider.KeyVaultTokenCallback));

            Console.WriteLine("Please enter the key vault name");

            var keyvaultName = Console.ReadLine();

            try
            {
                var secret = await keyVaultClient
                    .GetSecretAsync($"https://{keyvaultName}.vault.azure.net/secrets/secret")
                    .ConfigureAwait(false);

                Console.WriteLine($"Secret: {secret.Value}");

            }
            catch (Exception exp)
            {
                Console.WriteLine($"Something went wrong: {exp.Message}");
            }
            
        }

        private static async Task GetResourceGroups(AzureServiceTokenProvider azureServiceTokenProvider)
        {
            Console.WriteLine($"{Environment.NewLine}{Environment.NewLine}Please enter the subscription Id");

            var subscriptionId = Console.ReadLine();

            try
            {
                var serviceCreds = new TokenCredentials(await azureServiceTokenProvider.GetAccessTokenAsync("https://management.azure.com/").ConfigureAwait(false));

                var resourceManagementClient =
                    new ResourceManagementClient(serviceCreds) {SubscriptionId = subscriptionId};

                var resourceGroups = await resourceManagementClient.ResourceGroups.ListAsync();

                foreach (var resourceGroup in resourceGroups)
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