using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Documents.Client;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using MeatGeek.Sessions.Services.Models;

namespace MeatGeek.Sessions
{
    public class UpdateSession
    {
        private static IConfiguration Configuration { set; get; }

        [FunctionName("UpdateSession")]
        public async Task<IActionResult> Run(
            
            [HttpTrigger(AuthorizationLevel.Anonymous, "patch", "put", Route = "sessions/{id}")] HttpRequest req, 
                [CosmosDB(
                databaseName: "Sessions",
                collectionName: "sessions",
                ConnectionStringSetting = "CosmosDBConnection")] DocumentClient client,
                ILogger log,
                string id)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            var updatedSession = JsonConvert.DeserializeObject<Session>(requestBody);

            Uri sessionCollectionUri = UriFactory.CreateDocumentCollectionUri("Sessions", "sessions");

            var document = client.CreateDocumentQuery(sessionCollectionUri, 
                            new FeedOptions() { PartitionKey = new Microsoft.Azure.Documents.PartitionKey("inferno1")})
                .Where(t => t.Id == id)
                .AsEnumerable()
                .FirstOrDefault();

            if (document == null)
            {
                log.LogError($"Session {id} not found.");
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
