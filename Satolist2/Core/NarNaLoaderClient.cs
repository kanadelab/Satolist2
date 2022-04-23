using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Satolist2.Core
{
	//ここにななろだクライアントができたらいいなあ
	//ゴーストの一覧とかを出したいのでそもそもサーバ側の実装をなんとかしないといけない
	internal class NarNaLoaderClient
	{
		//微妙なレスポンスの埋め込み方になっちゃったのでなんとかしたいけどまた別のおはなし
		private static readonly Regex ResultRegex = new Regex("<!--result: (.*)--><!--resultcode: (.*)-->");

		public static async Task<NarnaloaderUploadResult> UploadFile(string fileName, string ghostId, NarnaloaderAccount account)
		{
			var boundary = Guid.NewGuid().ToString();
			using (var httpClient = new HttpClient())
			{
				using (var uploadStream = new System.IO.FileStream(fileName,System.IO.FileMode.Open, System.IO.FileAccess.Read, System.IO.FileShare.Read))
				{
					var request = new HttpRequestMessage();
					var multipartContent = new MultipartFormDataContent(boundary);
					multipartContent.Add(new StringContent("upload"), "formtype");
					multipartContent.Add(new StringContent(""), "admin");
					multipartContent.Add(new StringContent(ghostId), "ghost_id");
					multipartContent.Add(new StringContent(Utility.EncryptString.Decrypt(account.password)), "password");
					multipartContent.Add(new StringContent(account.id), "id");
					multipartContent.Add(new StreamContent(uploadStream), "upfile", "upfile.nar");

					var response = await httpClient.PostAsync(account.url, multipartContent);
					if(!response.IsSuccessStatusCode)
					{
						//HTTPエラー
						throw new Exception("HTTPエラー");
					}

					var result = await response.Content.ReadAsByteArrayAsync();
					var resultStr = Utility.Constants.EncodingShiftJis.GetString(result);
					System.Console.WriteLine(resultStr);

					var resultMatch = ResultRegex.Match(resultStr);
					var resultMessage = resultMatch.Groups[1].ToString();
					var resultCode = resultMatch.Groups[2].ToString();

					return new NarnaloaderUploadResult()
					{
						success = resultCode == "success",
						message = resultMessage
					};
				}
			}
		}

	}
	
	public struct NarnaloaderAccount
	{
		public string url;
		public string id;
		public string password;
	}

	public struct NarnaloaderUploadResult
	{
		public bool success;
		public string message;
	}
}
