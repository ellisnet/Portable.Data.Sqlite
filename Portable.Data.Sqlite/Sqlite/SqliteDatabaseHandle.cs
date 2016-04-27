using System;

#if !PORTABLE
using Microsoft.Win32.SafeHandles;
#else
using System.Runtime.InteropServices;
#endif

namespace Portable.Data.Sqlite
{
	internal sealed class SqliteDatabaseHandle
#if PORTABLE
		: CriticalHandle
#else
		: SafeHandleZeroOrMinusOneIsInvalid
#endif
	{

        private SqliteLockContext _lockContext;
        public SqliteLockContext LockContext {
            get { return _lockContext; }
            internal set {
                if (value == null) { throw new ArgumentNullException(nameof(value)); }
                _lockContext = value;
            }
        }

        public SqliteDatabaseHandle()
#if PORTABLE
			: base((IntPtr) 0)
#else
            : base(ownsHandle: true)
#endif
        {
        }

        public SqliteDatabaseHandle(SqliteLockContext lockContext)
#if PORTABLE
			: base((IntPtr) 0)
#else
			: base(ownsHandle: true)
#endif
        {
            if (lockContext == null) { throw new ArgumentNullException(nameof(lockContext));}
            _lockContext = lockContext;
        }

        public SqliteDatabaseHandle(IntPtr handle)
#if PORTABLE
			: base((IntPtr) 0)
#else
            : base(ownsHandle: true)
#endif
        {
            SetHandle(handle);
        }

        public SqliteDatabaseHandle(IntPtr handle, SqliteLockContext lockContext)
#if PORTABLE
			: base((IntPtr) 0)
#else
			: base(ownsHandle: true)
#endif
		{
            if (lockContext == null) { throw new ArgumentNullException(nameof(lockContext)); }
            _lockContext = lockContext;
            SetHandle(handle);
		}

#if PORTABLE
		public override bool IsInvalid
		{
			get
			{
				return handle == new IntPtr(-1) || handle == (IntPtr) 0;
			}
		}
#endif

		protected override bool ReleaseHandle()
		{
#if NET45
			return _lockContext.sqlite3_close_v2(handle) == SqliteErrorCode.Ok;
#else
			return _lockContext.sqlite3_close(handle) == SqliteErrorCode.Ok;
#endif
        }
    }
}
