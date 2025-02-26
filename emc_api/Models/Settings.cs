using System.Text.Json.Serialization;
using System.Collections.Generic;

namespace emc_api.Models
{
    public class Settings
    {
        
        [JsonPropertyName("remote_source")]
        public string RemoteSource { get; set; }

        [JsonPropertyName("station_orders")]
        public List<StationOrder> StationOrders { get; set; }

        [JsonPropertyName("tests")]
        public List<string> Tests { get; set; }

        [JsonPropertyName("project_engineers")]
        public List<string> ProjectEngineers { get; set; }

        [JsonPropertyName("user")]
        public User User { get; set; }

        [JsonPropertyName("testing_engineers")]
        public List<string> TestingEngineers { get; set; }

        [JsonPropertyName("loadSetting")]
        public LoadSetting LoadSetting { get; set; }
    }

    public class StationOrder
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("seq")]
        public int Seq { get; set; }
    }

    public class User
    {
        [JsonPropertyName("machine")]
        public string Machine { get; set; }

        [JsonPropertyName("user")]
        public string Username { get; set; }
    }

    public class LoadSetting
    {
        [JsonPropertyName("high_load")]
        public int HighLoad { get; set; }

        [JsonPropertyName("low_load")]
        public int LowLoad { get; set; }

        [JsonPropertyName("medium_load")]
        public int MediumLoad { get; set; }
    }
}