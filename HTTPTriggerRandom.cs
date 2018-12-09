using System;
using System.IO;
using System.Net.Http;
using System.Net;
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

            // Create new object if needed for all method calls.
            if(RandomJSONRPC == null)
            {
                RandomJSONRPC = new RandomJSONRPC(APIKEY);
            }

            string methodName = req.Query["methodName"].ToString().ToLower();

            // https://stackoverflow.com/questions/94305/what-is-quicker-switch-on-string-or-elseif-on-type 
            // if else faster that switch for small number of cases.
            if(string.Equals(methodName, nameof(RandomJSONRPC.GenerateDecimalFractions).ToLower()))
            {
                GenerateRandomDecimalFractions();
            } else
            {
                methodName = null;
            }

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            dynamic data = JsonConvert.DeserializeObject(requestBody);
            methodName = methodName ?? data?.name;
            
            return methodName != null
                ? (ActionResult)new OkObjectResult(JsonString) // can use JsonResult to force header
                : new BadRequestObjectResult(ExceptionCode);
        }

        private static void GenerateRandomDecimalFractions()
        {
            DoubleResult resultObject = new DoubleResult();
            resultObject.DecimalResults = RandomJSONRPC.GenerateDecimalFractions(10, 4);
            resultObject.GetBitsLeft = RandomJSONRPC.GetBitsLeft();
            resultObject.GetHashedAPIKey = RandomJSONRPC.GetHashedAPIKey;
            resultObject.GetSignature = RandomJSONRPC.GetSignature;
            resultObject.MethodName = nameof(RandomJSONRPC.GenerateDecimalFractions);
            JsonString = JsonConvert.SerializeObject(resultObject, Formatting.Indented);
        }

        private const string GenerateDecimalFractions = nameof(RandomJSONRPC.GenerateDecimalFractions);
        
        private static string JsonString { get; set; }

        private static RandomJSONRPC RandomJSONRPC { get; set; }

        private static int[] IntResults { get; set; }

        private static string[] StrResults { get; set; }

        private static double[] DoubleResults { get; set; }

        private static Guid[] GuidResults { get; set; }

        private static string APIKEY { get; set;}

        private static string ExceptionCode { get; set;}

        private static string KeyVaultEndpoint = "https://atbfunctionkeys.vault.azure.net";
    }
}
