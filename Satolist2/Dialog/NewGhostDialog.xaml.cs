using Microsoft.Win32;
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
	public partial class NewGhostDialog : Window
	{
		internal new NewGhostDialogViewModel DataContext
		{
			get => (NewGhostDialogViewModel)base.DataContext;
			set => base.DataContext = value;
		}

		public NewGhostDialog()
		{
			InitializeComponent();
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
		private bool isUseDefaultImportPath;
		private string importSatoriPath;
		private string importLicencePath;
		private string importMasterShellPath;

		private string defaultImportSatoriPath;
		private string defaultImportLicencePath;
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
						ImportLicencePath = defaultImportLicencePath;
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

		//ライセンスパス
		public string ImportLicencePath
		{
			get => importLicencePath;
			set
			{
				importLicencePath = value;
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
					!string.IsNullOrEmpty(ImportLicencePath) &&
					!string.IsNullOrEmpty(ImportMasterShellPath);
			}
		}

		public ActionCommand OpenTargetSelectDialogCommand { get; }
		public ActionCommand OpenImportSatoriSelectDialogCommand { get; }
		public ActionCommand OpenImportLicenceSelectDialogCommand { get; }
		public ActionCommand OpenImportMasterShellSelectDialogCommand { get; }
		public ActionCommand CancelCommand { get; }
		public ActionCommand SelectPathPageNextCommand { get; }
		public ActionCommand TemplateSelectorPageBackCommand { get; }
		public ActionCommand CreateGhostCommand { get; }

		public ReadOnlyObservableCollection<NewGhostDialogGhostTemplateViewModel> Templates
		{
			get => new ReadOnlyObservableCollection<NewGhostDialogGhostTemplateViewModel>(templates);
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

			defaultImportLicencePath = DictionaryUtility.NormalizeFullPath("data/template/satori_license.txt");
			defaultImportSatoriPath = DictionaryUtility.NormalizeFullPath("data/template/satori.dll");
			defaultImportMasterShellPath = DictionaryUtility.NormalizeFullPath("data/template/shell/master");

			importSatoriPath = defaultImportSatoriPath;
			importLicencePath = defaultImportLicencePath;
			ImportMasterShellPath = defaultImportMasterShellPath;

			
			var templateDirs = System.IO.Directory.GetDirectories("data/template/template");

			//インポートなしテンプレート
			var emptyTemplate = NewGhostDialogGhostTemplateViewModel.EmptyTempalte;
			SelectedTemplate = emptyTemplate;
			templates.Add(emptyTemplate);

			//テンプレートをロードする
			foreach (var item in templateDirs)
			{
				try
				{
					var t = new NewGhostDialogGhostTemplateViewModel(DictionaryUtility.NormalizeFullPath(item));
					templates.Add(t);
				}
				catch { }
			}

			//コマンド準備
			OpenTargetSelectDialogCommand = new ActionCommand(
				o =>
				{
					var path = OpenFileSelectDialog(CreatePath, "フォルダ|*", true);
					if (!string.IsNullOrEmpty(path))
						CreatePath = path;
				}
				);

			OpenImportSatoriSelectDialogCommand = new ActionCommand(
				o =>
				{
					var path = OpenFileSelectDialog(ImportSatoriPath, "里々モジュール(satori.dll)|satori.dll");
					if (!string.IsNullOrEmpty(path))
						ImportSatoriPath = path;
				}
				);

			OpenImportLicenceSelectDialogCommand = new ActionCommand(
				o =>
				{
					var path = OpenFileSelectDialog(ImportLicencePath, "里々ライセンス(satori_licence.txt)|satori_licence.txt");
					if (!string.IsNullOrEmpty(path))
						ImportLicencePath = path;
				}
				);

			OpenImportMasterShellSelectDialogCommand = new ActionCommand(
				o =>
				{
					var path = OpenFileSelectDialog(ImportMasterShellPath, "シェルフォルダ|*", false, true);
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
							CreateNewGhost(CreatePath, SelectedTemplate, ImportMasterShellPath, ImportSatoriPath, ImportLicencePath);
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

		private string OpenFileSelectDialog(string currentPath, string fileFilter,bool selectCreateDirectory = false, bool selectMasterShell = false)
		{
			var fileDialog = new SaveFileDialog();
			fileDialog.Filter = fileFilter;
			fileDialog.FileName = System.IO.Path.GetFileName(currentPath);
			fileDialog.InitialDirectory = System.IO.Path.GetDirectoryName(currentPath);
			fileDialog.OverwritePrompt = false;
			/*
			if(selectShellDirectory)
				fileDialog.CheckFileExists = false;
			*/

			fileDialog.FileOk += (sender, e) =>
			{
				if(selectCreateDirectory)
				{
					//フォルダ作成の場合は存在してるフォルダを選んだら無効
					if(System.IO.File.Exists(fileDialog.FileName))
					{
						MessageBox.Show("そのフォルダはすでに存在しているため、ゴーストの新規作成には使えません。");
						e.Cancel = true;
					}
				}
				else if(selectMasterShell)
				{
					//フォルダを選んだことにする
					var effectivePath = System.IO.Path.GetDirectoryName(fileDialog.FileName);
					if(!System.IO.File.Exists(effectivePath))
					{
						MessageBox.Show("そのファイルはありません。");
						e.Cancel = true;
					}

					//確定
					fileDialog.FileName = effectivePath;
				}
				else
				{
					//その他は存在してないといけない
					if (!System.IO.File.Exists(fileDialog.FileName))
					{
						MessageBox.Show("そのファイルはありません。");
						e.Cancel = true;
					}
				}

			};

			if( fileDialog.ShowDialog() == true)
			{
				return fileDialog.FileName;
			}
			return null;
		}

		//各種設定に問題ないかチェック、問題がある場合はメッセージを表示
		private bool ValidatePathSettings()
		{
			if(System.IO.File.Exists(CreatePath))
			{
				MessageBox.Show("すでに存在するフォルダは「作成する場所」には使えません。");
				return false;
			}

			if(!DictionaryUtility.IsValidFilePath(CreatePath))
			{
				MessageBox.Show("「作成する場所」のファイルパスが誤っています。");
				return false;
			}
			if(!System.IO.File.Exists(ImportSatoriPath))
			{
				MessageBox.Show("「里々本体」が指定されたパスに見つかりません。");
				return false;
			}

			if(!System.IO.File.Exists(ImportLicencePath))
			{
				MessageBox.Show("「里々ライセンス」が指定されたパスに見つかりません。");
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
		private void CreateNewGhost(string path, NewGhostDialogGhostTemplateViewModel template, string shellPath, string satoriPath, string licencePath)
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

				//里々とライセンスをコピー
				System.IO.Directory.CreateDirectory(dictionaryPath);
				System.IO.File.Copy(satoriPath, DictionaryUtility.ConbinePath(dictionaryPath, "satori.dll"), true);
				System.IO.File.Copy(licencePath, DictionaryUtility.ConbinePath(dictionaryPath, "satori_licence.txt"), true);

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
		public ReadOnlyObservableCollection<NewGhostDialogDictionaryViewModel> Dictionaries
		{
			get => new ReadOnlyObservableCollection<NewGhostDialogDictionaryViewModel>(dictionaries);
		}

		public IEnumerable<Model.GhostTemplateModel.GhostTemplateFileModel> GenerateGhostFiles
		{
			get
			{
				foreach(var item in Model.Files)
				{
					if (item.IsSatoriDictionary)
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
				if(item.IsSatoriDictionary)
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
