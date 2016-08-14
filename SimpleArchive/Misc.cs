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
    }
}
