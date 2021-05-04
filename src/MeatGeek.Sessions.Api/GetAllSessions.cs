using System;
using System.Net;
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
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Enums;
using Microsoft.OpenApi.Models;

#nullable enable
namespace MeatGeek.Sessions
{
   public class GetAllSessions
    {
        [FunctionName("GetAllSessions")]
        [OpenApiOperation(operationId: "GetAllSessions", tags: new[] { "name" }, Summary = "Gets the name", Description = "This gets the name.", Visibility = OpenApiVisibilityType.Important)]
        [OpenApiParameter(name: "name", In = ParameterLocation.Query, Required = true, Type = typeof(string), Summary = "The name", Description = "The name", Visibility = OpenApiVisibilityType.Important)]
        [OpenApiSecurity("function_key", SecuritySchemeType.ApiKey, Name = "code", In = OpenApiSecurityLocationType.Query)]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "text/plain", bodyType: typeof(string), Summary = "The response", Description = "This returns the response")]        
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "sessions")] HttpRequest req, 
                [CosmosDB(
                databaseName: "Sessions",
                collectionName: "sessions",
                ConnectionStringSetting = "CosmosDBConnection")] DocumentClient client,
                ILogger log)
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
