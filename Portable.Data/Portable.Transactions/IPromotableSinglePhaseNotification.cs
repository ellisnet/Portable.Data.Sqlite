//
// IPromotableSinglePhaseNotification.cs
//
// Author:
//	Atsushi Enomoto  <atsushi@ximian.com>
//
// (C)2005 Novell Inc,
//

//Was: namespace  System.Transactions 
namespace Portable.Transactions
{
    public interface IPromotableSinglePhaseNotification : ITransactionPromoter
    {
        void Initialize();

        void Rollback(SinglePhaseEnlistment enlistment);

        void SinglePhaseCommit(SinglePhaseEnlistment enlistment);
    }
}
