using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimpleArchive
{
    static class Misc
    {
        // value が二のべき乗数かを調べる。
        public static bool CheckExp2(long value)
        {
            int numBits = 0;
            for (int i = 0; i < (sizeof(long) * 8); ++i)
            {
                if ((value & ((long)1 << i)) != 0)
                {
                    numBits += 1;
                }
            }
            return (numBits == 1);
        }

        // s が ASCII 文字以外を含んでいる場合は EncoderFallbackException をスローする。
        // s が null の場合はスローしない。
        public static void ThrowIfContainsNoneASCIICharacters(string s)
        {
            if (s == null)
            {
                return;
            }

            var encoder = Encoding.ASCII.GetEncoder();
            encoder.Fallback = EncoderFallback.ExceptionFallback;
            char[] chars = s.ToArray();
            // chars に ASCII 文字に変換できないものが含まれている場合 EncoderFallbackException がスローされる
            encoder.GetByteCount(chars, 0, chars.Length, true);
        }
    }
}
