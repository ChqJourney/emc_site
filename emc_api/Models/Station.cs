using System.Text.Json.Serialization;

namespace emc_api.Models
{
    public class Station
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }
        [JsonPropertyName("name")]
        public string Name { get; set; }
        [JsonPropertyName("short_name")]
        public string Short_Name { get; set; }
        [JsonPropertyName("description")]
        public string Description { get; set; } = "";
        [JsonPropertyName("photo_path")]
        public string Photo_Path { get; set; } = "";
        [JsonPropertyName("status")]
        public string Status { get; set; } = "Active";
        [JsonPropertyName("created_on")]
        public string Created_On { get; set; }
        [JsonPropertyName("updated_on")]
        public string Updated_On { get; set; }
    }
}