using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Azure.Services.AppAuthentication;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Collections.Generic;

namespace ProvisioningFuncApp
{
    public static class PowerBIUpdateFunc
    {
        private static HttpClient client = null;
        private static string authToken = null;

        [FunctionName("PowerBIUpdateFunc")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");

            
            try
            {
                log.LogInformation("C# HTTP trigger function processed a request.");
                string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
                log.LogInformation("Request Body: " + requestBody);
                var data = JsonSerializer.Deserialize<IDictionary<string, object>>(requestBody);
                log.LogInformation("Data: " + data);
                log.LogInformation("Work space Id: " + data["id"].ToString());
                log.LogInformation("Work space Name: " + data["name"].ToString());
                log.LogInformation("Work space Description: " + data["description"].ToString());

                string workspaceid = data["id"].ToString();
                string workSpaceName = data["name"].ToString();
                string description = data["description"].ToString();

                log.LogInformation("Executing Job");

                //authToken = GetPowerBiApiClient(log).Result;
                authToken = GetAccessTokenUsingManagedIdenty(log).Result;
                log.LogInformation("AuthToken: " + authToken);
                InitHttpClient(authToken, log);
                UpdateWorkspace(workspaceid,workSpaceName,description,log);
                string responseMessage = "Success";

                return new OkObjectResult(responseMessage);
            }
            catch (Exception ex)
            {
                return new ObjectResult(ex);
            }
        }
        public static string UpdateWorkspace(string strWorkSpaceId, string workSpaceName, string description, ILogger log)
        {
            HttpResponseMessage response = null;
            HttpContent responseContent = null;
            string strContent = "";

            string serviceURL = $"https://api.powerbi.com/v1.0/myorg/admin/groups/{strWorkSpaceId}";
            try
            {
                log.LogInformation("- Posting data for Create WorkSpace: " + serviceURL);
                var data = new
                {
                    name = workSpaceName,
                    description = description
                };
                var json = JsonSerializer.Serialize(data);
                var buffer = System.Text.Encoding.UTF8.GetBytes(json);
                var byteContent = new ByteArrayContent(buffer);
                byteContent.Headers.ContentType = new MediaTypeHeaderValue("application/json");

                response = client.PatchAsync(serviceURL, byteContent).Result;

                log.LogInformation("   - Response code received: " + response.StatusCode);
                responseContent = response.Content;
                strContent = responseContent.ReadAsStringAsync().Result;
            }
            catch (Exception ex)
            {
                log.LogInformation("   - API Access Error: " + ex.ToString());
            }
            return strWorkSpaceId;
        }
        private static async Task<string> GetAccessTokenUsingManagedIdenty(ILogger log)
        {
            try
            {
                var azureServiceTokenProvider = new AzureServiceTokenProvider("RunAs=App;AppId=1abe78e7-3544-4386-bb29-b3d0bf9b3bc6");

                return await azureServiceTokenProvider.GetAccessTokenAsync("https://analysis.windows.net/powerbi/api").ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                log.LogInformation("ERROR: " + ex.Message);
            }
            return null;
        }

        public static void InitHttpClient(string authToken, ILogger log)
        {
            try
            {
                log.LogInformation("- Initializing client with generated auth token...");
                client = new HttpClient();
                client.DefaultRequestHeaders.Add("Authorization", "Bearer " + authToken);
            }
            catch (Exception ex)
            {
                log.LogInformation(ex.Message);
            }
        }
    }
}
