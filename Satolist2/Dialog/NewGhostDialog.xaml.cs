using Microsoft.Win32;
using Microsoft.WindowsAPICodePack.Dialogs;
using Satolist2.Utility;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
using System.Windows.Shapes;

namespace Satolist2.Dialog
{
	/// <summary>
	/// NewGhostDialog.xaml の相互作用ロジック
	/// </summary>
	public partial class NewGhostDialog : DialogContentBase
	{
		internal new NewGhostDialogViewModel DataContext
		{
			get => (NewGhostDialogViewModel)base.DataContext;
			set => base.DataContext = value;
		}

		internal NewGhostDialog(MainViewModel main)
		{
			InitializeComponent();
			Owner = main.MainWindow.RootWindow;
			DataContext = new NewGhostDialogViewModel(this);
		}
	}

	internal class NewGhostDialogViewModel : NotificationObject
	{
		private NewGhostDialog dialog;

		private ObservableCollection<NewGhostDialogGhostTemplateViewModel> templates;
		private NewGhostDialogGhostTemplateViewModel selectedTemplate;
		private NewGhostDialogDictionaryViewModel selectedTemplateDictionaryFile;
		private string descriptionText;

		private string createPath;
		private string createName;
		private bool isUseDefaultImportPath;
		private string importSatoriPath;
		private string importMasterShellPath;

		private string defaultImportSatoriPath;
		private string defaultImportMasterShellPath;

		//作成する場所
		public string CreatePath
		{
			get => createPath;
			set
			{
				createPath = value;
				NotifyChanged();
				NotifyChanged(nameof(IsValidIOPath));
			}
		}

		//ゴーストフォルダ名
		public string CreateName
		{
			get => createName;
			set
			{
				createName = value;
				NotifyChanged();
				NotifyChanged(nameof(IsValidIOPath));
			}
		}

		//結局の出力パス
		public string EffectiveCreatePath
		{
			get => DictionaryUtility.ConbinePath(
				DictionaryUtility.NormalizeFullPath(CreatePath),
				CreateName);
		}

		//デフォルト設定を使う
		public bool IsUseDefaultImportPath
		{
			get => isUseDefaultImportPath;
			set
			{
				if (isUseDefaultImportPath != value)
				{
					isUseDefaultImportPath = value;
					NotifyChanged();
					if(isUseDefaultImportPath)
					{
						ImportSatoriPath = defaultImportSatoriPath;
						ImportMasterShellPath = defaultImportMasterShellPath;
					}
				}
			}
		}

		//里々パス
		public string ImportSatoriPath
		{
			get => importSatoriPath;
			set
			{
				importSatoriPath = value;
				NotifyChanged();
				NotifyChanged(nameof(IsValidIOPath));
			}
		}

		//マスターシェルパス
		public string ImportMasterShellPath
		{
			get => importMasterShellPath;
			set
			{
				importMasterShellPath = value;
				NotifyChanged();
				NotifyChanged(nameof(IsValidIOPath));
			}
		}

		public bool IsValidIOPath
		{
			get
			{
				return
					!string.IsNullOrEmpty(CreatePath) &&
					!string.IsNullOrEmpty(ImportSatoriPath) &&
					!string.IsNullOrEmpty(ImportMasterShellPath) &&
					!string.IsNullOrEmpty(CreateName);
			}
		}

		public ActionCommand OpenTargetSelectDialogCommand { get; }
		public ActionCommand OpenImportSatoriSelectDialogCommand { get; }
		public ActionCommand OpenImportMasterShellSelectDialogCommand { get; }
		public ActionCommand CancelCommand { get; }
		public ActionCommand SelectPathPageNextCommand { get; }
		public ActionCommand TemplateSelectorPageBackCommand { get; }
		public ActionCommand CreateGhostCommand { get; }

		public CustomizedReadOnlyObservableCollection<NewGhostDialogGhostTemplateViewModel> Templates
		{
			get => new CustomizedReadOnlyObservableCollection<NewGhostDialogGhostTemplateViewModel>(templates);
		}

