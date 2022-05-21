using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace UpdateFileListGenerator
{
	//ファイルリストを単純な形で出力する
	class Program
	{
		static void Main(string[] args)
		{
			var items = System.IO.Directory.GetFiles(Environment.CurrentDirectory, "*", SearchOption.AllDirectories);
			List<string> files = new List<string>();

			Uri currentPath = new Uri(Environment.CurrentDirectory + @"\");

			foreach(var item in items)
			{
				Uri filePath = new Uri(item);
				Uri relativePath = currentPath.MakeRelativeUri(filePath);
				string outputPath = relativePath.ToString();
				files.Add(outputPath);
			}

			File.WriteAllLines(args[0], files.ToArray());
		}
	}
}
