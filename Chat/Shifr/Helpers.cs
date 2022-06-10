using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace Shifr
{
    public static class Helpers
    {
        public static bool IsProbablyPrime(this BigInteger value, Random rand, int witnesses = 10)
        {
            if (value <= 1)
                return false;

            if (witnesses <= 0)
                witnesses = 10;

            var d = value - 1;
            var s = 0;

            while (d % 2 == 0)
            {
                d /= 2;
                s += 1;
            }

            var bytes = new byte[value.ToByteArray().LongLength];

            for (var i = 0; i < witnesses; i++)
            {
                BigInteger a;
                do
                {
                    rand.NextBytes(bytes);

                    a = new BigInteger(bytes, true);
                } while (a < 2 || a >= value - 2);

                var x = BigInteger.ModPow(a, d, value);
                if (x == 1 || x == value - 1)
                    continue;

                for (var r = 1; r < s; r++)
                {
                    x = BigInteger.ModPow(x, 2, value);

                    if (x == 1)
                        return false;
                    if (x == value - 1)
                        break;
                }

                if (x != value - 1)
                    return false;
            }

            return true;
        }
    }
}
