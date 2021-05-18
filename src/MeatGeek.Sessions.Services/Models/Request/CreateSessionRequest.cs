using System;
using Newtonsoft.Json;

namespace MeatGeek.Sessions.Services.Models.Request
{
    public class CreateSessionRequest
    {
        [JsonProperty] public string SmokerId { get; set; }
        [JsonProperty] public string Title { get; set; }
        [JsonProperty] public string Description { get; set; }
        [JsonProperty] public DateTime? StartTime { get; set; }
    }
}
