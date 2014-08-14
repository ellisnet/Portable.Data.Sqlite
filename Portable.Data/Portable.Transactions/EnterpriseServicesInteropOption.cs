//
// EnterpriseServicesInteropOption.cs
//
// Author:
//	Atsushi Enomoto  <atsushi@ximian.com>
//
// (C)2005 Novell Inc,
//

// OK, I have to say, am not interested in implementing COM dependent stuff.
//Was: namespace  System.Transactions 
namespace Portable.Transactions
{
    public enum EnterpriseServicesInteropOption
    {
        None,
        Automatic,
        Full
    }
}
