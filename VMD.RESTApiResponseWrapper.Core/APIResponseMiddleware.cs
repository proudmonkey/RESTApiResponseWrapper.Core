using System;
using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;
using System.Net;
using Newtonsoft.Json;
using VMD.RESTApiResponseWrapper.Core.Wrappers;
using VMD.RESTApiResponseWrapper.Core.Extensions;
using System.IO;
using System.Text;
using Microsoft.Extensions.Logging;

namespace VMD.RESTApiResponseWrapper.Core
{
    public class APIResponseMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<APIResponseMiddleware> _logger;

        public APIResponseMiddleware(RequestDelegate next, ILogger<APIResponseMiddleware> logger)
        {
            _logger = logger;
            _next = next;
        }

        public async Task Invoke(HttpContext context)
        {
            if (IsSwagger(context))
                await this._next(context);
            else
            {
                var originalBodyStream = context.Response.Body;


                using (var bodyStream = new MemoryStream())
                {
                    try
                    {
                        context.Response.Body = bodyStream;

                        await _next.Invoke(context);

                        context.Response.Body = originalBodyStream;
                        if (context.Response.StatusCode == (int)HttpStatusCode.OK)
                        {
                            var bodyAsText = await FormatResponse(bodyStream);
                            await HandleSuccessRequestAsync(context, bodyAsText, context.Response.StatusCode);
                        }
                        else
                        {
                            await HandleNotSuccessRequestAsync(context, context.Response.StatusCode);
                        }
                    }
                    catch (System.Exception ex)
                    {
                        await HandleExceptionAsync(context, ex);
                        bodyStream.Seek(0, SeekOrigin.Begin);
                        await bodyStream.CopyToAsync(originalBodyStream);
                    }
                }


            }

        }

        private async Task<string> FormatResponse(Stream bodyStream)
        {
            bodyStream.Seek(0, SeekOrigin.Begin);
            var plainBodyText = await new StreamReader(bodyStream).ReadToEndAsync();
            bodyStream.Seek(0, SeekOrigin.Begin);

            return plainBodyText;
        }

        private static Task HandleExceptionAsync(HttpContext context, System.Exception exception)
        {
            ApiError apiError = null;
            APIResponse apiResponse = null;
            int code = 0;

            if (exception is ApiException)
            {
                var ex = exception as ApiException;
                apiError = new ApiError(ex.Message);
                apiError.ValidationErrors = ex.Errors;
                apiError.ReferenceErrorCode = ex.ReferenceErrorCode;
                apiError.ReferenceDocumentLink = ex.ReferenceDocumentLink;
                code = ex.StatusCode;
                context.Response.StatusCode = code;

            }
            else if (exception is UnauthorizedAccessException)
            {
                apiError = new ApiError("Unauthorized Access");
                code = (int)HttpStatusCode.Unauthorized;
                context.Response.StatusCode = code;
            }
            else
            {
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
                context.Response.StatusCode = code;
            }

            context.Response.ContentType = "application/json";

            apiResponse = new APIResponse(code, ResponseMessageEnum.Exception.GetDescription(), null, apiError);

            var json = JsonConvert.SerializeObject(apiResponse);

            return context.Response.WriteAsync(json);
        }

        private static Task HandleNotSuccessRequestAsync(HttpContext context, int code)
        {
            context.Response.ContentType = "application/json";

            ApiError apiError = null;

            if (code == (int)HttpStatusCode.NotFound)
                apiError = new ApiError("The specified URI does not exist. Please verify and try again.");
            else if (code == (int)HttpStatusCode.NoContent)
                apiError = new ApiError("The specified URI does not contain any content.");
            else
                apiError = new ApiError("Your request cannot be processed. Please contact a support.");

            APIResponse apiResponse = new APIResponse(code, ResponseMessageEnum.Failure.GetDescription(), null, apiError);
            context.Response.StatusCode = code;

            var json = ConvertToJSONString(apiResponse);

            return context.Response.WriteAsync(json);
        }

        private static Task HandleSuccessRequestAsync(HttpContext context, object body, int code)
        {
            context.Response.ContentType = "application/json";
            string jsonString = string.Empty;

            var bodyText = !body.ToString().IsValidJson() ? ConvertToJSONString(body) : body.ToString();

            dynamic bodyContent = JsonConvert.DeserializeObject<dynamic>(bodyText);
            Type type = bodyContent?.GetType();

            if (type.Equals(typeof(Newtonsoft.Json.Linq.JObject)))
            {
                APIResponse apiResponse = JsonConvert.DeserializeObject<APIResponse>(bodyText);
                if (apiResponse.StatusCode != code || apiResponse.Result != null)
                    jsonString = ConvertToJSONString(apiResponse);
                else
                    jsonString = ConvertToJSONString(code, bodyContent);
            }
            else
            {
                jsonString = ConvertToJSONString(code, bodyContent);
            }

            return context.Response.WriteAsync(jsonString);
        }

        private static string ConvertToJSONString(int code, object content)
        {
            return JsonConvert.SerializeObject(new APIResponse(code, ResponseMessageEnum.Success.GetDescription(), content, null));
        }
        private static string ConvertToJSONString(APIResponse apiResponse)
        {
            return JsonConvert.SerializeObject(apiResponse);
        }

        private static string ConvertToJSONString(object rawJSON)
        {
            return JsonConvert.SerializeObject(rawJSON);
        }

        private bool IsSwagger(HttpContext context)
        {
            return context.Request.Path.StartsWithSegments("/swagger");

        }

    }
}
