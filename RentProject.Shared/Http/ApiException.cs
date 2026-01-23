namespace RentProject.Shared.Http
{
    // WinForms 端「統一用一種例外」把 API 呼叫失敗的資訊往上丟，讓 UI 層可以用同一套 catch 來顯示錯誤、記錄 log、或做特殊處理
    public sealed class ApiException : Exception
    {
        public int StatusCode { get; set; }

        public string? Code { get; set; }

        public string? TraceId { get; set; }

        public string? RawBody { get; set; }

        public ApiException(int statusCode, string? code, string message, string? traceId = null, string? rawBody = null, Exception? inner = null)
            :base(message, inner)
        {
            StatusCode = statusCode;
            Code = code;
            TraceId = traceId;
            RawBody = rawBody;
        }
    }
}
