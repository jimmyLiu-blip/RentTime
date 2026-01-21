using RentProject.Domain;
using RentProject.Service;
using System;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using static System.Net.WebRequestMethods;

namespace RentProject.Clients
{
    // 這個 Client 是「打你自己的 WebAPI」，不是打 Procert 外部網站
    public sealed class RentProjectApiJobNoClient : IJobNoApiClient
    {
        private readonly HttpClient _httpClient;

        private static readonly JsonSerializerOptions _json = new()
        {
            PropertyNameCaseInsensitive = true
        };

        public RentProjectApiJobNoClient(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<JobNoMaster?> GetJobNoMasterAsync(string jobNo, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(jobNo)) return null;
            jobNo = jobNo.Trim();

            // 你的 WebAPI 路由：GET /api/jobno/{jobNo}
            var url = $"api/jobno/{Uri.EscapeDataString(jobNo)}";

            try
            {
                using var resp = await _httpClient.GetAsync(url, ct);

                if (resp.StatusCode == HttpStatusCode.NotFound) return null;
                if (!resp.IsSuccessStatusCode) return null;

                var json = await resp.Content.ReadAsStringAsync(ct);
                if (string.IsNullOrWhiteSpace(json)) return null;

                // WebAPI 回傳的是 JobNoMaster 物件
                return JsonSerializer.Deserialize<JobNoMaster>(json, _json);
            }
            catch (OperationCanceledException)
            {
                return null;
            }
            catch
            {
                return null;
            }
        }
    }
}
