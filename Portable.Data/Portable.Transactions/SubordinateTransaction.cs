//
// SubordinateTransaction.cs
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
    public sealed class SubordinateTransaction : Transaction
    {
        public SubordinateTransaction(IsolationLevel level,
                                      ISimpleTransactionSuperior superior)
        {
            throw new NotImplementedException();
        }
    }
}
