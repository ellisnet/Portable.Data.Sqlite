//
// Delegates.cs
//
// Author:
//	Atsushi Enomoto  <atsushi@ximian.com>
//
// (C)2005 Novell Inc,
//

//Was: namespace  System.Transactions 
namespace Portable.Transactions
{
    public delegate Transaction HostCurrentTransactionCallback();

    public delegate void TransactionCompletedEventHandler(object sender, TransactionEventArgs e);

    public delegate void TransactionStartedEventHandler(object sender, TransactionEventArgs e);
}
