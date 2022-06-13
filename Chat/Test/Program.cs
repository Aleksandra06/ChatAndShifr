using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Shifr;

namespace Test
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Hello World!");
            var message = "hello";

            //all
            var pqg = DiffieHellman.GetOpenParametersAll();
            //person 1
            var Xa = DiffieHellman.GetCloseKeyX(pqg);
            var Ya = DiffieHellman.GetOpenKeyY(pqg, Xa);
            //person b
            var Xb = DiffieHellman.GetCloseKeyX(pqg);
            var Yb = DiffieHellman.GetOpenKeyY(pqg, Xb);
            //Z
            var Zab = DiffieHellman.GetZ(Yb, Xa, pqg.P);
            var Zba = DiffieHellman.GetZ(Ya, Xb, pqg.P);
            if (Zba.CompareTo(Zab) != 0)
            {
                Console.WriteLine("Zab != Zba");
                return;
            }

            //a to b
            //var shifrMsg = Encrypt(message, Ya, Zab, pqg.P);
            //var msg = DecoderMsq(shifrMsg, Zab, pqg.P);
            var shifrMsgByte = Encrypt(message, Zab.ToString());
            var shifrMsg = Encoding.Default.GetString(shifrMsgByte);
            var msg = Decrypt(shifrMsgByte, Zab.ToString());
            Console.WriteLine(msg);
        }

        private static string DecoderMsq(string shifrMsg, BigInteger zab, BigInteger P)
        {
            var msgS = JsonSerializer.Deserialize<List<string>>(shifrMsg).First();
            var strM = msgS.Split("XXXKEYXXX");
            var y = new BigInteger(Encoding.ASCII.GetBytes(strM[0]));
            var message = string.Empty;
            var z = zab;
            var list = strM[1].Split(",");
            for (int i = 0; i < list.Length; i++)
            {
                var name = list[i];
                var e = DiffieHellman.Decript(new BigInteger(Encoding.ASCII.GetBytes(name)), z, P);
                var estr = e.ToString();
                message += char.ConvertFromUtf32(int.Parse(e.ToString()));
            }

            return message;
        }

        private static string Encrypt(string message, BigInteger Y, BigInteger Z, BigInteger p)
        {
            message = String.Format("{0}: {1}", "AToB", message);
            var msgList = new List<string>();

            var msg = Y.ToString() + "XXXKEYXXX";
            for (int i = 0; i < message.Length; i++)
            {
                var name = message[i];
                var e = DiffieHellman.Encript((int)name, Z, p);
                msg += e.ToString() + ",";
            }

            msg = msg.Substring(0, msg.Length - 1);
            msgList.Add(msg);


            return JsonSerializer.Serialize(msgList);
        }
        private static byte[] Encrypt(string clearText, string EncryptionKey = "123")
        {

            byte[] clearBytes = Encoding.UTF8.GetBytes(clearText);
            byte[] encrypted;
            using (Aes encryptor = Aes.Create())
            {
                Rfc2898DeriveBytes pdb = new Rfc2898DeriveBytes(EncryptionKey, new byte[] { 0x49, 0x76, 0x61, 0x6e, 0x20, 0x4d, 0x65, 0x64, 0x76, 0x65, 0x64, 0x65, 0x76 }); // еще один плюс шарпа в наличие таких вот костылей.
                encryptor.Key = pdb.GetBytes(32);
                encryptor.IV = pdb.GetBytes(16);
                using (MemoryStream ms = new MemoryStream())
                {
                    using (CryptoStream cs = new CryptoStream(ms, encryptor.CreateEncryptor(), CryptoStreamMode.Write))
                    {
                        cs.Write(clearBytes, 0, clearBytes.Length);
                        cs.Close();
                    }
                    encrypted = ms.ToArray();
                }
            }
            return encrypted;
        }

        private static string Decrypt(byte[] cipherBytes, string EncryptionKey)
        {
            string cipherText = "";
            using (Aes encryptor = Aes.Create())
            {
                Rfc2898DeriveBytes pdb = new Rfc2898DeriveBytes(EncryptionKey, new byte[] { 0x49, 0x76, 0x61, 0x6e, 0x20, 0x4d, 0x65, 0x64, 0x76, 0x65, 0x64, 0x65, 0x76 });
                encryptor.Key = pdb.GetBytes(32);
                encryptor.IV = pdb.GetBytes(16);
                using (MemoryStream ms = new MemoryStream())
                {
                    using (CryptoStream cs = new CryptoStream(ms, encryptor.CreateDecryptor(), CryptoStreamMode.Write))
                    {
                        cs.Write(cipherBytes, 0, cipherBytes.Length);
                        cs.Close();
                    }
                    cipherText = Encoding.UTF8.GetString(ms.ToArray());
                }
            }
            return cipherText;
        }
    }
}
