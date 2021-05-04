using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Live.com_Сombiner
{

    #region

    using System;
    using System.IO;
    using System.Text;

    using Org.BouncyCastle.Crypto;
    using Org.BouncyCastle.Crypto.Engines;
    using Org.BouncyCastle.Crypto.Modes;
    using Org.BouncyCastle.Crypto.Parameters;
    using Org.BouncyCastle.Security;

    #endregion

    public interface IEncryptionService
    {
        /// <summary>
        /// Simple Decryption & Authentication (AES-GCM) of a UTF8 Message
        /// </summary>
        /// <param name="encryptedMessage">The encrypted message.</param>
        /// <param name="key">The base 64 encoded 256 bit key.</param>
        /// <param name="nonSecretPayloadLength">Length of the optional non-secret payload.</param>
        /// <returns>Decrypted Message</returns>
        string DecryptWithKey(string encryptedMessage, string key, int nonSecretPayloadLength = 0);

        /// <summary>
        /// Simple Encryption And Authentication (AES-GCM) of a UTF8 string.
        /// </summary>
        /// <param name="messageToEncrypt">The string to be encrypted.</param>
        /// <param name="key">The base 64 encoded 256 bit key.</param>
        /// <param name="nonSecretPayload">Optional non-secret payload.</param>
        /// <returns>
        /// Encrypted Message
        /// </returns>
        /// <exception cref="System.ArgumentException">Secret Message Required!;secretMessage</exception>
        /// <remarks>
        /// Adds overhead of (Optional-Payload + BlockSize(16) + Message +  HMac-Tag(16)) * 1.33 Base64
        /// </remarks>
        string EncryptWithKey(string messageToEncrypt, string key, string keyId, byte[] nonSecretPayload = null);

        /// <summary>
        /// Helper that generates a random new key on each call.
        /// </summary>
        /// <returns>Base 64 encoded string</returns>
        string NewKey();
    }

    public class EncryptionService : IEncryptionService
    {
        #region  Wizard

        public static byte[] StringToByteArray(string hex)
        {
            return Enumerable.Range(0, hex.Length)
                .Where(x => x % 2 == 0)
                .Select(x => Convert.ToByte(hex.Substring(x, 2), 16))
                .ToArray();
        }
        public static string ByteArrayToString(byte[] ba)
        {
            StringBuilder hex = new StringBuilder(ba.Length * 2);
            foreach (byte b in ba)
                hex.AppendFormat("{0:x2}", b);
            return hex.ToString();
        }




        #endregion



        #region Constants and Fields

        private const int DEFAULT_KEY_BIT_SIZE = 256;
        private const int DEFAULT_MAC_BIT_SIZE = 128;
        private const int DEFAULT_NONCE_BIT_SIZE = 128;

        private readonly int _keySize;
        private readonly int _macSize;
        private readonly int _nonceSize;

        private readonly SecureRandom _random;

        #endregion

        #region Constructors and Destructors

        public EncryptionService()
            : this(DEFAULT_KEY_BIT_SIZE, DEFAULT_MAC_BIT_SIZE, DEFAULT_NONCE_BIT_SIZE)
        { }

        public EncryptionService(int keyBitSize, int macBitSize, int nonceBitSize)
        {
            _random = new SecureRandom();

            _keySize = keyBitSize;
            _macSize = macBitSize;
            _nonceSize = nonceBitSize;
        }

        #endregion

        #region Public Methods and Operators

        /// <summary>
        /// Simple Encryption And Authentication (AES-GCM) of a UTF8 string.
        /// </summary>
        /// <param name="messageToEncrypt">The string to be encrypted.</param>
        /// <param name="key">The base 64 encoded 256 bit key.</param>
        /// <param name="keyId">Sosi chlen</param>
        /// <param name="nonSecretPayload">Optional non-secret payload.</param>
        /// <returns>
        /// Encrypted Message
        /// </returns>
        /// <exception cref="System.ArgumentException">Secret Message Required!;secretMessage</exception>
        /// <remarks>
        /// Adds overhead of (Optional-Payload + BlockSize(16) + Message +  HMac-Tag(16)) * 1.33 Base64
        /// </remarks>
        public string EncryptWithKey(string messageToEncrypt, string key, string keyId, byte[] nonSecretPayload = null)
        {
            if (string.IsNullOrEmpty(messageToEncrypt))
            {
                throw new ArgumentException("Secret Message Required!", "messageToEncrypt");
            }

            var decodedKey = Convert.FromBase64String(key);

            var plainText = Encoding.UTF8.GetBytes(messageToEncrypt);
            var cipherText = EncryptWithKey(plainText, decodedKey, keyId, nonSecretPayload);
            return Convert.ToBase64String(cipherText);
        }

        /// <summary>
        /// Helper that generates a random new key on each call.
        /// </summary>
        /// <returns>Base 64 encoded string</returns>
        public string NewKey()
        {
            var key = new byte[_keySize / 8];
            _random.NextBytes(key);
            return Convert.ToBase64String(key);
        }

        #endregion

        #region Methods

        public byte[] EncryptWithKey(byte[] messageToEncrypt, byte[] keyBytes, string keyId, byte[] nonSecretPayload = null)
        {

            var key = new byte[32];
            _random.NextBytes(key);

            //User Error Checks
            CheckKey(key);

            //Non-secret Payload Optional
            nonSecretPayload = nonSecretPayload ?? new byte[] { };



            //Using random nonce large enough not to repeat
            var nonce = new byte[12];// new byte[_nonceSize / 8]; /// equals IV
                                     //  _random.NextBytes(nonce, 0, nonce.Length);

            var cipher = new GcmBlockCipher(new AesEngine());
            var parameters = new AeadParameters(new KeyParameter(key), _macSize, nonce, nonSecretPayload);

            cipher.Init(true, parameters);

            //Generate Cipher Text With Auth Tag
            var cipherText = new byte[cipher.GetOutputSize(messageToEncrypt.Length)];

            var len = cipher.ProcessBytes(messageToEncrypt, 0, messageToEncrypt.Length, cipherText, 0);
            cipher.DoFinal(cipherText, len);



            var encryptedKey = Sodium.SealedPublicKeyBox.Create(key, keyBytes);
            var bytesOfLen = ToBytes((short)encryptedKey.Length); // ToBytes = BitConverter.GetBytes(short);
            var info = new byte[] { 1, byte.Parse(keyId) };

            //Output auth tag
            var authTag = cipher.GetMac();


            // Console.WriteLine($"Cipher text (hex): {ByteArrayToString(cipherText)}");
            // Console.WriteLine($"Cipher tag (hex): {ByteArrayToString(authTag)}");

            var temp = cipherText;
            cipherText = new byte[messageToEncrypt.Length];
            for (int i = 0; i < cipherText.Length; i++)
            {
                cipherText[i] = temp[i];
            }
            // Console.WriteLine($"Cipher text after cut (hex): {ByteArrayToString(cipherText)}");


            //Assemble Message
            byte[] result;
            using (var combinedStream = new MemoryStream())
            {
                using (var binaryWriter = new BinaryWriter(combinedStream))
                {
                    //   info.Concat(bytesOfLen).
                    // Concat(encryptedKey).
                    // Concat(tag).
                    // Concat(cipherText); // Concat means that concat two array

                    binaryWriter.Write(info);
                    binaryWriter.Write(bytesOfLen);
                    binaryWriter.Write(encryptedKey);
                    binaryWriter.Write(authTag);
                    binaryWriter.Write(cipherText);
                    //Assemble Message
                    //Prepend Authenticated Payload
                    // binaryWriter.Write(nonSecretPayload);
                    // //Prepend Nonce
                    // binaryWriter.Write(nonce);
                    // //Write Cipher Text
                    // binaryWriter.Write(cipherText);
                }
                result = combinedStream.ToArray();
            }

            // string Info = "======== Bytes Info [sosi]=======\n";
            // Info+= $"Payload: {nonSecretPayload.Length}  ({ByteArrayToString(nonSecretPayload)})\n";
            // Info += $"Key: {ByteArrayToString(keyBytes)}\n";
            // Info += $"KeyId: {keyId}\n";
            //
            // Info += $"info: {info.Length} ({ByteArrayToString(info)})\n";
            // Info += $"bytesOfLen: {bytesOfLen.Length} ({ByteArrayToString(bytesOfLen)}) [{(short)key.Length}]\n";
            // Info += $"encryptedKey: {encryptedKey.Length} ({ByteArrayToString(encryptedKey)})\n";
            // Info += $"auth_tag: {authTag.Length} ({ByteArrayToString(authTag)})\n";
            // Info += $"cipherText: {cipherText.Length} ({ByteArrayToString(cipherText)})\n\n";
            // Info += $"Total: {result.Length} ({ByteArrayToString(result)})\n";
            // Info += "===========================\n";
            //
            // Console.WriteLine(Info);


            return result;
        }

        public static byte[] ToBytes(short sh)
        {
            return BitConverter.GetBytes(sh);
        }

        private void CheckKey(byte[] key)
        {
            if (key == null || key.Length != _keySize / 8)
            {
                throw new ArgumentException(String.Format("Key needs to be {0} bit! actual:{1}", _keySize, key?.Length * 8), "key");
            }
        }

        public string DecryptWithKey(string encryptedMessage, string key, int nonSecretPayloadLength = 0)
        {
            throw new NotImplementedException();
        }

        #endregion

        public static string GetEncryptPassword(string password, string public_key, string key_id, string version)
        {
            string time = JSTime(true);
            var crypt = new EncryptionService();
            var encryptPassword = crypt.EncryptWithKey(password,
                Convert.ToBase64String(StringToByteArray(public_key)), key_id,
                Encoding.UTF8.GetBytes(time));

            return $"#PWD_INSTAGRAM_BROWSER:{version}:{time}:{encryptPassword}"; ;
        }

        #region UnixTime
        public static string JSTime(bool cut = false)
        {
            try
            {
                string t = DateTime.UtcNow
                   .Subtract(new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc))
                   .TotalMilliseconds.ToString();

                if (t.Contains(","))
                    t = t.Substring(0, t.IndexOf(','));

                if (cut && t.Length > 10) t = t.Remove(t.Length - 3, 3);

                return t;
            }
            catch { }

            return "";
        }
        #endregion
    }

}
