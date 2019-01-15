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
using Newtonsoft.Json.Linq;
using System.Text;
using System.Collections.Generic;
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
            // Default state - reset exceptions for each trigger invocation;
            ExceptionCodes = new List<string>();

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
                    // ExceptionCodes.Add(exp.Message);
                    log.LogError(exp.Message);
                    return new BadRequestObjectResult("API KEY Failed to set: " + exp.Message);
                }
            }

            // Create new object if needed for all method calls.
            if(RandomJSONRPC == null)
            {
                RandomJSONRPC = new RandomJSONRPC(APIKEY);
            }

            // Extract Query Values
            string methodName = req.Query[nameof(methodName)].ToString().ToLower();
            int numberOfResults = ParseInt(req.Query[nameof(numberOfResults)].ToString(), nameof(numberOfResults));
            int decimalPlaces = ParseInt(req.Query[nameof(decimalPlaces)].ToString(), nameof(decimalPlaces));

            // Shouldnt be needed since we never send a body. Its (currently) always in the query. Test this with Postmaster
            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            dynamic data = JsonConvert.DeserializeObject(requestBody);
            methodName = methodName ?? data?.methodName;

            // https://stackoverflow.com/questions/94305/what-is-quicker-switch-on-string-or-elseif-on-type 
            // if else faster that switch for small number of cases.
            try 
            {
                if(string.Equals(methodName, nameof(RandomJSONRPC.GenerateDecimalFractions).ToLower()))
                {
                    GenerateRandomDecimalFractions(numberOfResults, decimalPlaces);
                } else
                {
                    methodName = null;
                }
            }
            catch (Exception exp)
            {
                ExceptionCodes.Add(exp.Message); // TODO: Add this to the BadRequestObjectResult
                log.LogError(exp.Message);
                methodName = null;
            }
            
            return methodName != null
                ? (ActionResult)new OkObjectResult(JsonString) // can use JsonResult to force header
                : new BadRequestObjectResult("Please pass a methodName on the query string or in the request body.");
        }

        private static void GenerateRandomDecimalFractions(int numberOfResults, int decimalPlaces)
        {
            JObject jObject = new JObject();
            try 
            {
                RandomJSONRPC.GenerateDecimalFractions(numberOfResults, decimalPlaces);
                jObject.Add(new JProperty(nameof(RandomJSONRPC.GenerateDecimalFractions), RandomJSONRPC.JSONResponse));
                jObject.Add(new JProperty(nameof(ExceptionCodes), 
                    new JArray(ExceptionCodes)));
                
            }
            catch (Exception exp)
            {
                jObject.Add(new JProperty(nameof(RandomJSONRPC.GenerateDecimalFractions), "Error"));
                jObject.Add(nameof(ExceptionCodes), exp.Message);
            }
            finally
            {
                JsonString = JsonConvert.SerializeObject(jObject,  Formatting.Indented);
            }
        }
    
        private static string JsonString { get; set; }

        private static RandomJSONRPC RandomJSONRPC { get; set; }

        private static int[] IntResults { get; set; }

        private static string[] StrResults { get; set; }

        private static double[] DoubleResults { get; set; }

        private static Guid[] GuidResults { get; set; }

        private static string APIKEY { get; set; }

        private static List<string> ExceptionCodes { get; set; }

        private static string KeyVaultEndpoint = "https://atbfunctionkeys.vault.azure.net";

        private static int sensibleDefaultInt = 2;

        private static int ParseInt(string stringValue, string methodName)
        {
            try 
            {
                return Convert.ToInt32(stringValue);
            }
            catch
            {
                ExceptionCodes.Add("Failed to convert parameter " + methodName + " to Int, defaulting to " 
                    + sensibleDefaultInt + ".");
                return sensibleDefaultInt;
            }
        }
    }
}
