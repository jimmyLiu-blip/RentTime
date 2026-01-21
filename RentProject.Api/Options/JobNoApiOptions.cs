namespace RentProject.Api.Options
{
    public class JobNoApiOptions
    {
        public string BaseUrl { get; set; } = "";

        public string Path { get; set; } = "";

        public string ApiKey { get; set; } = "";

        public string ApiKeyHeader { get; set; } = "X-Api-Key";

        public int TimeoutSeconds { get; set; } = 10;
    }
}
