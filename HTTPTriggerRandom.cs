using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Text;
using Microsoft.Azure.Services.AppAuthentication;
using Microsoft.Azure.KeyVault;
using org.random.JSONRPC;

namespace uk.co.arnoldthebat.functions
{
    public static class HTTPTriggerRandom
    {
        [FunctionName("HTTPTriggerRandom")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");

            // Default response unless we have an exception
            ExceptionCode = "Please pass a name on the query string or in the request body.";

            if(string.IsNullOrEmpty(APIKEY))
            {
                var azureServiceTokenProvider = new AzureServiceTokenProvider();

                try
                {
                    var keyVaultClient = new KeyVaultClient(
                    new KeyVaultClient.AuthenticationCallback(azureServiceTokenProvider.KeyVaultTokenCallback));
                    APIKEY = (await keyVaultClient.GetSecretAsync(KeyVaultEndpoint, "Random-APIKEY")).Value;
                }
                catch (Exception exp)
                {
                    APIKEY = null;
                    ExceptionCode = exp.Message;
                    log.LogError(exp.Message);
                }
            }

            GenerateRandomNess();
            sb = new StringBuilder().Append("\r\n");
            foreach(int i in IntResults)
            {
                sb.Append(i.ToString()).Append("\r\n");
            }

            string name = req.Query["name"];

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            dynamic data = JsonConvert.DeserializeObject(requestBody);
            name = name ?? data?.name;

            return name != null
                ? (ActionResult)new OkObjectResult($"Random Numbers are; {sb.ToString()}")
                : new BadRequestObjectResult(ExceptionCode);
        }

        private static void GenerateRandomNess()
        {
            org.random.JSONRPC.RandomJSONRPC TestRandomJSONRPC = new RandomJSONRPC(APIKEY);

            var n =  new Random().Next(1, 100);
            var min = new Random().Next(1, 1000000000);
            var max = new Random().Next(min, 1000000000);
            
            IntResults = TestRandomJSONRPC.GenerateIntegers(n, min, max); 
        }

        private static int[] IntResults { get; set; }

        private static string APIKEY {get; set;}

        private static string ExceptionCode {get; set;}

        private static string KeyVaultEndpoint = "https://atbfunctionkeys.vault.azure.net";

        private static StringBuilder sb {get; set;}
    }
}
