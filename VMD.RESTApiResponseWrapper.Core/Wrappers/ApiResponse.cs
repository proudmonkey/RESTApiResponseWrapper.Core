using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;

namespace VMD.RESTApiResponseWrapper.Core.Wrappers
{
    [DataContract]
    public class APIResponse
    {
        [DataMember]
        public string Version { get { return "1.0.0"; } }

        [DataMember]
        public int StatusCode { get; set; }

        [DataMember(EmitDefaultValue = false)]
        public ApiError ResponseException { get; set; }

        [DataMember(EmitDefaultValue = false)]
        public object Result { get; set; }

        public APIResponse(int statusCode, object result = null, ApiError apiError = null)
        {
            StatusCode = statusCode;
            Result = result;
            ResponseException = apiError;
        }
    }
}
