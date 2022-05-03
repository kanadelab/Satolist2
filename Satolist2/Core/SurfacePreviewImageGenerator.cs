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

		public SurfacePreviewImageGenerator()
		{
			//適当なセッションIDを用意
			sessionId = (IntPtr)Guid.NewGuid().GetHashCode();
			mutex = new Mutex();
		}

		public void Generate(SurfaceRangeCollection surfaces, MainViewModel main)
		{
			mutex.WaitOne();    //ミューテックス保持で初期化

			Task.Run(() =>
		   {
			   //SSTP登録
			   SSTPCallBackNativeWindow.Instance.RegisterCallback(SSTPCallBackNativeWindow.OperationProgressMessage, OnProgress);
			   
			   try
			   {
				   //FMOから現在のシェル情報を取得する
				   var fmoReader = new SakuraFMOReader();
				   fmoReader.Read();


				   //出力
				   var testScript = string.Format(@"\m[{0},{1},0]", SSTPCallBackNativeWindow.OperationProgressMessage, sessionId.ToString());

				   //SSTP接続性チェック
				   //Task.Run( () =>
				   Satorite.SendSSTP(main.Ghost, testScript, true, SSTPCallBackNativeWindow.Instance.HWnd);
				   //);
				   mutex.WaitOne();

				   
				   try
				   {
					   //シェルのパスを取得
					   mutex.ReleaseMutex();
					   SSTPCallBackNativeWindow.Instance.RegisterCallback(Win32Import.WM_COPYDATA, OnCopyData);
					   main.MainWindow.Dispatcher.Invoke(() => mutex.WaitOne());
					   Satorite.ExecuteSSTP(main.Ghost, "GetProperty", SSTPCallBackNativeWindow.Instance.HWnd);
					   mutex.WaitOne();
				   }
				   catch
				   {
					   throw;
				   }
				   finally
				   {
					   SSTPCallBackNativeWindow.Instance.UnregisterCallback(Win32Import.WM_COPYDATA);
				   }

				   //サーフェス生成
				   //とりあえずかなでさんのサーフェスを手動列挙
				   int[] surfaceList = { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 20, 21, 22, 23, 24, 25, 26, 27 };
				   string targetDirectory = DictionaryUtility.ConbinePath(main.Ghost.FullDictionaryPath, "profile/satolist/surfacepreview");
				   System.IO.Directory.CreateDirectory(targetDirectory);

				   foreach (var id in surfaceList)
				   {
					   //DispacherにMutexを取らせる
					   mutex.ReleaseMutex();
					   main.MainWindow.Dispatcher.Invoke(() => mutex.WaitOne() );

					   //次のスクリプトを実行
					   var generateScript = string.Format(@"\![execute,dumpsurface,{0},{1},{2}]\m[{3},{4},{5}]",
						   targetDirectory,
						   0,
						   id,
						   SSTPCallBackNativeWindow.OperationProgressMessage,
						   sessionId,
						   id);
					   Satorite.SendSSTP(main.Ghost, generateScript, true, SSTPCallBackNativeWindow.Instance.HWnd);
					   mutex.WaitOne();
				   }
			   }
			   catch
			   {
				   throw;
			   }
			   finally
			   {
				   SSTPCallBackNativeWindow.Instance.UnregisterCallback(SSTPCallBackNativeWindow.OperationProgressMessage);
				   
			   }
		   }
			);
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

		}


	}
}
