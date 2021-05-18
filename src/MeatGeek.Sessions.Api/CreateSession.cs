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

    public class CreateSession
    {
        private const string JsonContentType = "application/json";
        private static readonly ISessionsService SessionsService = new SessionsService(new SessionsRepository(), new EventGridPublisherService());

        [FunctionName("CreateSession")]
        [OpenApiOperation(operationId: "CreateSession", tags: new[] { "session" }, Summary = "Start a new session.", Description = "This add a new session (sessions are 'cooks' or BBQ sessions).", Visibility = OpenApiVisibilityType.Important)]
        [OpenApiRequestBody(contentType: "application/json", bodyType: typeof(SessionDetails), Required = true, Description = "Session object that needs to be added to the store")]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(SessionDetails), Summary = "New session details added", Description = "New session details added")]
        [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.MethodNotAllowed, Summary = "Invalid input", Description = "Invalid input")]        
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "sessions")] HttpRequest req, 
            ILogger log)
        {
            log.LogInformation("CreateSession API Triggered");
            var requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            CreateSessionRequest data;
            try
            {
                data = JsonConvert.DeserializeObject<CreateSessionRequest>(requestBody);
            }
            catch (JsonReaderException)
            {
                return new BadRequestObjectResult(new { error = "Body should be provided in JSON format." });
            }
            
            // validate request
            if (data == null || string.IsNullOrEmpty(data.Title))
            {
                return new BadRequestObjectResult(new { error = "Missing required property 'title'." });
            }

            if (string.IsNullOrEmpty(data.SmokerId))
            {
                return new BadRequestObjectResult(new { error = "Missing required property 'smokerid'." });
            }

            if (!data.StartTime.HasValue)
            {
                data.StartTime = DateTime.UtcNow;
            }

            // create session
            try
            {
                var sessionId = await SessionsService.AddSessionAsync(data.Title, data.SmokerId, data.StartTime.Value);
                return new OkObjectResult(new { id = sessionId });
            }
            catch (Exception ex)
            {
                log.LogError("Unhandled exception", ex);
                return new ExceptionResult(ex, false);
            }
        }

    }
}
