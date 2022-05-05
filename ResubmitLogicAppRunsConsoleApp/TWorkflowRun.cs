using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace ResubmitLogicAppRunsConsoleApp
{
    public class TRun
    {
        [JsonProperty("value")]
        public List<TWorkflowRun> value { get; set; }

        [JsonProperty("nextLink")]
        public string nextLink { get; set; }
    }

    public class TWorkflowRun
    {
        [JsonProperty("id")]
        public string id { get; set; }

        [JsonProperty("name")]
        public string name { get; set; }

        [JsonProperty("properties")]
        public TPropertyRun properties { get; set; }

        [JsonProperty("type")]
        public string type { get; set; }
    }

    public class TPropertyRun
    {
        [JsonProperty("status")]
        public string status { get; set; }

        [JsonProperty("startTime")]
        public DateTime startTime { get; set; }

        [JsonProperty("endTime")]
        public DateTime endTime { get; set; }

        [JsonProperty("waitEndTime")]
        public DateTime waitEndTime { get; set; }
        
        [JsonProperty("error")]
        public TError error { get; set; }
    }

    public class TError
    {
        [JsonProperty("code")]
        public string code { get; set; }

        [JsonProperty("message")]
        public string message { get; set; }
    }
}
