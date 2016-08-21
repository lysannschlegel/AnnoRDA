using System.Collections.Generic;
using System.IO;
#if !NETSTANDARD
using System.Runtime.InteropServices;
using System.Security;
#endif

namespace AnnoRDA.Util
{
#if NETSTANDARD
    internal static class NaturalStringComparerNative
    {
        public static int StrCmpLogicalW(string psz1, string psz2)
        {
            psz1 = psz1.ToLowerInvariant();
            psz2 = psz2.ToLowerInvariant();

            for (int i = 0; i < psz1.Length && i < psz2.Length; ++i) {
                int result = psz1[i].CompareTo(psz2[i]);
                if (result != 0) {
                    return result;
                }
            }
            if (psz1.Length > psz2.Length) {
                return 1;
            }
            else if (psz1.Length < psz2.Length) {
                return -1;
            } else {
                return 0;
            }
        }
    }
#else
    // http://stackoverflow.com/a/248613

    [SuppressUnmanagedCodeSecurity]
    internal static class NaturalStringComparerNative
    {
        [DllImport("shlwapi.dll", CharSet = CharSet.Unicode)]
        public static extern int StrCmpLogicalW(string psz1, string psz2);
    }
#endif

    public sealed class NaturalFilenameStringComparer : IComparer<string>
    {
        public int Compare(string a, string b)
        {
            return NaturalStringComparerNative.StrCmpLogicalW(a, b);
        }
    }

    public sealed class NaturalFileInfoNameComparer : IComparer<FileInfo>
    {
        public int Compare(FileInfo a, FileInfo b)
        {
            return NaturalStringComparerNative.StrCmpLogicalW(a.Name, b.Name);
        }
    }
}
