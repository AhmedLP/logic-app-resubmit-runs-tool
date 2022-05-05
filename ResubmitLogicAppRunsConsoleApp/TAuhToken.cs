using Newtonsoft.Json;

namespace ResubmitLogicAppRunsConsoleApp
{
    public class TAuhToken
    {
        [JsonProperty("token_type")]
        public string token_type { get; set; }

        [JsonProperty("access_token")]
        public string access_token { get; set; }
    }
}
