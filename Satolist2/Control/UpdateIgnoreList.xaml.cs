﻿using ICSharpCode.AvalonEdit.Document;
using Microsoft.Win32;
using Microsoft.WindowsAPICodePack.Dialogs;
using Satolist2.Model;
using Satolist2.Utility;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Satolist2.Control
{
	/// <summary>
	/// UpdateIgnoreList.xaml の相互作用ロジック
	/// </summary>
	public partial class UpdateIgnoreList : UserControl
	{
		public event Action<string[]> OnFileDrop;

		public UpdateIgnoreList()
		{
			InitializeComponent();
			

			MainList.DragOver += MainList_DragOver;
			MainList.Drop += MainList_Drop;
			MainList.AllowDrop = true;
		}

		private void MainList_Drop(object sender, DragEventArgs e)
		{
			e.Handled = true;
			var items = e.Data.GetData(DataFormats.FileDrop) as string[];
			if (items == null)
				return;
			OnFileDrop?.Invoke(items);
		}

		private void MainList_DragOver(object sender, DragEventArgs e)
		{
			if(e.Data.GetDataPresent(DataFormats.FileDrop))
			{
				e.Effects = DragDropEffects.Link;
			}
			else
			{
				e.Effects = DragDropEffects.None;
			}

			e.Handled = true;
		}

		public void RequestScroll(object itemViewModel)
		{
			MainList.ScrollIntoView(itemViewModel);
		}
	}

	internal class UpdateIgnoreListViewModel : NotificationObject, IDockingWindowContent, IDisposable, IControlBindedReceiver
	{
		public const string ContentId = "UpdateIgnoreList";
		public const string DeleteFilePath = "/delete.txt";
		public const string DeveloperOptionsFilePath = "/developer_options.txt";

		private const int TabIndexList = 0;

		private ObservableCollection<UpdateIgnoreListItemViewModel> items;
		private MainViewModel main;
		private int currentTabIndex;
		private List<string> deleteCommonLine;  //コメントリスト行？
		private List<string> developerOptionsCommonLine;
		private ListEditorSerializingScope serializingScope;

		public UpdateIgnoreList Control { get; private set; }
		public GhostModel Ghost => main.Ghost;
		public CustomizedReadOnlyObservableCollection<UpdateIgnoreListItemViewModel> Items => new CustomizedReadOnlyObservableCollection<UpdateIgnoreListItemViewModel>(items);

		public int CurrentTabIndex
		{
			get => currentTabIndex;
			set
			{
				if(currentTabIndex != value)
				{
					if(currentTabIndex == TabIndexList)
					{
						//リストから別の場所へ
						ListToText();
					}
					else if(value == TabIndexList)
					{
						//別の場所からリストへ
						TextToList();
					}
				}

				currentTabIndex = value;
				NotifyChanged();
			}
		}

		public ActionCommand RemoveItemCommand { get; }
		public ActionCommand AddItemCommand { get; }
		public SaveFileObjectWrapper DeveloperOptionsSaveObject { get; }
		public SaveFileObjectWrapper DeleteSaveObject { get; }

		public string DockingTitle => "更新除外設定";

		public string DockingContentId => ContentId;

		public UpdateIgnoreListViewModel(MainViewModel main)
		{
			this.main = main;

			items = new ObservableCollection<UpdateIgnoreListItemViewModel>();
			developerOptionsCommonLine = new List<string>();
			deleteCommonLine = new List<string>();
			serializingScope = new ListEditorSerializingScope();

			RemoveItemCommand = new ActionCommand(
				(o) =>
				{
					if (MessageBox.Show("選択中の項目を削除します。よろしいですか？", "更新除外設定", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
					{
						//選択中のアイテムを削除
						var removes = items.Where(i => i.IsSelected).ToArray();
						foreach (var item in removes)
							RemoveItem(item);
					}
				},
				(o) =>
				{
					return items.FirstOrDefault(i => i.IsSelected) != null;
				}
				);

			AddItemCommand = new ActionCommand(
				(o) =>
				{
					UnSelectAll();

					//アイテムを追加
					items.Add(new UpdateIgnoreListItemViewModel(this) {
						IsNoNar = true,
						IsNoUpdate = true
					}
					);

					//末尾追加のはずなので、追加したアイテムのとこに移動する
					items.Last().IsSelected = true;
					Control.RequestScroll(items.Last());
				}
				);

			DeleteSaveObject = new SaveFileObjectWrapper(DeleteFilePath, SaveDelete);
			DeveloperOptionsSaveObject = new SaveFileObjectWrapper(DeveloperOptionsFilePath, SaveDeveloperOptions);

			//読み込み処理
			if(main.Ghost != null)
				Load();
		}

		//リスト上にファイルドロップ
		private void Control_OnFileDrop(string[] addItems)
		{
			UnSelectAll();

			foreach(var item in addItems)
			{
				if(!DictionaryUtility.IsChildPath(main.Ghost.FullPath, item))
				{
					//非対応パス
					ShowErrorRelativePathDialog(item);
					return;
				}
			}

			//追加対象についてチェック
			foreach(var item in addItems)
			{
				//ファイルを追加
				var addItem = new UpdateIgnoreListItemViewModel(this);
				addItem.IsNoNar = true;
				addItem.IsNoUpdate = true;
				addItem.IsSelected = true;
				addItem.Path = DictionaryUtility.MakeRelativePath(main.Ghost.FullPath, item);
				items.Add(addItem);
			}

			//末尾追加のはずなので、追加したアイテムのとこに移動する
			Control.RequestScroll(items.Last());
		}

		private void UnSelectAll()
		{
			foreach (var item in items)
				item.IsSelected = false;
		}

		public void RemoveItem(UpdateIgnoreListItemViewModel item)
		{
			item.ChangedCurrentFile();
			items.Remove(item);
		}

		//エラーダイアログ
		public static void ShowErrorRelativePathDialog(string path)
		{
			MessageBox.Show(string.Format("{0}\r\n{1}", path, "ゴーストの配下のファイルではないため追加できません。"), "更新除外リスト", MessageBoxButton.OK, MessageBoxImage.Exclamation);
		}

		//読み書き系
		public void Load()
		{
			var deletePath = main.Ghost.FullPath + DeleteFilePath;
			var deveoperOptionsPath = main.Ghost.FullPath + DeveloperOptionsFilePath;

			//セットで考える
			DeleteSaveObject.LoadState = EditorLoadState.Initialized;
			DeveloperOptionsSaveObject.LoadState = EditorLoadState.Initialized;

			try
			{
				if (System.IO.File.Exists(deveoperOptionsPath))
				{
					var fileBody = System.IO.File.ReadAllText(deveoperOptionsPath, Constants.EncodingShiftJis);
					DeserializeDeveloperOptions(fileBody);
				}

				if (System.IO.File.Exists(deletePath))
				{
					var fileBody = System.IO.File.ReadAllText(deletePath, Constants.EncodingShiftJis);
					DeserializeDelete(fileBody);
				}
				DeleteSaveObject.LoadState = EditorLoadState.Loaded;
				DeveloperOptionsSaveObject.LoadState = EditorLoadState.Loaded;
			}
			catch
			{
				DeleteSaveObject.LoadState = EditorLoadState.LoadFailed;
				DeveloperOptionsSaveObject.LoadState = EditorLoadState.LoadFailed;
			}
			finally
			{
				DeleteSaveObject.IsChanged = false;
				DeveloperOptionsSaveObject.IsChanged = false;
			}
		}

		public bool SaveDeveloperOptions()
		{
			if (DeveloperOptionsSaveObject.LoadState != EditorLoadState.Loaded)
				return false;

			string saveText;
			if(CurrentTabIndex == TabIndexList)
			{
				//リスト化されているのでシリアライズして保存
				saveText = SerializeDeveloperOptions();
			}
			else
			{
				//テキスト編集モードなのでDocumentを保存
				saveText = Control.DeveloperOptionsEditor.MainTextEditor.Text;
			}

			try
			{
				var fullPath = main.Ghost.FullPath + DeveloperOptionsFilePath;
				System.IO.File.WriteAllText(fullPath, saveText, Constants.EncodingShiftJis);
				DeveloperOptionsSaveObject.IsChanged = false;
				return true;
			}
			catch
			{
				return false;
			}
		}

		public bool SaveDelete()
		{
			if (DeleteSaveObject.LoadState != EditorLoadState.Loaded)
				return false;

			string saveText;
			if(CurrentTabIndex == TabIndexList)
			{
				//リスト化されているのでシリアライズして保存
				saveText = SerializeDelete();
			}
			else
			{
				//テキスト編集モードなのでDocumentを保存
				saveText = Control.DeleteTextEditor.MainTextEditor.Text;
			}

			try
			{
				var fullPath = main.Ghost.FullPath + DeleteFilePath;
				System.IO.File.WriteAllText(fullPath, saveText, Constants.EncodingShiftJis);
				DeleteSaveObject.IsChanged = false;
				return true;
			}
			catch
			{
				return false;
			}
		}

		public void AddDeveloperOptionsRecord(string path, bool nonar, bool noupdate)
		{
			//すでに存在するレコードは統合する
			path = DictionaryUtility.NormalizePath(path);
			var record = items.FirstOrDefault(o => o.Path == path);
			if(record != null)
			{
				record.IsNoNar = nonar;
				record.IsNoUpdate = noupdate;
			}
			else
			{
				record = new UpdateIgnoreListItemViewModel(this);
				record.Path = path;
				record.IsNoNar = nonar;
				record.IsNoUpdate = noupdate;
				record.IsDelete = false;
				items.Add(record);
			}
		}

		public void AddDeleteRecord(string path)
		{
			path = DictionaryUtility.NormalizePath(path);
			var record = items.FirstOrDefault(o => o.Path == path);
			if(record != null)
			{
				record.IsDelete = true;
			}
			else
			{
				items.Add(new UpdateIgnoreListItemViewModel(this) { Path = path, IsDelete = true });
			}
		}

		public void DeserializeDeveloperOptions(string body)
		{
			//リセット
			developerOptionsCommonLine.Clear();

			var parser = new DeveloperOptionsParser(body);
			developerOptionsCommonLine.AddRange(parser.CommonLines);

			foreach(var item in parser.Records)
			{
				AddDeveloperOptionsRecord(item.Path, item.IsNonar, item.IsNoUpdate);
			}
		}

		public void DeserializeDelete(string body)
		{
			//リセット
			deleteCommonLine.Clear();

			//csvではない。ファイルパスの列挙が基本。
			var lines = Satolist2.Utility.DictionaryUtility.SplitLines(body);

			foreach(var item in lines)
			{
				//パスではなさそうなレコードを弾く
				bool isInvalid = false;
				if (item.IndexOfAny(System.IO.Path.GetInvalidPathChars()) >= 0)
				{
					isInvalid = true;
				}
				else if (item.IndexOf("//") == 0)
				{
					isInvalid = true;
				}
				if (!isInvalid && !string.IsNullOrEmpty(item))
				{
					AddDeleteRecord(item);
				}
				else
				{
					deleteCommonLine.Add(item);
				}
			}
		}

		public string SerializeDeveloperOptions()
		{
			var lines = new List<string>();

			foreach(var item in items)
			{
				if(item.IsNoNar && item.IsNoUpdate)
				{
					lines.Add(string.Concat(item.Path, ",nonar,noupdate"));
				}
				else if(item.IsNoNar)
				{
					lines.Add(string.Concat(item.Path, ",nonar"));
				}
				else if(item.IsNoUpdate)
				{
					lines.Add(string.Concat(item.Path, ",noupdate"));
				}
			}

			foreach (var item in developerOptionsCommonLine)
				lines.Add(item);

			return string.Join("\r\n", lines);
		}

		public string SerializeDelete()
		{
			var lines = new List<string>();

			foreach(var item in items)
			{
				if (item.IsDelete)
					lines.Add(DictionaryUtility.NormalizeDeletePath(item.Path));
			}

			foreach (var item in deleteCommonLine)
				lines.Add(item);

			return string.Join("\r\n", lines);
		}

		public void ListToText()
		{
			//シリアライズ操作
			using (serializingScope.NotifySerialize())
			{
				Control.DeveloperOptionsEditor.MainTextEditor.Text = SerializeDeveloperOptions();
				Control.DeleteTextEditor.MainTextEditor.Text = SerializeDelete();
			}
		}

		private void DeleteText_TextChanged(object sender, EventArgs e)
		{
			if (!serializingScope.IsSerializing)
			{
				DeleteSaveObject.Changed();
			}
		}

		private void DeveloperOptionsText_TextChanged(object sender, EventArgs e)
		{
			if (!serializingScope.IsSerializing)
			{
				DeveloperOptionsSaveObject.Changed();
			}
		}

		public void TextToList()
		{
			//デシリアライズ操作
			using (serializingScope.NotifySerialize())	//操作上書き換えフラグが立ってしまうのでオフにしておく
			{
				items.Clear();
				DeserializeDeveloperOptions(Control.DeveloperOptionsEditor.MainTextEditor.Text);
				DeserializeDelete(Control.DeleteTextEditor.MainTextEditor.Text);
			}
		}

		//ViewModelが外されたときに破棄として呼ばれる
		public void Dispose()
		{
			Control.OnFileDrop -= Control_OnFileDrop;
			Control.DeleteTextEditor.MainTextEditor.OnTextChanged -= DeleteText_TextChanged;
			Control.DeveloperOptionsEditor.MainTextEditor.OnTextChanged -= DeveloperOptionsText_TextChanged;
		}

		public void ControlBind(System.Windows.Controls.Control control)
		{
			this.Control = (UpdateIgnoreList)control;
			this.Control.OnFileDrop += Control_OnFileDrop;
			this.Control.DeleteTextEditor.MainTextEditor.OnTextChanged += DeleteText_TextChanged;
			this.Control.DeveloperOptionsEditor.MainTextEditor.OnTextChanged += DeveloperOptionsText_TextChanged;
		}
	}

	internal class UpdateIgnoreListItemViewModel : NotificationObject
	{
		private UpdateIgnoreListViewModel parent;
		private string path;
		private bool isNoNar;
		private bool isNoUpdate;
		private bool isDelete;
		private bool isSelected;
		private bool isContextMenuOpen;
		public ActionCommand RemoveItemCommand { get; }
		public ActionCommand RemoveSingleItemCommand { get; }
		public ActionCommand SelectFileCommand { get; }
		public ActionCommand SelectDirectoryCommand { get; }
		public ActionCommand ShowContextMenuCommand { get; }

		//指定パス
		public string Path
		{
			get => path;
			set
			{
				if (value.IndexOfAny(System.IO.Path.GetInvalidPathChars()) >= 0)
				{
					NotifyChanged();
					return;
				}

				if (path != value)
				{
					path = value;
					NotifyChanged();
					ChangedCurrentFile();
				}
			}
		}

		//developer_options.txt nonar
		public bool IsNoNar
		{
			get => isNoNar;
			set
			{
				if (isNoNar != value)
				{
					isNoNar = value;
					NotifyChanged();
					NotifyChanged(nameof(IsInvalid));
					parent.DeveloperOptionsSaveObject.Changed();
				}
			}
		}

		//developer_options.txt noupdate
		public bool IsNoUpdate
		{
			get => isNoUpdate;
			set
			{
				if (isNoUpdate != value)
				{
					isNoUpdate = value;
					NotifyChanged();
					NotifyChanged(nameof(IsInvalid));
					parent.DeveloperOptionsSaveObject.Changed();
				}
			}
		}

		//delete.txt
		public bool IsDelete
		{
			get => isDelete;
			set
			{
				if (isDelete != value)
				{
					isDelete = value;
					NotifyChanged();
					NotifyChanged(nameof(IsInvalid));
					parent.DeleteSaveObject.Changed();
				}
			}
		}

		public bool IsSelected
		{
			get => isSelected;
			set
			{
				isSelected = value;
				NotifyChanged();
			}
		}

		//無効なレコード。
		//テキスト化したら消滅する
		public bool IsInvalid
		{
			get => !isDelete && !isNoUpdate && !isNoNar;
		}

		public bool IsContextMenuOpen
		{
			get => isContextMenuOpen;
			set
			{
				isContextMenuOpen = value;
				NotifyChanged();
			}
		}

		public UpdateIgnoreListItemViewModel(UpdateIgnoreListViewModel parent)
		{
			this.parent = parent;

			//デフォルト。
			path = string.Empty;
			isNoNar = false;
			isNoUpdate = false;

			RemoveItemCommand = parent.RemoveItemCommand;
			RemoveSingleItemCommand = new ActionCommand(
				o =>
				{
					if (MessageBox.Show("項目を削除します。よろしいですか？", "更新除外設定", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
					{
						this.parent.RemoveItem(this);
					}
				}
				);

			SelectFileCommand = new ActionCommand(
				o =>
				{
					var fileDialog = new CommonOpenFileDialog();
					var fullPath = DictionaryUtility.ConbinePath(parent.Ghost.FullPath, Path);
					if (System.IO.File.Exists(fullPath))
					{
						fileDialog.InitialDirectory = System.IO.Path.GetDirectoryName(fullPath);
						fileDialog.DefaultFileName = System.IO.Path.GetFileName(fullPath);
					}
					else
					{
						fileDialog.InitialDirectory = parent.Ghost.FullPath;

					}
					if (fileDialog.ShowDialog() == CommonFileDialogResult.Ok)
					{
						if (!DictionaryUtility.IsChildPath(parent.Ghost.FullPath, fileDialog.FileName))
						{
							UpdateIgnoreListViewModel.ShowErrorRelativePathDialog(fileDialog.FileName);
							return;
						}
						Path = DictionaryUtility.MakeRelativePath(parent.Ghost.FullPath, fileDialog.FileName);
					}
				}
				);

			SelectDirectoryCommand = new ActionCommand(
				o =>
				{
					var fileDialog = new CommonOpenFileDialog();
					var fullPath = DictionaryUtility.ConbinePath(parent.Ghost.FullPath, Path);
					fileDialog.IsFolderPicker = true;
					if (System.IO.Directory.Exists(fullPath))
						fileDialog.DefaultDirectory = fullPath;
					else
						fileDialog.DefaultDirectory = parent.Ghost.FullPath;

					if(fileDialog.ShowDialog() == CommonFileDialogResult.Ok)
					{
						if (!DictionaryUtility.IsChildPath(parent.Ghost.FullPath, fileDialog.FileName))
						{
							UpdateIgnoreListViewModel.ShowErrorRelativePathDialog(fileDialog.FileName);
							return;
						}
						Path = DictionaryUtility.MakeRelativePath(parent.Ghost.FullPath, fileDialog.FileName) + "/";
					}
				}
				);

			ShowContextMenuCommand = new ActionCommand(
				o =>
				{
					parent.Control.Dispatcher.BeginInvoke(new Action(() => IsContextMenuOpen = true), System.Windows.Threading.DispatcherPriority.Render);
				}
				);
		}

		//チェックが入っている種別のファイルについて変更扱いにする
		public void ChangedCurrentFile()
		{
			if(isDelete)
			{
				parent.DeleteSaveObject.Changed();
			}

			if(isNoNar || isNoUpdate)
			{
				parent.DeveloperOptionsSaveObject.Changed();
			}
		}

		
	}

}
