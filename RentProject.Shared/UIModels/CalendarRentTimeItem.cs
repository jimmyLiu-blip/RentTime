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

        // 新增：是否為「摘要筆數」那種假 appointment
        public bool IsSummary { get; set; }

        // 新增：若是摘要，這裡放該日期所有 RentTimeId（用逗號串）
        public string RentTimeIds { get; set; } = "";

        public int Status { get; set; }

        public int LabelId { get; set; }
    }
}
