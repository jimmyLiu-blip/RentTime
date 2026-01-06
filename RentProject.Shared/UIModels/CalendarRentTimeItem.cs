namespace RentProject.Shared.UIModels
{
    public class CalendarRentTimeItem
    {
        public int RentTimeId { get; set; }

        public DateTime StartAt { get; set; }

        public DateTime EndAt { get; set; }

        // 月曆格子顯示用（例如：ProjectNo + CustomerName + TestItem）
        public string Subject { get; set; } = "";

        // 如果你要用場地做顏色/分組/過濾，也可放
        public string Location { get; set; } = "";
    }
}
