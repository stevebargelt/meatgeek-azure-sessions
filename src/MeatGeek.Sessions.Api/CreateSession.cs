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
using MeatGeek.Sessions.Services.Models;
using Honeycomb;

namespace MeatGeek.Sessions
{
    public class CreateSession
    {
        private static IConfiguration Configuration { set; get; }

        [FunctionName("CreateSession")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "sessions")] HttpRequest req, 
                [CosmosDB(
                databaseName: "Sessions",
                collectionName: "sessions",
                ConnectionStringSetting = "CosmosDBConnection")] IAsyncCollector<Session> sessions,
            ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");
            try 
            {
                string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
                log.LogInformation(requestBody);
                var input = JsonConvert.DeserializeObject<Session>(requestBody);

                var session = new Session {
                    SmokerId = input.SmokerId,
                    Title = input.Title,
                    Description = input.Description,
                    StartTime = input.StartTime,
                    EndTime = input.EndTime,
                    TimeStamp = DateTime.UtcNow
                };
                await sessions.AddAsync(session);
                return new OkObjectResult(session);
            }
            catch (Exception ex)
            {
                log.LogError($"Couldn't insert item. Exception thrown: {ex.Message}");
                return new StatusCodeResult(StatusCodes.Status500InternalServerError);
            }
        }

    }
}
