using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Documents.Client;
using Microsoft.Azure.Documents.Linq;
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
    public class UpdateSession
    {
        private static IConfiguration Configuration { set; get; }
        private static string HoneycombKey;
        private static string HoneycombDataset;        
        private static LibHoney _libHoney;

        public UpdateSession(CosmosClient cosmosClient)
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

        [FunctionName("UpdateSession")]
        public async Task<IActionResult> Run(
            
            [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "{id}")] HttpRequest req, 
                [CosmosDB(
                databaseName: "Inferno",
                collectionName: "sessions",
                ConnectionStringSetting = "CosmosDBConnection")] DocumentClient client,
                ILogger log,
                string id)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            var updatedSession = JsonConvert.DeserializeObject<Session>(requestBody);

            Uri sessionCollectionUri = UriFactory.CreateDocumentCollectionUri("Inferno", "sessions");

            var document = client.CreateDocumentQuery(sessionCollectionUri, 
                            new FeedOptions() { PartitionKey = new Microsoft.Azure.Documents.PartitionKey("inferno1")})
                .Where(t => t.Id == id)
                .AsEnumerable()
                .FirstOrDefault();

            if (document == null)
            {
                log.LogError($"Session {id} not found. It may not exist!");
                return new NotFoundResult();
            }

            if (!string.IsNullOrEmpty(updatedSession.Description))
            {
                document.SetPropertyValue("Description", updatedSession.Description);
            }
            if (!string.IsNullOrEmpty(updatedSession.Title))
            {
                document.SetPropertyValue("Title", updatedSession.Title);
            }
            if (updatedSession.EndTime.HasValue)
            {
                document.SetPropertyValue("EndTime", updatedSession.EndTime);
            }
            if (updatedSession.StartTime.HasValue)
            {
                document.SetPropertyValue("StartTime", updatedSession.StartTime);
            }
            await client.ReplaceDocumentAsync(document);

            Session updatedSessionDocument = (dynamic)document;

            return new OkObjectResult(updatedSessionDocument);
        }

    }
}
