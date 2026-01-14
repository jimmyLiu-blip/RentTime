namespace RentProject.Shared.UIModels
{
    public class ImportRentTimeRow
    {
        public int ExcelRowNo { get; set; }   // Excel 第幾列（2,3,4...）
        public string? Area { get; set; }   // 可不填，會由 Location 推導
        public string? Location { get; set; }
        public string? CustomerName { get; set; }
        public string? Sales {  get; set; }
        public string? PE { get; set; }

        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }

        public TimeSpan? StartTime { get; set; }
        public TimeSpan? EndTime { get; set; }


        public string? ProjectNo { get; set; }
        public string? ProjectName { get; set; }

        public string? JobNo { get; set; }
    }
}
