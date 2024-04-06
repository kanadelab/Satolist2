using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Satolist2.Utility
{
	//簡易暗号文字列 一応平文はやめておこうかなって感じの
	internal static class EncryptString
	{
		private static readonly byte[] K = { 0xC0, 0xAF, 0x1D, 0xB7, 0xEF, 0x05, 0xC3, 0x75, 0xDB, 0x1D, 0xB7, 0x8F, 0xE8, 0xEF, 0xB3, 0x76, 0x0B, 0x99, 0x14, 0xCD, 0xD8, 0xB7, 0x17, 0x71, 0xD0, 0x42, 0x60, 0x0E, 0xA7, 0xD2, 0x42, 0x1F };

		public static string Encrypt(string plaintext)
		{
			if (string.IsNullOrEmpty(plaintext))
				return string.Empty;

			using(var aes = Aes.Create())
			{
				aes.KeySize = 256;
				aes.GenerateIV();
				aes.Key = K;

				var enc = aes.CreateEncryptor();
				using(var ms = new MemoryStream())
				{
					ms.Write(aes.IV, 0, 16);	//先頭に初期化ベクトルを格納
					using(var cs = new CryptoStream(ms, enc, CryptoStreamMode.Write))
					{
						using (var writer = new StreamWriter(cs))
						{
							writer.Write(plaintext);
						}
					}
					return Convert.ToBase64String(ms.ToArray());
				}
			}
		}

		public static string Decrypt(string encrypted)
		{
			if (string.IsNullOrEmpty(encrypted))
				return string.Empty;

			var bytes = Convert.FromBase64String(encrypted);
			using(var aes = Aes.Create())
			{
				aes.KeySize = 256;
				aes.Key = K;
				var iv = new byte[16];

				using(var ms = new MemoryStream(bytes))
				{
					ms.Read(iv, 0, 16);
					aes.IV = iv;
					var enc = aes.CreateDecryptor();
					using (var cs = new CryptoStream(ms, enc, CryptoStreamMode.Read))
					{
						using(var reader = new StreamReader(cs))
						{
							return reader.ReadToEnd();
						}
					}
				}
			}
		}

		public static bool DecryptEquals(string e1, string e2)
		{
			return Decrypt(e1) == Decrypt(e2);
		}
	}
}
