using System;
using Newtonsoft.Json;

namespace MeatGeek.Sessions.Services.Models.Response
{
    public class SessionDetails
    {
        [JsonProperty("id")] public string Id { get; set; }
        [JsonProperty] public string SmokerId { get; set; }
        [JsonProperty] public string Title { get; set; }
        [JsonProperty] public string Description { get; set; }
        [JsonProperty] public DateTime? StartTime { get; set; }
        [JsonProperty] public DateTime? EndTime { get; set; }
        [JsonProperty] public DateTime TimeStamp { get; set; }
    }
}