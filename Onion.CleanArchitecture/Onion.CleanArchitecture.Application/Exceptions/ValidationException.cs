using FluentValidation.Results;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Onion.CleanArchitecture.Application.Exceptions
{
    public class ValidationException : Exception
    {
        public ValidationException() : base("1 hoặc nhiều lỗi xác thực đã xảy ra.")
        {
            Errors = new List<string>();
        }
        public List<string> Errors { get; }
        public ValidationException(IEnumerable<ValidationFailure> failures)
            : this()
        {
            foreach (var failure in failures)
            {
                Errors.Add(failure.ErrorMessage);
            }
        }

    }
}
