using System.Threading;

namespace Microsoft.Scripting.Utils {
    public static class ThreadingUtils {
        private static int id;
        private static System.Threading.ThreadLocal<int> threadIds = new System.Threading.ThreadLocal<int>(() => Interlocked.Increment(ref id));
        
        public static int GetCurrentThreadId() {
            return threadIds.Value;
        }
    }
}
