using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;


namespace Satolist
{
	public class Surfaces
	{
		private Dictionary<String, Bitmap> mLoadedFiles;	//読み込み済み画像
		public Dictionary<int, Bitmap> surfaceBitmaps;		//エレメント合成済み画像
		public Dictionary<int, SurfaceFile> surfaceFile;
		public SortedDictionary<int, SurfaceDescript> surfaces;
		public Dictionary<int, String> surfaceTable;    //サーフェステーブル
		public Point? defaultOffset = null;
		public Size defaultFrame;
		public SizeF defaultExpand;
		public bool defaultPaletteVisible;
		public bool defaultViewerVisible;
		public bool seriko_use_self_alpha;
		public const String SURFACE = "surface";
		public const String LINE_COMMENT = "//";
		public const String ELEMENT = "element";
		public const String ANIMATION = "animation";
		public const String PATTERN = "pattern";
		public const String INTERVAL = "interval";
		public const String PALETTE_OFFSET = "//satolist.palette_disabled";	//旧仕様互換 無効化済み
		public const String PALETTE_OFFSET_EX = "//satolist.palette.offset";
		public const String PALETTE_SIZE = "//satolist.palette.frame";
		public const String PALETTE_EXPAND = "//satolist.palette.expand";
		public const String PALETTE_VISIBLE = "//satolist.palette.visible";
		public const String VIEWER_VISIBLE = "//satolist.viewer.visible";
		public const String SURFACE_VISIBLE = "//satolist.surface.visible";
		public const String VIEWER_ACTIVE_PATTERN = "//satolist.surface.default";
		public const String SURFACE_CHARACTER_ID = "//satolist.scope";
		public const String SURFACE_CHARACTER_ID2 = "//satolist.surface.scope";	//新さとりすと対応の互換

		public const String COLLISION = "collision";
		public const String COLLISION_EX = "collisionex";
		public String mFilePath = "";
		public String mLastLoadInfomation;
		public String mDirectory;

		private readonly String[] SEPARATOR = new String[] { " ", "," };
		public enum BLESS_TYPE
		{
			//DESCRIPT,
			SURFACE,//サーフェスブレス
			UNKNOWN//非対応ブレス

		}

		public Surfaces()
		{
			surfaces = new SortedDictionary<int, SurfaceDescript>();
			surfaceFile = new Dictionary<int, SurfaceFile>();
			surfaceTable = new Dictionary<int, string>();
			surfaceBitmaps = new Dictionary<int, Bitmap>();
			mLoadedFiles = new Dictionary<string, Bitmap>();
			InitLog();
		}

