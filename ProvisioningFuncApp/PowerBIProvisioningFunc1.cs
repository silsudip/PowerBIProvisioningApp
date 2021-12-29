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
    public static class PowerBIProvisioningFunc1
    {

        private static HttpClient client = null;
        private static string authToken = null;

        [FunctionName("PowerBIProvisioningFunc1")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            try
            {
                log.LogInformation("C# HTTP trigger function processed a request.");

                //string workSpaceName = req.Query["workspacename"];
                string workSpaceName = string.Empty;
                //string userEmail = req.Query["useremail"];
                string userEmail = string.Empty;

                string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
                log.LogInformation("Request Body: " + requestBody);
                var data = JsonSerializer.Deserialize<IDictionary<string, object>>(requestBody);
                log.LogInformation("Data: " + data);
                log.LogInformation("Work space Name: " + data["workspacename"].ToString());
                log.LogInformation("User Email: " + data["useremail"].ToString());

                workSpaceName = data["workspacename"].ToString();
                userEmail = data["useremail"].ToString();
                if (!string.IsNullOrEmpty(workSpaceName) && !string.IsNullOrEmpty(userEmail))
                {
                    log.LogInformation("Executing Job");
                    
                    //authToken = GetPowerBiApiClient(log).Result;
                    authToken = GetAccessTokenUsingManagedIdenty(log).Result;
                    log.LogInformation("AuthToken: " + authToken);
                    InitHttpClient(authToken,log);
                    //GetWorkspaces(log);
                    string strWorkSpaceId = CreateWorkspace(log, workSpaceName);
                    AddUserToWorkspace(log, strWorkSpaceId, userEmail);
                }
                string responseMessage = "Success";

                return new OkObjectResult(responseMessage);
            }
            catch (Exception ex)
            {
                return new ObjectResult(ex);
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
        public static async void GetWorkspaces(ILogger log)
        {
            HttpResponseMessage response = null;
            HttpContent responseContent = null;
            string strContent = "";

            string serviceURL = "https://api.powerbi.com/v1.0/myorg/groups";

            try
            {
                log.LogInformation("");
                log.LogInformation("- Retrieving data from: " + serviceURL);

                response = await client.GetAsync(serviceURL);

                log.LogInformation("   - Response code received: " + response.StatusCode);
                responseContent = response.Content;
                strContent = responseContent.ReadAsStringAsync().Result;

                log.LogInformation("   - Response content received: " + strContent);
            }
            catch (Exception ex)
            {
                log.LogInformation("   - API Access Error: " + ex.ToString());
            }
        }

        public static string CreateWorkspace(ILogger log, string strWorkSpaceName)
        {
            HttpResponseMessage response = null;
            HttpContent responseContent = null;
            string strContent = "";

            string serviceURL = "https://api.powerbi.com/v1.0/myorg/groups";
            string strWorkSpaceId = "";
            try
            {
                log.LogInformation("- Posting data for Create WorkSpace: " + serviceURL);
                var data = new
                {
                    name = strWorkSpaceName
                };
                var json = JsonSerializer.Serialize(data);
                var buffer = System.Text.Encoding.UTF8.GetBytes(json);
                var byteContent = new ByteArrayContent(buffer);
                byteContent.Headers.ContentType = new MediaTypeHeaderValue("application/json");

                response = client.PostAsync(serviceURL, byteContent).Result;

                log.LogInformation("   - Response code received: " + response.StatusCode);
                responseContent = response.Content;
                strContent = responseContent.ReadAsStringAsync().Result;

                if (strContent.Length > 0)
                {
                    log.LogInformation("   - De-serializing Workspace Data...");
                    var resultObj = JsonSerializer.Deserialize<IDictionary<string, object>>(strContent);
                    strWorkSpaceId = resultObj["id"].ToString();
                }
            }
            catch (Exception ex)
            {
                log.LogInformation("   - API Access Error: " + ex.ToString());
            }
            return strWorkSpaceId;
        }

        public static void AddUserToWorkspace(ILogger log, string strWorkSpaceId, string userEmail)
        {
            HttpResponseMessage response = null;
            HttpContent responseContent = null;
            string strContent = "";

            string serviceURL = $"https://api.powerbi.com/v1.0/myorg/groups/{strWorkSpaceId}/users";

            try
            {
                log.LogInformation("");
                log.LogInformation("- Posting data for adding user in the workspace: " + serviceURL);
                var data = new
                {
                    groupUserAccessRight = "Admin",
                    emailAddress = userEmail
                };
                var json = JsonSerializer.Serialize(data);
                var buffer = System.Text.Encoding.UTF8.GetBytes(json);
                var byteContent = new ByteArrayContent(buffer);
                byteContent.Headers.ContentType = new MediaTypeHeaderValue("application/json");

                response = client.PostAsync(serviceURL, byteContent).Result;

                log.LogInformation("   - Response code received: " + response.StatusCode);
                responseContent = response.Content;
                strContent = responseContent.ReadAsStringAsync().Result;

                if (strContent.Length > 0)
                {
                }
            }
            catch (Exception ex)
            {
                log.LogInformation("   - API Access Error: " + ex.ToString());
            }
        }
    }
}
