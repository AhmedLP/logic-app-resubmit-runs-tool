using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using RestSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace ResubmitLogicAppRunsConsoleApp
{
    class Program
    {
        static void Main(string[] args)
        {
            ServiceCollection collection = new ServiceCollection();
            collection.AddLogging(configure => configure.AddConsole()).AddTransient<Program>();
            ServiceProvider provider = collection.BuildServiceProvider();
            ILogger log = provider.GetService<ILogger<Program>>();

            var config = GetConfiguration();

            //Getting client specification
            var clientId = config.GetSection("app:clientId").Value;
            var clientSecret = config.GetSection("app:clientSecret").Value;
            var resource = config.GetSection("app:resource").Value;
            var endpoint = config.GetSection("app:generateTokenEndpoint").Value;

            DateTime startDate = Convert.ToDateTime(config.GetSection("azureuri:startDate").Value);
            DateTime endDate = Convert.ToDateTime(config.GetSection("azureuri:endDate").Value);

            log.LogInformation($"Console Started, Start Date: {startDate}, End Date: {endDate}");

            bool isFirstSearch = true;
            string endpointNextLink = string.Empty;

            //////Specific For One Test Only//////
            //TWorkflowRun testSpecificRun = GetSpecificWorkflowHistoryRun(config, log, token).GetAwaiter().GetResult();
            //var submit = ResubmitFailedLogicApps(config, testSpecificRun, log, token).GetAwaiter().GetResult();
            //////Specific For One Test Only//////

            //Loop between dates to get every logic app jobs failed between date range
            //For each job will reprocess failed logic app accessing to Azure Management API resubmit endpoint
            while (startDate > endDate)
            {
                //Generate access token to transact with Azure Management API
                TAuhToken token = GenerateNewToken(clientId, clientSecret, resource, endpoint).GetAwaiter().GetResult();

                //Get workflow history runs with a maximum of 250 runs (that´s azure management api top amount rule)
                TRun failedWorkflowRuns = GetWorkflowHistoryRuns(config, startDate.ToString("yyyy-MM-ddTHH:mm:ss.fffffffZ"), isFirstSearch, endpointNextLink, log, token).GetAwaiter().GetResult();
                endpointNextLink = failedWorkflowRuns.nextLink;

                List<Task<bool>> processTasks = new List<Task<bool>>();
                for (int i = 0; i < failedWorkflowRuns.value.Count; i++)
                {
                    TWorkflowRun failedRun = failedWorkflowRuns.value[i];
                    log.LogInformation($"Run: {failedRun.name} to be resubmitted.");
                    var isResubmitted = ResubmitFailedLogicApps(config, failedRun, log, token);
                    processTasks.Add(isResubmitted);
                }

                if (failedWorkflowRuns.value.Count > 0)
                {
                    Task.WhenAll(processTasks).GetAwaiter().GetResult();
                    int totalUpdated = processTasks.Where(t => t.Result == true).Count();

                    startDate = failedWorkflowRuns.value[0].properties.startTime;
                                        
                    log.LogInformation($"Date: {startDate}, Failed state total runs: {failedWorkflowRuns.value.Count}");                    
                    log.LogInformation($"Date: {startDate}, Successfully resubmitted old-failed-stated total runs: {totalUpdated}");
                }
                else
                    log.LogInformation($"Date: {startDate}, No failed state runs found.");

                if (isFirstSearch) isFirstSearch = false;
            }
            log.LogInformation("Console Finished!!!.");
        }

        private static IConfigurationRoot GetConfiguration()
        {
            var builder = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);

            IConfigurationRoot configuration = builder.Build();
            return configuration;
        }

        private static async Task<TWorkflowRun> GetSpecificWorkflowHistoryRun(IConfiguration config, ILogger log, TAuhToken token)
        {
            try
            {
                var endpoint = string.Format(config.GetSection("endpoint:getSpecificWorkFlowRun").Value,
                    config.GetSection("azureuri:subscriptionid").Value,
                    config.GetSection("azureuri:resourcegroupname").Value,
                    config.GetSection("azureuri:logicappname").Value,
                    config.GetSection("azureuri:runName").Value);

                var client = new RestClient(endpoint);

                var request = new RestRequest(Method.GET);
                request.AddHeader("postman-token", "89d89855-367a-9b9a-0d0f-8a611e80b342");
                request.AddHeader("cache-control", "no-cache");
                request.AddHeader("authorization", $"{token.token_type} {token.access_token}");

                IRestResponse response = await client.ExecuteAsync(request);
                log.LogInformation($"{response.StatusCode}");

                TWorkflowRun specificRun = new TWorkflowRun();

                specificRun = JsonConvert.DeserializeObject<TWorkflowRun>(response.Content);
                log.LogInformation($"Run Name:{specificRun.name}, MessageError:{specificRun.properties.error.message}");
                return specificRun;
            }
            catch (Exception ex)
            {
                log.LogError(ex.Message);
                throw new Exception(ex.Message);
            }
        }

        private static async Task<TRun> GetWorkflowHistoryRuns(IConfiguration config, string dateTime, bool isFirstSearch, string endPointNextLink, ILogger log, TAuhToken token)
        {
            try
            {
                var endpoint = isFirstSearch ? 
                    string.Format(config.GetSection("endpoint:getWorkflowRuns").Value,
                    config.GetSection("azureuri:subscriptionid").Value,
                    config.GetSection("azureuri:resourcegroupname").Value,
                    config.GetSection("azureuri:logicappname").Value,
                    config.GetSection("azureuri:filterstatus").Value,
                    dateTime)
                    : endPointNextLink;

                var client = new RestClient(endpoint);
                var request = new RestRequest(Method.GET);
                request.AddHeader("postman-token", "89d89855-367a-9b9a-0d0f-8a611e80b342");
                request.AddHeader("cache-control", "no-cache");
                request.AddHeader("authorization", $"{token.token_type} {token.access_token}");

                TRun failedRuns = new TRun();
                int countTotalFailedRuns = 0;
                int lastFailedRuns = 0;
                bool isMaxRecordsRuns = false;
                do
                {
                    lastFailedRuns = countTotalFailedRuns;
                    IRestResponse response = await client.ExecuteAsync(request);
                    log.LogInformation($"{response.StatusCode}");

                    if (response.StatusCode != System.Net.HttpStatusCode.OK)
                        throw new Exception($"Response Status Code is {response.StatusCode}.");

                    failedRuns = JsonConvert.DeserializeObject<TRun>(response.Content);
                    countTotalFailedRuns = failedRuns.value.Count;
                                        
                    log.LogInformation($"Date: {dateTime}, Failed status total runs found: {countTotalFailedRuns}.");

                    if (countTotalFailedRuns == lastFailedRuns || failedRuns.value.Count == 250)
                        isMaxRecordsRuns = true;
                } while (!string.IsNullOrEmpty(failedRuns.nextLink) && !isMaxRecordsRuns);

                return failedRuns;
            }
            catch (Exception ex)
            {
                log.LogError($"Failed to execption at date: {dateTime}, Error: {ex.Message}");
                throw new Exception(ex.Message);
            }
        }

        private static async Task<bool> ResubmitFailedLogicApps(IConfiguration config, TWorkflowRun failedRun, ILogger log, TAuhToken token)
        {
            try
            {
                var endpoint = string.Format(config.GetSection("endpoint:resubmitWorkflowTrigger").Value,
                        config.GetSection("azureuri:subscriptionid").Value,
                        config.GetSection("azureuri:resourcegroupname").Value,
                        config.GetSection("azureuri:logicappname").Value,
                        config.GetSection("azureuri:trigger").Value,
                        failedRun.name);

                var client = new RestClient(endpoint);
                var request = new RestRequest(Method.POST);
                request.AddHeader("postman-token", "89d89855-367a-9b9a-0d0f-8a611e80b342");
                request.AddHeader("cache-control", "no-cache");
                request.AddHeader("authorization", $"{token.token_type} {token.access_token}");

                //Resubmitted logic app run response
                IRestResponse result = await client.ExecuteAsync(request);
                log.LogInformation($"Post Status for Run: {failedRun.name} : {result.StatusCode}");
            }
            catch (Exception ex)
            {
                log.LogError($"Run: {failedRun.name}, SubmittedError: {ex.Message}");
                return false;
            }
            return true;
        }

        private static async Task<TAuhToken> GenerateNewToken(string clientId, string clientSecret, string resource, string endpoint)
        {
            TAuhToken generatedToken = new TAuhToken();

            var client = new RestClient(endpoint);
            client.Timeout = -1;
            var request = new RestRequest(Method.POST);
            request.AddHeader("postman-token", "89d89855-367a-9b9a-0d0f-8a611e80b342");
            request.AddHeader("cache-control", "no-cache");
            request.AlwaysMultipartFormData = true;
            request.AddParameter("grant_type", "client_credentials");
            request.AddParameter("client_id", clientId);
            request.AddParameter("client_secret", clientSecret);
            request.AddParameter("resource", resource);

            IRestResponse response = await client.ExecuteAsync(request);
            generatedToken = JsonConvert.DeserializeObject<TAuhToken>(response.Content);

            return generatedToken;
        }
    }
}
