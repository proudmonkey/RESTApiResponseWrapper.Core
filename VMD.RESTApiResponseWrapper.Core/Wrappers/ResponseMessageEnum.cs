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
        Failure,
        [Description("Request denied.")]
        Information,
        [Description("Request responded with validation error(s).")]
        Warning,
        [Description("Unable to process the request.")]
        General
    }
}
