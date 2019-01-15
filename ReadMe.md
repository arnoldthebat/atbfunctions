# ArnoldTheBats Magical Functions

Needs Managed service identity setting up so the function can auth to the key vault.

Set this in the Platform features of the function being created.

Then set the 'select principle' to the function name within the access policy section of the KeyVault to allow for GET on secrets only.

Local Debug on Linux needs azure-cli installing. Run az login to get creds needs for authenticating to Azure. <https://docs.microsoft.com/en-us/cli/azure/install-azure-cli-apt?view=azure-cli-latest> for details.

References needed: dotnet add reference ../../../RandomJSONRPC/RandomJSONRPC.csproj - clone from <https://github.com/arnoldthebat/RandomJSONRPC_core.git>.

Needs packages Microsoft.Azure.KeyVault and Microsoft.Azure.Services.AppAuthentication.

## Functions

### HTTPTriggerRandom