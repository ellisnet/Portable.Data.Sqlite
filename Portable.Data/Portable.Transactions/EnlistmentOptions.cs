//
// EnlistmentOptions.cs
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
    [Flags]
    public enum EnlistmentOptions
    {
        None,
        EnlistDuringPrepareRequired,
    }
}
