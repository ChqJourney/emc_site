using System.Text.Json.Serialization;

namespace emc_api.Models
{
    public class SeventDto
    {
        [JsonPropertyName("name")]
        public string Name { get; set; }
        [JsonPropertyName("from_date")]
        public string FromDate { get; set; }
        [JsonPropertyName("to_date")]
        public string ToDate { get; set; }
        [JsonPropertyName("station_id")]
        public int StationId { get; set; }
        [JsonPropertyName("created_by")]
        public string CreatedBy { get; set; }
        [JsonPropertyName("updated_by")]
        public string UpdatedBy { get; set; }
    }

    public class Sevent
    {
        [JsonPropertyName("id")]
        public int Id { get; set; } = 0;
        [JsonPropertyName("name")]
        public string Name { get; set; }
        [JsonPropertyName("from_date")]
        public string From_Date { get; set; }
        [JsonPropertyName("to_date")]
        public string To_Date { get; set; }
        [JsonPropertyName("station_id")]
        public int Station_Id { get; set; }
        [JsonPropertyName("created_on")]
        public DateTime Created_On { get; set; }
        [JsonPropertyName("updated_on")]
        public DateTime Updated_On { get; set; }
        [JsonPropertyName("created_by")]
        public string Created_By { get; set; }
        [JsonPropertyName("updated_by")]
        public string Updated_By { get; set; }
    }
}