using System;

namespace Portable.Data.Common
{
    public abstract class DbException : Exception
    {
        protected DbException()
        {
        }

        protected DbException(string message)
            : base(message)
        {
        }
        
        protected DbException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        protected DbException(string message, int errorCode)
            : base(message)
        {
            HResult = errorCode;
        }

        public int ErrorCode
        {
            get { return HResult; }
        }
    }

}
