using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using MeatGeek.Sessions.Services.Models;

namespace MeatGeek.Sessions
{
    public class GetSessionById
    {

        [FunctionName("GetSessionById")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "sessions/{id}")] HttpRequest req, 
                [CosmosDB(
                databaseName: "Sessions",
                collectionName: "sessions",
                ConnectionStringSetting = "CosmosDBConnection",
                Id = "{id}", PartitionKey = "inferno1")] Object session,
            ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");

            await Task.Yield();
            if (session == null)
            {
                log.LogInformation($"Session not found");
                return new NotFoundResult();
            }
            return new OkObjectResult(session);

        }

    }
}