using PlagScanAPI.Infrastructure.Exceptions;
using System.Net;

namespace PlagScanAPI.Infrastructure.Middlewares
{
    public class ExceptionHandlingMiddleware
    {
        private readonly RequestDelegate _next;

        public ExceptionHandlingMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext httpContext)
        {
            try
            {
                await _next(httpContext);
            }
            catch (ExceptionWithStatusCode ex)
            {
                await HandleExceptionAsync(
                    httpContext,
                    ex.Message,
                    ex.StatusCode);
            }
            catch (Exception ex)
            {
                await HandleExceptionAsync(
                    httpContext,
                    ex.Message,
                    HttpStatusCode.InternalServerError);
            }
        }

        private async Task HandleExceptionAsync(
            HttpContext context,
            string message,
            HttpStatusCode httpStatusCode)
        {
            HttpResponse response = context.Response;

            response.StatusCode = (int)httpStatusCode;

            var errorResponse = new
            {
                Message = message,
                StatusCode = (int)httpStatusCode
            };

            await response.WriteAsJsonAsync(errorResponse);
        }
    }
}
