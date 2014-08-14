//
// TransactionManagerCommunicationException.cs
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
    public class TransactionManagerCommunicationException : TransactionException
    {
        protected TransactionManagerCommunicationException()
        {
        }

        public TransactionManagerCommunicationException(string message)
            : base(message)
        {
        }

        public TransactionManagerCommunicationException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
