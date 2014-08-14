//
// TransactionAbortedException.cs
//
// Author:
//	Atsushi Enomoto  <atsushi@ximian.com>
//
// (C)2005 Novell Inc,
//

using System;

//Was: namespace  System.Transactions 
namespace Portable.Transactions
{
    public class TransactionAbortedException : TransactionException
    {
        public TransactionAbortedException()
        {
        }

        public TransactionAbortedException(string message)
            : base(message)
        {
        }

        public TransactionAbortedException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
