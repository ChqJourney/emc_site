
public class User
    {
        public int Id { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string MachineName { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Team { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;

        public string Password { get; set; } = string.Empty;
        public string LoginAt { get; set; } = string.Empty;
    }

    public class UserActivity
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string ApiUsed { get; set; } = string.Empty;
        public string Timestamp { get; set; } = string.Empty;
    }

    public class Job
    {
        public int Id { get; set; }
        public string Job_No { get; set; } = string.Empty;
        public string Job_Status { get; set; } = string.Empty;
        public string Job_Openning_Date { get; set; } = string.Empty;
        public string Job_Complete_Date { get; set; } = string.Empty;
        public string Invoice_Team { get; set; } = string.Empty;
        public string Quotation_To { get; set; } = string.Empty;
        public string Applicant { get; set; } = string.Empty;
        public string Manufacturer { get; set; } = string.Empty;
        public string Agent { get; set; } = string.Empty;
        public double Job_Fee_Including_Retest { get; set; }
        public string Standards { get; set; } = string.Empty;
        public double Invoiced_Amount { get; set; }
        public double InIncome_Amount { get; set; }
        public string Job_Creator { get; set; } = string.Empty;
        public string Engineers { get; set; } = string.Empty;
        public string Product { get; set; } = string.Empty;
        public string Models { get; set; } = string.Empty;
        public string Services { get; set; } = string.Empty;
        public double Total_AccruedSubFee { get; set; }
        public string Quotation_No { get; set; } = string.Empty;
        public double Quotation_Amount { get; set; }
        public string Related_Jobs { get; set; } = string.Empty;
        public string EI_Creator { get; set; } = string.Empty;
        public string EI_Create_Date { get; set; } = string.Empty;
        public string Quotation_Sales { get; set; } = string.Empty;
        public string Retailer { get; set; } = string.Empty;
        public string Testing_Team { get; set; } = string.Empty;
        public string UCID { get; set; } = string.Empty;
        public string Updated_On { get; set; } = string.Empty;
        public string Created_On { get; set; } = string.Empty;
        public bool Is_Deleted { get; set; }
    }

   public class UserDto
{
    public string UserName { get; set; }
    public string MachineName { get; set; }
    public string FullName { get; set; }
    public string Team { get; set; }
    public string Role { get; set; }
    public DateTime LoginAt { get; set; }
}