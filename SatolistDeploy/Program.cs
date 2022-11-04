using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Microsoft.VisualBasic.FileIO;


namespace SatolistDeploy
{
	//さとりすとの出力フォルダから公開に必要なファイルだけ抜き出す
	class Program
	{
		static void Main(string[] args)
		{
			string input = args[0];
			string output = args[1];
			CopyFiles(input, output);
		}

		static void CopyFiles(string inputDirectory, string outputDirectory)
		{
			//必要なアイテム。直接その位置にコピーする
			string[] copyItems =
			{
				"data",
				"Satolist2.exe",
				"readme.txt",
				"licence.txt",
				"MahApps.Metro.dll",
			};

			//必要なアセンブリ。雑多にあるので data/assembly に移動。
			string[] copyAssembly =
			{
				"ca-ES",
				"cs-CZ",
				"de",
				"es",
				"fr",
				"hu",
				"it",
				"ja",
				"ko",
				"nl-BE",
				"pl",
				"pt",
				"ro",
				"ru",
				"sv",
				"zh-Hans",
				"zh-Hant",
				"AvalonDock.dll",
				"AvalonDock.Themes.VS2013.dll",
				"ControlzEx.dll",
				"FluentFTP.dll",
				"ICSharpCode.AvalonEdit.dll",
				"Microsoft.WindowsAPICodePack.dll",
				"Microsoft.WindowsAPICodePack.ExtendedLinguisticServices.dll",
				"Microsoft.WindowsAPICodePack.Sensors.dll",
				"Microsoft.WindowsAPICodePack.Shell.dll",
				"Microsoft.WindowsAPICodePack.ShellExtensions.dll",
				"Microsoft.Xaml.Behaviors.dll",
				"Newtonsoft.Json.dll",
				"SatolistUpdator.exe",
				"AngleSharp.dll",
				"System.Buffers.dll",
				"System.Memory.dll",
				"System.Numerics.Vectors.dll",
				"System.Runtime.CompilerServices.Unsafe.dll",
				"System.Text.Encoding.CodePages.dll"
			};

			foreach(var item in copyItems )
			{
				CopyItem(inputDirectory, outputDirectory, item);
			}

			var assemblyOutput = Path.Combine(outputDirectory, "data", "assembly");
			Directory.CreateDirectory(assemblyOutput);

			foreach(var item in copyAssembly)
			{
				CopyItem(inputDirectory, assemblyOutput, item);
			}
		}

		static void CopyItem(string inputDirectory, string outputDirectory, string itemName)
		{
			var sourcePath = Path.Combine(inputDirectory, itemName);
			var destPath = Path.Combine(outputDirectory, itemName);

			if (Directory.Exists(sourcePath))
			{
				FileSystem.CopyDirectory(sourcePath, destPath, true);
			}
			else if (File.Exists(sourcePath))
			{
				File.Copy(sourcePath, destPath, true);
			}
			else
			{
				throw new FileNotFoundException(sourcePath);
			}
		}

		
	}
}
