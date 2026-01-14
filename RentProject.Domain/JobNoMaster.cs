namespace RentProject.Domain
{
    public class JobNoMaster
    {
        public int JobId { get; set; }
        public string JobNo { get; set; } = null!;

        public string? ProjectNo { get; set; }
        public string? ProjectName { get; set; }
        public string? PE { get; set; }

        public string? CustomerName { get; set; }
        public string? Sales { get; set; }

        public string? SampleNo { get; set; }
        public string? SampleModel { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime? ModifiedAt { get; set; }
        public bool IsActive { get; set; }
    }
}
