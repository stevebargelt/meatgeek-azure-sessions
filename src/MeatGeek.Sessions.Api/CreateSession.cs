using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using MeatGeek.Sessions.Services.Models;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Enums;

namespace MeatGeek.Sessions
{

    public class CreateSession
    {
        private static IConfiguration Configuration { set; get; }

        [FunctionName("CreateSession")]
        [OpenApiOperation(operationId: "CreateSession", tags: new[] { "session" }, Summary = "Start a new session.", Description = "This add a new session (sessions are 'cooks' or BBQ sessions).", Visibility = OpenApiVisibilityType.Important)]
        [OpenApiRequestBody(contentType: "application/json", bodyType: typeof(Session), Required = true, Description = "Session object that needs to be added to the store")]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(Session), Summary = "New session details added", Description = "New session details added")]
        [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.MethodNotAllowed, Summary = "Invalid input", Description = "Invalid input")]        
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "sessions")] HttpRequest req, 
                [CosmosDB(
                databaseName: "Sessions",
                collectionName: "sessions",
                ConnectionStringSetting = "CosmosDBConnection")] IAsyncCollector<Session> sessions,
            ILogger log)
        {
            log.LogInformation("CreateSession API Triggered");
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
