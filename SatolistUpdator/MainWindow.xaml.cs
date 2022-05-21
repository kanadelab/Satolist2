using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;

namespace SatolistUpdator
{
	/// <summary>
	/// MainWindow.xaml の相互作用ロジック
	/// </summary>
	public partial class MainWindow : Window
	{
		public MainWindow()
		{
			InitializeComponent();
		}
	}

	/*
	 * 
	 * 1. ファイルリストに従ってバックアップをとる
	 * 2. ファイルリストに従ってファイルを削除する
	 * 3. 更新後のファイルをテンポラリに展開する
	 * 4. 更新後のファイルをファイルリストに従って展開する
	 * 
	 */ 


	internal class MainWindowViewModel
	{
		public string[] CurrentFileList;
		public string SatolistDirectory;
		public string BackupDirectory;
		public string ExtractDirectory;

		private string[] ReadFileList(string path)
		{
			return File.ReadAllLines(path);
		}

		//アップデート実行部
		public void Update()
		{
			SatolistDirectory = Environment.CurrentDirectory;	//アセンブリ位置とかでもいいのかな
			CurrentFileList = ReadFileList("files.txt");
			BackupDirectory = MakeTemporaryDirectory();
			ExtractDirectory = MakeTemporaryDirectory();

			//TODO: なにかしらzipのダウンロード

			BackupFiles();
			//ExtractZip();
		}

		public string MakeTemporaryDirectory()
		{
			string dir = null;
			do
			{
				dir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
			} while (Directory.Exists(dir) || File.Exists(dir));
			Directory.CreateDirectory(dir);
			return dir;
		}

		public void BackupFiles()
		{
			foreach(var file in CurrentFileList)
			{
				if (string.IsNullOrEmpty(file))
					continue;
				var filePath = Path.Combine(SatolistDirectory, file);

				//バックアップ先にコピー
				var backupFilePath = Path.Combine(BackupDirectory, file);
				Directory.CreateDirectory(Path.GetDirectoryName(backupFilePath));
				File.Copy(filePath, backupFilePath);
			}
		}

		//zipを解凍
		public void ExtractZip(string zipFile)
		{
			string extractDirectory = null;
			do
			{
				extractDirectory = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
			} while (!Directory.Exists(extractDirectory));
			ZipFile.ExtractToDirectory(zipFile, extractDirectory);

			//解凍先からファイルをコピー

			//ファイルリストをロード
			string[] fileList = ReadFileList(Path.Combine(extractDirectory, "files.txt"));

			foreach(var file in fileList)
			{
				if (string.IsNullOrEmpty(file))
					continue;

				//ファイルを対象にコピー
				var targetFilePath = Path.Combine(SatolistDirectory, file);
				var extractFilePath = Path.Combine(extractDirectory, file);
				Directory.CreateDirectory(Path.GetDirectoryName(targetFilePath));
				File.Copy(extractFilePath, targetFilePath);
			}
		}

		//バックアップ完了後、バックアップ元のファイルを削除
		public void RemovebackupedFiles()
		{

		}

	}


}