		//選択中のテンプレート
		public NewGhostDialogGhostTemplateViewModel SelectedTemplate
		{
			get => selectedTemplate;
			set
			{
				selectedTemplate = value;
				if(selectedTemplate != null)
					DescriptionText = selectedTemplate.Model.ReadMe;
				NotifyChanged();
			}
		}

		//選択中のチェックボックス項目
		public NewGhostDialogDictionaryViewModel SelectedTemplateDictionaryFile
		{
			get => selectedTemplateDictionaryFile;
			set
			{
				selectedTemplateDictionaryFile = value;
				if(selectedTemplateDictionaryFile != null)
					DescriptionText = selectedTemplateDictionaryFile.Model.Description;
				NotifyChanged();
			}
		}

		//説明ペインのテキスト
		public string DescriptionText
		{
			get => descriptionText;
			set
			{
				descriptionText = value;
				NotifyChanged();
			}
		}

		public NewGhostDialogViewModel(NewGhostDialog dialog)
		{
			this.dialog = dialog;

			templates = new ObservableCollection<NewGhostDialogGhostTemplateViewModel>();
			isUseDefaultImportPath = true;

			defaultImportSatoriPath = DictionaryUtility.NormalizeFullPath("data/template/satori.dll");
			defaultImportMasterShellPath = DictionaryUtility.NormalizeFullPath("data/template/shell/master");

			importSatoriPath = defaultImportSatoriPath;
			ImportMasterShellPath = defaultImportMasterShellPath;

			//インポートなしテンプレート
			var emptyTemplate = NewGhostDialogGhostTemplateViewModel.EmptyTempalte;
			SelectedTemplate = emptyTemplate;
			templates.Add(emptyTemplate);

			//組み込みテンプレートをロードする
			var templateDirs = System.IO.Directory.GetDirectories("data/template/template");
			foreach (var item in templateDirs)
			{
				try
				{
					var t = new NewGhostDialogGhostTemplateViewModel(DictionaryUtility.NormalizeFullPath(item));
					templates.Add(t);
				}
				catch { }
			}

			//ユーザーテンプレートをロードする
			try
			{
				var userTemplateDirs = System.IO.Directory.GetDirectories("template");
				foreach (var item in userTemplateDirs)
				{
					try
					{
						var t = new NewGhostDialogGhostTemplateViewModel(DictionaryUtility.NormalizeFullPath(item));
						templates.Add(t);
					}
					catch { }
				}
			}
			catch { }

			//コマンド準備
			OpenTargetSelectDialogCommand = new ActionCommand(
				o =>
				{
					var path = OpenFileSelectDialog(CreatePath, null, null, true);
					if (!string.IsNullOrEmpty(path))
						CreatePath = path;
				}
				);

			OpenImportSatoriSelectDialogCommand = new ActionCommand(
				o =>
				{
					var path = OpenFileSelectDialog(ImportSatoriPath, "里々モジュール(satori.dll)", "dll");
					if (!string.IsNullOrEmpty(path))
						ImportSatoriPath = path;
				}
				);

			OpenImportMasterShellSelectDialogCommand = new ActionCommand(
				o =>
				{
					var path = OpenFileSelectDialog(ImportMasterShellPath, null, null, false, true);
					if (!string.IsNullOrEmpty(path))
						ImportMasterShellPath = path;
				}
				);

			CancelCommand = new ActionCommand(
				o =>
				{
					dialog.Close();
				}
				);

			SelectPathPageNextCommand = new ActionCommand(
				o =>
				{
					if (ValidatePathSettings())
					{
						dialog.SelectPathPage.Visibility = Visibility.Collapsed;
						dialog.TemplateSelectorPage.Visibility = Visibility.Visible;
					}
				}
				);

			TemplateSelectorPageBackCommand = new ActionCommand(
				o =>
				{
					dialog.SelectPathPage.Visibility = Visibility.Visible;
					dialog.TemplateSelectorPage.Visibility = Visibility.Collapsed;
				}
				);

			CreateGhostCommand = new ActionCommand(
				o =>
				{
					var result = MessageBox.Show("この設定でゴーストを新規作成します。", "ゴーストの新規作成", MessageBoxButton.OKCancel, MessageBoxImage.Question);
					if(result == MessageBoxResult.OK)
					{
						try
						{
							CreateNewGhost(EffectiveCreatePath, SelectedTemplate, ImportMasterShellPath, ImportSatoriPath);
							dialog.DialogResult = true;
							dialog.Close();
						}
						catch
						{
							MessageBox.Show("新規作成に失敗しました。", "ゴーストの新規作成", MessageBoxButton.OK, MessageBoxImage.Exclamation);
						}
					}
				}
				);
		}

