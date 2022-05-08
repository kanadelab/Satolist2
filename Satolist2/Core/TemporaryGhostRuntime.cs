using Microsoft.VisualBasic.FileIO;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Satolist2.Core
{
	//SSPでゴーストを一時的にだけ起動するための仕組み
	internal class TemporaryGhostRuntime : IDisposable
	{
		private static bool isBooted;	//1つしか起動できないので起動中かどうか
		private Process process;
		public Model.GhostModel Ghost { get; private set; }

		private TemporaryGhostRuntime()
		{
			if (isBooted)
				throw new Exception();  //すでに起動中
		}

		//ゴーストを起動する準備
		public static TemporaryGhostRuntime PrepareGhost(string ghostPath)
		{
			try
			{
				//一時起動ゴーストを破棄
				Directory.Delete("data/ssp/ghost/temporaryghost");
			}
			catch { }

			Directory.CreateDirectory("data/ssp/ghost");
			FileSystem.CopyDirectory(ghostPath, "data/ssp/ghost/temporaryghost");

			var obj = new TemporaryGhostRuntime()
			{
				Ghost = new Model.GhostModel("data/ssp/ghost/temporaryghost")
			};
			return obj;
		}

		//シェルをダミーゴーストで起動する準備
		public static TemporaryGhostRuntime PrepareShell(string shellPath)
		{
			try
			{
				//一時起動ゴーストを破棄
				Directory.Delete("data/ssp/ghost/temporaryghost", true);
			}
			catch { }

			//ダミーゴーストを展開
			Directory.CreateDirectory("data/ssp/ghost");
			FileSystem.CopyDirectory("data/temporaryghost", "data/ssp/ghost/temporaryghost", true);
			Directory.CreateDirectory("data/ssp/ghost/temporaryghost/shell");

			//テンプレートから里々を展開
			File.Copy("data/template/satori.dll", "data/ssp/ghost/temporaryghost/ghost/master/satori.dll");

			//シェルを展開
			FileSystem.CopyDirectory(shellPath, "data/ssp/ghost/temporaryghost/shell/master");

			var obj = new TemporaryGhostRuntime()
			{
				Ghost = new Model.GhostModel("data/ssp/ghost/temporaryghost")
			};
			return obj;
		}

		public void Boot()
		{
			isBooted = true;
			var startInfo = new ProcessStartInfo();
			startInfo.FileName = Path.GetFullPath(@"data\ssp\ssp.exe");
			//startInfo.Arguments = "/g temporaryghost /o nobootcheck";
			startInfo.UseShellExecute = true;
			startInfo.WorkingDirectory = Path.GetFullPath(@"data\ssp");

			process = Process.Start(startInfo);
		}

		public void Dispose()
		{
			try
			{
				process.Kill();
			}
			catch { }
			try
			{
				//一時起動ゴーストを破棄
				Directory.Delete("data/ssp/ghost/temporaryghost");
			}
			catch { }
			isBooted = false;
		}
	}
}
