namespace RentProject.Shared.UIModels
{
    // 放你要過濾的資料
    public class AdvancedFilter
    {
        public string? BookingNo { get; set; }

        public string? Area { get; set; }

        public string? Location { get; set; }

        public string? PE { get; set; }


        public DateTime? StartDate { get; set; }

        public DateTime? EndDate { get; set; }


        public string? ProjectNo { get; set; }

        public string? ProjectName { get; set; }

        public string? CustomerName { get; set; }

        public int? Status { get; set; }

    }
}
