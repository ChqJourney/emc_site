namespace emc_api.Models
{
    public class Station
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Short_Name { get; set; }
        public string Description { get; set; }
        public string Photo_Path { get; set; }
        public string Status { get; set; }
        public string Created_On { get; set; }
        public string Updated_On { get; set; }
    }
}