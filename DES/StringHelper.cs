using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DES
{
    public static class StringHelper
    {
        public static IEnumerable<string> SplitIntoNParts(this string source, int n)
        {
            for(int i = 0; i < source.Length; i += n)
            {
                yield return source.Substring(i, n);
            }
        }
    }
}
