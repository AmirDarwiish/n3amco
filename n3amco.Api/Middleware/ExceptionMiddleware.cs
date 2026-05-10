using System.Text.Json;
using DairySystem.Api.Common;

namespace DairySystem.Api.Middleware
{
    public class ExceptionMiddleware
    {
        private readonly RequestDelegate _next;

        public ExceptionMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task Invoke(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (AppException ex)
            {
                await HandleAppException(context, ex);
            }
            catch (Exception ex)
            {
                await HandleServerException(context, ex);
            }
        }

        private static async Task HandleAppException(HttpContext context, AppException ex)
        {
            context.Response.ContentType = "application/json";
            context.Response.StatusCode = ex.StatusCode;

            var response = new ApiResponse<object>
            {
                Success = false,
                ErrorCode = ex.ErrorCode,
                Message = ex.Message
            };

            var json = JsonSerializer.Serialize(response);

            await context.Response.WriteAsync(json);
        }

        private static async Task HandleServerException(HttpContext context, Exception ex)
        {
            context.Response.ContentType = "application/json";
            context.Response.StatusCode = 500;

            var response = new ApiResponse<object>
            {
                Success = false,
                ErrorCode = "SERVER_ERROR",
                Message = ex.Message + " | " + ex.InnerException?.Message
            };

            var json = JsonSerializer.Serialize(response);
            await context.Response.WriteAsync(json);
        }
    }
}