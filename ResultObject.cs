using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;

namespace uk.co.arnoldthebat.functions
{
    /// <summary>
    /// ResultObject - needs to remain public to allow JsonConvert.SerializeObject to use reflection on the object
    /// </summary>
    public class ResultObject
    {
        public ResultObject()
        {
            jObject = new JObject();
        }

        private JObject jObject {get; set;}

        private static string error = "Error";


        public void LoadValidObjectResponse(string methodName, JObject jSONResponse, 
            string exceptionCodeName, List<string> exceptionCodes)
        {
            jObject.Add(new JProperty(methodName, jSONResponse));
                jObject.Add(new JProperty(exceptionCodeName, 
                    new JArray(exceptionCodes)));
        }

        public void LoadInvalidObjectResponse(string methodName, string exceptionCodeName, 
            string exceptionMessage)
        {
            jObject.Add(new JProperty(methodName, error));
                jObject.Add(exceptionCodeName, exceptionMessage);
        }

        public string GetJSONResponse()
        {
            return JsonConvert.SerializeObject(jObject,  Formatting.Indented);
        }
    }
}