		public Bitmap GetElementBitmap(int id)
		{
			if (surfaceBitmaps.ContainsKey(id))
			{
				return surfaceBitmaps[id];
			}
			else
			{
				//未取得の場合　作りなおす
				Bitmap target = null;
				Graphics gr = null;

				if (!surfaces.ContainsKey(id) || surfaces[id].element.Count <= 0)
				{
					//element定義がない - idから画像拾って設定
					if (surfaceFile.ContainsKey(id))
					{
						try
						{
							Bitmap b = GetBitmap(mDirectory + "/" + surfaceFile[id].fileName, -1);
							if (b != null)
							{
								if (target == null)
								{
									target = new Bitmap(ProjectData.Instance.surfaceViewerSizeX, ProjectData.Instance.surfaceViewerSizeY, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
									gr = Graphics.FromImage(target);
									surfaceBitmaps.Add(id, target);
								}
								//surfaces[id].autoElement = mDirectory + "/" + surfaceFile[id].fileName;
								AddLayer(b, new Point(0, 0), target, gr);
							}
						}
						catch (Exception)
						{
						}
					}
					//これがelseなら何も表示するものがない

				}
				else
				{
					//element定義がある
					foreach (KeyValuePair<int, SurfaceElement> p in surfaces[id].element)
					{
						try
						{
							Bitmap b = GetBitmap(mDirectory + "/" + p.Value.fileName, -1);
							if (b == null)
							{
								continue;
							}
							if (target == null)
							{
								target = new Bitmap(ProjectData.Instance.surfaceViewerSizeX, ProjectData.Instance.surfaceViewerSizeY, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
								gr = Graphics.FromImage(target);
								surfaceBitmaps.Add(id, target);
							}
								AddLayer(b, new Point(p.Value.x, p.Value.y), target, gr);

						}
						catch (Exception )
						{
						}
					}
				}
		
				if (target == null)
				{
					surfaceBitmaps.Add(id, null);
				}

				//surfaceBitmaps.Add(id, target);
				return target;
			}
		}

		private void RenderElements()
		{
			surfaceBitmaps.Clear();
			foreach (KeyValuePair<int, SurfaceDescript> s in surfaces)
			{
				Bitmap target = null;
				Graphics gr = null;
				

				int id = s.Key;

				if (!surfaces.ContainsKey(id) || surfaces[id].element.Count <= 0)
				{
					//element定義がない - idから画像拾って設定
					if (surfaceFile.ContainsKey(id))
					{
						try
						{
							Bitmap b = GetBitmap(mDirectory + "/" + surfaceFile[id].fileName, -1);
							if (target == null)
							{
								target = new Bitmap(ProjectData.Instance.surfaceViewerSizeX, ProjectData.Instance.surfaceViewerSizeY, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
								gr = Graphics.FromImage(target);
								surfaceBitmaps.Add(s.Key, target);
							}
							AddLayer(b, new Point(0, 0), target, gr);
						}
						catch (Exception )
						{
						}
					}
					//これがelseなら何も表示するものがない

				}
				else
				{
					//element定義がある
					foreach (KeyValuePair<int, SurfaceElement> p in surfaces[id].element)
					{
						try
						{
							Bitmap b = GetBitmap(mDirectory + "/" + p.Value.fileName, -1);
							if (target == null)
							{
								
								target = new Bitmap(ProjectData.Instance.surfaceViewerSizeX, ProjectData.Instance.surfaceViewerSizeY, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
								gr = Graphics.FromImage(target);
								surfaceBitmaps.Add(s.Key, target);
							}
							if (b != null)
							{
								AddLayer(b, new Point(p.Value.x, p.Value.y), target, gr);
							}
							
						}
						catch (Exception )
						{
						}
					}
				}

			}
		}

		private void AddLayer(Bitmap bitmap, Point pos, Bitmap canvas, Graphics canvasGraph )
		{
			BlendBitmap b = new BlendBitmap(canvas);
			b.Draw(bitmap, pos.X, pos.Y, canvasGraph, 1.0f, SurfaceAnimationPattern.Patterns.Overlay, null);
		}

		public Bitmap GetBitmapEx( String fileName, int id, bool isAsis = false)
		{
			return GetBitmap( mDirectory + "/" + fileName, id, isAsis);
		}

		public Bitmap GetBitmap(String fileName, int id, bool isAsis = false)
		{
			//現状isasisは無効パラメータです。
			if (GetElementBitmap(id) != null)
			{
				return GetElementBitmap(id);
			}

			if (fileName == null)
			{
				return null;
			}

			if (mLoadedFiles.ContainsKey(fileName))
			{
				return mLoadedFiles[fileName];
			}
			else
			{

				if (File.Exists(fileName))
				{
					Bitmap b = null;
					using (System.IO.FileStream fs = new System.IO.FileStream(fileName, FileMode.Open, FileAccess.Read))
					{
						b = new Bitmap(fs);
					}

					if (b != null)
					{

						if (!seriko_use_self_alpha || Image.GetPixelFormatSize(b.PixelFormat) < 32)
						{
							b.MakeTransparent(b.GetPixel(0, 0));
						}
						mLoadedFiles.Add(fileName, b);
						return mLoadedFiles[fileName];
					}
					return null;
				}
				else
				{
					//なかった！　拡張子付与でなんとかしろ
					String[] ex = { "png", "jpg", "jpeg", "PNG", "JPG", "JPEG" };

					foreach (String s in ex)
					{
						if (File.Exists(fileName + "." + s))
						{
							Bitmap b = null;
							using (System.IO.FileStream fs = new System.IO.FileStream(fileName + ".", FileMode.Open, FileAccess.Read))
							{
								b = new Bitmap(fs);
							}

							if (b != null)
							{
								if (!seriko_use_self_alpha || Image.GetPixelFormatSize(b.PixelFormat) < 32)
								{
									b.MakeTransparent(b.GetPixel(0, 0));
								}
								mLoadedFiles.Add(fileName, b);
								return mLoadedFiles[fileName];
							}
							return null;
						}
					}
					return null;

				}


			}
		}


		public void LoadSurfaces(String dir)
		{
			InitLog();
			surfaceBitmaps.Clear();
			mLoadedFiles.Clear();
			if (dir == null)
			{
				AddLog("エラー: シェルフォルダが未指定です。");
				return;
			}

			mFilePath = dir + "\\" + "surfaces.txt";
			mDirectory = dir;

			try
			{
				defaultOffset = null;
				defaultExpand = new SizeF(1.0f, 1.0f);
				defaultFrame = new Size(100, 100);
				defaultPaletteVisible = true;
				defaultViewerVisible = true;
				seriko_use_self_alpha = false;

				surfaceFile.Clear();
				surfaces.Clear();
				surfaceTable.Clear();

				//if( !File.Exists(dir+"/surfaces.txt"))
				//{
				//    return;
				//}

				String[] files = Directory.GetFiles(dir);
				String data = "";
				Regex pat = new Regex("^surfaces.*\\.txt$");

				foreach (String fileName in files)
				{
					String matchName = Path.GetFileName(fileName);
					if (pat.Match(matchName).Success)
					{
						AddLog("ファイルをロードします: " + fileName);
						data += ReadFile(fileName);
					}

				}

				EnumFile(dir);

				if (!String.IsNullOrEmpty(data))
				{



					//String data = ReadFile(dir + "/surfacestxt");
					//String data = ReadFile(dir+"/surfaces.txt");
					String bress = "";          //ブレス宣言部
					String bressValue = "";     //ブレス中括弧の内容

					int cursor = 0;
					//int startPos = 0;



					//サーフェスを解析。
					while (true)
					{

						int spos = data.IndexOf(SURFACE, cursor);
						if (spos != -1)
						{
							//サーフェスブレスの開始を検出
							int epos = data.IndexOf("{", spos);
							if (epos != -1)
							{
								//ブレス宣言部の終了を検出

								bress = data.Substring(spos, epos - spos);
								int cpos = data.IndexOf("}", epos);
								if (cpos != -1)
								{
									//ブレスの終了を検出
									bressValue = data.Substring(epos + "{".Length, cpos - epos - "{".Length);

									//ブレスの終了を検出したら、追加作業にはいる。
									//bress を解析して保存対象を指定。
									//指定された保存対象にbressValueを解析して追加。

									//編集対象を取得
									Dictionary<int, SurfaceDescript> edit = GetEditDesc(bress);
									String addline = "surface: ";
									foreach (KeyValuePair<int, SurfaceDescript> loop in edit)
									{
										addline += loop.Key + " ";
									}
									AddLog(addline);

									//内容を設定する
									AddSurface(edit, bressValue);

									cursor = cpos + "}".Length;

								}
								else
								{
									throw new Exception("対応する閉じカッコが見つかりません。");
								}
							}
							else
							{
								throw new Exception("対応する開きカッコが見つかりません。");
							}

						}
						else
						{
							//終了
							break;
						}


					}
				}

				//定義を受けないファイルを組み込む
				if (ProjectData.Instance.isSurfaceUseNoSetting)
				{
					foreach (KeyValuePair<int, SurfaceFile> p in surfaceFile)
					{
						if (!surfaces.ContainsKey(p.Key))
						{
							SurfaceDescript desc = new SurfaceDescript(p.Key);
							desc.paletteOffset = defaultOffset;
							desc.paletteExpand = defaultExpand;
							desc.isPaletteVisible = defaultPaletteVisible;
							desc.isViewerVisible = defaultViewerVisible;

							surfaces.Add(p.Key, desc);
						}


					}


				}

				LoadDescript(dir);



				//続いてdurfacetable
				String table = dir + "/surfacetable.txt";
				if (File.Exists(table))
				{
					StreamReader read = new StreamReader(table, Encoding.GetEncoding("Shift_JIS"));
					String line;
					String[] sp;
					while (true)
					{
						line = read.ReadLine();
						if (line == null)
							break;

						try
						{
							sp = line.Split(',');
							if (sp.Length == 2)
							{
								int id = int.Parse(sp[0]);
								surfaceTable.Add(id, sp[1]);
							}

						}
						catch
						{

						}

					}

					read.Close();
				}

				AddLog("Completed! - 読み込みは正常に終了しました。");

			}
			catch (Exception e)
			{
				AddLog("...Failed... - 読み込みエラーが発生したため、完了できませんでした。");
				AddLog(e.ToString());
				MessageBox.Show("シェルの読み込みに失敗しました。\r\nsurfaces.txt が正しくない可能性があります。", "サーフェス ビューワ");
			}
			//RenderElements();

		}

		public void LoadDescript(String dir)
		{
			StreamReader read = null;
			try
			{
				read = new StreamReader(dir + "/" + "descript.txt", Encoding.GetEncoding("Shift_JIS"));

				String line;
				while (true)
				{
					line = read.ReadLine();
					if (line == null)
					{
						break;
					}

					int comment = line.IndexOf("//");
					if (comment != -1)
					{
						line = line.Substring(0, comment);
					}

					line = line.Replace(" ", "");
					line = line.Replace("　", "");
					line = line.Replace("\t", "");

					String[] sp = line.Split('.');
					
					if( line == "seriko.use_self_alpha,1")
					{
						//トランスペアレント
						seriko_use_self_alpha = true;
					}

					if (sp.Length >= 3)
					{
						String[] sp2 = sp[2].Split(',');

						if (sp2[0] == "name")
						{
							int characterId = GetCharacterID(sp[0]);
							int id = int.Parse(sp[1].Substring("bindgroup".Length));

							foreach (KeyValuePair<int, SurfaceDescript> p in surfaces)
							{
								if (p.Value.animation.ContainsKey(id))
								{
									//キャラクタidは？
									p.Value.animation[id].AddBindDescript(characterId).mName = sp2[1] + "/" + sp2[2];
								}

							}
						}

						else if (sp2[0] == "default")
						{
							try
							{
								int characterId = GetCharacterID(sp[0]);
								int id = int.Parse(sp[1].Substring("bindgroup".Length));
								int defValue;
								int.TryParse(sp2[1], out defValue);
								bool def = defValue != 0;
								foreach (KeyValuePair<int, SurfaceDescript> p in surfaces)
								{
									if (p.Value.animation.ContainsKey(id))
									{
										p.Value.animation[id].AddBindDescript(characterId).mIsDefault = def;
									}

								}
							}
							catch
							{
							}
						}
							/*
						else if (sp2[0] == VIEWER_ACTIVE_PATTERN)
						{
							try
							{
								int characterId = GetCharacterID(sp[0]);
								int id = int.Parse(sp[1].Substring("bindgroup".Length));
								int defValue;
								int.TryParse(sp2[1], out defValue);
								bool def = defValue != 0;
								foreach (KeyValuePair<int, SurfaceDescript> p in surfaces)
								{
									if (p.Value.animation.ContainsKey(id))
									{
										p.Value.animation[id].AddBindDescript(characterId).mIsViewerActive = def;
									}

								}
							}
							catch { }
						}
							 * */


					}


				}
			}
			catch
			{
			}

			if (read != null)
				read.Close();



		}

		public static int GetCharacterID(String strData)
		{
			if (strData == "sakura")
			{
				return 0;
			}
			else if( strData == "kero")
			{
				return 1;
			}
			else if( strData.IndexOf("char") == 0 )
			{
				int i;
				bool isParse = int.TryParse(strData.Substring("char".Length), out i);
				if (isParse)
				{
					return i;
				}
				else
				{
					return -1;
				}
			}
			else
			{
				return -1;
			}

		}

		public void LoadSurfaceCollisionOnly(String dir)
		{
			InitLog();
			if (dir == null)
			{
				AddLog("エラー: シェルフォルダが未指定です。");
				return;
			}
			AddLog("さわり判定情報のみを読み込みます。");
			try
			{
				foreach (KeyValuePair<int, SurfaceDescript> p in surfaces)
				{
					p.Value.collisions.Clear();
				}

				String[] files = Directory.GetFiles(dir);
				String data = "";
				Regex pat = new Regex("^surfaces.*\\.txt$");

				foreach (String fileName in files)
				{
					String matchName = Path.GetFileName(fileName);
					if (pat.Match(matchName).Success)
					{
						AddLog("ファイルをロードします: " + fileName);
						data += ReadFile(fileName);
					}

				}

				if (String.IsNullOrEmpty(data))
				{
					return;
				}


				//String data = ReadFile(dir + "/surfacestxt");
				//String data = ReadFile(dir+"/surfaces.txt");
				String bress = "";          //ブレス宣言部
				String bressValue = "";     //ブレス中括弧の内容

				int cursor = 0;
				//int startPos = 0;

				EnumFile(dir);

				//サーフェスを解析。
				while (true)
				{

					int spos = data.IndexOf(SURFACE, cursor);
					if (spos != -1)
					{
						//サーフェスブレスの開始を検出
						int epos = data.IndexOf("{", spos);
						if (epos != -1)
						{
							//ブレス宣言部の終了を検出

							bress = data.Substring(spos, epos - spos);
							int cpos = data.IndexOf("}", epos);
							if (cpos != -1)
							{
								//ブレスの終了を検出
								bressValue = data.Substring(epos + "{".Length, cpos - epos - "{".Length);

								//ブレスの終了を検出したら、追加作業にはいる。
								//bress を解析して保存対象を指定。
								//指定された保存対象にbressValueを解析して追加。

								//編集対象を取得
								Dictionary<int, SurfaceDescript> edit = GetEditDesc(bress);

								//内容を設定する
								AddSurfaceCollisionOnly(edit, bressValue);

								cursor = cpos + "}".Length;

							}
							//elseならブレスが終わらない



						}
						//elseならブレスが始まらない

					}
					else
					{
						//終了
						break;
					}


				}

				AddLog("Completed! - 読み込みは正常に終了しました。");


			}
			catch (Exception e)
			{
				AddLog("...Failed... - 読み込みエラーが発生したため、完了できませんでした。");
				AddLog(e.ToString());
				MessageBox.Show("シェルの読み込みに失敗しました。\r\nsurfaces.txt が正しくない可能性があります。", "サーフェス ビューワ");
			}
		}

		private void InitLog()
		{
			mLastLoadInfomation = "サーフェスビューワ 読み込みログ:\r\n";
		}

		private void AddLog(String logLine)
		{
			mLastLoadInfomation += logLine + "\r\n";
		}

		public void EnumFile(String dir)
		{
			//使用可能なサーフェス ファイルを列挙します。

			if (dir == null || dir.Length <= 0)
				return;

			String[] files = Directory.GetFiles(dir);

			Regex pat = new Regex("^surface[0-9]+\\.png");
			String f;

			foreach (String name in files)
			{
				f = Path.GetFileName(name);
				Match m = pat.Match(f);
				if (m.Success)
				{
					//サーフェスid指定で使用可能なpngを列挙する
					SurfaceFile sfile = new SurfaceFile();

					Regex patNum = new Regex("[0-9]+");
					Match matchNum = patNum.Match(f);

					sfile.id = int.Parse(matchNum.Value);
					sfile.fileName = f;
					sfile.idName = matchNum.Value;

					if (surfaceFile.ContainsKey(sfile.id))
					{
						//ファイルIDが重なった場合 - ファイル数字の短い方優先
						if (surfaceFile[sfile.id].idName.Length < sfile.idName.Length)
						{
							surfaceFile[sfile.id] = sfile;
						}
					}
					else
					{
						//ファイルIDが重ならなかった場合
						surfaceFile.Add(sfile.id, sfile);
					}


				}


			}

		}

		//surface ブレスの編集対象をリストにマトメル
		public Dictionary<int, SurfaceDescript> GetEditDesc(String bress)
		{
			Dictionary<int, SurfaceDescript> ret = new Dictionary<int, SurfaceDescript>();
			List<int> disable = new List<int>();

			bool isAppend = false;
			if (bress.IndexOf(".append") != -1)
			{
				isAppend = true;
			}

			bress = bress.Replace(SURFACE, "");
			bress = bress.Replace(".append", "");
			bress = bress.Replace("\r\n", "");
			bress = bress.Replace(" ", "");
			bress = bress.Replace("　", "");
			bress = bress.Replace("\t", "");

			String[] sp = bress.Split(',');

			foreach (String ss in sp)
			{
				String s = ss;
				bool isDisable = false;

				if (s.Length <= 0)
					continue;

				if (s[0] == '!')
				{
					isDisable = true;
					s = s.Substring(1);
				}

				if (s.Length <= 0)
					continue;


				//カンマセパレート
				String[] se = s.Split('-');

				if (se.Length >= 2)
				{
					//セパレートしてる
					int st = int.Parse(se[0]);
					int en = int.Parse(se[se.Length - 1]);
					if (st > en)
					{
						int t = st;
						st = en;
						en = t;
					}

					if (!isDisable)
					{
						for (int i = st; i <= en; i++)
						{
							AddEditDesc(ret, i, isAppend);
						}
					}
					else
					{
						for (int i = st; i <= en; i++)
						{
							disable.Add(i);
						}
					}

				}
				else
				{
					if (s == ".alias")
						continue;

					//してない
					if (!isDisable)
					{
						AddEditDesc(ret, int.Parse(s), isAppend);
					}
					else
					{
						disable.Add(int.Parse(s));
					}
				}



			}

			foreach (int i in disable)
			{
				if (ret.ContainsKey(i))
				{
					ret.Remove(i);
				}
			}


			return ret;

		}

		private void AddEditDesc(Dictionary<int, SurfaceDescript> desc, int id, bool isAppend)
		{
			if (desc.ContainsKey(id))
				return;

			//編集対象としてdescに追加します。
			//surfacesに存在しない場合はそっちにも追加します。
			if (surfaces.ContainsKey(id))
			{
				//サーフェスマップに存在する
				desc.Add(id, surfaces[id]);

			}
			else
			{
				if (!isAppend)
				{
					//サーフェスマップに存在しない
					SurfaceDescript add = new SurfaceDescript(id);
					surfaces.Add(id, add);
					add.paletteOffset = defaultOffset;
					add.paletteExpand = defaultExpand;
					add.isViewerVisible = defaultViewerVisible;
					add.isPaletteVisible = defaultPaletteVisible;
					desc.Add(id, add);
				}
			}
		}

		//surfaceブレスの中括弧の内容を解析して操作対象に追加します
		void AddSurface(Dictionary<int, SurfaceDescript> edit, String bressValue)
		{
			//bressValue = bressValue.Replace(" ", "");
			//bressValue = bressValue.Replace("　", "");
			//bressValue = bressValue.Replace("\t", "");

			Regex pat;
			Match mat;
			String[] sp;

			Regex patInterval = new Regex("^[0-9]+interval");
			Regex patPattern = new Regex("^[0-9]+pattern[0-9]+");
			//Regex patCollision = new Regex("^[0-9]+")



			StringReader read = new StringReader(bressValue);
			String line;
			while (true)
			{
				line = read.ReadLine();
				if (line == null)
					return;

				line = EraseIndent(line);

				pat = new Regex("^" + ELEMENT + "[0-9]+");
				mat = pat.Match(line);

				if (mat.Success && mat.Index == 0)
				{
					//elementが見つかった
					sp = line.Split(SEPARATOR, StringSplitOptions.RemoveEmptyEntries);

					if (sp.Length >= 5)
					{

						AddLog("Element: " + line);

						SurfaceElement elem = new SurfaceElement();
						elem.num = int.Parse(sp[0].Replace(ELEMENT, ""));
						elem.pattern = sp[1];
						elem.fileName = sp[2];
						elem.x = int.Parse(sp[3]);
						elem.y = int.Parse(sp[4]);



						foreach (KeyValuePair<int, SurfaceDescript> p in edit)
						{
							if (p.Value.element.ContainsKey(elem.num))
							{
								//element定義がかぶってる
								p.Value.element[elem.num] = elem;
							}
							else
							{
								//element定義がかぶってない
								p.Value.element.Add(elem.num, elem);
							}
						}

						AddLog("...OK!");
					}
				}
				else if (line.IndexOf("collision") == 0)
				{
					//collision情報
					AddLog("Collision: " + line);
					InspectCollision(line, edit);
					AddLog("...OK!");
				}
				else if (line.IndexOf("animation") == 0)
				{
					//animation 定義
					sp = SurfacesSplit(line);
					String[] sp2 = sp[0].Split('.');

					int animationId = int.Parse(sp2[0].Substring(ANIMATION.Length));

					if (sp2[1].IndexOf(INTERVAL) == 0)
					{
						AddLog("Animation Interval: " + line);
						sp = SurfacesSplit(line, -1, 2);
						//アニメーションインターバル
						SetAnimationInterval(edit, animationId, sp[1]);
						AddLog("...OK!");

					}
					else if (sp2[1].IndexOf(PATTERN) == 0)
					{
						//アニメーション パターン
						if (sp.Length > 3)
						{
							AddLog("Animation Pattern: " + line);
							int patternId = int.Parse(sp2[1].Substring(PATTERN.Length));
							SurfaceAnimationPattern pattern = new SurfaceAnimationPattern();
							pattern.id = patternId;

							pattern.Set(sp, 1, false);
							SetAnimationPattern(edit, animationId, patternId, pattern);
							AddLog("...OK!");

						}
					}
					else if (sp2[1].IndexOf("collision") == 0)
					{
						//アニメーション コリジョン
						//collision情報
						//int patternId = int.Parse(sp2[1].Substring(PATTERN.Length));
						AddLog("Animation Collision: " + line);
						InspectCollision(line, edit, animationId);
						AddLog("...OK!");
					}
					else if( sp2[1].IndexOf("option") == 0)
					{
						//オプション。
						foreach( KeyValuePair<int,SurfaceDescript> p in edit)
						{
							if( !p.Value.animation.ContainsKey(animationId))
							{
								p.Value.animation.Add(animationId, new SurfaceAnimation());
							}
							p.Value.animation[animationId].option = sp[1];
						}

					}


				}
				else if (patInterval.IsMatch(line))
				{
					//旧アニメーション
					AddLog("Animation Interval(old): " + line);
					sp = SurfacesSplit(line, -1, 2);
					int index = sp[0].IndexOf(INTERVAL);
					int animationId = int.Parse(sp[0].Substring(0, index));
					SetAnimationInterval(edit, animationId, sp[1]);
					AddLog("...OK!");

				}
				else if (patPattern.IsMatch(line))
				{
					//旧アニメーション
					sp = SurfacesSplit(line);
					if (sp.Length > 3)
					{
						AddLog("Animation Pattern(old): " + line);
						int index = sp[0].IndexOf(PATTERN);
						int animationId = int.Parse(sp[0].Substring(0, index));
						int patternId = int.Parse(sp[0].Substring(index + PATTERN.Length));
						SurfaceAnimationPattern pattern = new SurfaceAnimationPattern();

						pattern.Set(sp, 1, true);
						pattern.id = patternId;

						SetAnimationPattern(edit, animationId, patternId, pattern);
						AddLog("...OK!");
					}

				}
				else if (line.IndexOf(PALETTE_OFFSET) == 0 || line.IndexOf(PALETTE_OFFSET_EX) == 0)
				{
					//さとりすと サーフェスパレット オフセット
					String[] sp2 = line.Split(SEPARATOR, StringSplitOptions.RemoveEmptyEntries);
					if (sp2.Length >= 3)
					{
						AddLog("Satolist Surface Palette Offset: " + line);
						try
						{
							foreach (KeyValuePair<int, SurfaceDescript> p in edit)
							{
								p.Value.paletteOffset = new Point(int.Parse(sp2[1]), int.Parse(sp2[2]));
							}
							AddLog("...OK!");
						}
						catch
						{
							//foreach (KeyValuePair<int, SurfaceDescript> p in edit)
							//{
							//	p.Value.paletteOffset = new Point(int.Parse(sp2[1]), int.Parse(sp2[2]));
							//}
							AddLog("...ERROR - 数値変換できませんでした");
						}

					}
					else
					{

					}

				}
				else if (line.IndexOf(PALETTE_EXPAND) == 0)
				{
					//さとりすと サーフェスパレット　エクスパンド


					String[] sp2 = line.Split(SEPARATOR, StringSplitOptions.RemoveEmptyEntries);
					if (sp2.Length >= 2)
					{
						AddLog("Satolist Surface Palette Expand: " + line);
						try
						{
							foreach (KeyValuePair<int, SurfaceDescript> p in edit)
							{
								p.Value.paletteExpand = new SizeF(float.Parse(sp2[1]), float.Parse(sp2[1]));
							}
							AddLog("...OK!");
						}
						catch
						{
							//foreach (KeyValuePair<int, SurfaceDescript> p in edit)
							//{
							//	p.Value.paletteOffset = new Point(int.Parse(sp2[1]), int.Parse(sp2[2]));
							//}
							AddLog("...ERROR - 数値変換できませんでした");
						}

					}
					else
					{

					}

				}
				else if (line.IndexOf(PALETTE_VISIBLE) == 0)
				{
					//さとりすと　サーフェスパレット
					String[] sp2 = line.Split(SEPARATOR, StringSplitOptions.RemoveEmptyEntries);
					AddLog("Satolist Surface Palette Visible: " + line);
					try
					{
						foreach (KeyValuePair<int, SurfaceDescript> p in edit)
						{
							p.Value.isPaletteVisible = int.Parse(sp2[1]) != 0;
						}
						AddLog("...OK!");
					}
					catch
					{
						AddLog("...ERROR - 数値変換できませんでした");
					}
				}
				else if (line.IndexOf(VIEWER_VISIBLE ) == 0)
				{
					//さとりすと　サーフェスパレット
					String[] sp2 = line.Split(SEPARATOR, StringSplitOptions.RemoveEmptyEntries);
					AddLog("Satolist Surface Viewer Visible: " + line);
					try
					{
						foreach (KeyValuePair<int, SurfaceDescript> p in edit)
						{
							p.Value.isViewerVisible = int.Parse(sp2[1]) != 0;
						}
						AddLog("...OK!");
					}
					catch
					{
						AddLog("...ERROR - 数値変換できませんでした");
					}
				}
				else if (line.IndexOf(SURFACE_CHARACTER_ID) == 0 || line.IndexOf(SURFACE_CHARACTER_ID2) == 0)
				{
					String[] sp2 = line.Split(SEPARATOR, StringSplitOptions.RemoveEmptyEntries);
					AddLog("Satolist Surface Scope: " + line);
					try
					{
						foreach (KeyValuePair<int, SurfaceDescript> p in edit)
						{
							p.Value.scope = int.Parse(sp2[1]);
						}
						AddLog("...OK!");
					}
					catch
					{
						AddLog("...ERROR - 数値変換できませんでした");
					}
				}
				else if (line.IndexOf(VIEWER_ACTIVE_PATTERN ) == 0)
				{
					String[] sp2 = line.Split(SEPARATOR, StringSplitOptions.RemoveEmptyEntries);
					AddLog("Satolist Surface Animation Pattern Default: " + line);
					try
					{
						int animationId = int.Parse(sp2[1]);
						int animationPattern = int.Parse(sp2[2]);
						SetAnimationDefault(edit, animationId, animationPattern);
						AddLog("...OK!");
					}
					catch
					{
						AddLog("...ERROR - 数値変換できませんでした");
					}
				}
			}

			//return;
		}

		//アニメーションインターバルを検出
		void SetAnimationInterval(Dictionary<int, SurfaceDescript> edit, int id, String interval)
		{
			CreateAnimation(edit, id);

			String[] sp = interval.Split('+');
			List<SurfaceAnimationInterval> ilist = new List<SurfaceAnimationInterval>();

			foreach (String s in sp)
			{
				SurfaceAnimationInterval i = new SurfaceAnimationInterval(s);
				ilist.Add(i);
			}

			foreach (KeyValuePair<int, SurfaceDescript> p in edit)
			{
				foreach (SurfaceAnimationInterval i in ilist)
				{
					p.Value.animation[id].interval.Add(i);
				}
			}

		}

		void SetAnimationPattern(Dictionary<int, SurfaceDescript> edit, int animationId, int patternId, SurfaceAnimationPattern pattern)
		{
			CreateAnimation(edit, animationId);
			foreach (KeyValuePair<int, SurfaceDescript> p in edit)
			{
				if (!p.Value.animation[animationId].pattern.ContainsKey(patternId))
				{
					p.Value.animation[animationId].pattern.Add(patternId, pattern);
				}
				else
				{
					p.Value.animation[animationId].pattern[patternId] = pattern;
				}
			}
		}

		void SetAnimationDefault( Dictionary<int,SurfaceDescript> edit, int animationId, int patternId)
		{
			CreateAnimation(edit, animationId);
			foreach (KeyValuePair<int, SurfaceDescript> p in edit)
			{
				p.Value.animation[animationId].mDefaultActive = patternId;
			}
		}

		void CreateAnimation(Dictionary<int, SurfaceDescript> edit, int id)
		{
			//アニメーション インスタンスの作成
			foreach (KeyValuePair<int, SurfaceDescript> p in edit)
			{
				if (!p.Value.animation.ContainsKey(id))
				{
					SurfaceAnimation anime = new SurfaceAnimation();
					anime.id = id;
					p.Value.animation.Add(id, anime);
				}
			}
		}

		void AddSurfaceCollisionOnly(Dictionary<int, SurfaceDescript> edit, String bressValue)
		{
			//bressValue = bressValue.Replace(" ", "");
			//bressValue = bressValue.Replace("　", "");
			//bressValue = bressValue.Replace("\t", "");


			Regex patCollision = new Regex("^animation[0-9]+\\.collision");

			StringReader read = new StringReader(bressValue);
			String line;
			while (true)
			{
				line = read.ReadLine();
				if (line == null)
					return;
				line = EraseIndent(line);

				if (line.IndexOf("collision") == 0)
				{
					//collision情報
					AddLog("Collision: " + line);
					InspectCollision(line, edit);
					AddLog("...OK!");
				}
				else if (patCollision.IsMatch(line))
				{
					AddLog("Animation Collision: " + line);
					String[] sp2 = line.Split('.');
					int animationId = -1;
					int.TryParse(sp2[0].Substring(ANIMATION.Length), out animationId);
					InspectCollision(line, edit, animationId);
					AddLog("...OK!");
				}

			}

			//return;
		}

		//ファイルを文字列として読み込みます。
		//その際にコメントアウトされた内容を削除します。
		String ReadFile(String fileName)
		{
			String ret = "";
			StreamReader read = new StreamReader(fileName, Encoding.GetEncoding("Shift_JIS"));
			try
			{



				String line;
				int cpos;

				bool globalFlag = true;

				while (true)
				{
					line = read.ReadLine();
					if (line == null)
						break;

					//line = line.Replace(" ", "");
					//line = line.Replace("　", "");
					//line = line.Replace("\t", "");
					line = EraseIndent(line);

					if (line.IndexOf("{") != -1)
					{
						globalFlag = false;
					}

					if (globalFlag && (line.IndexOf(PALETTE_OFFSET) == 0 || line.IndexOf(PALETTE_OFFSET_EX ) == 0))
					{
						//パレットデフォルトオフセット
						String[] sp2 = line.Split(SEPARATOR, StringSplitOptions.RemoveEmptyEntries);
						if (sp2.Length >= 3)
						{
							try
							{
								defaultOffset = new Point(int.Parse(sp2[1]), int.Parse(sp2[2]));
							}
							catch
							{
							}
						}

					}

					if (globalFlag && line.IndexOf(PALETTE_EXPAND ) == 0)
					{
						//パレットデフォルトオフセット
						String[] sp2 = line.Split(SEPARATOR, StringSplitOptions.RemoveEmptyEntries);
						if (sp2.Length >= 2)
						{
							try
							{
								defaultExpand = new SizeF(float.Parse(sp2[1]), float.Parse(sp2[1]));
							}
							catch
							{
							}
						}

					}

					if (globalFlag && line.IndexOf(PALETTE_SIZE) == 0)
					{
						//パレットデフォルトオフセット
						String[] sp2 = line.Split(SEPARATOR, StringSplitOptions.RemoveEmptyEntries);
						if (sp2.Length >= 3)
						{
							try
							{
								defaultFrame = new Size(int.Parse(sp2[1]), int.Parse(sp2[2]));
							}
							catch
							{
							}
						}

					}

					if (globalFlag && line.IndexOf(VIEWER_VISIBLE) == 0)
					{
						//パレット表示
						String[] sp2 = line.Split(SEPARATOR, StringSplitOptions.RemoveEmptyEntries);
						if (sp2.Length >= 2)
						{
							try
							{
								defaultViewerVisible = int.Parse(sp2[1]) != 0;
							}
							catch
							{
							}
						}
					}

					if (globalFlag && line.IndexOf(PALETTE_VISIBLE) == 0)
					{
						//パレット表示
						String[] sp2 = line.Split(SEPARATOR, StringSplitOptions.RemoveEmptyEntries);
						if (sp2.Length >= 2)
						{
							try
							{
								defaultPaletteVisible = int.Parse(sp2[1]) != 0;
							}
							catch
							{
							}
						}
					}

					//さとりすとの設定コメントアウト処理を無効
					cpos = line.IndexOf(LINE_COMMENT);

					if (line.IndexOf("satolist") != -1 && cpos != -1)
					{
						int spos = line.IndexOf("satolist") + 1;
						cpos = line.IndexOf(LINE_COMMENT, spos);
						ret += line + "\r\n";
					}
					else
					{

						if (cpos != -1)
						{
							ret += line.Substring(0, cpos) + "\r\n";
						}
						else
						{
							ret += line + "\r\n";
						}
					}
				}


			}
			catch
			{

			}
			read.Close();
			return ret;
		}

		void InspectCollision(String line, Dictionary<int, SurfaceDescript> targets, int animationId = -1)
		{
			//collision行を解析。
			String[] sp = line.Split(SEPARATOR, StringSplitOptions.RemoveEmptyEntries);
			SurfaceCollision col = new SurfaceCollision();
			Regex patCollision = new Regex("^animation[0-9]+\\.collisionex");
			String[] positions;
			if (sp[0].IndexOf(COLLISION_EX) == 0 || patCollision.IsMatch(sp[0]))
			{
				//拡張collisionex宣言
				col.id = sp[1];
				col.colType = ToCollisionType(sp[2]);
				switch (col.colType)
				{
					case CollisionType.RECT:
					case CollisionType.ELLIPSE:
						positions = PositionSplit(sp, 3);
						col.points = new Point[2];
						col.points[0] = new Point(int.Parse(positions[0]), int.Parse(positions[1]));
						col.points[1] = new Point(int.Parse(positions[2]), int.Parse(positions[3]));

						if (col.points[0].X > col.points[1].X)
						{
							int tmp = col.points[0].X;
							col.points[0].X = col.points[1].X;
							col.points[1].X = tmp;
						}

						if (col.points[0].Y > col.points[1].Y)
						{
							int tmp = col.points[0].Y;
							col.points[0].Y = col.points[1].Y;
							col.points[1].Y = tmp;
						}

						AddCollision(col, targets, animationId);
						col.center = new Point((col.points[0].X + col.points[1].X) / 2, (col.points[0].Y + col.points[1].Y) / 2);
						break;
					case CollisionType.CIRCLE:
						positions = PositionSplit(sp, 3);
						col.points = new Point[1];
						col.points[0] = new Point(int.Parse(positions[0]), int.Parse(positions[1]));
						col.circleLength = int.Parse(positions[2]) * 2;
						AddCollision(col, targets, animationId);
						col.center = col.points[0];
						break;
					case CollisionType.POLYGON:
						positions = PositionSplit(sp, 3);
						int len = (positions.Length) / 2;
						col.points = new Point[len];
						int index = 0;
						for (int i = 0; i < positions.Length; i += 2)
						{
							col.points[index] = new Point(int.Parse(positions[i]), int.Parse(positions[i + 1]));
							index++;
						}
						col.center = col.points[0];
						AddCollision(col, targets, animationId);
						break;

				}

			}
			else
			{
				//通常collision宣言
				//とりあえずID要らない気がして無視
				//col.id = int.Parse(sp[0].Substring(COLLISION.Length));
				if (sp.Length >= 6)
				{

					col.colType = CollisionType.RECT;

					col.points = new Point[2];
					positions = PositionSplit(sp, 1);
					col.id = positions[4];
					col.points[0] = new Point(int.Parse(positions[0]), int.Parse(positions[1]));
					col.points[1] = new Point(int.Parse(positions[2]), int.Parse(positions[3]));

					if (col.points[0].X > col.points[1].X)
					{
						int tmp = col.points[0].X;
						col.points[0].X = col.points[1].X;
						col.points[1].X = tmp;
					}

					if (col.points[0].Y > col.points[1].Y)
					{
						int tmp = col.points[0].Y;
						col.points[0].Y = col.points[1].Y;
						col.points[1].Y = tmp;
					}

					col.center = new Point((col.points[0].X + col.points[1].X) / 2, (col.points[0].Y + col.points[1].Y) / 2);
					AddCollision(col, targets, animationId);
				}
				else
				{
					//引数不足
				}

			}

		}

		//サーフェス謎split関数をゴリ押しで実装する
		public static String[] SurfacesSplit(String str, int startIndex = -1, int count = -1)
		{
			//startindexは空白split可能index
			List<String> ret = new List<string>();
			String sp = "";

			//bool isSpliting = true;
			bool isKakko = false;
			bool isUseSpace = false;
			bool isLimited = false;

			foreach (char ch in str)
			{
				if (isLimited)
				{
					sp += ch;
				}
				else
				{
					if (startIndex == ret.Count)
					{
						isUseSpace = true;
					}

					if (IsSpliter(ch, isUseSpace) && !isKakko)
					{
						ret.Add(sp);
						sp = "";
						if (ret.Count == count - 1)
						{
							isLimited = true;
							continue;
						}
					}
					else if (ch == '[' || ch == '(')
					{
						isKakko = true;
						//sp += ch;
					}
					else if (ch == ']' || ch == ')')
					{
						isKakko = false;
						//sp += ch;
					}
					else
					{
						sp += ch;
					}
				}
			}

			if (sp.Length > 0)
			{
				ret.Add(sp);
			}

			return ret.ToArray();
		}

		private static bool IsSpliter(char ch, bool isUseSpace)
		{
			return isUseSpace ? ch == ',' || ch == ' ' : ch == ',';
		}

		public static String[] PositionSplit(String[] sp, int startIndex)
		{
			String sum = sp[startIndex];
			for (int i = startIndex + 1; i < sp.Length; i++)
			{
				sum += "," + sp[i];
			}

			Regex r = new Regex("[, ]{2}");
			sum = r.Replace(sum, ",");

			char[] spliter = { ',', ' ' };
			/*
			String[] sp2 = sum.Split(spliter);
			List<String> ret = new List<string>();
			int res;
			foreach (String s in sp2)
			{
				if (int.TryParse(s, out res))
				{
					ret.Add(s);
				}
			}
			return ret.ToArray();
			 * */
			return sum.Split(spliter);
		}

		public static String EraseIndent(String line)
		{
			int i;
			for (i = 0; i < line.Length; i++)
			{
				if (line[i] == ' ' || line[i] == '　' || line[i] == '\t')
				{
					//インデントっぽい

				}
				else
				{
					break;
				}
			}
			return line.Substring(i);
		}

		public static CollisionType ToCollisionType(String str)
		{
			switch (str)
			{
				case "rect":
					return CollisionType.RECT;
				case "ellipse":
					return CollisionType.ELLIPSE;
				case "circle":
					return CollisionType.CIRCLE;
				case "polygon":
					return CollisionType.POLYGON;
				default:
					return CollisionType.DISABLED;
			}

		}

		public void AddCollision(SurfaceCollision col, Dictionary<int, SurfaceDescript> targets, int animationId)
		{
			if (animationId == -1)
			{
				foreach (KeyValuePair<int, SurfaceDescript> p in targets)
				{
					p.Value.collisions.Add(col);
				}
			}
			else
			{
				foreach (KeyValuePair<int, SurfaceDescript> p in targets)
				{
					if (!p.Value.animation.ContainsKey(animationId))
					{
						p.Value.animation.Add(animationId, new SurfaceAnimation());
						p.Value.animation[animationId].id = animationId;
					}
					p.Value.animation[animationId].collisions.Add(col);
				}
			}

		}



	}

	//多分、サーフェス一枚ごとに、SurfaceDescriptをもって、
	//そのなかで設定の数だけSurfaceOperationを使う

	//サーフェス定義
	public class SurfaceDescript
	{
		public int surfaceID;
		public Point? paletteOffset;
		public SizeF paletteExpand;
		public bool isViewerVisible;
		public bool isPaletteVisible;
		//public String autoElement = null;					//定義なしにより自動ロードされたelement
		public Dictionary<int, SurfaceElement> element;
		public List<SurfaceCollision> collisions;
		public SortedDictionary<int, SurfaceAnimation> animation;
		public int scope = -1;
		//public List<SurfaceDefaultActivePattern> defaultActive;	//読み込み時のセットアップ用。実際に使うのは、pattern側の記述


		public SurfaceDescript(int id)
		{
			element = new Dictionary<int, SurfaceElement>();
			surfaceID = id;
			paletteOffset = null;
			paletteExpand = new SizeF(1.0f, 1.0f);
			collisions = new List<SurfaceCollision>();
			animation = new SortedDictionary<int, SurfaceAnimation>();
			//defaultActive = new List<SurfaceDefaultActivePattern>();
		}

		

	}

	//サーフェスパレット/ビューワ用の デフォルトで表示状態になるアニメーションパターン定義
	public class BindDescript
	{
		public int mCharacterId;
		public bool mIsDefault = false;
		public String mName = "no_name";

	}

	//定義情報
	//element*,パターン定義,ファイル名,X座標,Y座標

	//collision系
	//collision*,始点X,始点Y,終点X,終点Y,ID
	//collisionex*,ID,タイプ,...

	//animation系
	//animation*.interval,タイミング定義
	//animation*.pattern*,パターン定義,サーフェス番号,ウェイト,X,Y
	//animation*.option,exclusive
	//animation*.option,background
	//animation*.option,オプション（↑２個）
	//animation*.collision*,当たり判定定義
	//animation*.collision*,当たり判定定義ex



	public class SurfaceAnimation
	{
		public int id;
		public List<SurfaceAnimationInterval> interval = new List<SurfaceAnimationInterval>();
		public Dictionary<int, BindDescript> mBindData = new Dictionary<int, BindDescript>();
		public int mDefaultActive = -1;	//-1 で無効。自然数ならデフォルト表示パターン
		public String option = "";

		public SortedDictionary<int, SurfaceAnimationPattern> pattern = new SortedDictionary<int, SurfaceAnimationPattern>();
		public List<SurfaceCollision> collisions = new List<SurfaceCollision>();


		public BindDescript AddBindDescript( int characterId )
		{
			if (!mBindData.ContainsKey(characterId))
			{
				mBindData.Add(characterId, new BindDescript());
			}
			return mBindData[characterId];
		}

		public List<SurfaceAnimationInterval> GetIntervals( SurfaceAnimationInterval.Intervals inter)
		{
			List<SurfaceAnimationInterval> ret = new List<SurfaceAnimationInterval>();
			foreach (SurfaceAnimationInterval i in interval)
			{
				if (i.interval == inter)
				{
					ret.Add(i);
				}
			}
			return ret;
		}

	}

	public class SurfaceAnimationPattern
	{
		public enum Patterns
		{
			Invalid,	//不明
			Base,
			Overlay,
			Overlayfast,
			Replace,
			Interpolate,
			Asis,
			Move,
			Bind,
			Add,
			Reduce,
			Insert,
			Start,
			Stop,
			Alternativestart,
			Alternativestop
		}


		public int id;
		public Patterns pattern;
		public int surface;
		public int wait;
		public int x, y;
		public int[] args;

		public static Patterns GetPattern(String patternString)
		{
			switch (patternString)
			{
				case "base":
					return Patterns.Base;
				case "overlay":
					return Patterns.Overlay;
				case "overlayfast":
					return Patterns.Overlayfast;
				case "replace":
					return Patterns.Replace;
				case "interpolate":
					return Patterns.Interpolate;
				case "asis":
					return Patterns.Asis;
				case "move":
					return Patterns.Move;
				case "bind":
					return Patterns.Bind;
				case "add":
					return Patterns.Add;
				case "reduce":
					return Patterns.Reduce;
				case "insert":
					return Patterns.Insert;
				case "start":
					return Patterns.Start;
				case "stop":
					return Patterns.Stop;
				case "alternativestart":
					return Patterns.Alternativestart;
				case "alternativestop":
					return Patterns.Alternativestop;
				default:
					return Patterns.Invalid;
			}
		}
		public static bool IsUseArgsPattern(Patterns pat)
		{
			switch (pat)
			{
				case Patterns.Start:
				case Patterns.Stop:
				case Patterns.Alternativestop:
				case Patterns.Alternativestart:
					return true;
				default:
					return false;
			}


		}

		public void Set(String[] array, int startIndex, bool isOldVersion)
		{
			int surfaceOffset;
			int patternOffset;
			int waitOffset;
			int positionXOffset;
			int positionYOffset;

			if (isOldVersion)
			{
				surfaceOffset = 0;
				patternOffset = 2;
				waitOffset = 1;
				positionXOffset = 3;

			}
			else
			{
				surfaceOffset = 1;
				patternOffset = 0;
				waitOffset = 2;
				positionXOffset = 3;
			}
			positionYOffset = positionXOffset + 1;


			pattern = SurfaceAnimationPattern.GetPattern(array[startIndex + patternOffset]);
			if (SurfaceAnimationPattern.IsUseArgsPattern(pattern))
			{
				//この場合sp[2]が引数に使用される
				String[] patternArgs = array[startIndex + patternOffset + 1].Split('.', ',');
				args = new int[patternArgs.Length];
				for (int i = 0; i < patternArgs.Length; i++)
				{
					args[i] = int.Parse(patternArgs[i]);
				}
				surface = int.Parse(array[startIndex + surfaceOffset + 1]);
				int.TryParse(array[startIndex + waitOffset + 1], out wait);
				if (array.Length > startIndex + positionXOffset + 1)
				{
					x = int.Parse(array[startIndex + positionXOffset + 1]);
				}
				if (array.Length > startIndex + positionYOffset + 1)
				{
					y = int.Parse(array[startIndex + positionYOffset + 1]);
				}


			}
			else
			{
				surface = int.Parse(array[startIndex + surfaceOffset]);
				int.TryParse(array[startIndex + waitOffset], out wait);

				if (array.Length > startIndex + positionXOffset)
				{
					x = int.Parse(array[startIndex + positionXOffset]);
				}
				if (array.Length > startIndex + positionYOffset)
				{
					y = int.Parse(array[startIndex + positionYOffset]);
				}

				

			}

			if (isOldVersion)
			{
				wait *= 10;
			}

		}

	}


	public class SurfaceCollision
	{
		public String id;
		public CollisionType colType;
		public Point[] points;
		public int circleLength;
		public Point center;
	}

	public class SurfaceElement
	{
		public int num;
		public String pattern;
		public String fileName;
		public int x, y;
	}

	//サーフェスのファイル定義。
	//番号アクセスの場合に使用
	public class SurfaceFile
	{
		public int id;
		public String fileName;
		public String idName;

	}

	public class SurfaceAnimationInterval
	{

		public SurfaceAnimationInterval(Intervals inter, int val = 0)
		{
			interval = inter;
			value = val;
		}

		public SurfaceAnimationInterval( String inter)
		{
			String[] sp = inter.Split(',', '.');
			switch( sp[0])
			{
				case "sometimes":
					interval = Intervals.Sometimes;
					break;
				case "rarely":
					interval = Intervals.Rarely;
					break;
				case "alwarys":
					interval = Intervals.Always;
					break;
				case "runonce":
					interval = Intervals.Runonce;
					break;
				case "never":
					interval = Intervals.Never;
					break;
				case "yen-e":
					interval = Intervals.Yen_e;
					break;
				case "bind":
					interval = Intervals.Bind;
					break;
				case "random":
					interval = Intervals.Random;
					break;
				case "periodic":
					interval = Intervals.Periodic;
					break;
				case "talk":
					interval = Intervals.Talk;
					break;
				default:
					interval = Intervals.Invalid;
					break;
			}

			value = 0;
			if (sp.Length >= 2)
			{
				int.TryParse(sp[1], out value);
			}

		
		}


		public enum Intervals
		{
			Invalid,
			Sometimes,
			Rarely,
			Random,
			Periodic,
			Always,
			Runonce,
			Never,
			Yen_e,
			Talk,
			Bind
		}

		public Intervals interval;
		public int value;
	}



}
