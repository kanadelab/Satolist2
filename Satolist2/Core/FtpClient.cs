using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentFTP;

namespace Satolist2.Core
{
	//FTPクライアント
	public class FtpClient : IDisposable
	{
		private List<FtpRequestBase> requestQueue;
		private Task runningTask;

		public FluentFTP.FtpClient Implement { get; }
		public bool ConnectFailed;

		public FtpClient(FtpAccount account)
		{
			requestQueue = new List<FtpRequestBase>();
			Implement = new FluentFTP.FtpClient(account.url);
			Implement.Credentials = new System.Net.NetworkCredential(account.username, Utility.EncryptString.Decrypt(account.password));
			Implement.EncryptionMode = FtpEncryptionMode.Auto;
			Implement.UploadDataType = FtpDataType.Binary;
			Implement.DownloadDataType = FtpDataType.Binary;
		}

		//同期実行
		//キューにコマンドが残ってたらまずいかも？
		public void ExecuteCommand(FtpRequestBase request)
		{
			request.Execute(this);
		}

		public void Connect()
		{
			Implement.Connect();
		}

		public void UploadFile(string localPath, string remotePath)
		{
			//上書き、フォルダ自動生成
			var bytes = System.IO.File.ReadAllBytes(localPath);

			//5回までリトライ
			for (int i = 0; i < 5; i++)
			{
				var status = Implement.Upload(bytes, remotePath, FtpRemoteExists.Overwrite, true);
				if (status == FtpStatus.Success)
					return;
			}
			throw new Exception("アップロードに失敗しました。");
		}

		public byte[] DownloadFile(string remotePath)
		{
			byte[] resultBytes;
			bool result = Implement.Download(out resultBytes, remotePath);
			if (!result)
				throw new Exception("ダウンロードに失敗しました。");

			return resultBytes;
		}

		//非同期実行
		//こちらで非同期するべきではないかも…
		public Task ExecuteCommandAsync(FtpRequestBase request)
		{
			lock(this)
			{
				//キューイング
				requestQueue.Add(request);

				//今の所実行されてなければ新しくタスクを作る
				if (runningTask == null)
				{
					runningTask = Task.Run(
					() =>
					{
						while (true)
						{
							FtpRequestBase item = null;
							lock (this)
							{
								if (requestQueue.Count > 0)
								{
									item = requestQueue[0];
									requestQueue.RemoveAt(0);
								}
								else
								{
									//タスクの終了
									runningTask = null;
									break;
								}
							}

							//接続してなかったら接続する
							if(!Implement.IsConnected)
							{
								Implement.Connect();
							}

							//コマンドの実行
							item.Execute(this);
						}
					}
					);
				}
			}

			return runningTask;
		}

		public void Dispose()
		{
			//ここは待つ必要はないはず
			Implement.DisconnectAsync();
		}
	}

	public abstract class FtpRequestBase
	{
		public delegate void Finished(FtpRequestBase request);
		public event Finished OnFinished;
		
		public bool IsSucceed { get; protected set; }

		public virtual void Execute(FtpClient client)
		{
			OnFinished?.Invoke(this);
		}
	}

	public class FtpFileListRequest : FtpRequestBase
	{
		public string RemotePath { get; set; }
		public FluentFTP.FtpListItem[] Result { get; private set; }
		public string ResultCurrentPath { get; private set; }

		public FtpFileListRequest()
		{ }

		public FtpFileListRequest(string path)
		{
			RemotePath = path;
		}

		public override void Execute(FtpClient client)
		{
			try
			{
				ResultCurrentPath = client.Implement.GetWorkingDirectory();
				Result = client.Implement.GetListing(RemotePath);
				IsSucceed = true;
			}
			catch
			{
				IsSucceed = false;
			}
			
			base.Execute(client);
		}
	}

	public struct FtpAccount
	{
		public string url;
		public string username;
		public string password;
	}
}
