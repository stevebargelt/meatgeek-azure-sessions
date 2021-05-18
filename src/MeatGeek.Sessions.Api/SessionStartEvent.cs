using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Cosmos.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Documents;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Azure.Documents.Client;
using Newtonsoft.Json;

using MeatGeek.Sessions.Services.Models;


namespace MeatGeek.Sessions
{
    public class SessionStart
    {
        private readonly CosmosClient _cosmosClient;

        public SessionStart(CosmosClient cosmosClient)
        {
            _cosmosClient = cosmosClient;
        }

        [FunctionName("StartSession")]
        public async void Run([CosmosDBTrigger(
            databaseName: "Sessions",
            collectionName: "sessions",
            ConnectionStringSetting = "CosmosDBConnection",
            LeaseCollectionName = "leases",
            LeaseCollectionPrefix = "sessions",
            CreateLeaseCollectionIfNotExists = true,
            StartFromBeginning = true)]IReadOnlyList<Document> sessionEvents,
            ILogger log)
        {
            var sessionEvent = sessionEvents.FirstOrDefault();
            log.LogInformation("*****************************************************************************************")
            log.LogInformation("sessionEvent "+sessionEvent);
            log.LogInformation("*****************************************************************************************")
            var session = JsonConvert.DeserializeObject<Session>(sessionEvents.FirstOrDefault().ToString());

            log.LogInformation("SmokerId "+session.SmokerId);
            log.LogInformation("SessionId "+session.Id);
            var StatusPartitionKey = $"{session.SmokerId}-{session.StartTime:yyyy-MM}";
            log.LogInformation($"Status PartitionKey = {StatusPartitionKey}");

            var EndTime = session.EndTime.HasValue ? session.EndTime : DateTime.UtcNow;
            log.LogInformation($"StartTime = {session.StartTime} EndTime = {EndTime}");
            log.LogInformation($"UTC StartTime = {session.StartTime} UTC EndTime = {EndTime}");

        }        
        
    }
}
