using RentProject.Domain;
using System.Net;
using System.Text.Json;


namespace RentProject.Service
{
    // sealed：這個類別不允許被繼承。
    // 回傳格式:{"data":{...}}
    public sealed class JobNoApiResponse
    { 
        public JobNoApiData? Data { get; set; }   // JSON: data
    }

    public sealed class JobNoApiData
    {
        public string? JobNo { get; set; }        // JSON: jobNo
        public string? ProjectNo { get; set; }    // JSON: projectNo
        public string? ProjectName { get; set; }  // JSON: projectName
        public string? JobPMName { get; set; }           // JSON: jobPMName

        public string? Applicant { get; set; } // JSON: applicant
        public string? SalesName { get; set; }    // JSON: salesName
 
        public string? ClientProductName { get; set; }     // JSON: clientProductName
        public string? Model { get; set; }  // JSON: model
    }

    public class RealJobNoApiClient : IJobNoApiClient, IDisposable
    {
        // 真正發送 HTTP 的工具
        private readonly HttpClient _httpClient;
        private readonly string _path;
        private readonly string? _apiKey;
        private readonly string _apiKeyHeader;

        private static readonly JsonSerializerOptions _jsonOption = new()
        {
            // 讓反序列化時「欄位對應不分大小寫」
            PropertyNameCaseInsensitive = true,
        };

        public RealJobNoApiClient(string baseUrl, string path, string? apiKey = null, string apiKeyHeader = "X-Api-Key", int timeoutSeconds = 10)
        {
            if (string.IsNullOrWhiteSpace(baseUrl)) throw new ArgumentException("baseUrl不可為空");
            if (string.IsNullOrWhiteSpace(path)) throw new ArgumentException("path不可為空");

            _path = path.TrimStart('/'); //如果 path 是 /api/job，把前面的 / 去掉，避免後面組 URL 變成 //api/job。
            _apiKey = string.IsNullOrWhiteSpace(apiKey) ? null : apiKey;
            _apiKeyHeader = string.IsNullOrWhiteSpace(apiKeyHeader) ? "X-Api-Key" : apiKeyHeader;

            // UriKind.Absolute：這個 URI 必須是完整網址
            _httpClient = new HttpClient
            {
                BaseAddress = new Uri(baseUrl, UriKind.Absolute),
                Timeout = TimeSpan.FromSeconds(timeoutSeconds <= 0 ? 10 : timeoutSeconds)
            };
        }



        // 建 HttpRequestMessage => 加 headers（API key）=> SendAsync() 送出 => 得到 HttpResponseMessage => 判斷 StatusCode => 讀 Content（body） => 解析 JSON
        public async Task<JobNoMaster?> GetJobNoMasterAsync(string jobNo, CancellationToken ct = default)
        {
            if(string.IsNullOrWhiteSpace(jobNo)) return null;
            jobNo = jobNo.Trim();

            // Uri.EscapeDataString：把「使用者輸入的字串」變成URL 安全格式，避免網址壞掉或被誤解析
            var url = $"{_path}?jobno={Uri.EscapeDataString(jobNo)}";

            try
            {
                // 1. 建立 request
                using var req = new HttpRequestMessage(HttpMethod.Get, url);

                // 2. ApiKey：有填才帶
                if (!string.IsNullOrWhiteSpace(_apiKey))
                    req.Headers.TryAddWithoutValidation(_apiKeyHeader, _apiKey);

                // 3. 送出 request
                // HttpCompletionOption.ResponseHeadersRead：收到 Header 就先回來，不要等整個 Body 下載完才回來
                using var response = await _httpClient.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, ct);

                // 查不到:回null
                if (response.StatusCode == HttpStatusCode.NotFound) return null;

                // 外網可能查詢不到，這裡不要throw讓UI死掉
                if (!response.IsSuccessStatusCode) return null;

                // ReadAsStringAsync：把 response body（內容）讀成字串
                var json = await response.Content.ReadAsStringAsync(ct);

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

        // fallbackJobNo是「備援用的 jobNo」，因為 API 回來的 data.jobNo 可能是空的或缺欄位，不想最後存進 DB 的 JobNo 是空的
        private static JobNoMaster? ParseToJobNoMaster(string json, string fallbackJobNo)
        {
            // json = {"data":{"jobNo":"JOB0001","projectNo":"P-1001"}}
            // _jsonOption 解析 JSON 的規則/設定
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

        public void Dispose() => _httpClient.Dispose();
    }
}
