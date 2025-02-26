using System.Text.Json.Serialization;

namespace emc_api.Models
{
    public class SeventDto
    {
        public string Name { get; set; }
        public string FromDate { get; set; }
        public string ToDate { get; set; }
        public int StationId { get; set; }
        public string CreatedBy { get; set; }
        public string UpdatedBy { get; set; }
    }

    public class Sevent
    {
        [JsonPropertyName("id")]
        public int Id { get; set; } = 0;
        [JsonPropertyName("name")]
        public string Name { get; set; }
        [JsonPropertyName("from_date")]
        public string FromDate { get; set; }
        [JsonPropertyName("to_date")]
        public string ToDate { get; set; }
        [JsonPropertyName("station_id")]
        public int StationId { get; set; }
        [JsonPropertyName("created_on")]
        public DateTime CreatedOn { get; set; }
        [JsonPropertyName("updated_on")]
        public DateTime UpdatedOn { get; set; }
        [JsonPropertyName("created_by")]
        public string CreatedBy { get; set; }
        [JsonPropertyName("updated_by")]
        public string UpdatedBy { get; set; }
    }
}