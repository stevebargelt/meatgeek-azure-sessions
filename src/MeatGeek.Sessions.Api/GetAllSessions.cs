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
using Inferno.Common.Models;
using Honeycomb;

#nullable enable
namespace MeatGeek.Sessions
{
   public class GetAllSessions
    {
        private static IConfiguration Configuration { set; get; }
        private static string HoneycombKey;
        private static string HoneycombDataset;        
        private static LibHoney _libHoney;

        static GetAllSessions()
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

        [FunctionName("GetAllSessions")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "")] HttpRequest req, 
                [CosmosDB(
                databaseName: "Inferno",
                collectionName: "sessions",
                ConnectionStringSetting = "CosmosDBConnection")] DocumentClient client,
                ILogger log, string? traceid, string? parentspanid)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");

            var spanId = Guid.NewGuid().ToString();
            Int32 unixTimestamp = (Int32)(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;

            var stopWatch = new Stopwatch();
            stopWatch.Start();
            var sessions = new List<Session>();
            var activity = new Activity("CallToCosmosDB")
                .AddBaggage("GetAllSessionAzureFunction", "v0.1")
                .Start();
            try {
                Uri sessionCollectionUri = UriFactory.CreateDocumentCollectionUri(databaseId: "Inferno", collectionId: "sessions");
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
            finally {
                traceid ??= "";
                parentspanid ??= "";
                activity.Stop();
                stopWatch.Stop();
                _libHoney.SendNow (new Dictionary<string, object> () {
                    ["name"] = "sessions",
                    ["service_name"] = "GetAllSessions",
                    ["trace.trace_id"] = traceid,
                    ["trace.span_id"] = spanId,
                    ["trace.parent_id"] = parentspanid,
                    ["duration_ms"] = stopWatch.ElapsedMilliseconds,
                    ["Timestamp"] = unixTimestamp,
                    ["method"] = "get",
                    ["activity.TraceId"] = activity.TraceId,
                    ["activity.SpanId"] = activity.SpanId,
                    ["activity.Duration"] = activity.Duration,
                    ["activity.ParentId"] = activity.ParentId,
                    ["activity.ParentSpanId"] = activity.ParentSpanId,
                    ["activity.Id"] = activity.Id,

                });
            }

            if (sessions == null || sessions.Count == 0)
            {
                return new NotFoundResult();
            }
            return new OkObjectResult(sessions);
        }
    }       

}
