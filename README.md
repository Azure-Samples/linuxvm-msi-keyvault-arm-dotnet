## Background
For Service-to-Azure-Service authentication, where the Azure service supports Azure AD based authentication, the approach so far involved creating an Azure AD application and associated credential, and using that credential to get a token. 

While this approach works well, there are two shortcomings:
1. The Azure AD application credentials are typically hard coded in the source code. Developers tend to push the code to source repositories as-is, which leads to credentials in source.
2. The Azure AD application credentials expire, and so need to be renewed, else can lead to application downtime.

With [Managed Service Identity (MSI)](https://docs.microsoft.com/en-us/azure/active-directory/msi-overview), both these problems are solved. This sample shows how a .NET Core application deployed on an Azure Linux VM can authenticate to Azure Key Vault and Azure Resource Manager without the need to explicitly create an Azure AD application or manage its credentials. 

>Here's another sample that shows how to fetch a secret from Azure Key Vault at run-time from an App Service with a Managed Service Identity (MSI) - [https://github.com/Azure-Samples/app-service-msi-keyvault-dotnet/](https://github.com/Azure-Samples/app-service-msi-keyvault-dotnet/)

>Here's another sample that shows how to programatically deploy an ARM template from a .NET Console application running on an Azure VM with a Managed Service Identity (MSI) - [https://github.com/Azure-Samples/windowsvm-msi-arm-dotnet](https://github.com/Azure-Samples/windowsvm-msi-arm-dotnet)

## Prerequisites
To run and deploy this sample, you need the following:
1. Azure subscription to create an Azure VM with MSI. 
2. [.NET Core 2.0](https://www.microsoft.com/net/download/core) since this application targets .NET Core 2.0. 
3. [Azure CLI 2.0](https://docs.microsoft.com/en-us/cli/azure/install-azure-cli?view=azure-cli-latest) to run the application on your local development machine.

## Step 1: Create an Azure VM with a Managed Service Identity (MSI) 
<a href="https://portal.azure.com/#create/Microsoft.Template/uri/https%3A%2F%2Fraw.githubusercontent.com%2FAzure-Samples%2Flinuxvm-msi-keyvault-arm-dotnet%2Fmaster%2Fazuredeploy.json" target="_blank">
    <img src="http://azuredeploy.net/deploybutton.png"/>
</a>

Use the "Deploy to Azure" button to deploy an ARM template to create the following resources:
1. Azure Linux VM with MSI.
2. Key Vault with a secret, and an access policy that grants the Azure VM access to **Get Secrets**.
>Note: When filling out the template you will see a textbox labelled 'Key Vault Secret' in the settings section. Enter a secret value there. Then a Key Vault secret with the name 'secret' and value from what you entered will be created in the Key Vault.

Review the resources created using the Azure portal. You should see the Azure VM and a Key Vault. View the access policies of the Key Vault to see that the Azure VM has access to it. 

## Step 2: Grant yourself data plane access to the Key Vault
Using the Azure Portal, go to the Key Vault's access policies, and grant yourself **Secret Management** access to the Key Vault. This will allow you to run the application on your local development machine. 

1.	Click your Key Vault name in “Search Resources dialog box” in Azure Portal.
2.	Select "Overview", and click on Access policies
3.	Click on "Add New", select "Secret Management" from the dropdown for "Configure from template"
4.	Click on "Select Principal", add your account 
5.	Save the Access Policies

## Step 3: Grant the MSI "Reader" access to the subscription
Using the Azure Portal, navigate to the subscriptions blade, and grant the MSI "Reader" access to the subscription. The code will use this access to list resource groups in the subscription. 

Click the "Access control (IAM)" page of the subscription, and click "+ Add." Then specify the Role as "Reader", Assign access to a "Virtual Machine", and specify the corresponding Subscription and Resource Group where the VM resides. Under the search criteria area, you should see the resource show up. Hit "Save".

## Step 4: Clone the repo 
Clone the repo to your development machine. 

The relevant Nuget packages are:
1. Microsoft.Azure.Services.AppAuthentication - makes it easy to fetch access tokens for service to Azure service authentication scenarios. 
2. Microsoft.Azure.Management.ResourceManager - contains methods for interacting with Azure Resource Manager. 
3. Microsoft.Azure.KeyVault - contains methods for interacting with Key Vault. 

The relevant code is in Program.cs file. The AzureServiceTokenProvider class (which is part of Microsoft.Azure.Services.AppAuthentication) tries the following methods to get an access token, to call Azure services:-
1. Managed Service Identity (MSI) - for scenarios where the code is deployed to Azure, and the Azure resource supports MSI. 
2. [Azure CLI](https://docs.microsoft.com/en-us/cli/azure/install-azure-cli?view=azure-cli-latest) (for local development) - Azure CLI version 2.0.12 and above supports the **get-access-token** option. AzureServiceTokenProvider uses this option to get an access token for local development. 

> For applications targeting .NET Framework, Active Directory Integrated Authentication is also tried for local development. 

```csharp    
private static async Task GetSecretFromKeyVault(AzureServiceTokenProvider azureServiceTokenProvider)
{
    KeyVaultClient keyVaultClient =
        new KeyVaultClient(
            new KeyVaultClient.AuthenticationCallback(azureServiceTokenProvider.KeyVaultTokenCallback));

    Console.WriteLine("Please enter the key vault name");

    var keyVaultName = Console.ReadLine();

    try
    {
        var secret = await keyVaultClient
            .GetSecretAsync($"https://{keyVaultName}.vault.azure.net/secrets/secret")
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
```

## Step 5: Run the application on your local development machine
Open a command prompt and navigate to the folder with the project file. Run **dotnet restore**. Then run **dotnet run**. 

Since the code is running on your local development machine, AzureServiceTokenProvider will use the developer's security context to get a token to authenticate to Azure Services.
This removes the need to create a service principal, and share it with the development team. It also prevents credentials from being checked in to source code. 
AzureServiceTokenProvider will use **Azure CLI** to authenticate to Azure AD to get a token.  

Azure CLI will work if the following conditions are met:
 1. You have [Azure CLI 2.0](https://docs.microsoft.com/en-us/cli/azure/install-azure-cli?view=azure-cli-latest) installed. Version 2.0.12 supports the get-access-token option used by AzureServiceTokenProvider. If you have an earlier version, please upgrade. 
 2. You are logged into Azure CLI. You can login using **az login** command.
 
Since your developer account has access to the Key Vault and the subscription, you should see the secret that you created and the list of resource groups in the subscription. 

>You can also use a service principal to run the application on your local development machine. See the section "Running the application using a service principal" later in the tutorial on how to do this. 

## Step 6: Deploy the application to the Azure VM

In the Azure Portal, browse to the Azure VM you created, and click on "Connect". 

1. SSH into the Azure VM, and run below commands from the command line. 
2. Install [.NET Core 2.0](https://www.microsoft.com/net/learn/get-started/linux/)
3. Clone the repo using 
   **git clone https://github.com/Azure-Samples/linuxvm-msi-keyvault-arm-dotnet/**
4. Navigate to the folder with the project file.
5. Run **dotnet restore**. Then run **dotnet run**. 

It will run the same code that was run on the local development machine, but will use Managed Service Identity, instead of your developer context. 

## Summary
The .NET Core application was successfully able to authenticate to Azure Services using your developer account during development, and using MSI when deployed to Azure, without any code change between local development environment and Azure. 
As a result, you did not have to explicitly handle a service principal credential to authenticate to Azure AD to get a token to call Azure Services. 
You do not have to worry about renewing the service principal credential either, since MSI takes care of that.  

## Troubleshooting

### Common issues during local development:

1. Azure CLI is not installed, or you are not logged in, or you do not have the latest version. 
Run **az account get-access-token** to see if Azure CLI shows a token for you. If it says no such program found, please install Azure CLI 2.0. If you have installed it, you may be prompted to login. 

2. AzureServiceTokenProvider cannot find the path for Azure CLI.
AzureServiceTokenProvider finds Azure CLI at its default install locations. If it cannot find Azure CLI, please set environment variable **AzureCLIPath** to the Azure CLI installation folder. AzureServiceTokenProvider will add the environment variable to the Path environment variable.

### Common issues across environments:

1. Access denied (Forbidden)

The principal used does not have access to the subscription or the Key Vault. 

2. AggregateException/CloudException: Long running operation failed with status 'Failed'.

There was an unspecified error during the deployment. You can view the specific error message by using Azure Portal. First, navigate to the Activity Log by searching for "activity log" in the “Search Resources dialog box”. Then, in the Activity Log you can search for the failed deployment by filtering to the subscription and resource group you specified in step #4. After this step, you should see the failed deployment that you can expand and view the specific deployment error below in the summary field. A common deployment issue is that the storage account name specified in step #4 is either already taken or is not a valid storage account name.

## Running the application using a service principal in local development environment

>Note: It is recommended to use your developer context for local development, since you do not need to create or share a service principal for that. If that does not work for you, you can use a service principal, but do not check in the certificate or secret in source repos, and share them securely.

To run the application using a service principal in the local development environment, follow these steps

Service principal using a certificate:
1. Create a service principal certificate. Follow steps [here](https://docs.microsoft.com/en-us/azure/azure-resource-manager/resource-group-authenticate-service-principal) to create a service principal. 
2. Set an environment variable named **AzureServicesAuthConnectionString** to **RunAs=App;AppId=AppId;TenantId=TenantId;CertificateThumbprint=Thumbprint;CertificateStoreLocation=CurrentUser**. 
You need to replace AppId, TenantId, and Thumbprint with actual values from step #1.
3. Run the application in your local development environment. No code change is required. AzureServiceTokenProvider will use this environment variable and use the certificate to authenticate to Azure AD. 

Service principal using a password:
1. Create a service principal with a password. Follow steps [here](https://docs.microsoft.com/en-us/azure/azure-resource-manager/resource-group-authenticate-service-principal) to create a service principal and grant it permissions to the Key Vault. 
2. Set an environment variable named **AzureServicesAuthConnectionString** to **RunAs=App;AppId=AppId;TenantId=TenantId;AppKey=Secret**. You need to replace AppId, TenantId, and Secret with actual values from step #1. 
3. Run the application in your local development environment. No code change is required. AzureServiceTokenProvider will use this environment variable and use the service principal to authenticate to Azure AD. 