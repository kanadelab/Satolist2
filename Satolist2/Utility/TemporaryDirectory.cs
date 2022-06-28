using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Satolist2.Utility
{
	//テンポラリディレクトリをusing内でおわらせられるもの
	public class TemporaryDirectory : IDisposable
	{
		public string FullPath { get; private set; }
		public TemporaryDirectory()
		{
			FullPath = DictionaryUtility.ConbinePath(System.IO.Path.GetTempPath(), "satolist2", Guid.NewGuid().ToString());
			System.IO.Directory.CreateDirectory(FullPath);
		}

		public void Dispose()
		{
			try
			{
				//削除は失敗しても仕方ないという感じにしておく
				//そのうちクリーンアップされる判断
				System.IO.Directory.Delete(FullPath, true);
			}
			catch { }
		}

		//文字列扱いで結合やらができるよう
		public override string ToString()
		{
			return FullPath;
		}
	}

	public class TemporaryFile : IDisposable
	{
		public string FullPath { get; private set; }
		public TemporaryFile(bool pathOnly = false)
		{
			FullPath = System.IO.Path.GetTempFileName();
			if(pathOnly)
			{
				//パスだけ欲しいので一時的に生成したファイルは削除
				//上書き保存をサポートしてない場合など
				System.IO.File.Delete(FullPath);
			}

			Console.WriteLine("tempFile: " + FullPath);
		}

		public void Dispose()
		{
			try
			{
				System.IO.File.Delete(FullPath);
			}
			catch { }
		}

		public override string ToString()
		{
			return FullPath;
		}
	}
}
