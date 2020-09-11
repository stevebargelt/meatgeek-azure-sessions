using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Inferno.Common.Models;
using Honeycomb;

namespace MeatGeek.Sessions
{
    public class GetSessionById
    {
        private static IConfiguration Configuration { set; get; }
        private static string HoneycombKey;
        private static string HoneycombDataset;        
        private static LibHoney _libHoney;

        public GetSessionById(CosmosClient cosmosClient)
        {
            Activity.DefaultIdFormat = ActivityIdFormat.W3C;
            var builder = new ConfigurationBuilder();
            var connString = Environment.GetEnvironmentVariable("APP_CONFIG_CONN_STRING", EnvironmentVariableTarget.Process);
            builder.AddAzureAppConfiguration(connString);
            Configuration = builder.Build();
            HoneycombKey = Configuration["HoneycombKey"];
            HoneycombDataset = Configuration["HoneycombDataset"];
            _libHoney = new LibHoney(HoneycombKey, HoneycombDataset);
        }

        [FunctionName("GetSessionById")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "{id}")] HttpRequest req, 
                [CosmosDB(
                databaseName: "Inferno",
                collectionName: "sessions",
                ConnectionStringSetting = "CosmosDBConnection",
                Id = "{id}", PartitionKey = "inferno1")] Object session,
            ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");
            // log.LogInformation("Id" + d);

            if (session == null)
            {
                log.LogInformation($"Session not found");
                return new NotFoundResult();
            }
            return new OkObjectResult(session);

        }

    }
}