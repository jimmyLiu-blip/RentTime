using RentProject.Shared.Http;
using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace RentProject.UI.Http
{
    // DelegatingHandler 是 HttpClient 的「管線」元件
    // 每次 HttpClient 送出 request 前，都會先經過這個 handler，可以在這裡統一加 header、記 log、做重試
    public sealed class CorrelationIdHandler : DelegatingHandler
    {
        public const string HeaderName = "X-Correlation-Id";

        // 這個方法原本在父類別（這裡是 DelegatingHandler）就存在，要「改寫」它的行為：在原本行為之前/之後加一些自己的邏輯
        // HttpRequestMessage request：代表「這一次要送出的請求本體」，裡面包含：URL（要打哪個 API）、HTTP 方法（GET/POST/PUT/DELETE）、Headers（你要加的 X-Correlation-Id 就在這）、Body（如果是 POST/PUT 通常會有 JSON 內容）
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            // 同一次操作（BeginNew 範圍內）會拿到同一個 id；若沒有 BeginNew 就保底產生一個
            var id = CorrelationIdContext.Current ?? Guid.NewGuid().ToString("N");
            System.Diagnostics.Debug.WriteLine($"[CID] {id} {request.Method} {request.RequestUri}");

            // 避免重複附加
            if (request.Headers.Contains(HeaderName))
                request.Headers.Remove(HeaderName);

            // 把一個 Header 加進 request，但不做（或少做）格式驗證，並且用「嘗試加入」的方式回傳成功/失敗。
            request.Headers.TryAddWithoutValidation(HeaderName, id);

            // base 指的是「父類別」的實作。在這裡父類別是 DelegatingHandler。
            // 意思就是：「我處理完了，請繼續走下去」，（走到下一個 handler，最後走到最底層真正送出 HTTP 封包）
            return base.SendAsync(request, cancellationToken);
        }
    }
}
