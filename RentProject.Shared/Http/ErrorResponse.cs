using System.Text.Json;

namespace RentProject.Shared.Http
{
    // 接住 WebAPI 回傳的「錯誤 JSON 內容」，把它轉成 C# 物件，方便 WinForms 端讀取與顯示
    public sealed class ErrorResponse
    {
        public string? Code { get; set; }

        public string? Message { get; set; }

        public string? TraceId { get; set; }

        public JsonElement? Details { get; set; }
    }
}
