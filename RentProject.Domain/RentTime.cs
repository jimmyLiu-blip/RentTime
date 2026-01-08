namespace RentProject.Domain
{
    public class RentTime
    {
        public int RentTimeId { get; set; }

        public string BookingNo { get; set; } = null!;

        public string? BookingSeq { get; set; }

        public string CreatedBy { get; set; } = null!;

        public DateTime CreatedDate { get; set; }

        public string? ModifiedBy { get; set; } = null;

        public DateTime? ModifiedDate { get; set; }


        // 新增租時單必填
        public string Area { get; set; } = null!;

        public string CustomerName { get; set; } = null!;

        public string Sales { get; set; } = null!;

        public string ProjectNo { get; set; } = null!;

        public string ProjectName { get; set; } = null!;

        public string PE {  get; set; } = null!;

        public string Location { get; set; } = null!;

        // 編輯租時單可補上
        public string? ContactName {  get; set; }

        public string? Phone { get; set; }

        public string? TestInformation { get; set; }

        public string? EngineerName { get; set; }

        public string? SampleModel { get; set; }

        public string? SampleNo { get; set; }

        public string? TestMode { get; set; }

        public string? TestItem { get; set; }

        public string? Notes { get; set; }

        public DateTime? StartDate { get; set; }

        public DateTime? EndDate { get; set; }

        public TimeSpan? StartTime { get; set; }

        public TimeSpan? EndTime { get; set; }

        public bool HasLunch { get; set; } = false;

        public int LunchMinutes { get; set; } = 60;

        public bool HasDinner { get; set; } = false;

        public int DinnerMinutes { get; set; }

        public int EstimatedMinutes { get; set; }

        public decimal EstimatedHours { get; set; }

        public string Status { get; set; } = "Draft";

        public bool IsDeleted { get; set; }

        public int? JobId { get; set; }

        public string? JobNo { get; set; }
    }
}
