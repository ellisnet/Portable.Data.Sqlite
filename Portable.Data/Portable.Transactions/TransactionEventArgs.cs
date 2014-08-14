//
// TransactionEventArgs.cs
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
    public class TransactionEventArgs : EventArgs
    {
        private readonly Transaction transaction;

        public TransactionEventArgs()
        {
        }

        internal TransactionEventArgs(Transaction transaction)
            : this()
        {
            this.transaction = transaction;
        }

        public Transaction Transaction
        {
            get { return this.transaction; }
        }
    }
}
