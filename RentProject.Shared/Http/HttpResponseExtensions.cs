using System.Text.Json;

namespace RentProject.Shared.Http
{
    public static class HttpResponseExtensions
    {
        // 讓 JSON 欄位名稱大小寫不一致也能對得起來
        private static readonly JsonSerializerOptions _jsonOpt = new()
        {
            PropertyNameCaseInsensitive = true,
        };

        // 有了 this，你就可以用「像物件自己的方法」來呼叫：await resp.EnsureSuccessOrThrowApiExceptionAsync(ct);
        // 這裡的 this 告訴編譯器：這個 static 方法，是要被當成 HttpResponseMessage 的『擴充方法』使用，也就是說，第一個參數 resp 就是未來呼叫時的那個物件
        public static async Task EnsureSuccessOrThrowApiExceptionAsync(this HttpResponseMessage resp, CancellationToken ct)
        {
            if (resp.IsSuccessStatusCode) return;

            string? body = null;
            try
            {
                body = resp.Content == null ? null : await resp.Content.ReadAsStringAsync(ct);
            }
            catch
            {// 讀不到 body 也沒關係，後面用預設訊息
            }

            ErrorResponse? err = null;
            if (!string.IsNullOrWhiteSpace(body))
            {
                try { err = JsonSerializer.Deserialize<ErrorResponse>(body, _jsonOpt); }
                catch { /* 解析失敗就走 fallback */ }
            }

            // fallback：就算 API 目前還沒完全照 ErrorResponse 回，也能先止血
            var status = (int)resp.StatusCode;
            var code = err?.Code ?? $"HTTP_{status}";
            var traceId = err?.TraceId;

            var message =
                err?.Message
                ?? $"呼叫 API 失敗（HTTP {status}）。{(string.IsNullOrWhiteSpace(traceId) ? "" : $"TraceId: {traceId}")}";

            throw new ApiException(status, code, message, traceId, body);
        }
    }
}
