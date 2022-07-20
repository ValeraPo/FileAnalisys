using FileAnalisys.API.Models;
using FileAnalisys.BLL.Exceptions;
using System.Net;
using System.Text.Json;

namespace FileAnalisys.API.Infrastructure
{
    // Global error handling
    public class ErrorExceptionMiddleware
    {
        private readonly RequestDelegate _next;

        public ErrorExceptionMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task Invoke(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (FormatException error)
            {
                await ConstructResponse(context, HttpStatusCode.BadRequest, error.Message);
            }
            catch (ServiceUnavailableException error)
            {
                await ConstructResponse(context, HttpStatusCode.ServiceUnavailable, error.Message);
            }
            catch (TimeoutException error)
            {
                await ConstructResponse(context, HttpStatusCode.GatewayTimeout, error.Message);
            }
            // catch all other exceptions
            catch (Exception ex)
            {
                await ConstructResponse(context, HttpStatusCode.InternalServerError, ex.Message);
            }
        }

        // If we catch error, program will not stop working
        private async Task ConstructResponse(HttpContext context, HttpStatusCode code, string message)
        {
            context.Response.ContentType = "application/json";
            context.Response.StatusCode = (int)code;

            var updateModel = new ExceptionResponse { Code = (int)code, Message = message };

            var result = JsonSerializer.Serialize(updateModel);
            await context.Response.WriteAsync(result);
        }
    }
}
