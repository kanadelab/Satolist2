using Microsoft.VisualBasic.FileIO;
using Satolist2.Utility;
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
	//一時フォルダを使うので理屈上いくつでも起動できるハズ
	internal class TemporaryGhostRuntimeEx : IDisposable
	{
		private ChildProcess process;

		public string FMOName { get; }
		public Model.GhostModel Ghost { get; private set; }
		public TemporaryDirectory RuntimeDirectory { get; private set; }

		private TemporaryGhostRuntimeEx(string fmoName)
		{
			FMOName = fmoName;
		}

		//シェルをダミーゴーストで起動する準備
		public static TemporaryGhostRuntimeEx PrepareShell(string shellPath, string fmoName = SakuraFMOReader.DefaultFMOName)
		{
			try
			{
				//TODO: 古い方TemporaryGhostRuntimeと共存できないので注意
				//一時起動ゴーストを破棄
				Directory.Delete("data/ssp/ghost/temporaryghost", true);
			}
			catch { }

			var runtimeDirectory = new TemporaryDirectory();
			FileSystem.CopyDirectory("data/ssp", Path.Combine(runtimeDirectory.FullPath, "ssp"));

			//ダミーゴーストを展開
			Directory.CreateDirectory(Path.Combine(runtimeDirectory.FullPath, "ssp/ghost"));
			FileSystem.CopyDirectory("data/temporaryghost", Path.Combine(runtimeDirectory.FullPath, "ssp/ghost/temporaryghost"), true);
			Directory.CreateDirectory(Path.Combine(runtimeDirectory.FullPath, "ssp/ghost/temporaryghost/shell"));

			//テンプレートから里々を展開
			File.Copy("data/template/satori.dll", Path.Combine(runtimeDirectory.FullPath, "ssp/ghost/temporaryghost/ghost/master/satori.dll"), true);

			//シェルを展開
			FileSystem.CopyDirectory(shellPath, Path.Combine(runtimeDirectory.FullPath, "ssp/ghost/temporaryghost/shell/master"));

			var obj = new TemporaryGhostRuntimeEx(fmoName)
			{
				Ghost = new Model.GhostModel(Path.Combine(runtimeDirectory.FullPath, "ssp/ghost/temporaryghost")),
				RuntimeDirectory = runtimeDirectory
			};
			return obj;
		}

		public void Boot()
		{
			var startInfo = new ProcessStartInfo();
			startInfo.FileName = Path.Combine( RuntimeDirectory.FullPath, @"ssp\ssp.exe");
			startInfo.Arguments = $"/o bootunlock,standalone,kiosk,dpiaware /f {FMOName} /g temporaryghost";
			startInfo.UseShellExecute = true;
			startInfo.WorkingDirectory = Path.Combine(RuntimeDirectory.FullPath, @"ssp");

			process = new ChildProcess(startInfo);
		}

		public void Dispose()
		{
			try
			{
				process.Dispose();
			}
			catch { }
			RuntimeDirectory.Dispose();
		}
	}
}
