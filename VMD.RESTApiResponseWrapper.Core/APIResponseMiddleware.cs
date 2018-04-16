using System;
using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;
using System.Net;
using Newtonsoft.Json;
using VMD.RESTApiResponseWrapper.Core.Wrappers;

namespace VMD.RESTApiResponseWrapper.Core
{
    public class APIResponseMiddleware
    {
        private readonly RequestDelegate _next;

        public APIResponseMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task Invoke(HttpContext context)
        {
            try
            {
                await _next.Invoke(context);
            }
            catch (System.Exception ex)
            {
                await HandleExceptionAsync(context, ex);
            }

            if (!context.Response.HasStarted)
            {
                await HandleNotFoundAsync(context, context.Response.StatusCode);
            }
        }

        private static Task HandleExceptionAsync(HttpContext context, System.Exception exception)
        {
            ApiError apiError = null;
            APIResponse apiResponse = null;
            int code = 0;

            if (exception is ApiException)
            {
                // handle explicit 'known' API errors
                var ex = exception as ApiException;
                apiError = new ApiError(ex.Message);
                code = ex.StatusCode;
                apiError.ValidationErrors = ex.Errors;

                apiResponse = new APIResponse(code, null, apiError);
                context.Response.StatusCode = code;
            }
            else if (exception is UnauthorizedAccessException)
            {
                apiError = new ApiError("Unauthorized Access");
                code = (int)HttpStatusCode.Unauthorized;
                apiResponse.StatusCode = code;
                context.Response.StatusCode = code;

                // handle logging here
            }
            else
            {
                // Unhandled errors
                #if !DEBUG
                    var msg = "An unhandled error occurred.";                
                    string stack = null;
                #else
                var msg = exception.GetBaseException().Message;
                string stack = exception.StackTrace;
                #endif

                apiError = new ApiError(msg);
                apiError.Details = stack;
                code = (int)HttpStatusCode.InternalServerError;
                apiResponse = new APIResponse(code, null, apiError);
                context.Response.StatusCode = code;

                // handle logging here
            }

            context.Response.ContentType = "application/json";
            var json = JsonConvert.SerializeObject(apiResponse);

            return context.Response.WriteAsync(json);
        }

        private static Task HandleNotFoundAsync(HttpContext context, int code)
        {
            context.Response.ContentType = "application/json";

            ApiError apiError = null;
            APIResponse apiResponse = null;

            if (code == (int)HttpStatusCode.NotFound)
                apiError = new ApiError("The specified URI does not exist. Please verify and try again.");
            else if (code == (int)HttpStatusCode.NoContent)
                apiError = new ApiError("The specified URI does not contain any content.");
            else
                apiError = new ApiError("Your request cannot be processed. Please contact a support.");

            apiResponse = new APIResponse(code, null, apiError);
            context.Response.StatusCode = code;

            var json = JsonConvert.SerializeObject(apiResponse);

            return context.Response.WriteAsync(json);
        }

    }
}
