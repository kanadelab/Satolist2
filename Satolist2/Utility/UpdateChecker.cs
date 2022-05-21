using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace Satolist2.Utility
{
	//さとりすとのリリースデータに対するクライアント
	public static class UpdateChecker
	{
		private const string ReleaseListAPI = @"https://api.github.com/repos/kanadelab/Satolist2/releases";
		private static readonly Regex ReleaseIndexPattern = new Regex(@"\[networkupdate:([0-9]+)\]");
		public static UpdateReleaseInfo GetRelease(bool acceptPreRelease)
		{
			try
			{
				//URLを叩く
				var client = (HttpWebRequest)WebRequest.Create(ReleaseListAPI);
				client.UserAgent = "satolist/2.0";
				var result = new StreamReader(client.GetResponse().GetResponseStream()).ReadToEnd();
				var items = JArray.Parse(result);

				//ここまでで上位30件が取得できるはず…。
				//ドラフトを取り除くのと、プレリリースを許容するかどうかで最新を抜き出す(ドラフトはアクセス権がいるらしいけど一応)
				foreach (JObject jobj in items)
				{
					var isDraft = jobj.Value<bool>("draft");
					var isPrerelease = jobj.Value<bool>("prerelease");
					var tagName = jobj.Value<string>("tag_name");
					var name = jobj.Value<string>("name");
					var body = jobj.Value<string>("body");
					var versionMatch = ReleaseIndexPattern.Match(body);
					long versionIndex;

					//bodyを解析してバージョンを取得
					if (isDraft)
						continue;
					if (isPrerelease && !acceptPreRelease)
						continue;
					if (!versionMatch.Success)
						continue;
					if (!long.TryParse(versionMatch.Groups[1].Value, out versionIndex))
						continue;

					//収集する
					JArray assets = (JArray)jobj["assets"];
					foreach (JObject asset in assets)
					{
						var fileName = asset.Value<string>("name");
						if (Path.GetExtension(fileName) != ".zip")
							continue;
						//URLを決定
						var downloadUrl = asset.Value<string>("browser_download_url");

						//確定
						return new UpdateReleaseInfo()
						{
							ZipURL = downloadUrl,
							TagName = tagName,
							Label = name,
							Description = body,
							VersionIndex = versionIndex
						};
					}
				}

				//continueされなければ正しいはず
				return null;
			}
			catch
			{
				return null;
			}
		}
	}

	public class UpdateReleaseInfo
	{
		//GUI用表示名
		public string Label { get; set; }
		//リリースタグ名
		public string TagName { get; set; }
		//バイナリデータ
		public string ZipURL { get; set; }
		//コメントボディ
		public string Description { get; set; }
		//アップデート番号(単純に大きいと新しい)
		public long VersionIndex { get; set; }
	}
}
