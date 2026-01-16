namespace RentProject.Shared.UIModels
{
    // 右側詳細資訊面板要顯示什麼欄位，就把那些欄位放在 CalendarRentTimeDetailItem
    public class CalendarRentTimeDetailItem
    {
        // --- Key：用來查詢 ---
        public int? RentTimeId { get; set; }

        // --- 清單顯示 & 排序用 ---
        public string BookingNo { get; set; } = "";
        public DateTime StartAt { get; set; }
        public DateTime EndAt { get; set; }

        // --- 右側詳細顯示用 ---
        public string CustomerName { get; set; } = "";
        public string Location { get; set; } = "";
        public string Area { get; set; } = "";
        public string ProjectNo { get; set; } = "";
        public string Phone { get; set; } = "";
        public string ContactName { get; set; } = "";
        public string PE { get; set; } = "";
    }
}
