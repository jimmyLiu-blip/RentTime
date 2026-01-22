using Microsoft.Extensions.Options;
using RentProject.Api.Options;
using RentProject.Domain;
using RentProject.Service;
using System.Net;
using System.Text.Json;

namespace RentProject.Api.Clients
{
    // 回傳格式: {"data": {...}}
    public sealed class JobNoApiResponse
    {
        public JobNoApiData? Data { get; set; }  // JSON: data
    }

    public sealed class JobNoApiData
    {
        public string? JobNo { get; set; }        // JSON: jobNo
        public string? ProjectNo { get; set; }    // JSON: projectNo
        public string? ProjectName { get; set; }  // JSON: projectName
        public string? JobPMName { get; set; }    // JSON: jobPMName

        public string? Applicant { get; set; }    // JSON: applicant
        public string? SalesName { get; set; }    // JSON: salesName

        public string? ClientProductName { get; set; } // JSON: clientProductName
        public string? Model { get; set; }             // JSON: model
    }

    public class ProcertJobNoApiClient : IExternalJobNoClient
    { 
        private readonly HttpClient _httpClient;
        private readonly JobNoApiOptions _opt;

        private static readonly JsonSerializerOptions _jsonOption = new()
        {
            PropertyNameCaseInsensitive = true,
        };

        public ProcertJobNoApiClient(HttpClient httpClient, IOptions<JobNoApiOptions> opt)
        {
            _httpClient = httpClient;
            _opt = opt.Value;
        }

        public async Task<JobNoMaster?> GetJobNoMasterAsync(string jobNo, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(jobNo)) return null;
            jobNo = jobNo.Trim();

            // 注意：舊版用 jobno（小寫），如果對方 API 其實是 jobNo / job_no，要以對方為準
            var path = (_opt.Path ?? "").TrimStart('/');
            var url = $"{path}?jobno={Uri.EscapeDataString(jobNo)}";

            try
            {
                using var req = new HttpRequestMessage(HttpMethod.Get, url);

                // ApiKey：有填才帶
                if (!string.IsNullOrWhiteSpace(_opt.ApiKey))
                    req.Headers.TryAddWithoutValidation(_opt.ApiKeyHeader, _opt.ApiKey);

                using var resp = await _httpClient.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, ct);

                if (resp.StatusCode == HttpStatusCode.NotFound) return null;
                if (!resp.IsSuccessStatusCode) return null;

                var json = await resp.Content.ReadAsStringAsync(ct);
                if (string.IsNullOrWhiteSpace(json)) return null;

                return ParseToJobNoMaster(json, fallbackJobNo: jobNo);
            }
            catch (OperationCanceledException)
            {
                return null;
            }
            catch (HttpRequestException)
            {
                return null;
            }
            catch
            {
                return null;
            }
        }

        private static JobNoMaster? ParseToJobNoMaster(string json, string fallbackJobNo)
        {
            try
            {
                var wrapper = JsonSerializer.Deserialize<JobNoApiResponse>(json, _jsonOption);
                var d = wrapper?.Data;
                if (d == null) return null;

                return new JobNoMaster
                {
                    JobNo = string.IsNullOrWhiteSpace(d.JobNo) ? fallbackJobNo : d.JobNo,

                    ProjectNo = d.ProjectNo,
                    ProjectName = d.ProjectName,
                    Sales = d.SalesName,
                    CustomerName = d.Applicant,
                    PE = d.JobPMName,

                    SampleNo = d.ClientProductName,
                    SampleModel = d.Model,

                    IsActive = true
                };
            }
            catch
            {
                return null;
            }
        }
    }
}
