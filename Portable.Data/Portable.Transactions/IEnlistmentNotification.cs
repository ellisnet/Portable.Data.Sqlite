//
// IEnlistmentNotification.cs
//
// Author:
//	Atsushi Enomoto  <atsushi@ximian.com>
//
// (C)2005 Novell Inc,
//

//Was: namespace  System.Transactions 
namespace Portable.Transactions
{
    public interface IEnlistmentNotification
    {
        void Commit(Enlistment enlistment);

        void InDoubt(Enlistment enlistment);

        void Prepare(PreparingEnlistment preparingEnlistment);

        void Rollback(Enlistment enlistment);
    }
}
