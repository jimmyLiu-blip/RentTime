using System.Diagnostics;
using System.Text.Json;

namespace RentProject.Api
{
    public sealed class ApiExceptionMiddleware
    {
        private readonly RequestDelegate _next;

        public ApiExceptionMiddleware(RequestDelegate next)
        { 
            _next = next;
        }

        public async Task Invoke(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                var traceId = Activity.Current?.Id ?? context.TraceIdentifier;

                // 你可以依需求擴充：ArgumentException / InvalidOperationException / 自訂 BusinessRuleException
                var status = ex is InvalidOperationException or ArgumentException
                    ? StatusCodes.Status400BadRequest
                    : StatusCodes.Status500InternalServerError;

                context.Response.StatusCode = status;
                context.Response.ContentType = "application/json; charset=utf-8";

                var payload = new
                {
                    message = status >= 500 ? "系統忙碌或發生錯誤，請稍後再試。" : ex.Message,
                    traceId
                };

                await context.Response.WriteAsync(JsonSerializer.Serialize(payload));
            }
        }
    }
}
