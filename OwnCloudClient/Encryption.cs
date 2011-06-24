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
		public static byte[] DecryptBytes(byte[] bytes)
		{
			RijndaelManaged myRijndael = new RijndaelManaged();
			myRijndael.Key = Settings.EncryptionKey;
			myRijndael.IV = Settings.InitilizationVector;

			//Get a decryptor that uses the same key and IV as the encryptor.
			ICryptoTransform decryptor = myRijndael.CreateDecryptor(Settings.EncryptionKey, Settings.InitilizationVector);

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
			myRijndael.Key = Settings.EncryptionKey;
			myRijndael.IV = Settings.InitilizationVector;

			//Get an encryptor.
			ICryptoTransform encryptor = myRijndael.CreateEncryptor(Settings.EncryptionKey, Settings.InitilizationVector);

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
