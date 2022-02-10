using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Microsoft.SharePoint.Client;
using Microsoft.Azure.Services.AppAuthentication;
using OfficeDevPnP.Core;

namespace ProvisioningFuncApp
{
    public static class SiteProvisioningFunc
    {
        [FunctionName("SiteProvisioningFunc")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {

            log.LogInformation("C# HTTP trigger function processed a request.");
            try
            {
                log.LogInformation("Getting access token.");
                string siteUrl = "https://brainwareltd.sharepoint.com/sites/TestSite1";
                using (var cc = new AuthenticationManager().GetAppOnlyAuthenticatedContext(siteUrl, "f8d33c24-ae19-4283-880d-2d7c5a8a0f64", "cy~7Q~1YVKGsoS5_5zpxKoyr2olcgLRG~2eoS"))
                {
                    cc.Load(cc.Web, p => p.Title);
                    cc.ExecuteQuery();
                    log.LogInformation("Title:" + cc.Web.Title);
                };
                /*var azureServiceTokenProvider = new AzureServiceTokenProvider();
                string accessToken = await azureServiceTokenProvider
                    .GetAccessTokenAsync("https://brainwareltd.sharepoint.com/");
                log.LogInformation("Access Token: " + accessToken);

                ClientContext context = new ClientContext("https://brainwareltd.sharepoint.com/sites/TestSite1");

                context.ExecutingWebRequest += (sender, args) =>
                {
                    args.WebRequestExecutor.RequestHeaders["Authorization"] = "Bearer " + accessToken;
                };

                // The SharePoint web at the URL.
                Web web = context.Web;

                context.Load(web, w => w.Title, w => w.Description);

                // Execute the query to the server.
                context.ExecuteQuery();
                log.LogInformation("Title:" + web.Title);

                string responseMessage = "Title: " + web.Title;
                */
                return new OkObjectResult("Success");
            }
            catch (Exception ex)
            {
                log.LogError("Error: " + ex.Message);
                return new OkObjectResult("Error");
            }
            
        }
    }
}
