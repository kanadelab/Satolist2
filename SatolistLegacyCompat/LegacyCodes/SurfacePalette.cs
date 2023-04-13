using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Satolist
{
	public partial class SurfacePalette : Form
	{
		private RootWindow mRoot;
		private List<SurfaceBox> mSurfaceBox;
		public System.Drawing.Font mUseFont;
		public System.Drawing.Brush mUseBrush;
		private int mPaletteCount;
	
		public SurfacePalette(RootWindow root)
		{
			InitializeComponent();
			mRoot = root;
			mSurfaceBox = new List<SurfaceBox>();
			mUseFont = new System.Drawing.Font("ＭＳ ゴシック", 8, FontStyle.Regular);
			mUseBrush = System.Drawing.Brushes.Black;
			UpdateOptionStatus();
		}

		public void UpdateData()
		{
			foreach (SurfaceBox s in mSurfaceBox)
			{
				Controls.Remove(s.pict);
				s.graph.Dispose();
				s.bitmap.Dispose();
				//toolTip1.
			}
			mSurfaceBox.Clear();

			if (ProjectData.Instance.isNoAutoSurfacePalette)
			{
				label1.Visible = true;
				return;
			}
			else
			{
				label1.Visible = false;
			}

			//表示数の取得
			mPaletteCount = 0;
			foreach (KeyValuePair<int, SurfaceDescript> p in mRoot.surfaces.surfaces.surfaces)
			{
				if (!p.Value.isPaletteVisible)
				{
					continue;
				}
				mPaletteCount++;
			}

			mRoot.mOpeningGhostDialog = new OpeningGhostDialog();

			Task.Factory.StartNew(() =>
			{
				DrawPalette();
			});

			
			mRoot.mOpeningGhostDialog.ShowDialog(this);
			
		}

		private void DrawPalette()
		{

			foreach (KeyValuePair<int, SurfaceDescript> p in mRoot.surfaces.surfaces.surfaces)
			{
				SurfaceBox add = null;
				if (!p.Value.isPaletteVisible)
				{
					continue;
				}

				//パレットオフセットを作成
				if(p.Value.paletteOffset == null)
				{
					//デフォルトをゼロに指定
					p.Value.paletteOffset = new Point(0, 0);
					int found_level = 0;
					//顔コリジョンを自動検出
					foreach(SurfaceCollision col in p.Value.collisions)
					{
						String col_name = col.id;
						col_name = col_name.ToLower();

						switch( col_name)
						{
							case "face":
							case "kao":
							case "顔":
							case "かお":
							case "フェイス":
							case "フェース":
							case "カオ":
							case "お顔":
							case "おかお":
							case "オカオ":
							case "okao":
								{
									switch(col.colType)
									{
										case CollisionType.CIRCLE:
											p.Value.paletteOffset = new Point(
												col.center.X - mRoot.surfaces.surfaces.defaultFrame.Width/2,
												col.center.Y - mRoot.surfaces.surfaces.defaultFrame.Height / 2
												);
											break;
										default:
											float x_max = -float.MaxValue;
											float x_min = float.MaxValue;
											float y_max = -float.MaxValue;
											float y_min = float.MaxValue;
											foreach(Point point in col.points)
											{
												//センターは計算して出す
												if (point.X > x_max) x_max = (float)point.X;
												if (point.X < x_min) x_min = (float)point.X;
												if (point.Y > y_max) y_max = (float)point.Y;
												if (point.Y < y_min) y_min = (float)point.Y;
											}
											p.Value.paletteOffset = new Point(
												(int)((x_max-x_min)*0.5f + x_min) - mRoot.surfaces.surfaces.defaultFrame.Width/2,
												(int)((y_max-y_min)*0.5f + y_min) - mRoot.surfaces.surfaces.defaultFrame.Height/2
												);
											break;
									}
									found_level = 10;	//最大
								}
								break;
							case "mouth":
							case "口":
							case "唇":
							case "kuti":
							case "kuchi":
							case "lip":
							case "くち":
							case "クチ":
							case "クチビル":
							case "くちびる":
								if( found_level >= 5)
								{
									break;
								}

								{
									switch (col.colType)
									{
										case CollisionType.CIRCLE:
											p.Value.paletteOffset = new Point(
												col.center.X - (int)(mRoot.surfaces.surfaces.defaultFrame.Width * 0.5f),
												col.center.Y - (int)(mRoot.surfaces.surfaces.defaultFrame.Height * 0.65)
												);
											break;
										default:
											float x_max = -float.MaxValue;
											float x_min = float.MaxValue;
											float y_max = -float.MaxValue;
											float y_min = float.MaxValue;
											foreach (Point point in col.points)
											{
												//センターは計算して出す
												if (point.X > x_max) x_max = (float)point.X;
												if (point.X < x_min) x_min = (float)point.X;
												if (point.Y > y_max) y_max = (float)point.Y;
												if (point.Y < y_min) y_min = (float)point.Y;
											}
											p.Value.paletteOffset = new Point(
												(int)((x_max - x_min) * 0.5f + x_min) - (int)(mRoot.surfaces.surfaces.defaultFrame.Width * 0.5f),
												(int)((y_max - y_min) * 0.5f + y_min) - (int)(mRoot.surfaces.surfaces.defaultFrame.Height * 0.65f)
												);
											break;
									}
									found_level = 5;
								}
								break;
							case "head":
							case "atama":
							case "あたま":
							case "アタマ":
							case "頭":
								if (found_level >= 3)
								{
									break;
								}
								{
									switch (col.colType)
									{
										case CollisionType.CIRCLE:
											p.Value.paletteOffset = new Point(
												col.center.X - (int)(mRoot.surfaces.surfaces.defaultFrame.Width * 0.5f),
												col.center.Y + col.circleLength  - (int)(mRoot.surfaces.surfaces.defaultFrame.Height * 0.2f)
												);
											break;
										default:
											float x_max = -float.MaxValue;
											float x_min = float.MaxValue;
											float y_max = -float.MaxValue;
											float y_min = float.MaxValue;
											foreach (Point point in col.points)
											{
												//センターは計算して出す
												if (point.X > x_max) x_max = (float)point.X;
												if (point.X < x_min) x_min = (float)point.X;
												if (point.Y > y_max) y_max = (float)point.Y;
												if (point.Y < y_min) y_min = (float)point.Y;
											}
											p.Value.paletteOffset = new Point(
												(int)((x_max - x_min) * 0.5f + x_min) - (int)(mRoot.surfaces.surfaces.defaultFrame.Width * 0.5f),
												(int)( y_max) - (int)(mRoot.surfaces.surfaces.defaultFrame.Height * 0.2f)
												);
											break;
									}
									found_level = 3;
								}
								break;
						}
						if( found_level >= 10)
						{
							//これ以上優先がないので打ち切り
							break;
						}
					}
				}

				//イメージ作成処理
				if (mRoot.surfaces.surfaces.surfaceTable.ContainsKey(p.Key))
				{
					add = new SurfaceBox(mRoot.surfaces.surfaces.defaultFrame, p.Key.ToString() + " [" + mRoot.surfaces.surfaces.surfaceTable[p.Key] + "]", this);
					//listBox1.Items.Add(p.Key.ToString() + " [" + surfaces.surfaceTable[p.Key] + "]");
				}
				else
				{
					add = new SurfaceBox(mRoot.surfaces.surfaces.defaultFrame, p.Key.ToString(), this);
					//listBox1.Items.Add(p.Key.ToString());
				}

				foreach (KeyValuePair<int, SurfaceAnimation> a in mRoot.surfaces.surfaces.surfaces[p.Key].animation)
				{
					if( a.Value.option.IndexOf("background") < 0)
					{
						continue;
					}
					if (a.Value.pattern.ContainsKey(a.Value.mDefaultActive))
					{
						SurfaceAnimationPattern anime = a.Value.pattern[a.Value.mDefaultActive];
						Bitmap b = mRoot.surfaces.GetPatternBitmap(p.Key, a.Key, a.Value.mDefaultActive);

						Point loc = new Point(-mRoot.surfaces.surfaces.surfaces[p.Key].paletteOffset.Value.X, 0 + -mRoot.surfaces.surfaces.surfaces[p.Key].paletteOffset.Value.Y);
						loc.X = loc.X + anime.x;
						loc.Y = loc.Y + anime.y;
						loc = new Point((int)(loc.X * mRoot.surfaces.surfaces.surfaces[p.Key].paletteExpand.Width), (int)(loc.Y * mRoot.surfaces.surfaces.surfaces[p.Key].paletteExpand.Width));
						Size size = new Size((int)(b.Width * mRoot.surfaces.surfaces.surfaces[p.Key].paletteExpand.Width), (int)(b.Height * mRoot.surfaces.surfaces.surfaces[p.Key].paletteExpand.Height));

						add.graph.DrawImage(b, new Rectangle(loc.X, loc.Y, size.Width, size.Height));
					}
				}

				int elementId = p.Key;
				Bitmap element = null;
				try
				{
					if (mRoot.surfaces.surfaces.surfaceFile.ContainsKey(elementId))
					{
						element = mRoot.surfaces.surfaces.GetBitmap(mRoot.surfaces.directory + "/" + mRoot.surfaces.surfaces.surfaceFile[elementId].fileName, elementId);
					}
					else
					{
						element = mRoot.surfaces.surfaces.GetBitmap(null, elementId);

					}
					if (element != null)
					{

						Point loc = new Point(-mRoot.surfaces.surfaces.surfaces[p.Key].paletteOffset.Value.X, 0 + -mRoot.surfaces.surfaces.surfaces[p.Key].paletteOffset.Value.Y);
						loc = new Point((int)(loc.X * mRoot.surfaces.surfaces.surfaces[p.Key].paletteExpand.Width), (int)(loc.Y * mRoot.surfaces.surfaces.surfaces[p.Key].paletteExpand.Width));
						Size size = new Size((int)(element.Width * mRoot.surfaces.surfaces.surfaces[p.Key].paletteExpand.Width), (int)(element.Height * mRoot.surfaces.surfaces.surfaces[p.Key].paletteExpand.Height));
						add.graph.DrawImage(element, new Rectangle(loc.X, loc.Y, size.Width, size.Height));
					}
				}
				catch { }

				foreach (KeyValuePair<int, SurfaceAnimation> a in mRoot.surfaces.surfaces.surfaces[p.Key].animation)
				{
					try
					{
						if (a.Value.option.IndexOf("background") >= 0)
						{
							continue;
						}
						if (a.Value.pattern.ContainsKey(a.Value.mDefaultActive))
						{
							SurfaceAnimationPattern anime = a.Value.pattern[a.Value.mDefaultActive];
							Bitmap b = mRoot.surfaces.GetPatternBitmap(p.Key, a.Key, a.Value.mDefaultActive);

							Point loc = new Point(-mRoot.surfaces.surfaces.surfaces[p.Key].paletteOffset.Value.X, 0 + -mRoot.surfaces.surfaces.surfaces[p.Key].paletteOffset.Value.Y);
							loc.X = loc.X + anime.x;
							loc.Y = loc.Y + anime.y;
							loc = new Point((int)(loc.X * mRoot.surfaces.surfaces.surfaces[p.Key].paletteExpand.Width), (int)(loc.Y * mRoot.surfaces.surfaces.surfaces[p.Key].paletteExpand.Width));
							Size size = new Size((int)(b.Width * mRoot.surfaces.surfaces.surfaces[p.Key].paletteExpand.Width), (int)(b.Height * mRoot.surfaces.surfaces.surfaces[p.Key].paletteExpand.Height));

							add.graph.DrawImage(b, new Rectangle(loc.X, loc.Y, size.Width, size.Height));
						}
					}
					catch { }	//TODO: -1を指定するアニメーションがあるとコケるので一旦封殺しておく…　そのうち対応する
				}

				//コンテナ追加処理
				add.graph.FillRectangle(Brushes.White, new Rectangle(0, mRoot.surfaces.surfaces.defaultFrame.Height - 10, mRoot.surfaces.surfaces.defaultFrame.Width, 10));
				if (mRoot.surfaces.surfaces.surfaceTable.ContainsKey(p.Key))
				{
					add.graph.DrawString(p.Key.ToString() + " [" + mRoot.surfaces.surfaces.surfaceTable[p.Key] + "]", mUseFont, mUseBrush, 2, mRoot.surfaces.surfaces.defaultFrame.Height - 10);
					//listBox1.Items.Add(p.Key.ToString() + " [" + surfaces.surfaceTable[p.Key] + "]");
					toolTip1.SetToolTip(add.pict, p.Key.ToString() + " [" + mRoot.surfaces.surfaces.surfaceTable[p.Key] + "]");
				}
				else
				{
					add.graph.DrawString(p.Key.ToString(), mUseFont, mUseBrush, 2, mRoot.surfaces.surfaces.defaultFrame.Height - 10);
					toolTip1.SetToolTip(add.pict, p.Key.ToString());
					//listBox1.Items.Add(p.Key.ToString());
				}


				mSurfaceBox.Add(add);
				if (mRoot.mOpeningGhostDialog != null)
				{
					mRoot.mOpeningGhostDialog.AddProgress(mSurfaceBox.Count, mPaletteCount);
				}
			}
			
			Invoke(new Action(DrawPictureBox));
		}

		//ピクチャボックス追加（非マルチスレッド）
		private void DrawPictureBox()
		{
			
			
			int index = 0;
			foreach (SurfaceBox add in mSurfaceBox)
			{
				add.pict.Location = GetNextLocation(index);
				

				Controls.Add(add.pict);
				index++;
			}

			//ここでパレットは読み込み完了
			if (mRoot.mOpeningGhostDialog != null)
			{

				//Task.Factory.StartNew(() =>
				//{
				//	System.Threading.Thread.Sleep(100);
					mRoot.mOpeningGhostDialog.CloseInvoke();
				//});
			}

			
		}


		public Point GetNextLocation( int num )
		{
			int widthCount = (Size.Width - 50) / mRoot.surfaces.surfaces.defaultFrame.Width;
			if (widthCount < 1)
				widthCount = 1;
			return new Point((num % widthCount) * (mRoot.surfaces.surfaces.defaultFrame.Width+10) + 20, (num / widthCount) * (mRoot.surfaces.surfaces.defaultFrame.Height+10) + 40);
		}

		public void Relocate()
		{
			for (int i = 0; i < mSurfaceBox.Count; i++)
			{
				Point p = GetNextLocation(i);
				p.Offset(-HorizontalScroll.Value, -VerticalScroll.Value);
				mSurfaceBox[i].pict.Location = p;
			}
			
			//Point mp = new Point(0,0);
			//mp.Offset(HorizontalScroll.Value, VerticalScroll.Value);
			//menuStrip1.Location = mp;
		}

		private void SurfacePalette_SizeChanged(object sender, EventArgs e)
		{
			Relocate();
		}

		private void シェルを選択ToolStripMenuItem_Click(object sender, EventArgs e)
		{
			
		}

		private void 使い方ToolStripMenuItem_Click(object sender, EventArgs e)
		{
#if LEGACY
			(new SurfacePaletteHelp()).ShowDialog(this);
#endif
		}

		private void SurfacePalette_FormClosing(object sender, FormClosingEventArgs e)
		{
			Hide();
			e.Cancel = true;
		}

		public void palet_DoubleClick(object sender, EventArgs e)
		{
			mRoot.surfaces.SurfaceDoubleClick(((PictureBox)sender).Name);
		}

		private void リストをダブルクリックでサーフェス変更を挿入ToolStripMenuItem_Click(object sender, EventArgs e)
		{
#if LEGACY
			ProjectData.Instance.isUseSurfaceInsert = !ProjectData.Instance.isUseSurfaceInsert;
			UpdateOptionStatus();
			mRoot.surfaces.UpdateOptionStatus();
#endif
		}

		private void 挿入に里々形式のサーフェス変更を使用するToolStripMenuItem_Click(object sender, EventArgs e)
		{
#if LEGACY
			ProjectData.Instance.isUseSurfaceInsertSatori = !ProjectData.Instance.isUseSurfaceInsertSatori;
			UpdateOptionStatus();
			mRoot.surfaces.UpdateOptionStatus();
#endif
		}

		private void オプションToolStripMenuItem_Click(object sender, EventArgs e)
		{

			//ToolStripMenuItem item = (ToolStripMenuItem)sender;
			//((ToolStripMenuItem)item.DropDownItems["リストをダブルクリックでサーフェス変更を挿入ToolStripMenuItem"]).Checked = ProjectData.Instance.isUseSurfaceInsert;
			//((ToolStripMenuItem)item.DropDownItems["挿入に里々形式のサーフェス変更を使用するToolStripMenuItem"]).Checked = ProjectData.Instance.isUseSurfaceInsertSatori;

		}

		public void UpdateOptionStatus()
		{
			ToolStripMenuItem item = (ToolStripMenuItem)menuStrip1.Items["オプションToolStripMenuItem"];
			((ToolStripMenuItem)item.DropDownItems["リストをダブルクリックでサーフェス変更を挿入ToolStripMenuItem"]).Checked = ProjectData.Instance.isUseSurfaceInsert;
			((ToolStripMenuItem)item.DropDownItems["挿入に里々形式のサーフェス変更を使用するToolStripMenuItem"]).Checked = ProjectData.Instance.isUseSurfaceInsertSatori;
			((ToolStripMenuItem)item.DropDownItems["定義のないサーフェスも表示するリロードが必要ToolStripMenuItem"]).Checked = ProjectData.Instance.isSurfaceUseNoSetting;
			((ToolStripMenuItem)item.DropDownItems["サーフェスパレットを無効にするToolStripMenuItem"]).Checked = ProjectData.Instance.isNoAutoSurfacePalette;
			//((ToolStripMenuItem)item.DropDownItems["着せ替えリストを表示ToolStripMenuItem"]).Checked = ProjectData.Instance.isShowSurfaceBind.mValue;

			if (ProjectData.Instance.isNoAutoSurfacePalette)
			{
				label1.Visible = true;
				
			}
			else
			{
				label1.Visible = false;
			}
		}

		private void シェルを読み込むToolStripMenuItem_Click(object sender, EventArgs e)
		{
			mRoot.surfaces.シェルを読み込むToolStripMenuItem_Click(sender, e);
		}

		private void シェルをリロードToolStripMenuItem_Click(object sender, EventArgs e)
		{
			mRoot.surfaces.シェルをリロードToolStripMenuItem_Click(sender, e);
		}

		private void 定義のないサーフェスも表示するリロードが必要ToolStripMenuItem_Click(object sender, EventArgs e)
		{
			ProjectData.Instance.isSurfaceUseNoSetting = !ProjectData.Instance.isSurfaceUseNoSetting;
			UpdateOptionStatus();
			mRoot.surfaces.UpdateOptionStatus();
		}

		private void 着せ替えリストを表示ToolStripMenuItem_Click(object sender, EventArgs e)
		{
			//ProjectData.Instance.isShowSurfaceBind.mValue = !ProjectData.Instance.isShowSurfaceBind.mValue;
			//UpdateOptionStatus();
			//mRoot.surfacePalette.UpdateOptionStatus();
		}

		private void サーフェスパレットを無効にするToolStripMenuItem_Click(object sender, EventArgs e)
		{
			ProjectData.Instance.isNoAutoSurfacePalette = !ProjectData.Instance.isNoAutoSurfacePalette;
			UpdateOptionStatus();
		}

		private void toolStripMenuItem1_Click(object sender, EventArgs e)
		{

		}

		private void 更新ファイル作成ToolStripMenuItem_Click(object sender, EventArgs e)
		{
			mRoot.surfaces.更新ファイル作成ToolStripMenuItem_Click(sender, e);
		}

		private void narファイル作成ToolStripMenuItem_Click(object sender, EventArgs e)
		{
			mRoot.surfaces.narファイル作成ToolStripMenuItem_Click(sender, e);
		}

		private void 更新ファイルnar作成ToolStripMenuItem_Click(object sender, EventArgs e)
		{
			mRoot.surfaces.更新ファイルnar作成ToolStripMenuItem_Click(sender, e);
		}

		private void 更新ファイルを作成してアップロードToolStripMenuItem_Click(object sender, EventArgs e)
		{
			mRoot.surfaces.更新ファイルを作成してアップロードToolStripMenuItem_Click(sender, e);
		}

		private void シェルをエクスプローラで開くToolStripMenuItem_Click(object sender, EventArgs e)
		{
			mRoot.surfaces.シェルをエクスプローラで開くToolStripMenuItem_Click(sender, e);
		}

		public void UpdateMenuEnable(bool is_enable)
		{
			{
				ToolStripMenuItem item = (ToolStripMenuItem)menuStrip1.Items["ツールtoolStripMenuItem1"];
				item.DropDownItems["更新ファイル作成ToolStripMenuItem"].Enabled = is_enable;
				item.DropDownItems["narファイル作成ToolStripMenuItem"].Enabled = is_enable;
				item.DropDownItems["更新ファイルnar作成ToolStripMenuItem"].Enabled = is_enable;
				item.DropDownItems["更新ファイルを作成してアップロードToolStripMenuItem"].Enabled = is_enable;
			}


			{
				ToolStripMenuItem item = (ToolStripMenuItem)menuStrip1.Items["シェルを選択ToolStripMenuItem"];
				item.DropDownItems["シェルをエクスプローラで開くToolStripMenuItem"].Enabled = is_enable;
			}
		}
	}


	class SurfaceBox
	{
		public Bitmap bitmap;
		public Graphics graph;
		public PictureBox pict;

		public SurfaceBox( Size size, String text, SurfacePalette box )
		{
			bitmap = new Bitmap(size.Width, size.Height);
			graph = Graphics.FromImage(bitmap);
			pict = new PictureBox();
			pict.Image = bitmap;
			pict.Size = size;
			pict.Name = text;
			pict.DoubleClick += box.palet_DoubleClick;
			graph.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.High;
			
		}

	}

}
