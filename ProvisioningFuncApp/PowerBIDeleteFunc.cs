using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Net.Http;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Microsoft.Azure.Services.AppAuthentication;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Collections.Generic;

namespace ProvisioningFuncApp
{
    public static class PowerBIDeleteFunc
    {

        private static HttpClient client = null;
        private static string authToken = null;

        [FunctionName("PowerBIDeleteFunc")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = null)] HttpRequest req,
            ILogger log)
        {
            try
            {
                log.LogInformation("C# HTTP trigger function processed a request.");


                log.LogInformation("Executing Job");

                //authToken = GetPowerBiApiClient(log).Result;
                authToken = GetAccessTokenUsingManagedIdenty(log).Result;
                log.LogInformation("AuthToken: " + authToken);
                InitHttpClient(authToken, log);
                string workspaceid = req.Query["id"];
                DeleteWorkspace(log,workspaceid);
                string responseMessage = "Success";

                return new OkObjectResult(responseMessage);
            }
            catch (Exception ex)
            {
                return new ObjectResult(ex);
            }

        }
        public static void DeleteWorkspace(ILogger log, string workspaceid)
        {
            HttpResponseMessage response = null;
            HttpContent responseContent = null;
            string strContent = "";

            string serviceURL = $"https://api.powerbi.com/v1.0/myorg/groups/{workspaceid}";
            try
            {
                log.LogInformation("- Posting data for Delete WorkSpace: " + serviceURL);

                response = client.DeleteAsync(serviceURL).Result;

                log.LogInformation("   - Response code received: " + response.StatusCode);
                responseContent = response.Content;
                strContent = responseContent.ReadAsStringAsync().Result;
                log.LogInformation("Content " + strContent);
            }
            catch (Exception ex)
            {
                log.LogInformation("   - API Access Error: " + ex.ToString());
            }
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
