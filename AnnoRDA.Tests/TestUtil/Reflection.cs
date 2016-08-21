using System.Reflection;

namespace AnnoRDA.Tests.TestUtil
{
    internal static class Reflection
    {
        #if !NETSTANDARD
        internal static object CallInstanceMethod(object target, string name, params object[] arguments)
        {
            try {
                return target.GetType().InvokeMember(
                    name,
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.InvokeMethod,
                    null,
                    target,
                    arguments);
            } catch (TargetInvocationException ex) {
                throw ex.InnerException;
            }
        }
        #endif
    }
}
