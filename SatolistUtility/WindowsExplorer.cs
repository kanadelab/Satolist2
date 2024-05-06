using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Satolist.Utility
{
	//エクスプローラのヘルパ
	public static class WindowsExplorer
	{
		private const string BinaryPath = "explorer.exe";

		//フォルダを開く
		public static void OpenDirectory(string path, bool isNewWindow)
		{
			try
			{
				if (isNewWindow)
				{
					Process.Start(BinaryPath, string.Format(@"/n ""{0}""", path));
				}
				else
				{
					Process.Start(BinaryPath, string.Format(@"""{0}""", path));
				}
			}
			catch { }
		}

		//指定ファイルの親を開き、ファイルを選択状態にする
		public static void OpenDirectoryFileSelect(string path, bool isNewWindow)
		{
			try
			{
				var parent = Path.GetDirectoryName(path);
				if (isNewWindow)
				{
					Process.Start(BinaryPath, string.Format(@"/n /select ""{0}"" ""{1}""", parent, path));
				}
				else
				{
					Process.Start(BinaryPath, string.Format(@"/select ""{0}"" ""{1}""", parent, path));
				}
			}
			catch { }
		}
	}
}
