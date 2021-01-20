using System;
using System.IO;
using System.Security.Cryptography;

namespace Mikodev.Network
{
    internal static class LinkCrypto
    {
        internal const int _Key = 32;

        internal const int _Block = 16;

        internal readonly static Random s_ran = new Random();

        public static byte[] GetKey()
        {
            var buf = new byte[_Key];
            lock (s_ran)
                s_ran.NextBytes(buf);
            return buf;
        }

        public static byte[] GetBlock()
        {
            var buf = new byte[_Block];
            lock (s_ran)
                s_ran.NextBytes(buf);
            return buf;
        }

        public static byte[] Encrypt(this AesManaged aes, byte[] buf) => Transform(buf, 0, buf.Length, aes.CreateEncryptor());

        public static byte[] Decrypt(this AesManaged aes, byte[] buf) => Transform(buf, 0, buf.Length, aes.CreateDecryptor());

        internal static byte[] Transform(byte[] buffer, int offset, int count, ICryptoTransform tramsform)
        {
            var mst = new MemoryStream();
            using (var cst = new CryptoStream(mst, tramsform, CryptoStreamMode.Write))
                cst.Write(buffer, offset, count);
            return mst.ToArray();
        }
    }
}
