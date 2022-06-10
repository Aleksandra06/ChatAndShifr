using System;
using System.Numerics;
using System.Text;

namespace Shifr
{
    public record CryptoInitializers
    {
        public BigInteger P { get; set; }
        public BigInteger Q { get; set; }
        public BigInteger G { get; set; }
    }
    public record CryptoInitializersString
    {
        public string P { get; set; }
        public string Q { get; set; }
        public string G { get; set; }
    }
    public static  class DiffieHellman
    {
        private static CryptoInitializers CyclingSubgroupPowerOfQ(Random random, int keySizeQ, int keySizeP)
        {
            BigInteger q;
            q = ((Func<BigInteger>)(() =>
            {
                var buffer = new byte[keySizeQ];
                while (true)
                {
                    random.NextBytes(buffer);
                    buffer[^1] |= 0x80;

                    var value = new BigInteger(buffer, true);

                    if (value.IsProbablyPrime(random))
                        return value;
                }
            }))();
            //Console.WriteLine($"Q: {q}[{q.GetBitLength()}b]");

            BigInteger p;
            p = ((Func<BigInteger>)(() =>
            {
                var buffer = new byte[keySizeP - keySizeQ];
                while (true)
                {
                    random.NextBytes(buffer);
                    buffer[^1] |= 0x80;

                    var value = (new BigInteger(buffer, true) * q + 1);

                    if (value.IsProbablyPrime(random))
                        return value;
                }
            }))();
            //Console.WriteLine($"P: {p}[{p.GetBitLength()}b]");

            var g = ((Func<BigInteger>)(() =>
            {
                var buffer = new byte[keySizeP - keySizeQ];
                while (true)
                {
                    random.NextBytes(buffer);

                    var r = new BigInteger(buffer, true);
                    var g = BigInteger.ModPow(r, (p - 1) / q, p);

                    if (g > 1 && BigInteger.ModPow(g, q, p) == 1)
                        return g;
                }
            }))();
            //Console.WriteLine($"G: {g}[{g.GetBitLength()}b]");

            return new CryptoInitializers
            {
                P = p,
                Q = q,
                G = g
            };
        }

        public static CryptoInitializers GetOpenParametersAll()
        {
            var rand = new Random();
            var init = CyclingSubgroupPowerOfQ(rand, 32, 128);
            return init;
        }
        public static BigInteger GetCloseKeyX(CryptoInitializers init)
        {
            var rand = new Random();
            var a = ((Func<BigInteger>)(() =>
            {
                var buffer = new byte[(init.P - 1).GetByteCount(true)];
                while (true)
                {
                    rand.NextBytes(buffer);

                    var value = new BigInteger(buffer, true);
                    if (value < init.P - 1 && BigInteger.GreatestCommonDivisor(value, init.P - 1) == 1)
                        return value;
                }
            }))();

            return a;
        }

        public static CryptoInitializersString Converter(CryptoInitializers model)
        {
            var modelString = new CryptoInitializersString()
            {
                G = model.G.ToString(),
                P = model.P.ToString(),
                Q = ""
            };
            return modelString;
        }
        public static CryptoInitializers Converter(CryptoInitializersString model)
        {
            var item = new CryptoInitializers()
            {
                G = new BigInteger(Encoding.ASCII.GetBytes(model.G)),
                P = new BigInteger(Encoding.ASCII.GetBytes(model.P)),
                Q = new BigInteger()
            };
            return item;
        }

        public static BigInteger GetOpenKeyY(CryptoInitializers init, BigInteger x)
        {
            var A = BigInteger.ModPow(init.G, x, init.P);
            return A;
        }
        private static void DiffieHellmanGroup()
        {
            var rand = new Random();

            //Console.WriteLine("\n Diffie Hellman New \n");

            var init = CyclingSubgroupPowerOfQ(rand, 32, 128);

            //Person 1
            var a = ((Func<BigInteger>)(() =>
            {
                var buffer = new byte[(init.P - 1).GetByteCount(true)];
                while (true)
                {
                    rand.NextBytes(buffer);

                    var value = new BigInteger(buffer, true);
                    if (value < init.P - 1 && BigInteger.GreatestCommonDivisor(value, init.P - 1) == 1)
                        return value;
                }
            }))();
            var A = BigInteger.ModPow(init.G, a, init.P);

            //Person 2
            var b = ((Func<BigInteger>)(() =>
            {
                var buffer = new byte[(init.P - 1).GetByteCount(true)];
                while (true)
                {
                    rand.NextBytes(buffer);

                    var value = new BigInteger(buffer, true);
                    if (value < init.P - 1 && BigInteger.GreatestCommonDivisor(value, init.P - 1) == 1)
                        return value;
                }
            }))();
            var B = BigInteger.ModPow(init.G, b, init.P);

            var Zab = BigInteger.ModPow(B, a, init.P);
            var Zba = BigInteger.ModPow(A, b, init.P);

            //Console.Write($"Result Key Zab: {Zab}\n");
            //Console.Write($"Result Key Zba: {Zba}\n");
        }
    }
}
