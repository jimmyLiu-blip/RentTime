using RentProject.Domain;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace RentProject.Clients
{
    public sealed class RentProjectApiRentTimeClient : IRentTimeApiClient
    {
        private readonly HttpClient _httpClient;

        // JSON 欄位大小寫不敏感，例如 API 回 bookingNo、BookingNo、BOOKINGNO 都能對到你的 C# 屬性 BookingNo。
        private static readonly JsonSerializerOptions _json = new()
        {
            PropertyNameCaseInsensitive = true,
        };

        public RentProjectApiRentTimeClient(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<List<RentTime>> GetProjectViewListAsync(CancellationToken ct = default)
        {
            // 你的 WebAPI 路由：GET /api/renttimes/project-view
            const string url = "api/renttimes/project-view";

            try
            {
                // ConfigureAwait(false) 的意思是：不要強制回 UI 執行緒，回來後在哪個 thread 都可以繼續跑
                using var resp = await _httpClient.GetAsync(url, ct).ConfigureAwait(false);
                if (!resp.IsSuccessStatusCode) return new List<RentTime>();

                var json = await resp.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
                return JsonSerializer.Deserialize<List<RentTime>>(json, _json) ?? new List<RentTime>();
            }
            catch
            { 
                return new List<RentTime>();
            }
        }

        public async Task<RentTime?> GetByIdAsync(int rentTimeId, CancellationToken ct = default)
        {
            var url = $"api/renttimes/{rentTimeId}";

            using var resp = await _httpClient.GetAsync(url, ct).ConfigureAwait (false);

            if (resp.StatusCode == System.Net.HttpStatusCode.NotFound) return null;

            resp.EnsureSuccessStatusCode();

            var json = await resp.Content.ReadAsStringAsync(ct).ConfigureAwait (false);
            return JsonSerializer.Deserialize < RentTime >(json, _json);
        }

        public async Task<CreateRentTimeResult> CreateRentTimeFromApiAsync(RentTime model, long? bookingBatchId = null, CancellationToken ct = default)
        {
            var url = bookingBatchId.HasValue
                ? $"api/renttimes?bookingBatchId={bookingBatchId.Value}"
                : "api/renttimes";

            /*using var resp = await _httpClient.PostAsJsonAsync(url, model, ct).ConfigureAwait(false);
            resp.EnsureSuccessStatusCode();

            var json = await resp.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
            return JsonSerializer.Deserialize<CreateRentTimeResult>(json, _json)
                ?? throw new Exception("CreateAsync回傳空結果");*/

            using var resp = await _httpClient.PostAsJsonAsync(url, model, ct).ConfigureAwait(false);
            var body = await resp.Content.ReadAsStringAsync(ct).ConfigureAwait(false);

            if (!resp.IsSuccessStatusCode)
            {
                throw new Exception($"CreateAsync 失敗：HTTP {(int)resp.StatusCode} {resp.ReasonPhrase}\n{body}");
            }

            return JsonSerializer.Deserialize<CreateRentTimeResult>(body, _json)
                   ?? throw new Exception("CreateAsync 回傳空結果");
        }

        public async Task UpdateRentTimeFromApiAsync(int rentTimeId, RentTime model, string user, CancellationToken ct = default)
        {
            var url = $"api/renttimes/{rentTimeId}?user={Uri.EscapeDataString(user)}";

            using var resp = await _httpClient.PutAsJsonAsync(url, model, ct).ConfigureAwait (false);
            resp.EnsureSuccessStatusCode();
        }

        public async Task StartRentTimeFromApiAsync(int rentTimeId, string user, CancellationToken ct = default)
        {
            var url = $"api/renttimes/{rentTimeId}/start";
            using var resp = await _httpClient.PostAsJsonAsync(url, new { User = user }, ct).ConfigureAwait(false);
            resp.EnsureSuccessStatusCode();
        }

        public async Task FinishRentTimeFromApiAsync(int rentTimeId, string user, CancellationToken ct = default)
        {
            var url = $"api/renttimes/{rentTimeId}/finish";
            using var resp = await _httpClient.PostAsJsonAsync(url, new { User = user }, ct).ConfigureAwait(false);
            resp.EnsureSuccessStatusCode();
        }

        public async Task RestoreToDraftByIdAsync(int rentTimeId, string user, CancellationToken ct = default)
        {
            var url = $"api/renttimes/{rentTimeId}/restore";
            using var resp = await _httpClient.PostAsJsonAsync(url, new { User = user }, ct).ConfigureAwait(false);
            resp.EnsureSuccessStatusCode();
        }

        public async Task DeleteRentTimeByIdAsync(int rentTimeId, string user, CancellationToken ct = default)
        {
            var url = $"api/renttimes/{rentTimeId}?user={Uri.EscapeDataString(user)}";
            using var resp = await _httpClient.DeleteAsync(url, ct).ConfigureAwait(false);
            resp.EnsureSuccessStatusCode();
        }

        public async Task SubmitToAssistantByIdAsync(int rentTimeId, string user, CancellationToken ct = default)
        {
            var url = $"api/renttimes/{rentTimeId}/submit";
            using var resp = await _httpClient.PostAsync(url, JsonContent.Create(new { User = user }), ct).ConfigureAwait(false);
            resp.EnsureSuccessStatusCode();
        }

        public async Task<bool> ChangeDraftPeriodWithSplitAsync(int rentTimeId, DateTime newStart, DateTime newEnd, string user, CancellationToken ct = default)
        {
            var url = $"api/renttimes/{rentTimeId}/change-period";
            var body = new { NewStart = newStart, NewEnd = newEnd, User = user };

            using var resp = await _httpClient.PostAsync(url, JsonContent.Create(body), ct).ConfigureAwait(false);
            resp.EnsureSuccessStatusCode();

            var txt = await resp.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
            return JsonSerializer.Deserialize<bool>(txt, _json);
        }

        public async Task<CreateRentTimeResult> CopyRentTimeByIdAsync(int rentTimeId, bool isHandOver, string user, CancellationToken ct = default)
        {
            var url = $"api/renttimes/{rentTimeId}/copy";
            var body = new { IsHandOver = isHandOver, User = user };

            using var resp = await _httpClient.PostAsJsonAsync(url, body, ct).ConfigureAwait(false);
            resp.EnsureSuccessStatusCode();

            var json = await resp.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
            return JsonSerializer.Deserialize<CreateRentTimeResult>(json, _json)
                   ?? throw new Exception("CopyAsync 回傳空結果");
        }

        public async Task<long> CreateBookingBatchAsync(CancellationToken ct = default)
        {
            const string url = "api/renttimes/booking-batch";

            using var resp = await _httpClient.PostAsync(url, content: null, ct).ConfigureAwait(false);
            resp.EnsureSuccessStatusCode();

            // 你的後端回傳 Ok(long) => JSON 會是純數字，例如：123
            var batchId = await resp.Content.ReadFromJsonAsync<long>(cancellationToken: ct).ConfigureAwait(false);
            return batchId;
        }

        public async Task<string> PingDBAsync(CancellationToken ct = default)
        {
            // 這支要在 WebAPI 端實作：GET /api/health/db
            const string url = "api/health/db";

            using var resp = await _httpClient.GetAsync(url, ct).ConfigureAwait(false);

            // 不管成功或失敗，都把 body 讀出來（失敗時 body 才是你要顯示給使用者的原因）
            var body = await resp.Content.ReadAsStringAsync(ct).ConfigureAwait(false);

            if(resp.IsSuccessStatusCode)
                return body;

            var msg = string.IsNullOrWhiteSpace(body)
                ?(resp.ReasonPhrase ?? "Unknown error")
                : body;

            throw new Exception($"API + DB Health Fail ({(int)resp.StatusCode} {resp.StatusCode}):{msg}");
        }
    }
}
