using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Security.Cryptography;
using System.IO;

namespace OwnCloudClient
{
	public static class Encryption
	{
		private static readonly byte[] key = new byte[] { 147, 122, 247, 25, 250, 51, 36, 252, 154, 68, 83, 252, 47, 87, 43, 7, 198, 247, 152, 134, 187, 141, 143, 72, 244, 54, 193, 66, 210, 109, 100, 71 };
		private static readonly byte[] IV = new byte[] { 93, 55, 86, 74, 27, 224, 42, 162, 169, 110, 253, 250, 202, 169, 194, 221 };

		public static byte[] DecryptBytes(byte[] bytes)
		{
			RijndaelManaged myRijndael = new RijndaelManaged();
			myRijndael.Key = key;
			myRijndael.IV = IV;

			//Get a decryptor that uses the same key and IV as the encryptor.
			ICryptoTransform decryptor = myRijndael.CreateDecryptor(key, IV);

			//Now decrypt the previously encrypted message using the decryptor obtained in the above step.
			MemoryStream msDecrypt = new MemoryStream(bytes);
			CryptoStream csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read);

			//Read the data out of the crypto stream.
			byte[] decryptedBytes = new byte[bytes.Length];
			int bytesRead = csDecrypt.Read(decryptedBytes, 0, decryptedBytes.Length);
			return decryptedBytes.Take(bytesRead).ToArray();		
		}

		public static byte[] DecryptFile(string fileName)
		{
			return DecryptBytes(System.IO.File.ReadAllBytes(fileName));
		}

		public static byte[] EncryptBytes(byte[] bytes)
		{
			RijndaelManaged myRijndael = new RijndaelManaged();
			myRijndael.Key = key;
			myRijndael.IV = IV;

			//Get an encryptor.
			ICryptoTransform encryptor = myRijndael.CreateEncryptor(key, IV);

			//Encrypt the data.
			MemoryStream msEncrypt = new MemoryStream();
			CryptoStream csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write);

			//Write all data to the crypto stream and flush it.
			csEncrypt.Write(bytes, 0, bytes.Length);
			csEncrypt.FlushFinalBlock();

			//Get encrypted array of bytes.
			return msEncrypt.ToArray();
		}

		public static byte[] EncryptFile(string fileName)
		{
			return EncryptBytes(System.IO.File.ReadAllBytes(fileName));
		}
	}
}
