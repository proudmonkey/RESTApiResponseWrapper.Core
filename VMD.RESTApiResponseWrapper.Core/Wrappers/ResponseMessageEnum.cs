using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;

namespace VMD.RESTApiResponseWrapper.Core.Wrappers
{
    public enum ResponseMessageEnum
    {
        [Description("Request successful.")]
        Success,
        [Description("Request responded with exceptions.")]
        Exception,
        [Description("Request denied.")]
        UnAuthorized,
        [Description("Request responded with validation error(s).")]
        ValidationError,
        [Description("Unable to process the request.")]
        Failure
    }
}
