using System.Text.Json.Serialization;

namespace emc_api.Models
{
    public class Reservation
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }
        [JsonPropertyName("reservation_date")]
        public string Reservation_Date { get; set; }
        [JsonPropertyName("time_slot")]
        public string Time_Slot { get; set; }
        [JsonPropertyName("station_id")]
        public int Station_Id { get; set; }
        [JsonPropertyName("client_name")]
        public string Client_Name { get; set; }
        [JsonPropertyName("product_name")]
        public string Product_Name { get; set; }
        [JsonPropertyName("tests")]
        public string Tests { get; set; }
        [JsonPropertyName("job_no")]
        public string? Job_No { get; set; }
        [JsonPropertyName("project_engineer")]
        public string? Project_Engineer { get; set; }
        [JsonPropertyName("testing_engineer")]
        public string? Testing_Engineer { get; set; }
        [JsonPropertyName("purpose_description")]
        public string? Purpose_Description { get; set; }
        [JsonPropertyName("contact_name")]
        public string? Contact_Name { get; set; }
        [JsonPropertyName("contact_phone")]
        public string? Contact_Phone { get; set; }
        [JsonPropertyName("sales")]
        public string? Sales { get; set; }
        [JsonPropertyName("reservate_by")]
        public string Reservate_By { get; set; }
        [JsonPropertyName("reservation_status")]
        public string Reservation_Status { get; set; }
        [JsonPropertyName("created_on")]
        public string Created_On { get; set; }
        [JsonPropertyName("updated_on")]
        public string Updated_On { get; set; }
    }
    public class ReservationDTO 
    {
        [JsonPropertyName("reservation_date")]
        public string Reservation_Date { get; set; }
        [JsonPropertyName("time_slot")]
        public string Time_Slot { get; set; }
        [JsonPropertyName("station_id")]
        public int Station_Id { get; set; }
        [JsonPropertyName("client_name")]
        public string Client_Name { get; set; }
        [JsonPropertyName("product_name")]
        public string Product_Name { get; set; }
        [JsonPropertyName("tests")]
        public string Tests { get; set; }
        [JsonPropertyName("job_no")]
        public string Job_No { get; set; }
        [JsonPropertyName("project_engineer")]
        public string Project_Engineer { get; set; }
        [JsonPropertyName("testing_engineer")]
        public string Testing_Engineer { get; set; }
        [JsonPropertyName("purpose_description")]
        public string Purpose_Description { get; set; }
        [JsonPropertyName("contact_name")]
        public string Contact_Name { get; set; }
        [JsonPropertyName("contact_phone")]
        public string Contact_Phone { get; set; }
        [JsonPropertyName("sales")]
        public string Sales { get; set; }
        [JsonPropertyName("reservate_by")]
        public string Reservate_By { get; set; }
        [JsonPropertyName("reservation_status")]
        public string Reservation_Status { get; set; }
    }
}