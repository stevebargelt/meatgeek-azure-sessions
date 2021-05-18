using System;
using Newtonsoft.Json;

namespace MeatGeek.Sessions.Services.Models.Response
{
    public class SessionSummary
    {
        [JsonProperty("id")] public string Id { get; set; }
        [JsonProperty] public string SmokerId { get; set; }
        [JsonProperty] public string Title { get; set; }
    }
}