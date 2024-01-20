using Newtonsoft.Json;
using Satolist2.Utility;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace Satolist2.Core
{
	internal class SurfacePreviewImageGenerator
	{
		public const int CurrentVersion = 2;
		public const int OperationProgressMessage = 0x0401;

		private IntPtr sessionId;
		private Mutex mutex;
		private string shellPath;
		private Action<bool> completedCallback;
		private Action<Progress> progressCallback;
		private SSTPCallBackNativeWindow callbackWindow;

		public SurfacePreviewImageGenerator()
		{
			//適当なセッションIDを用意
			sessionId = (IntPtr)Math.Abs(Guid.NewGuid().GetHashCode());
			mutex = new Mutex();
		}

		public Task GenerateShellOnly(MainViewModel main, string shellPath, Action<bool> completedCallback, Action<Progress> progressCallback, CancellationToken cancel)
		{
			this.completedCallback = completedCallback;
			this.progressCallback = progressCallback;

			return Task.Run(() =>
			{
				try
				{
					main.MainWindow.Dispatcher.Invoke(() =>
					progressCallback(new Progress()
					{
						Message = "準備中です...",
						UseProgress = false
					}));

					var targetPath = DictionaryUtility.ConbinePath(shellPath, "profile/satolist/surfacepreview");
					using (var temporaryRuntime = TemporaryGhostRuntimeEx.PrepareShell(shellPath))
					{
						temporaryRuntime.Boot();
						GenerateSurfaceTask(targetPath, temporaryRuntime.Ghost, main, cancel);
					}
				}
				catch
				{
					main.MainWindow.Dispatcher.Invoke(() =>
					progressCallback(new Progress()
					{
						Message = "起動処理に失敗しました。",
						UseProgress = false
					}));
				}
			});
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

		private void GenerateSurfaceTask(string outputPath, Model.GhostModel ghost, MainViewModel main, CancellationToken cancel)
		{
			//テンポラリのフォルダを用意
			string temporaryDumpDirectory = DictionaryUtility.ConbinePath(ghost.FullDictionaryPath, "profile/satolist/surfacepreview");
			try
			{
				System.IO.Directory.Delete(temporaryDumpDirectory, true);
			}
			catch { }
			System.IO.Directory.CreateDirectory(temporaryDumpDirectory);
			System.IO.Directory.CreateDirectory(outputPath);

			main.MainWindow.Dispatcher.Invoke(() =>
			{
				callbackWindow = new SSTPCallBackNativeWindow(main.MainWindow.HWnd);
			});

			try
			{
				//SSTP登録
				using (var operationProgress = callbackWindow.RegisterCallback(OperationProgressMessage, OnProgress))
				{
					using (var copyDataHandler = callbackWindow.RegisterCallback(Win32Import.WM_COPYDATA, OnCopyData))
					{
						main.MainWindow.Dispatcher.Invoke(() => { mutex.WaitOne(); });

						main.MainWindow.Dispatcher.Invoke(() =>
							progressCallback(new Progress()
							{
								Message = "SSPの起動を待機中...",
								UseProgress = false
							}));

						//SSPの起動を待機
						for (int i = 0; i < 10; i++)
						{
							if (SakuraFMOReader.Exists(ghost))
								break;
							Thread.Sleep(2000);
							cancel.ThrowIfCancellationRequested();
						}
						RetryableAction(() => Satorite.ExecuteSSTP(ghost, "GetProperty[currentghost.shelllist.current.path]", callbackWindow.HWnd));
					}

					//シェルデータを読み込んで、さとりすとが出力すべき情報をもってくる
					cancel.ThrowIfCancellationRequested();
					var shell = new LiteSurfaceAnalyzer();
					shell.Load(shellPath);
					cancel.ThrowIfCancellationRequested();

					//さとりすとが選択するサーフェス情報を持ってくる
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
							FrameX = record.SatolistPaletteFrameX,
							FrameY = record.SatolistPaletteFrameY,
							SurfaceTableLabel = record.SurfaceTableLabel,
							Scope = record.SatolistScope,
							Expand = record.SatolistPaletteExpand
						});
					}
					cancel.ThrowIfCancellationRequested();

					//サーフェス生成
					if (System.IO.Directory.Exists(temporaryDumpDirectory))
					{
						System.IO.Directory.Delete(temporaryDumpDirectory, true);
					}
					System.IO.Directory.CreateDirectory(temporaryDumpDirectory);
					cancel.ThrowIfCancellationRequested();

					//ループ中邪魔が入らないようにパッシブモードに入れる
					Satorite.SendSSTP(ghost, @"\![enter,passivemode]", true, true);
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

							//画像出力
							var generateScript = string.Format(@"\![execute,dumpsurface,{0},{1},{2}]\m[{3},{4},{5}]",
								temporaryDumpDirectory,
								scope,
								surfaceId,
								OperationProgressMessage,
								sessionId,
								surfaceId);
							RetryableAction(() => Satorite.SendSSTP(ghost, generateScript, true, true, callbackWindow.HWnd));

							//DispacherにMutexを取らせる
							mutex.ReleaseMutex();
							main.MainWindow.Dispatcher.Invoke(() => mutex.WaitOne());

							//出力した画像をコピー
							var previewFileName = string.Format("surface{0}.png", surfaceId);
							using(var previewBitmap = Bitmap.FromFile(DictionaryUtility.ConbinePath(temporaryDumpDirectory, previewFileName)))
							{
								item.SizeWidth = previewBitmap.Width;
								item.SizeHeight = previewBitmap.Height;
							}
							System.IO.File.Copy(DictionaryUtility.ConbinePath(temporaryDumpDirectory, previewFileName), DictionaryUtility.ConbinePath(outputPath, previewFileName), true);

							//0位置切り出し画像を作成
							generateScript = string.Format(@"\![execute,dumpsurface,{0},{1},{2},surfacezero,,1]\m[{3},{4},{5}]",
								temporaryDumpDirectory,
								scope,
								surfaceId,
								OperationProgressMessage,
								sessionId,
								surfaceId);
							RetryableAction(() => Satorite.SendSSTP(ghost, generateScript, true, true, callbackWindow.HWnd));

							//画像サイズをロード
							var zeroFileName = string.Format("surfacezero{0}.png", surfaceId);
							using (var zeroBitmap = Bitmap.FromFile(DictionaryUtility.ConbinePath(temporaryDumpDirectory, zeroFileName)))
							{
								item.BaseSizeWidth = zeroBitmap.Width;
								item.BaseSizeHeight = zeroBitmap.Height;
							}

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
						Satorite.SendSSTP(ghost, @"\![leave,passivemode]", true, true);
					}

					//掃除
					try
					{
						System.IO.Directory.Delete(temporaryDumpDirectory, true);
					}
					catch { }

					//完了したらjsonに内容を保存
					var metadata = new SurfacePreviewMetaData();
					metadata.Items = generateSurfaces.ToArray();
					metadata.Version = CurrentVersion;
					var metadataPath = DictionaryUtility.ConbinePath(outputPath, "surfaces.json");
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
					if (System.IO.Directory.Exists(temporaryDumpDirectory))
					{
						System.IO.Directory.Delete(temporaryDumpDirectory, true);
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
					Message = "失敗しました: " + ex.ToString(),
					UseProgress = true,
					Value = 0
				}));

				//失敗したら削除
				try
				{
					if (System.IO.Directory.Exists(temporaryDumpDirectory))
					{
						System.IO.Directory.Delete(temporaryDumpDirectory, true);
					}
				}
				catch { }
				main.MainWindow.Dispatcher.Invoke(() => Complete(false));
			}
			finally
			{
				main.MainWindow.Dispatcher.Invoke(() =>
				{
					callbackWindow.Dispose();
				});
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
			var executeResult = Win32Import.CopyDataStructToString(lparam, Win32Import.SSTP_DWDATA);
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
		public const string SurfacePreviewPath = "profile/satolist/surfacepreview";
		public const string SurfacePreviewMetadataPath = "surfaces.json";

		[JsonProperty]
		public SurfacePreviewMetaDataRecord[] Items { get; set; }

		[JsonProperty]
		public int Version { get; set; }

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
		public int FrameX { get; set; }
		[JsonProperty]
		public int FrameY { get; set; }
		[JsonProperty]
		public string SurfaceTableLabel { get; set; }
		[JsonProperty]
		public double Expand { get; set; }
		[JsonProperty]
		public int SizeWidth { get; set; }
		[JsonProperty]
		public int SizeHeight { get; set; }
		//当たり判定マイナス座標を抜いたサイズ
		[JsonProperty]
		public int BaseSizeWidth { get; set; }
		[JsonProperty]
		public int BaseSizeHeight { get; set; }


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
			Expand = 1.0;
			FrameX = 100;
			FrameY = 100;
		}
	}
}
