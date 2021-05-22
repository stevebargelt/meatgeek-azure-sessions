using System;
using System.IO;
using System.Net;
using System.Web.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Enums;

using MeatGeek.Sessions.Services.Models;
using MeatGeek.Sessions.Services;
using MeatGeek.Sessions.Services.Models.Request;
using MeatGeek.Sessions.Services.Models.Results;
using MeatGeek.Sessions.Services.Models.Response;
using MeatGeek.Sessions.Services.Repositories;
using MeatGeek.Shared;

namespace MeatGeek.Sessions
{
    public class UpdateSession
    {
        private readonly ILogger<CreateSession> _log;
        private readonly ISessionsService _sessionsService; 

        public UpdateSession(ILogger<CreateSession> log, ISessionsService sessionsService)
        {
            _log = log;
            _sessionsService = sessionsService;
        }

        [FunctionName("UpdateSession")]
        [OpenApiOperation(operationId: "UpdateSession", tags: new[] { "session" }, Summary = "Updated an existing session.", Description = "Updates a session (sessions are 'cooks' or BBQ sessions).", Visibility = OpenApiVisibilityType.Important)]
        [OpenApiRequestBody(contentType: "application/json", bodyType: typeof(SessionDetails), Required = true, Description = "Session object with updated values")]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(SessionDetails), Summary = "Session dtails updated", Description = "Session details updated")]
        [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.MethodNotAllowed, Summary = "Invalid input", Description = "Invalid input")]         
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "patch", "put", Route = "sessions/{id}")] HttpRequest req, 
                ILogger log,
                string id)
        {
            log.LogInformation("UpdateSession Called");

            // get the request body
            var requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            UpdateSessionRequest data;
            try
            {
                data = JsonConvert.DeserializeObject<UpdateSessionRequest>(requestBody);
            }
            catch (JsonReaderException)
            {
                return new BadRequestObjectResult(new { error = "Body should be provided in JSON format." });
            }

            // validate request
            if (data == null)
            {
                return new BadRequestObjectResult(new { error = "Missing required properties. Nothing to update." });
            }
            if (data.Id != null && id != null && data.Id != id)
            {
                return new BadRequestObjectResult(new { error = "Property 'id' does not match the identifier specified in the URL path." });
            }
            if (string.IsNullOrEmpty(data.Id))
            {
                data.Id = id;
            }
            // if (string.IsNullOrEmpty(data.Name))
            // {
            //     return new BadRequestObjectResult(new { error = "Missing required property 'name'." });
            // }

            var smokerId = "meatgeek2";
            //TODO: Get smokerID 
            // get the user ID
            // if (! await UserAuthenticationService.GetUserIdAsync(req, out var userId, out var responseResult))
            // {
            //     return responseResult;
            // }

            // update category name
            // TODO: Add description, startdate, enddate, etc. 
            try
            {
                var result = await _sessionsService.UpdateSessionAsync(data.Id, smokerId, data.Title, data.EndTime.Value);
                if (result == UpdateSessionResult.NotFound)
                {
                    return new NotFoundResult();
                }

                return new NoContentResult();
            }
            catch (Exception ex)
            {
                log.LogError("Unhandled exception", ex);
                return new ExceptionResult(ex, false);
            }


            // string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            // var updatedSession = JsonConvert.DeserializeObject<Session>(requestBody);

            // log.LogInformation("updatedSession.SmokerId = " + updatedSession.SmokerId);
            // log.LogInformation("updatedSession.PartitionKey = " + updatedSession.PartitionKey);
            // Uri sessionCollectionUri = UriFactory.CreateDocumentCollectionUri("Sessions", "sessions");

            // var document = client.CreateDocumentQuery(sessionCollectionUri, 
            //                 new FeedOptions() { PartitionKey = new Microsoft.Azure.Documents.PartitionKey(updatedSession.SmokerId)})
            //     .Where(t => t.Id == id)
            //     .AsEnumerable()
            //     .FirstOrDefault();

            // if (document == null)
            // {
            //     log.LogError($"Session {id} not found.");
            //     return new NotFoundResult();
            // }

            // if (!string.IsNullOrEmpty(updatedSession.Description))
            // {
            //     document.SetPropertyValue("Description", updatedSession.Description);
            // }
            // if (!string.IsNullOrEmpty(updatedSession.Title))
            // {
            //     document.SetPropertyValue("Title", updatedSession.Title);
            // }
            // if (updatedSession.EndTime.HasValue)
            // {
            //     document.SetPropertyValue("EndTime", updatedSession.EndTime);
            // }
            // if (updatedSession.StartTime.HasValue)
            // {
            //     document.SetPropertyValue("StartTime", updatedSession.StartTime);
            // }
            // await client.ReplaceDocumentAsync(document);

            // Session updatedSessionDocument = (dynamic)document;

            // return new OkObjectResult(updatedSessionDocument);
        }

    }
}
