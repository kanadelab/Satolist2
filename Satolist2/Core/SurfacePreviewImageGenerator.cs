using Newtonsoft.Json;
using Satolist2.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Satolist2.Core
{
	internal class SurfacePreviewImageGenerator
	{
		private IntPtr sessionId;
		private Mutex mutex;
		private string shellPath;
		private Action<bool> completedCallback;
		private Action<Progress> progressCallback;

		public SurfacePreviewImageGenerator()
		{
			//適当なセッションIDを用意
			sessionId = (IntPtr)Math.Abs(Guid.NewGuid().GetHashCode());
			mutex = new Mutex();
		}

		public Task Generate(MainViewModel main, Action<bool> completedCallback, Action<Progress> progressCallback, CancellationToken cancel)
		{
			this.completedCallback = completedCallback;
			this.progressCallback = progressCallback;
			return Task.Run(() => GenerateSurfaceTask(main, cancel) );
		}

		private void RetryableAction(Action action)
		{
			//タイムアウト
			for(int i = 0; i < 3; i++)
			{
				action.Invoke();
				if(mutex.WaitOne(3000))
				{
					return;
				}
			}
			//有効なレスポンスがない
			throw new Exception();
		}

		private void GenerateSurfaceTask(MainViewModel main, CancellationToken cancel)
		{
			string targetDirectory = DictionaryUtility.ConbinePath(main.Ghost.FullDictionaryPath, "profile/satolist/surfacepreview");
			try
			{
				//SSTP登録
				using (var operationProgress = SSTPCallBackNativeWindow.Instance.RegisterCallback(SSTPCallBackNativeWindow.OperationProgressMessage, OnProgress))
				{
					using (var copyDataHandler = SSTPCallBackNativeWindow.Instance.RegisterCallback(Win32Import.WM_COPYDATA, OnCopyData))
					{
						main.MainWindow.Dispatcher.Invoke(() => { mutex.WaitOne(); });
						RetryableAction(() => Satorite.ExecuteSSTP(main.Ghost, "GetProperty[currentghost.shelllist.current.path]", SSTPCallBackNativeWindow.Instance.HWnd));
					}

					//シェルデータを読み込んで、さとりすとが出力すべき情報をもってくる
					cancel.ThrowIfCancellationRequested();
					var shell = new LiteSurfaceAnalyzer();
					shell.Load(shellPath);
					cancel.ThrowIfCancellationRequested();

					//さとりすとが選択するサーフェス情報を持ってくる
					//TODO: scope対応
					var generateSurfaces = new List<SurfacePreviewMetaDataRecord>();
					foreach (var id in shell.SurfaceIDList)
					{
						var record = shell.Records[id];
						if (!record.SatolistViewerVisible && !record.SatolistPaletteVisible)
							continue;
						if (record.IsImageFileOnly)
							continue;
						generateSurfaces.Add(new SurfacePreviewMetaDataRecord()
						{
							Id = id,
							IsEnableSurfacePalette = record.SatolistPaletteVisible,
							IsEnableSurfaceViewer = record.SatolistViewerVisible,
							OffsetX = record.SatolistPaletteOffsetX,
							OffsetY = record.SatolistPaletteOffsetY,
							SurfaceTableLabel = record.SurfaceTableLabel,
							Scope = 0
						});
					}
					cancel.ThrowIfCancellationRequested();

					//サーフェス生成
					if (System.IO.Directory.Exists(targetDirectory))
					{
						System.IO.Directory.Delete(targetDirectory, true);
					}
					System.IO.Directory.CreateDirectory(targetDirectory);
					cancel.ThrowIfCancellationRequested();

					//ループ中邪魔が入らないようにパッシブモードに入れる
					Satorite.SendSSTP(main.Ghost, @"\![enter,passivemode]", true, true);
					try
					{
						//foreach (var item in generateSurfaces)
						for (var i = 0; i < generateSurfaces.Count; i++)
						{
							cancel.ThrowIfCancellationRequested();
							var item = generateSurfaces[i];

							//出力対象のサーフェスIdとスコープ
							var surfaceId = item.Id;
							var scope = item.Scope;

							//DispacherにMutexを取らせる
							mutex.ReleaseMutex();
							main.MainWindow.Dispatcher.Invoke(() => mutex.WaitOne());

							//次のスクリプトを実行
							var generateScript = string.Format(@"\![execute,dumpsurface,{0},{1},{2}]\m[{3},{4},{5}]",
								targetDirectory,
								scope,
								surfaceId,
								SSTPCallBackNativeWindow.OperationProgressMessage,
								sessionId,
								surfaceId);
							RetryableAction(() => Satorite.SendSSTP(main.Ghost, generateScript, true, true, SSTPCallBackNativeWindow.Instance.HWnd));

							//進捗の通知
							var progressMessage = string.Format("({1}/{2}) surface{0}.png", surfaceId, i+1, generateSurfaces.Count);
							main.MainWindow.Dispatcher.Invoke(() =>
							progressCallback(new Progress()
							{
								Message = progressMessage,
								UseProgress = true,
								Value = ((double)i / (double)generateSurfaces.Count) * 100.0
							}));
						}
					}
					catch
					{
						throw;
					}
					finally
					{
						//パッシブモード解除
						Satorite.SendSSTP(main.Ghost, @"\![leave,passivemode]", true, true);
					}

					//完了したらjsonに内容を保存
					var metadata = new SurfacePreviewMetaData();
					metadata.Items = generateSurfaces.ToArray();
					var metadataPath = DictionaryUtility.ConbinePath(targetDirectory, "surfaces.json");
					JsonUtility.SerializeToFile(metadataPath, metadata);
				}
				main.MainWindow.Dispatcher.Invoke(() => Complete(true));
			}
			catch(OperationCanceledException)
			{
				main.MainWindow.Dispatcher.Invoke(() =>
				progressCallback(new Progress()
				{
					Message = "キャンセルしました。",
					UseProgress = true,
					Value = 0
				}));

				//失敗したら削除
				try
				{
					if (System.IO.Directory.Exists(targetDirectory))
					{
						System.IO.Directory.Delete(targetDirectory, true);
					}
				}
				catch { }
				main.MainWindow.Dispatcher.Invoke(() => Complete(false));
			}
			catch(Exception ex)
			{
				main.MainWindow.Dispatcher.Invoke(() =>
				progressCallback(new Progress()
				{
					Message = "失敗しました: " + ex.Message,
					UseProgress = true,
					Value = 0
				}));

				//失敗したら削除
				try
				{
					if (System.IO.Directory.Exists(targetDirectory))
					{
						System.IO.Directory.Delete(targetDirectory, true);
					}
				}
				catch { }
				main.MainWindow.Dispatcher.Invoke(() => Complete(false));
			}
		}

		//タスク完了時の呼び出し
		public void Complete(bool succes)
		{
			completedCallback?.Invoke(succes);
		}


		public void OnProgress(int msg, IntPtr wparam, IntPtr lparam)
		{
			//セッションIDが一致しない情報は無効
			if (wparam != sessionId)
				return;

			//進捗があった場合にミューテックスを開放
			mutex.ReleaseMutex();
		}

		public void OnCopyData(int msg, IntPtr wparam, IntPtr lparam)
		{
			//Executeの戻り、今回のケースだとシェルのパスが来るはず
			var executeResult = Win32Import.CopyDataStructToString(lparam);
			var response = new ProtocolBuilder();
			response.Deserialize(executeResult);

			//シェルのパスを格納
			shellPath = DictionaryUtility.NormalizePath(response.AppendData);

			mutex.ReleaseMutex();
		}
	}

	[JsonObject]
	internal class SurfacePreviewMetaData
	{
		[JsonProperty]
		public SurfacePreviewMetaDataRecord[] Items { get; set; }

		public SurfacePreviewMetaData()
		{
			Items = Array.Empty<SurfacePreviewMetaDataRecord>();
		}
	}

	[JsonObject]
	internal class SurfacePreviewMetaDataRecord
	{
		[JsonProperty]
		public bool IsEnableSurfaceViewer { get; set; }
		[JsonProperty]
		public bool IsEnableSurfacePalette { get; set; }
		[JsonProperty]
		public long Id { get; set; }
		[JsonProperty]
		public int Scope { get; set; }
		[JsonProperty]
		public int OffsetX { get; set; }
		[JsonProperty]
		public int OffsetY { get; set; }
		[JsonProperty]
		public string SurfaceTableLabel { get; set; }

		[JsonIgnore]
		public string FileName
		{
			get
			{
				return string.Format("surface{0}.png", Id);
			}
		}

		[JsonIgnore]
		public string Label
		{
			get
			{
				if (string.IsNullOrEmpty(SurfaceTableLabel))
					return Id.ToString();
				else
					return string.Format("{0} [{1}]", Id, SurfaceTableLabel);
			}
		}

		public SurfacePreviewMetaDataRecord()
		{
			OffsetX = 0;
			OffsetY = 0;
		}
	}
}
