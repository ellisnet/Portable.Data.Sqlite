//
// TransactionPromotionException.cs
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
    public class TransactionPromotionException : TransactionException
    {
        protected TransactionPromotionException()
        {
        }

        public TransactionPromotionException(string message)
            : base(message)
        {
        }

        public TransactionPromotionException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
