using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

namespace HeartOfTheNight.Hung
{
    /// <summary>
    /// Mã hóa save (AES-256-CBC + HMAC-SHA256). Chống sửa JSON tay.
    /// Key nằm trong build — không chống được người reverse game.
    /// </summary>
    public static class SaveCrypto
    {
        public const string Header = "HOTN1.";

        [Serializable]
        private class CloudEnvelope
        {
            public string enc;
        }

        private static byte[] _aesKey;
        private static byte[] _macKey;

        public static string Seal(string plainJson)
        {
            if (plainJson == null)
                plainJson = "";

            EnsureKeys();
            byte[] plain = Encoding.UTF8.GetBytes(plainJson);
            byte[] iv = new byte[16];
            using (var rng = RandomNumberGenerator.Create())
                rng.GetBytes(iv);

            byte[] cipher;
            using (var aes = Aes.Create())
            {
                aes.Key = _aesKey;
                aes.IV = iv;
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;
                using (var encryptor = aes.CreateEncryptor())
                    cipher = encryptor.TransformFinalBlock(plain, 0, plain.Length);
            }

            byte[] mac = ComputeMac(iv, cipher);
            var packed = new byte[iv.Length + mac.Length + cipher.Length];
            Buffer.BlockCopy(iv, 0, packed, 0, iv.Length);
            Buffer.BlockCopy(mac, 0, packed, iv.Length, mac.Length);
            Buffer.BlockCopy(cipher, 0, packed, iv.Length + mac.Length, cipher.Length);
            return Header + Convert.ToBase64String(packed);
        }

        public static string WrapForCloud(string plainJson)
        {
            return JsonUtility.ToJson(new CloudEnvelope { enc = Seal(plainJson) });
        }

        public static bool TryGetPlainJson(string stored, out string json)
        {
            json = null;
            if (string.IsNullOrWhiteSpace(stored))
                return false;

            stored = stored.Trim().TrimStart('\uFEFF');
            if (IsQuotedJsonString(stored))
                stored = UnquoteJsonString(stored);

            if (LooksLikeEnvelope(stored))
            {
                try
                {
                    var envelope = JsonUtility.FromJson<CloudEnvelope>(stored);
                    if (envelope != null && !string.IsNullOrEmpty(envelope.enc))
                        stored = envelope.enc.Trim();
                }
                catch
                {
                    return false;
                }
            }

            if (stored.StartsWith(Header, StringComparison.Ordinal))
                return TryOpenSealed(stored, out json);

            if (stored.StartsWith("{", StringComparison.Ordinal) || stored.StartsWith("[", StringComparison.Ordinal))
            {
                json = stored;
                return true;
            }

            return false;
        }

        public static GameData ParseGameData(string stored)
        {
            if (!TryGetPlainJson(stored, out string json))
                return null;
            return JsonUtility.FromJson<GameData>(json);
        }

        public static void OverwriteGameData(string stored, GameData target)
        {
            if (target == null)
                throw new ArgumentNullException(nameof(target));
            if (!TryGetPlainJson(stored, out string json))
                throw new InvalidDataException("Save is corrupt, tampered, or not a Heart Of The Night save.");
            JsonUtility.FromJsonOverwrite(json, target);
        }

        private static bool TryOpenSealed(string sealedText, out string json)
        {
            json = null;
            byte[] packed;
            try
            {
                packed = Convert.FromBase64String(sealedText.Substring(Header.Length));
            }
            catch
            {
                return false;
            }

            const int ivLen = 16;
            const int macLen = 32;
            if (packed == null || packed.Length <= ivLen + macLen)
                return false;

            byte[] iv = new byte[ivLen];
            byte[] mac = new byte[macLen];
            int cipherLen = packed.Length - ivLen - macLen;
            byte[] cipher = new byte[cipherLen];
            Buffer.BlockCopy(packed, 0, iv, 0, ivLen);
            Buffer.BlockCopy(packed, ivLen, mac, 0, macLen);
            Buffer.BlockCopy(packed, ivLen + macLen, cipher, 0, cipherLen);

            EnsureKeys();
            if (!FixedEquals(mac, ComputeMac(iv, cipher)))
                return false;

            try
            {
                using (var aes = Aes.Create())
                {
                    aes.Key = _aesKey;
                    aes.IV = iv;
                    aes.Mode = CipherMode.CBC;
                    aes.Padding = PaddingMode.PKCS7;
                    using (var decryptor = aes.CreateDecryptor())
                    {
                        byte[] plain = decryptor.TransformFinalBlock(cipher, 0, cipher.Length);
                        json = Encoding.UTF8.GetString(plain);
                        return !string.IsNullOrEmpty(json);
                    }
                }
            }
            catch
            {
                return false;
            }
        }

        private static bool LooksLikeEnvelope(string stored)
        {
            return stored.StartsWith("{", StringComparison.Ordinal)
                   && stored.IndexOf("\"enc\"", StringComparison.Ordinal) >= 0
                   && stored.IndexOf(Header, StringComparison.Ordinal) >= 0;
        }

        private static bool IsQuotedJsonString(string stored)
        {
            return stored.Length >= 2 && stored[0] == '"' && stored[stored.Length - 1] == '"';
        }

        private static string UnquoteJsonString(string quoted)
        {
            var inner = quoted.Substring(1, quoted.Length - 2);
            return inner.Replace("\\\\", "\\").Replace("\\\"", "\"");
        }

        private static byte[] ComputeMac(byte[] iv, byte[] cipher)
        {
            var data = new byte[iv.Length + cipher.Length];
            Buffer.BlockCopy(iv, 0, data, 0, iv.Length);
            Buffer.BlockCopy(cipher, 0, data, iv.Length, cipher.Length);
            using (var hmac = new HMACSHA256(_macKey))
                return hmac.ComputeHash(data);
        }

        private static bool FixedEquals(byte[] a, byte[] b)
        {
            if (a == null || b == null || a.Length != b.Length)
                return false;
            int diff = 0;
            for (int i = 0; i < a.Length; i++)
                diff |= a[i] ^ b[i];
            return diff == 0;
        }

        private static void EnsureKeys()
        {
            if (_aesKey != null)
                return;

            // Salt cố định: cùng key mọi máy → Google cloud sync được. Không gắn device id.
            byte[] material = Encoding.UTF8.GetBytes(
                "HeartOfTheNight|MauriceTeam|save-v1|7f3c9a2e-b41d-4e08-9c6a-hotn-slot");
            byte[] macMaterial = Concat(Encoding.UTF8.GetBytes("mac|"), material);
            using (var sha = SHA256.Create())
            {
                _aesKey = sha.ComputeHash(material);
                _macKey = sha.ComputeHash(macMaterial);
            }
        }

        private static byte[] Concat(byte[] a, byte[] b)
        {
            var all = new byte[a.Length + b.Length];
            Buffer.BlockCopy(a, 0, all, 0, a.Length);
            Buffer.BlockCopy(b, 0, all, a.Length, b.Length);
            return all;
        }
    }
}
