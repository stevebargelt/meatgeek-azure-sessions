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
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Enums;

using MeatGeek.Sessions.Services.Models;
using MeatGeek.Sessions.Services;
using MeatGeek.Sessions.Services.Models.Request;
using MeatGeek.Sessions.Services.Models.Results;
using MeatGeek.Sessions.Services.Models.Response;
using MeatGeek.Sessions.Services.Repositories;
using MeatGeek.Shared;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

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
            var updateData = new UpdateSessionRequest {};
            JObject data;
            try
            {
                data = JObject.Parse(requestBody);
            }
            catch (JsonReaderException)
            {
                return new BadRequestObjectResult(new { error = "Body should be provided in JSON format." });
            }

            if (data == null || !data.HasValues)
            {
                return new BadRequestObjectResult(new { error = "Missing required properties. Nothing to update." });
            }
            if (!string.IsNullOrEmpty(data["SmokerId"].ToString()))
            {
                updateData.SmokerId = data["SmokerId"].ToString();
            }
            else
            {
                return new BadRequestObjectResult(new { error = "Missing required property: SmokerId is REQUIRED." });
            }

            if (!string.IsNullOrEmpty(data["Title"].ToString()))
            {
                updateData.Title = data["Title"].ToString();
            }
            if (!string.IsNullOrEmpty(data["Description"].ToString()))
            {
                updateData.Description = data["Description"].ToString();
            }
            if (!string.IsNullOrEmpty(data["EndTime"].ToString()))
            {
                updateData.EndTime = DateTime.Parse(data["EndTime"].ToString());
            }
            try
            {
                var result = await _sessionsService.UpdateSessionAsync(id, updateData.SmokerId, updateData.Title, updateData.Description, updateData.EndTime);
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


        }

    }
}
