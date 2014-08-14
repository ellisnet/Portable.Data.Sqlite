//
// ISimpleTransactionSuperior.cs
//
// Author:
//	Atsushi Enomoto  <atsushi@ximian.com>
//
// (C)2005 Novell Inc,
//

//Was: namespace  System.Transactions 
namespace Portable.Transactions
{
    public interface ISimpleTransactionSuperior : ITransactionPromoter
    {
        void Rollback();
    }
}