		private string OpenFileSelectDialog(string currentPath, string fileFilterLabel, string fileFilterExtension, bool selectCreateDirectory = false, bool selectMasterShell = false)
		{
			var fileDialog = new CommonOpenFileDialog();
			

			if(fileFilterLabel != null)
				fileDialog.Filters.Add(new CommonFileDialogFilter(fileFilterLabel, fileFilterExtension));

			if (selectCreateDirectory || selectMasterShell)
			{
				fileDialog.IsFolderPicker = true;
				fileDialog.DefaultFileName = string.Empty;
				fileDialog.InitialDirectory = DictionaryUtility.NormalizeWindowsPath(currentPath);
			}
			else
			{
				fileDialog.DefaultFileName = System.IO.Path.GetFileName(currentPath);
				fileDialog.InitialDirectory = System.IO.Path.GetDirectoryName(currentPath);
			}

			if (fileDialog.ShowDialog() == CommonFileDialogResult.Ok)
			{
				return DictionaryUtility.NormalizeFullPath(fileDialog.FileName);
			}
			return null;
		}

		//各種設定に問題ないかチェック、問題がある場合はメッセージを表示
		private bool ValidatePathSettings()
		{
			if(System.IO.File.Exists(EffectiveCreatePath))
			{
				MessageBox.Show("すでに存在するフォルダには新規作成できません。");
				return false;
			}

			if(!DictionaryUtility.IsValidFilePath(EffectiveCreatePath))
			{
				MessageBox.Show("「作成する場所」のファイルパスが誤っています。");
				return false;
			}
			if(!System.IO.File.Exists(ImportSatoriPath))
			{
				MessageBox.Show("「里々本体」が指定されたパスに見つかりません。");
				return false;
			}

			if(!System.IO.Directory.Exists(ImportMasterShellPath))
			{
				MessageBox.Show("「マスターシェル」が指定されたパスに見つかりません。");
				return false;
			}

			return true;
		}

		//テンプレートを使用してゴーストを新規作成
		private void CreateNewGhost(string path, NewGhostDialogGhostTemplateViewModel template, string shellPath, string satoriPath)
		{
			//作成先が存在してないことを確認
			if (System.IO.File.Exists(path))
			{
				throw new Exception("ファイルが既に存在してます。");
			}

			try
			{
				//ベーステンプレートをコピー
				var baseTemplatePath = DictionaryUtility.NormalizeFullPath("data/template/base");
				var baseTemplateFiles = System.IO.Directory.GetFiles(baseTemplatePath, "*.*", System.IO.SearchOption.AllDirectories);
				foreach(var item in baseTemplateFiles)
				{
					var sourceFullPath = DictionaryUtility.NormalizePath(item);
					var relativePath = DictionaryUtility.MakeRelativePath(baseTemplatePath, sourceFullPath);
					var destPath = DictionaryUtility.ConbinePath(path, relativePath);
					System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(destPath));
					System.IO.File.Copy(sourceFullPath, destPath);
				}

				//辞書の作成
				var dictionaryPath = DictionaryUtility.ConbinePath(path, "ghost/master");

				//里々をコピー
				System.IO.Directory.CreateDirectory(dictionaryPath);
				System.IO.File.Copy(satoriPath, DictionaryUtility.ConbinePath(dictionaryPath, "satori.dll"), true);

				//ファイルをコピー
				foreach (var file in template.GenerateGhostFiles)
				{
					var destFullPath = DictionaryUtility.ConbinePath(dictionaryPath, file.RelativePath);
					System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(destFullPath));
					System.IO.File.Copy(file.FullPath, destFullPath, true);
				}

