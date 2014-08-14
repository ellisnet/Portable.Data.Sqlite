//
// TransactionInDoubtException.cs
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
    public class TransactionInDoubtException : TransactionException
    {
        protected TransactionInDoubtException()
        {
        }

        public TransactionInDoubtException(string message)
            : base(message)
        {
        }

        public TransactionInDoubtException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
