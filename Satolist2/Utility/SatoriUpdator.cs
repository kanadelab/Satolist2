using Newtonsoft.Json.Linq;
using Satolist2.Dialog;
using Satolist2.Model;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace Satolist2.Utility
{
	class SatoriUpdator
	{
		private const string ReleaseListAPI = @"https://api.github.com/repos/ponapalt/satoriya-shiori/releases";
		
		//TODO: 他にも使うかもしれないし、機能自体は本体と統一できる？
		//でも本体更新は１つだけでいいし、微妙か？
		public static IEnumerable<SatoriReleaseInfo> GetRelease()
		{
			var resultItems = new List<SatoriReleaseInfo>();
			JArray items =null;
			try
			{
				//URLを叩く
				var client = (HttpWebRequest)WebRequest.Create(ReleaseListAPI);
				client.UserAgent = "satolist/2.0";
				var result = new StreamReader(client.GetResponse().GetResponseStream()).ReadToEnd();
				items = JArray.Parse(result);

				if (items != null)
				{
					//ここまでで上位30件が取得できるはず
					foreach (JObject jobj in items)
					{
						var isDraft = jobj.Value<bool>("draft");
						var isPrerelease = jobj.Value<bool>("prerelease");
						var tagName = jobj.Value<string>("tag_name");
						var name = jobj.Value<string>("name");
						var body = jobj.Value<string>("body");

						//bodyを解析してバージョンを取得
						if (isDraft)
							continue;
						if (isPrerelease)
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
							resultItems.Add(new SatoriReleaseInfo()
							{
								ZipURL = downloadUrl,
								Label = name,
								Description = body,
							});
							break;
						}
					}
				}
				return resultItems;
			}
			catch
			{
				throw;
			}
		}

		public static Task UpdateSatori(ProgressDialogViewModel progressViewModel,  SatoriReleaseInfo release, GhostModel ghost)
		{
			return Task.Run(() =>
		   {
			   try
			   {
				   using (var tempDir = new TemporaryDirectory())
				   {
					   //各種のパスを作成
					   var localZipPath = DictionaryUtility.ConbinePath(tempDir.FullPath, "downloaded.zip");
					   var extractDirectoryPath = DictionaryUtility.ConbinePath(tempDir.FullPath, "extract");
					   var extractSatoriPath = DictionaryUtility.ConbinePath(extractDirectoryPath, "satori.dll");
					   var localSatoriPath = DictionaryUtility.ConbinePath(ghost.FullDictionaryPath, "satori.dll");

					   //対象のReeaseをダウンロードして展開
					   try
					   {
						   Application.Current.Dispatcher.Invoke(() => progressViewModel.SetMessage(string.Format("{0} をダウンロード中。", release.Label)));
						   var client = new WebClient();
						   client.DownloadFile(release.ZipURL, localZipPath);
					   }
					   catch
					   {
						   Application.Current.Dispatcher.Invoke(() => progressViewModel.SetMessage("ダウンロードに失敗しました。"));
						   throw;
					   }

					   try
					   {
						   //ダウンロードしたファイルを解凍
						   Application.Current.Dispatcher.Invoke(() => progressViewModel.SetMessage("ダウンロード完了。アーカイブを解凍します。"));
						   ZipFile.ExtractToDirectory(localZipPath, extractDirectoryPath);
					   }
					   catch
					   {
						   Application.Current.Dispatcher.Invoke(() => progressViewModel.SetMessage("アーカイブの解凍に失敗しました。"));
						   throw;
					   }

					   try
					   {
						   //里々があるかを確認
						   if (!File.Exists(extractSatoriPath))
							   throw new Exception();
					   }
					   catch
					   {
						   Application.Current.Dispatcher.Invoke(() => progressViewModel.SetMessage("アーカイブ内に satori.dll が見つからないため続行できませんでした。"));
						   throw;
					   }


					   //ゴーストが起動しているか確認
					   bool ghostExists = SakuraFMOReader.Exists(ghost);

					   //ゴーストが起動している場合には一度Shioriをアンロードする
					   if (ghostExists)
					   {
						   Application.Current.Dispatcher.Invoke(() => progressViewModel.SetMessage("対象のゴーストが起動しているためSSPから里々をアンロードします。"));
						   Satorite.SendSSTP(ghost, @"\![unload,shiori]", true, true);
					   }

					   //アンロードに時間がかかるかもしれないので何度かリトライする
					   bool success = false;
					   for (int i = 0; i < 10; i++)
					   {
						   try
						   {
							   File.Copy(extractSatoriPath, localSatoriPath, true);
							   success = true;
							   Application.Current.Dispatcher.Invoke(() => progressViewModel.SetMessage("ゴーストの里々を上書きしました。"));
							   break;
						   }
						   catch { }
						   Thread.Sleep(1000);
					   }

					   if (!success)
					   {
						   Application.Current.Dispatcher.Invoke(() => progressViewModel.SetMessage("里々の上書きに失敗しました。"));
					   }

					   //アンロードした場合はロードする
					   if (ghostExists)
					   {
						   Application.Current.Dispatcher.Invoke(() => progressViewModel.SetMessage("起動中のゴーストの里々をSSPに再ロードします。"));
						   Satorite.SendSSTP(ghost, @"\![load,shiori]", true, true);
					   }

					   if (success)
					   {
						   Application.Current.Dispatcher.Invoke(() => {
							   progressViewModel.SetMessage("アップデート完了しました。");
							   progressViewModel.SetCompletedProgress();
							   var updatedVersion = Satorite.GetSatoriVersion(ghost);
							   if (!string.IsNullOrEmpty(updatedVersion))
							   {
								   Core.LogMessage.AddLog(string.Format("里々のアップデートを行いました。現在のバージョンは {0} です。", updatedVersion));
							   }
						   });
					   }
					   else
					   {
						   Application.Current.Dispatcher.Invoke(() => { progressViewModel.SetMessage("アップデートに失敗しました。"); progressViewModel.SetFailedProgress(); });
					   }
				   }
			   }
			   catch { }
		   });
		}
	}

	

	public class SatoriReleaseInfo
	{
		//GUI用表示名
		public string Label { get; set; }
		//バイナリデータ
		public string ZipURL { get; set; }
		//コメントボディ
		public string Description { get; set; }
	}
}
