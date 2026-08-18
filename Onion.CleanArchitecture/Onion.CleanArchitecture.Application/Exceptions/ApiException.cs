using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Onion.CleanArchitecture.Application.Exceptions
{
    public class ApiException : Exception
    {
        // thường dùng
        public ApiException() : base() { }

        public ApiException(string message) : base(message) { }
        
        
        // dạng format "Sai {0} ..."
        public ApiException(string message, params object[] args)
            : base(String.Format(CultureInfo.CurrentCulture, message, args))
        {
        }
    }
}