				//マスターシェルの作成
				var masterShellPath = DictionaryUtility.ConbinePath(path, "shell/master");
				System.IO.Directory.CreateDirectory(masterShellPath);

				var files = System.IO.Directory.GetFiles(shellPath, "*.*", System.IO.SearchOption.AllDirectories);
				foreach (var file in files)
				{
					var normalizedSourcePath = DictionaryUtility.NormalizePath(file);
					var relativePath = DictionaryUtility.MakeRelativePath(shellPath, normalizedSourcePath);
					var destPath = DictionaryUtility.ConbinePath(masterShellPath, relativePath);
					System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(destPath));
					System.IO.File.Copy(file, destPath, true);
				}
			}
			catch
			{
				try
				{
					//中途半端に作成されたファイルを消して再度投げる
					System.IO.Directory.Delete(path, true);
				}
				catch { }
				throw;
			}
		}
	}

	//ゴーストテンプレートデータのViewModel
	internal class NewGhostDialogGhostTemplateViewModel : NotificationObject
	{
		private ObservableCollection<NewGhostDialogDictionaryViewModel> dictionaries;

		public Model.GhostTemplateModel Model { get; private set; }
		public CustomizedReadOnlyObservableCollection<NewGhostDialogDictionaryViewModel> Dictionaries
		{
			get => new CustomizedReadOnlyObservableCollection<NewGhostDialogDictionaryViewModel>(dictionaries);
		}

		public IEnumerable<Model.GhostTemplateModel.GhostTemplateFileModel> GenerateGhostFiles
		{
			get
			{
				foreach(var item in Model.Files)
				{
					if (item.IsCheckable)
					{
						//里々の辞書ファイルの場合は使用有無のチェックボックスがあるので確認
						var viewModel = Dictionaries.First(o => ReferenceEquals(o.Model, item));
						if (viewModel.IsChecked)
							yield return item;
					}
					else
					{
						yield return item;
					}
				}
			}
		}

		public static NewGhostDialogGhostTemplateViewModel EmptyTempalte => new NewGhostDialogGhostTemplateViewModel() { Model = Satolist2.Model.GhostTemplateModel.EmptyTemplate };

		private NewGhostDialogGhostTemplateViewModel()
		{
			Model = null;
			dictionaries = new ObservableCollection<NewGhostDialogDictionaryViewModel>();
		}

		public NewGhostDialogGhostTemplateViewModel(string templatePath)
		{
			Model = new Model.GhostTemplateModel(templatePath);
			dictionaries = new ObservableCollection<NewGhostDialogDictionaryViewModel>();

			foreach(var item in Model.Files)
			{
				if(item.IsCheckable)
				{
					dictionaries.Add(new NewGhostDialogDictionaryViewModel(item));
				}
			}
		}

	}

	//ゴーストテンプレートの辞書ファイル単位のViewModle
	internal class NewGhostDialogDictionaryViewModel : NotificationObject
	{
		private bool isCheced;

		public Model.GhostTemplateModel.GhostTemplateFileModel Model { get; }
		public bool IsChecked
		{
			get => isCheced;
			set
			{
				isCheced = value;
				NotifyChanged();
			}
		}

		public NewGhostDialogDictionaryViewModel(Model.GhostTemplateModel.GhostTemplateFileModel model)
		{
			Model = model;
			IsChecked = model.IsDefaultChecked;
		}

		public void RestoreDefault()
		{
			IsChecked = Model.IsDefaultChecked;
		}
	}
}
