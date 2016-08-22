using System;
using System.Collections.Generic;
using System.IO;

namespace AnnoRDA.Util
{
    internal static class NaturalStringComparer
    {
        // http://stackoverflow.com/a/9745132/467840
        public static int Compare(string a, string b)
        {
            int result;

            // Try quick return via string length
            if (a.Length == 0) {
                if (b.Length == 0) {
                    return 0;
                } else {
                    return -1;
                }
            } else if (b.Length == 0) {
                return 1;
            }

            // Numbers are smaller than other text
            if (Char.IsDigit(a[0]) && !Char.IsDigit(b[0])) {
                return -1;
            } else if (!Char.IsDigit(a[0]) && Char.IsDigit(b[0])) {
                return 1;
            }

            // Non-Number text is compared case-insensitively
            if (!Char.IsDigit(a[0]) && !Char.IsDigit(b[0])) {
                result = Char.ToLowerInvariant(a[0]) - Char.ToLowerInvariant(b[0]);
                if (result != 0) {
                    return result;
                } else {
                    return Compare(a.Substring(1), b.Substring(1));
                }
            }

            // Both strings begin with digit --> parse both numbers
            string aRemainder, bRemainder;
            string aNumberStr = ExtractNumber(a, out aRemainder);
            string bNumberStr = ExtractNumber(b, out bRemainder);
            result = CompareNumberStrings(aNumberStr, bNumberStr);
            if (result != 0) {
                return result;
            }

            // Numbers are the same --> remove numbers and recurse
            return Compare(aRemainder, bRemainder);
        }

        internal static string ExtractNumber(string str, out string remainder)
        {
            string result = "";
            remainder = "";

            for (int i = 0; i < str.Length; i++) {
                if (Char.IsDigit(str[i])) {
                    result += str[i];
                } else {
                    remainder = str.Substring(i);
                    break;
                }
            }

            return result;
        }

        internal static int CompareNumberStrings(string a, string b)
        {
            a = a.TrimStart('0');
            b = b.TrimStart('0');
            if (a.Length != b.Length) {
                return a.Length - b.Length;
            }

            for (int i = 0; i < a.Length; ++i) {
                if (a[i] != b[i]) {
                    return a[i] - b[i];
                }
            }
            return 0;
        }
    }

    public sealed class NaturalFilenameStringComparer : IComparer<string>
    {
        public int Compare(string a, string b)
        {
            return NaturalStringComparer.Compare(a, b);
        }
    }

    public sealed class NaturalFileInfoNameComparer : IComparer<FileInfo>
    {
        public int Compare(FileInfo a, FileInfo b)
        {
            return NaturalStringComparer.Compare(a.Name, b.Name);
        }
    }
}
