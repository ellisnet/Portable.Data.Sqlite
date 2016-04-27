using System.Threading;

namespace Portable.Data.Sqlite
{
#if PORTABLE
    internal class Thread
	{
		public static void Sleep(int millisecondsTimeout)
		{
			using (var handle = new EventWaitHandle(false, EventResetMode.ManualReset))
			{
				handle.WaitOne(millisecondsTimeout);
			}
		}
	}
#endif
}
