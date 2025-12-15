using System.Security.Cryptography;
using System.Text;

namespace EventSync_Manager.Services;

public class EncryptionService
{
    public static byte[] GenerateAesKey()
    {
        using var aes = Aes.Create();
        aes.KeySize = 256;
        aes.GenerateKey();
        return aes.Key;
    }

    public static byte[] EncryptData(byte[] data, byte[] key)
    {
        // Генерируем случайный nonce (12 байт для GCM)
        var nonce = new byte[12];
        using (var rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(nonce);
        }

        // Используем AesGcm для шифрования
        using var aesGcm = new AesGcm(key);
        var tag = new byte[16]; // Authentication tag для GCM
        var ciphertext = new byte[data.Length];

        aesGcm.Encrypt(nonce, data, ciphertext, tag);

        // Формат: nonce (12) + ciphertext + tag (16)
        var result = new byte[12 + ciphertext.Length + 16];
        Array.Copy(nonce, 0, result, 0, 12);
        Array.Copy(ciphertext, 0, result, 12, ciphertext.Length);
        Array.Copy(tag, 0, result, 12 + ciphertext.Length, 16);

        return result;
    }

    public static byte[] DecryptData(byte[] encryptedData, byte[] key)
    {
        if (encryptedData.Length < 28) // минимум 12 (nonce) + 0 (data) + 16 (tag)
            throw new ArgumentException("Недостаточно данных для расшифровки");

        // Извлекаем компоненты
        var nonce = new byte[12];
        Array.Copy(encryptedData, 0, nonce, 0, 12);

        var tag = new byte[16];
        Array.Copy(encryptedData, encryptedData.Length - 16, tag, 0, 16);

        var ciphertext = new byte[encryptedData.Length - 28];
        Array.Copy(encryptedData, 12, ciphertext, 0, ciphertext.Length);

        // Расшифровываем
        using var aesGcm = new AesGcm(key);
        var plaintext = new byte[ciphertext.Length];
        aesGcm.Decrypt(nonce, ciphertext, tag, plaintext);

        return plaintext;
    }

    public static string ComputeSha256Hash(byte[] data)
    {
        using var sha256 = SHA256.Create();
        var hash = sha256.ComputeHash(data);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    public static string ComputeSha256Hash(string data)
    {
        return ComputeSha256Hash(Encoding.UTF8.GetBytes(data));
    }

    public static byte[] SignData(byte[] data, byte[] privateKey)
    {
        using var rsa = RSA.Create();
        rsa.ImportRSAPrivateKey(privateKey, out _);
        return rsa.SignData(data, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
    }

    public static bool VerifySignature(byte[] data, byte[] signature, byte[] publicKey)
    {
        using var rsa = RSA.Create();
        rsa.ImportRSAPublicKey(publicKey, out _);
        return rsa.VerifyData(data, signature, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
    }

    public static (byte[] PrivateKey, byte[] PublicKey) GenerateRsaKeyPair()
    {
        using var rsa = RSA.Create(2048);
        return (rsa.ExportRSAPrivateKey(), rsa.ExportRSAPublicKey());
    }
}

