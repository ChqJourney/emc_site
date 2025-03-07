using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

public class AesEncryption
{
    // 使用 AES-256 需要32字节的密钥
    private static readonly int KeySize = 256;
    private static readonly int BlockSize = 128;
    private static readonly Encoding encoding = Encoding.UTF8;

    public static string Decrypt(string cipherText, string key)
    {
        // Console.WriteLine(cipherText, key);
        try
        {
            byte[] encryptedBytes = Convert.FromBase64String(CleanBase64(cipherText));

            // 提取Salt（OpenSSL格式第二个8字节）
            if (encryptedBytes.Length < 16 || !encryptedBytes.Take(8).SequenceEqual(Encoding.ASCII.GetBytes("Salted__")))
                throw new ArgumentException("非OpenSSL加密格式");

            byte[] salt = encryptedBytes.Skip(8).Take(8).ToArray();
            byte[] cipherData = encryptedBytes.Skip(16).ToArray();

            // 使用OpenSSL密钥派生方式
            var keyDeriv = OpenSSLKeyDerivation(key, salt, 32, 16); // AES-256需要32字节Key+16字节IV
            byte[] aesKey = keyDeriv.Item1;
            byte[] iv = keyDeriv.Item2;

            using (Aes aes = Aes.Create())
            {
                aes.KeySize = KeySize;
                aes.BlockSize = BlockSize;
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;
                aes.Key = aesKey;
                aes.IV = iv;



                using (MemoryStream ms = new MemoryStream(cipherData))
                using (CryptoStream cs = new CryptoStream(ms, aes.CreateDecryptor(), CryptoStreamMode.Read))
                using (StreamReader sr = new StreamReader(cs, Encoding.UTF8))
                {
                    return sr.ReadToEnd();
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Decryption error: {ex.Message}");
            return null;
        }
    }
    private static Tuple<byte[], byte[]> OpenSSLKeyDerivation(string passphrase, byte[] salt, int keyLength, int ivLength)
    {
        // OpenSSL的EVP_BytesToKey密钥派生算法
        var md5 = MD5.Create();
        byte[] passBytes = Encoding.UTF8.GetBytes(passphrase);
        byte[] combined = passBytes.Concat(salt).ToArray();

        byte[] hash1 = md5.ComputeHash(combined);
        byte[] hash2 = md5.ComputeHash(hash1.Concat(combined).ToArray());
        byte[] hash3 = md5.ComputeHash(hash2.Concat(combined).ToArray());

        byte[] result = hash1.Concat(hash2).Concat(hash3).ToArray();
        return Tuple.Create(
            result.Take(keyLength).ToArray(),
            result.Skip(keyLength).Take(ivLength).ToArray()
        );
    }
    private static string CleanBase64(string input)
    {
        // 1. 替换URL安全Base64字符
        input = input.Replace('-', '+').Replace('_', '/');

        // 2. 移除非Base64字符（保留字母、数字、+/=）
        input = Regex.Replace(input, @"[^A-Za-z0-9+/=]", "");

        // 3. 补足填充符
        switch (input.Length % 4)
        {
            case 2: input += "=="; break;
            case 3: input += "="; break;
        }

        return input;
    }
    public static string EncryptOpenSSL(string plainText, string password)
    {
        // 生成随机Salt（8字节，符合OpenSSL规范）
        byte[] salt = new byte[8];
        using (var rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(salt);
        }

        // 通过密码派生密钥和IV
        var keyDeriv = OpenSSLKeyDerivation(password, salt, 32, 16);
        byte[] aesKey = keyDeriv.Item1;
        byte[] iv = keyDeriv.Item2;

        // 执行加密
        byte[] encryptedBytes;
        using (Aes aes = Aes.Create())
        {
            aes.KeySize = 256;
            aes.BlockSize = 128;
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;
            aes.Key = aesKey;
            aes.IV = iv;

            using (MemoryStream ms = new MemoryStream())
            {
                // 写入OpenSSL头 "Salted__"+salt
                ms.Write(Encoding.ASCII.GetBytes("Salted__"), 0, 8);
                ms.Write(salt, 0, salt.Length);

                using (CryptoStream cs = new CryptoStream(ms, aes.CreateEncryptor(), CryptoStreamMode.Write))
                {
                    byte[] plainBytes = Encoding.UTF8.GetBytes(plainText);
                    cs.Write(plainBytes, 0, plainBytes.Length);
                    cs.FlushFinalBlock();
                }
                encryptedBytes = ms.ToArray();
            }
        }

        // 返回Base64编码结果
        return Convert.ToBase64String(encryptedBytes);
    }
}