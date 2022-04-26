using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Satolist2.Utility
{
	//SSP起動機能
	public static class SSPBootUtility
	{
		//ssp.exeを検索して起動
		public static string Boot(string ghostPath)
		{
			var path = ghostPath;
			while(true)
			{
				path = System.IO.Directory.GetParent(path)?.FullName;
				if (string.IsNullOrEmpty(path))
				{
					throw new System.IO.FileNotFoundException();
				}

				var executablePath = DictionaryUtility.ConbinePath(path, "ssp.exe");
				if(System.IO.File.Exists(executablePath))
				{
					//起動
					Boot(ghostPath, executablePath);

					//起動に使用したパスを返す
					return executablePath;
				}
			}
		}

		//ssp.exeを直接指定して起動
		public static void Boot(string ghostPath, string executablePath)
		{
			var startInfo = new ProcessStartInfo();
			startInfo.FileName = executablePath;
			startInfo.WorkingDirectory = System.IO.Directory.GetParent(executablePath).FullName;
			startInfo.UseShellExecute = true;
			startInfo.Arguments = string.Format("/o nobootcheck /g {0}", ghostPath);
			Process.Start(startInfo);
		}
	}
}
