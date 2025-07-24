using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Security;
using System;
using System.Text;

namespace WebApplication1.Helper
{
    public class AESHelperNew
    {
        /// <summary>
        /// 默认算法(AES算法-仅支持16位)
        /// </summary>
        public const string DefaultAlgorithm_AES_CBC_PKCS7Padding = "AES/ECB/PKCS7Padding";

        /// <summary>
        /// 默认算法(DES算法-仅支持8位)
        /// </summary>
        public const string DefaultAlgorithm_DES_CBC_PKCS7Padding = "DES/ECB/PKCS7Padding";

        /// <summary>
        /// AES加密解密Key，Key必须十六位。固定死了。
        /// </summary>
        public const string AESKey = "bixushi16weimima";

        /// <summary>
        ///  AES 加密
        /// </summary>
        /// <param name="plainText"></param>
        /// <param name="key">密钥</param>
        /// <param name="iv">偏移量</param>
        /// <param name="algorithm">算法</param>
        /// <returns></returns>
        public static string Encrypt(string plainText, string key = AESKey, string iv = null, string algorithm = DefaultAlgorithm_AES_CBC_PKCS7Padding)
        {
            // 创建 AES 加密器
            IBufferedCipher cipher = CipherUtilities.GetCipher(algorithm);

            //(AES算法 - 仅支持16位)
            if (algorithm.StartsWith("AES/"))
            {
                key = key.Length > 16 ? key.Substring(0, 16) : key;
                iv = iv != null && iv.Length > 16 ? iv.Substring(0, 16) : iv;
            }
            //(DES算法 - 仅支持8位)
            else if (algorithm.StartsWith("DES/"))
            {
                key = key.Length > 8 ? key.Substring(0, 8) : key;
                iv = iv != null && iv.Length > 8 ? iv.Substring(0, 8) : iv;
            }

            // 初始化加密器
            byte[] keyBytes = Encoding.UTF8.GetBytes(key);
            if (!string.IsNullOrWhiteSpace(iv))
            {
                byte[] ivBytes = Encoding.UTF8.GetBytes(iv);
                cipher.Init(true, new Org.BouncyCastle.Crypto.Parameters.ParametersWithIV(new Org.BouncyCastle.Crypto.Parameters.KeyParameter(keyBytes), ivBytes));
            }
            else
            {
                cipher.Init(true, new Org.BouncyCastle.Crypto.Parameters.KeyParameter(keyBytes));
            }


            // 要加密的数据
            byte[] data = Encoding.UTF8.GetBytes(plainText);

            // 加密数据
            byte[] encryptedData = cipher.DoFinal(data);

            // 输出加密结果
            return Convert.ToBase64String(encryptedData);
        }

        /// <summary>
        ///  AES 解密
        /// </summary>
        /// <param name="encryptText"></param>
        /// <param name="key">密钥</param>
        /// <param name="iv">偏移量</param>
        /// <param name="algorithm">算法</param>
        /// <returns></returns>
        public static string Decrypt(string encryptText, string key = AESKey, string iv = null, string algorithm = DefaultAlgorithm_AES_CBC_PKCS7Padding)
        {
            // 创建 AES 解密器
            IBufferedCipher cipher = CipherUtilities.GetCipher(algorithm);

            //(AES算法 - 仅支持16位)
            if (algorithm.StartsWith("AES/"))
            {
                key = key.Length > 16 ? key.Substring(0, 16) : key;
                iv = iv != null && iv.Length > 16 ? iv.Substring(0, 16) : iv;
            }
            //(DES算法 - 仅支持8位)
            else if (algorithm.StartsWith("DES/"))
            {
                key = key.Length > 8 ? key.Substring(0, 8) : key;
                iv = iv != null && iv.Length > 8 ? iv.Substring(0, 8) : iv;
            }

            // 初始化解密器
            var keyBytes = Encoding.UTF8.GetBytes(key);
            if (!string.IsNullOrWhiteSpace(iv))
            {
                byte[] ivBytes = Encoding.UTF8.GetBytes(iv);
                cipher.Init(false, new Org.BouncyCastle.Crypto.Parameters.ParametersWithIV(new Org.BouncyCastle.Crypto.Parameters.KeyParameter(keyBytes), ivBytes));
            }
            else
            {
                cipher.Init(false, new Org.BouncyCastle.Crypto.Parameters.KeyParameter(keyBytes));
            }

            // 要解密的数据

            byte[] encryptTextArray = Convert.FromBase64String(encryptText);

            // 解密数据
            byte[] decryptedData = cipher.DoFinal(encryptTextArray);

            // 输出解密结果
            return Encoding.UTF8.GetString(decryptedData);
        }
    }
}
