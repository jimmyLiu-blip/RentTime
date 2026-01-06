namespace RentProject.Shared.UIModels
{
    public class CalendarRentTimeDetailItem
    {
        public int RentTimeId { get; set; }

        public string BookingNo { get; set; } = "";

        public DateTime StartAt { get; set; }

        public DateTime EndAt { get; set; }

        public string ProjectNo { get; set; } = "";

        public string ProjectName { get; set; } = "";

        public string CustomerName { get; set; } = "";

        public string ContactName { get; set; } = "";

        public string Phone { get; set; } = "";

        public string PE { get; set; } = "";

        public string Location { get; set; } = "";

        public string TestItem { get; set; } = "";

        public string Notes { get; set; } = "";

        public string Area { get; set; } = "";

    }
}
