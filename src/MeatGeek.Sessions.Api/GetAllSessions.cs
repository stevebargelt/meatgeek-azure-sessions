using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Documents.Linq;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Azure.Documents.Client;
using MeatGeek.Sessions.Services.Models;

#nullable enable
namespace MeatGeek.Sessions
{
   public class GetAllSessions
    {

        [FunctionName("GetAllSessions")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "sessions")] HttpRequest req, 
                [CosmosDB(
                databaseName: "Sessions",
                collectionName: "sessions",
                ConnectionStringSetting = "CosmosDBConnection")] DocumentClient client,
                ILogger log, string? traceid, string? parentspanid)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");

            var sessions = new List<Session>();
            try {
                Uri sessionCollectionUri = UriFactory.CreateDocumentCollectionUri(databaseId: "Sessions", collectionId: "sessions");
                IDocumentQuery<Session> query = client.CreateDocumentQuery<Session>(sessionCollectionUri).AsDocumentQuery();
                while (query.HasMoreResults)
                {
                    foreach (Session session in await query.ExecuteNextAsync())
                    {
                        sessions.Add(session);
                    }
                }  
            }
            catch {
                return new NotFoundResult();
            }

            if (sessions == null || sessions.Count == 0)
            {
                return new NotFoundResult();
            }
            return new OkObjectResult(sessions);
        }
    }       

}
