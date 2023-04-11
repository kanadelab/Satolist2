namespace Satolist
{
    partial class SurfaceViewer
    {
        /// <summary>
        /// 必要なデザイナー変数です。
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// 使用中のリソースをすべてクリーンアップします。
        /// </summary>
        /// <param name="disposing">マネージ リソースが破棄される場合 true、破棄されない場合は false です。</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows フォーム デザイナーで生成されたコード

        /// <summary>
        /// デザイナー サポートに必要なメソッドです。このメソッドの内容を
        /// コード エディターで変更しないでください。
        /// </summary>
        private void InitializeComponent()
        {
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(SurfaceViewer));
			this.textBox1 = new System.Windows.Forms.TextBox();
			this.menuStrip1 = new System.Windows.Forms.MenuStrip();
			this.シェルを選択ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.シェルを読み込むToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.シェルをリロードToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.シェルのさわり判定のみ再読み込みToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripSeparator4 = new System.Windows.Forms.ToolStripSeparator();
			this.シェルをエクスプローラで開くToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripSeparator5 = new System.Windows.Forms.ToolStripSeparator();
			this.撮影ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.オプションToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.リストをダブルクリックでサーフェス変更を挿入ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.里々形式のサーフェス変更を使用するToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.さわり判定箇所の表示ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.定義のないサーフェスも表示するリロードが必要ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.着せ替えリストを表示ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.スコープToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.sakuraToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.keroToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripMenuItem2 = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripMenuItem3 = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripMenuItem4 = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripMenuItem5 = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripMenuItem6 = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripMenuItem7 = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripMenuItem8 = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripMenuItem9 = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripSeparator1 = new System.Windows.Forms.ToolStripSeparator();
			this.surfacestxtによるスコープ指定を優先ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.表示倍率ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.x01ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.x025ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.x05ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.x075ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.x10ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.x125ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.x15ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.x175ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.x20ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.アニメーションを再生を有効にするToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.描画領域の調整ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.ツールToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.surfacestxtの座標倍率を変化ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.読み込みログToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.sERIKO定義を旧定義から新定義に置換ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripSeparator2 = new System.Windows.Forms.ToolStripSeparator();
			this.更新ファイル作成ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.narファイル作成ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.更新ファイルnar作成ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripSeparator3 = new System.Windows.Forms.ToolStripSeparator();
			this.更新ファイルを作成してアップロードToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.ベータ版についてToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.splitter1 = new System.Windows.Forms.Splitter();
			this.splitter2 = new System.Windows.Forms.Splitter();
			this.checkedListBox1 = new System.Windows.Forms.CheckedListBox();
			this.listBox1 = new System.Windows.Forms.ListBox();
			this.pictureBox1 = new System.Windows.Forms.PictureBox();
			this.menuStrip1.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).BeginInit();
			this.SuspendLayout();
			// 
			// textBox1
			// 
			resources.ApplyResources(this.textBox1, "textBox1");
			this.textBox1.Name = "textBox1";
			this.textBox1.ReadOnly = true;
			// 
			// menuStrip1
			// 
			this.menuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.シェルを選択ToolStripMenuItem,
            this.オプションToolStripMenuItem,
            this.ツールToolStripMenuItem,
            this.ベータ版についてToolStripMenuItem});
			resources.ApplyResources(this.menuStrip1, "menuStrip1");
			this.menuStrip1.Name = "menuStrip1";
			// 
			// シェルを選択ToolStripMenuItem
			// 
			this.シェルを選択ToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.シェルを読み込むToolStripMenuItem,
            this.シェルをリロードToolStripMenuItem,
            this.シェルのさわり判定のみ再読み込みToolStripMenuItem,
            this.toolStripSeparator4,
            this.シェルをエクスプローラで開くToolStripMenuItem,
            this.toolStripSeparator5,
            this.撮影ToolStripMenuItem});
			this.シェルを選択ToolStripMenuItem.Name = "シェルを選択ToolStripMenuItem";
			resources.ApplyResources(this.シェルを選択ToolStripMenuItem, "シェルを選択ToolStripMenuItem");
			this.シェルを選択ToolStripMenuItem.Click += new System.EventHandler(this.シェルを選択ToolStripMenuItem_Click);
			// 
			// シェルを読み込むToolStripMenuItem
			// 
			this.シェルを読み込むToolStripMenuItem.Name = "シェルを読み込むToolStripMenuItem";
			resources.ApplyResources(this.シェルを読み込むToolStripMenuItem, "シェルを読み込むToolStripMenuItem");
			this.シェルを読み込むToolStripMenuItem.Click += new System.EventHandler(this.シェルを読み込むToolStripMenuItem_Click);
			// 
			// シェルをリロードToolStripMenuItem
			// 
			this.シェルをリロードToolStripMenuItem.Name = "シェルをリロードToolStripMenuItem";
			resources.ApplyResources(this.シェルをリロードToolStripMenuItem, "シェルをリロードToolStripMenuItem");
			this.シェルをリロードToolStripMenuItem.Click += new System.EventHandler(this.シェルをリロードToolStripMenuItem_Click);
			// 
			// シェルのさわり判定のみ再読み込みToolStripMenuItem
			// 
			this.シェルのさわり判定のみ再読み込みToolStripMenuItem.Name = "シェルのさわり判定のみ再読み込みToolStripMenuItem";
			resources.ApplyResources(this.シェルのさわり判定のみ再読み込みToolStripMenuItem, "シェルのさわり判定のみ再読み込みToolStripMenuItem");
			this.シェルのさわり判定のみ再読み込みToolStripMenuItem.Click += new System.EventHandler(this.シェルのさわり判定のみ再読み込みToolStripMenuItem_Click);
			// 
			// toolStripSeparator4
			// 
			this.toolStripSeparator4.Name = "toolStripSeparator4";
			resources.ApplyResources(this.toolStripSeparator4, "toolStripSeparator4");
			// 
			// シェルをエクスプローラで開くToolStripMenuItem
			// 
			this.シェルをエクスプローラで開くToolStripMenuItem.Name = "シェルをエクスプローラで開くToolStripMenuItem";
			resources.ApplyResources(this.シェルをエクスプローラで開くToolStripMenuItem, "シェルをエクスプローラで開くToolStripMenuItem");
			this.シェルをエクスプローラで開くToolStripMenuItem.Click += new System.EventHandler(this.シェルをエクスプローラで開くToolStripMenuItem_Click);
			// 
			// toolStripSeparator5
			// 
			this.toolStripSeparator5.Name = "toolStripSeparator5";
			resources.ApplyResources(this.toolStripSeparator5, "toolStripSeparator5");
			// 
			// 撮影ToolStripMenuItem
			// 
			this.撮影ToolStripMenuItem.Name = "撮影ToolStripMenuItem";
			resources.ApplyResources(this.撮影ToolStripMenuItem, "撮影ToolStripMenuItem");
			this.撮影ToolStripMenuItem.Click += new System.EventHandler(this.撮影ToolStripMenuItem_Click);
			// 
			// オプションToolStripMenuItem
			// 
			this.オプションToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.リストをダブルクリックでサーフェス変更を挿入ToolStripMenuItem,
            this.里々形式のサーフェス変更を使用するToolStripMenuItem,
            this.さわり判定箇所の表示ToolStripMenuItem,
            this.定義のないサーフェスも表示するリロードが必要ToolStripMenuItem,
            this.着せ替えリストを表示ToolStripMenuItem,
            this.スコープToolStripMenuItem,
            this.表示倍率ToolStripMenuItem,
            this.アニメーションを再生を有効にするToolStripMenuItem,
            this.描画領域の調整ToolStripMenuItem});
			this.オプションToolStripMenuItem.Name = "オプションToolStripMenuItem";
			resources.ApplyResources(this.オプションToolStripMenuItem, "オプションToolStripMenuItem");
			this.オプションToolStripMenuItem.Click += new System.EventHandler(this.オプションToolStripMenuItem_Click);
			// 
			// リストをダブルクリックでサーフェス変更を挿入ToolStripMenuItem
			// 
			this.リストをダブルクリックでサーフェス変更を挿入ToolStripMenuItem.Name = "リストをダブルクリックでサーフェス変更を挿入ToolStripMenuItem";
			resources.ApplyResources(this.リストをダブルクリックでサーフェス変更を挿入ToolStripMenuItem, "リストをダブルクリックでサーフェス変更を挿入ToolStripMenuItem");
			this.リストをダブルクリックでサーフェス変更を挿入ToolStripMenuItem.Click += new System.EventHandler(this.リストをダブルクリックでサーフェス変更を挿入ToolStripMenuItem_Click);
			// 
			// 里々形式のサーフェス変更を使用するToolStripMenuItem
			// 
			this.里々形式のサーフェス変更を使用するToolStripMenuItem.Name = "里々形式のサーフェス変更を使用するToolStripMenuItem";
			resources.ApplyResources(this.里々形式のサーフェス変更を使用するToolStripMenuItem, "里々形式のサーフェス変更を使用するToolStripMenuItem");
			this.里々形式のサーフェス変更を使用するToolStripMenuItem.Click += new System.EventHandler(this.里々形式のサーフェス変更を使用するToolStripMenuItem_Click);
			// 
			// さわり判定箇所の表示ToolStripMenuItem
			// 
			this.さわり判定箇所の表示ToolStripMenuItem.Name = "さわり判定箇所の表示ToolStripMenuItem";
			resources.ApplyResources(this.さわり判定箇所の表示ToolStripMenuItem, "さわり判定箇所の表示ToolStripMenuItem");
			this.さわり判定箇所の表示ToolStripMenuItem.Click += new System.EventHandler(this.さわり判定箇所の表示ToolStripMenuItem_Click);
			// 
			// 定義のないサーフェスも表示するリロードが必要ToolStripMenuItem
			// 
			this.定義のないサーフェスも表示するリロードが必要ToolStripMenuItem.Name = "定義のないサーフェスも表示するリロードが必要ToolStripMenuItem";
			resources.ApplyResources(this.定義のないサーフェスも表示するリロードが必要ToolStripMenuItem, "定義のないサーフェスも表示するリロードが必要ToolStripMenuItem");
			this.定義のないサーフェスも表示するリロードが必要ToolStripMenuItem.Click += new System.EventHandler(this.定義のないサーフェスも表示するリロードが必要ToolStripMenuItem_Click);
			// 
			// 着せ替えリストを表示ToolStripMenuItem
			// 
			this.着せ替えリストを表示ToolStripMenuItem.Name = "着せ替えリストを表示ToolStripMenuItem";
			resources.ApplyResources(this.着せ替えリストを表示ToolStripMenuItem, "着せ替えリストを表示ToolStripMenuItem");
			this.着せ替えリストを表示ToolStripMenuItem.Click += new System.EventHandler(this.着せ替えリストを表示ToolStripMenuItem_Click);
			// 
			// スコープToolStripMenuItem
			// 
			this.スコープToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.sakuraToolStripMenuItem,
            this.keroToolStripMenuItem,
            this.toolStripMenuItem2,
            this.toolStripMenuItem3,
            this.toolStripMenuItem4,
            this.toolStripMenuItem5,
            this.toolStripMenuItem6,
            this.toolStripMenuItem7,
            this.toolStripMenuItem8,
            this.toolStripMenuItem9,
            this.toolStripSeparator1,
            this.surfacestxtによるスコープ指定を優先ToolStripMenuItem});
			this.スコープToolStripMenuItem.Name = "スコープToolStripMenuItem";
			resources.ApplyResources(this.スコープToolStripMenuItem, "スコープToolStripMenuItem");
			// 
			// sakuraToolStripMenuItem
			// 
			this.sakuraToolStripMenuItem.Checked = true;
			this.sakuraToolStripMenuItem.CheckState = System.Windows.Forms.CheckState.Checked;
			this.sakuraToolStripMenuItem.Name = "sakuraToolStripMenuItem";
			resources.ApplyResources(this.sakuraToolStripMenuItem, "sakuraToolStripMenuItem");
			this.sakuraToolStripMenuItem.Click += new System.EventHandler(this.sakuraToolStripMenuItem_Click);
			// 
			// keroToolStripMenuItem
			// 
			this.keroToolStripMenuItem.Name = "keroToolStripMenuItem";
			resources.ApplyResources(this.keroToolStripMenuItem, "keroToolStripMenuItem");
			this.keroToolStripMenuItem.Click += new System.EventHandler(this.sakuraToolStripMenuItem_Click);
			// 
			// toolStripMenuItem2
			// 
			this.toolStripMenuItem2.Name = "toolStripMenuItem2";
			resources.ApplyResources(this.toolStripMenuItem2, "toolStripMenuItem2");
			this.toolStripMenuItem2.Click += new System.EventHandler(this.sakuraToolStripMenuItem_Click);
			// 
			// toolStripMenuItem3
			// 
			this.toolStripMenuItem3.Name = "toolStripMenuItem3";
			resources.ApplyResources(this.toolStripMenuItem3, "toolStripMenuItem3");
			this.toolStripMenuItem3.Click += new System.EventHandler(this.sakuraToolStripMenuItem_Click);
			// 
			// toolStripMenuItem4
			// 
			this.toolStripMenuItem4.Name = "toolStripMenuItem4";
			resources.ApplyResources(this.toolStripMenuItem4, "toolStripMenuItem4");
			this.toolStripMenuItem4.Click += new System.EventHandler(this.sakuraToolStripMenuItem_Click);
			// 
			// toolStripMenuItem5
			// 
			this.toolStripMenuItem5.Name = "toolStripMenuItem5";
			resources.ApplyResources(this.toolStripMenuItem5, "toolStripMenuItem5");
			this.toolStripMenuItem5.Click += new System.EventHandler(this.sakuraToolStripMenuItem_Click);
			// 
			// toolStripMenuItem6
			// 
			this.toolStripMenuItem6.Name = "toolStripMenuItem6";
			resources.ApplyResources(this.toolStripMenuItem6, "toolStripMenuItem6");
			this.toolStripMenuItem6.Click += new System.EventHandler(this.sakuraToolStripMenuItem_Click);
			// 
			// toolStripMenuItem7
			// 
			this.toolStripMenuItem7.Name = "toolStripMenuItem7";
			resources.ApplyResources(this.toolStripMenuItem7, "toolStripMenuItem7");
			this.toolStripMenuItem7.Click += new System.EventHandler(this.sakuraToolStripMenuItem_Click);
			// 
			// toolStripMenuItem8
			// 
			this.toolStripMenuItem8.Name = "toolStripMenuItem8";
			resources.ApplyResources(this.toolStripMenuItem8, "toolStripMenuItem8");
			this.toolStripMenuItem8.Click += new System.EventHandler(this.sakuraToolStripMenuItem_Click);
			// 
			// toolStripMenuItem9
			// 
			this.toolStripMenuItem9.Name = "toolStripMenuItem9";
			resources.ApplyResources(this.toolStripMenuItem9, "toolStripMenuItem9");
			this.toolStripMenuItem9.Click += new System.EventHandler(this.sakuraToolStripMenuItem_Click);
			// 
			// toolStripSeparator1
			// 
			this.toolStripSeparator1.Name = "toolStripSeparator1";
			resources.ApplyResources(this.toolStripSeparator1, "toolStripSeparator1");
			// 
			// surfacestxtによるスコープ指定を優先ToolStripMenuItem
			// 
			this.surfacestxtによるスコープ指定を優先ToolStripMenuItem.Name = "surfacestxtによるスコープ指定を優先ToolStripMenuItem";
			resources.ApplyResources(this.surfacestxtによるスコープ指定を優先ToolStripMenuItem, "surfacestxtによるスコープ指定を優先ToolStripMenuItem");
			this.surfacestxtによるスコープ指定を優先ToolStripMenuItem.Click += new System.EventHandler(this.surfacestxtによるスコープ指定を優先ToolStripMenuItem_Click);
			// 
			// 表示倍率ToolStripMenuItem
			// 
			this.表示倍率ToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.x01ToolStripMenuItem,
            this.x025ToolStripMenuItem,
            this.x05ToolStripMenuItem,
            this.x075ToolStripMenuItem,
            this.x10ToolStripMenuItem,
            this.x125ToolStripMenuItem,
            this.x15ToolStripMenuItem,
            this.x175ToolStripMenuItem,
            this.x20ToolStripMenuItem});
			this.表示倍率ToolStripMenuItem.Name = "表示倍率ToolStripMenuItem";
			resources.ApplyResources(this.表示倍率ToolStripMenuItem, "表示倍率ToolStripMenuItem");
			// 
			// x01ToolStripMenuItem
			// 
			this.x01ToolStripMenuItem.Name = "x01ToolStripMenuItem";
			resources.ApplyResources(this.x01ToolStripMenuItem, "x01ToolStripMenuItem");
			this.x01ToolStripMenuItem.Click += new System.EventHandler(this.x01ToolStripMenuItem_Click);
			// 
			// x025ToolStripMenuItem
			// 
			this.x025ToolStripMenuItem.Name = "x025ToolStripMenuItem";
			resources.ApplyResources(this.x025ToolStripMenuItem, "x025ToolStripMenuItem");
			this.x025ToolStripMenuItem.Click += new System.EventHandler(this.x01ToolStripMenuItem_Click);
			// 
			// x05ToolStripMenuItem
			// 
			this.x05ToolStripMenuItem.Name = "x05ToolStripMenuItem";
			resources.ApplyResources(this.x05ToolStripMenuItem, "x05ToolStripMenuItem");
			this.x05ToolStripMenuItem.Click += new System.EventHandler(this.x01ToolStripMenuItem_Click);
			// 
			// x075ToolStripMenuItem
			// 
			this.x075ToolStripMenuItem.Name = "x075ToolStripMenuItem";
			resources.ApplyResources(this.x075ToolStripMenuItem, "x075ToolStripMenuItem");
			this.x075ToolStripMenuItem.Click += new System.EventHandler(this.x01ToolStripMenuItem_Click);
			// 
			// x10ToolStripMenuItem
			// 
			this.x10ToolStripMenuItem.Checked = true;
			this.x10ToolStripMenuItem.CheckState = System.Windows.Forms.CheckState.Checked;
			this.x10ToolStripMenuItem.Name = "x10ToolStripMenuItem";
			resources.ApplyResources(this.x10ToolStripMenuItem, "x10ToolStripMenuItem");
			this.x10ToolStripMenuItem.Click += new System.EventHandler(this.x01ToolStripMenuItem_Click);
			// 
			// x125ToolStripMenuItem
			// 
			this.x125ToolStripMenuItem.Name = "x125ToolStripMenuItem";
			resources.ApplyResources(this.x125ToolStripMenuItem, "x125ToolStripMenuItem");
			this.x125ToolStripMenuItem.Click += new System.EventHandler(this.x01ToolStripMenuItem_Click);
			// 
			// x15ToolStripMenuItem
			// 
			this.x15ToolStripMenuItem.Name = "x15ToolStripMenuItem";
			resources.ApplyResources(this.x15ToolStripMenuItem, "x15ToolStripMenuItem");
			this.x15ToolStripMenuItem.Click += new System.EventHandler(this.x01ToolStripMenuItem_Click);
			// 
			// x175ToolStripMenuItem
			// 
			this.x175ToolStripMenuItem.Name = "x175ToolStripMenuItem";
			resources.ApplyResources(this.x175ToolStripMenuItem, "x175ToolStripMenuItem");
			this.x175ToolStripMenuItem.Click += new System.EventHandler(this.x01ToolStripMenuItem_Click);
			// 
			// x20ToolStripMenuItem
			// 
			this.x20ToolStripMenuItem.Name = "x20ToolStripMenuItem";
			resources.ApplyResources(this.x20ToolStripMenuItem, "x20ToolStripMenuItem");
			this.x20ToolStripMenuItem.Click += new System.EventHandler(this.x01ToolStripMenuItem_Click);
			// 
			// アニメーションを再生を有効にするToolStripMenuItem
			// 
			this.アニメーションを再生を有効にするToolStripMenuItem.Name = "アニメーションを再生を有効にするToolStripMenuItem";
			resources.ApplyResources(this.アニメーションを再生を有効にするToolStripMenuItem, "アニメーションを再生を有効にするToolStripMenuItem");
			this.アニメーションを再生を有効にするToolStripMenuItem.Click += new System.EventHandler(this.アニメーションを再生を有効にするToolStripMenuItem_Click);
			// 
			// 描画領域の調整ToolStripMenuItem
			// 
			this.描画領域の調整ToolStripMenuItem.Name = "描画領域の調整ToolStripMenuItem";
			resources.ApplyResources(this.描画領域の調整ToolStripMenuItem, "描画領域の調整ToolStripMenuItem");
			this.描画領域の調整ToolStripMenuItem.Click += new System.EventHandler(this.描画領域の調整ToolStripMenuItem_Click);
			// 
			// ツールToolStripMenuItem
			// 
			this.ツールToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.surfacestxtの座標倍率を変化ToolStripMenuItem,
            this.読み込みログToolStripMenuItem,
            this.sERIKO定義を旧定義から新定義に置換ToolStripMenuItem,
            this.toolStripSeparator2,
            this.更新ファイル作成ToolStripMenuItem,
            this.narファイル作成ToolStripMenuItem,
            this.更新ファイルnar作成ToolStripMenuItem,
            this.toolStripSeparator3,
            this.更新ファイルを作成してアップロードToolStripMenuItem});
			this.ツールToolStripMenuItem.Name = "ツールToolStripMenuItem";
			resources.ApplyResources(this.ツールToolStripMenuItem, "ツールToolStripMenuItem");
			// 
			// surfacestxtの座標倍率を変化ToolStripMenuItem
			// 
			this.surfacestxtの座標倍率を変化ToolStripMenuItem.Name = "surfacestxtの座標倍率を変化ToolStripMenuItem";
			resources.ApplyResources(this.surfacestxtの座標倍率を変化ToolStripMenuItem, "surfacestxtの座標倍率を変化ToolStripMenuItem");
			this.surfacestxtの座標倍率を変化ToolStripMenuItem.Click += new System.EventHandler(this.surfacestxtの座標倍率を変化ToolStripMenuItem_Click);
			// 
			// 読み込みログToolStripMenuItem
			// 
			this.読み込みログToolStripMenuItem.Name = "読み込みログToolStripMenuItem";
			resources.ApplyResources(this.読み込みログToolStripMenuItem, "読み込みログToolStripMenuItem");
			this.読み込みログToolStripMenuItem.Click += new System.EventHandler(this.読み込みログToolStripMenuItem_Click);
			// 
			// sERIKO定義を旧定義から新定義に置換ToolStripMenuItem
			// 
			this.sERIKO定義を旧定義から新定義に置換ToolStripMenuItem.Name = "sERIKO定義を旧定義から新定義に置換ToolStripMenuItem";
			resources.ApplyResources(this.sERIKO定義を旧定義から新定義に置換ToolStripMenuItem, "sERIKO定義を旧定義から新定義に置換ToolStripMenuItem");
			this.sERIKO定義を旧定義から新定義に置換ToolStripMenuItem.Click += new System.EventHandler(this.sERIKO定義を旧定義から新定義に置換ToolStripMenuItem_Click);
			// 
			// toolStripSeparator2
			// 
			this.toolStripSeparator2.Name = "toolStripSeparator2";
			resources.ApplyResources(this.toolStripSeparator2, "toolStripSeparator2");
			// 
			// 更新ファイル作成ToolStripMenuItem
			// 
			this.更新ファイル作成ToolStripMenuItem.Name = "更新ファイル作成ToolStripMenuItem";
			resources.ApplyResources(this.更新ファイル作成ToolStripMenuItem, "更新ファイル作成ToolStripMenuItem");
			this.更新ファイル作成ToolStripMenuItem.Click += new System.EventHandler(this.更新ファイル作成ToolStripMenuItem_Click);
			// 
			// narファイル作成ToolStripMenuItem
			// 
			this.narファイル作成ToolStripMenuItem.Name = "narファイル作成ToolStripMenuItem";
			resources.ApplyResources(this.narファイル作成ToolStripMenuItem, "narファイル作成ToolStripMenuItem");
			this.narファイル作成ToolStripMenuItem.Click += new System.EventHandler(this.narファイル作成ToolStripMenuItem_Click);
			// 
			// 更新ファイルnar作成ToolStripMenuItem
			// 
			this.更新ファイルnar作成ToolStripMenuItem.Name = "更新ファイルnar作成ToolStripMenuItem";
			resources.ApplyResources(this.更新ファイルnar作成ToolStripMenuItem, "更新ファイルnar作成ToolStripMenuItem");
			this.更新ファイルnar作成ToolStripMenuItem.Click += new System.EventHandler(this.更新ファイルnar作成ToolStripMenuItem_Click);
			// 
			// toolStripSeparator3
			// 
			this.toolStripSeparator3.Name = "toolStripSeparator3";
			resources.ApplyResources(this.toolStripSeparator3, "toolStripSeparator3");
			// 
			// 更新ファイルを作成してアップロードToolStripMenuItem
			// 
			this.更新ファイルを作成してアップロードToolStripMenuItem.Name = "更新ファイルを作成してアップロードToolStripMenuItem";
			resources.ApplyResources(this.更新ファイルを作成してアップロードToolStripMenuItem, "更新ファイルを作成してアップロードToolStripMenuItem");
			this.更新ファイルを作成してアップロードToolStripMenuItem.Click += new System.EventHandler(this.更新ファイルを作成してアップロードToolStripMenuItem_Click);
			// 
			// ベータ版についてToolStripMenuItem
			// 
			this.ベータ版についてToolStripMenuItem.Name = "ベータ版についてToolStripMenuItem";
			resources.ApplyResources(this.ベータ版についてToolStripMenuItem, "ベータ版についてToolStripMenuItem");
			this.ベータ版についてToolStripMenuItem.Click += new System.EventHandler(this.ベータ版についてToolStripMenuItem_Click);
			// 
			// splitter1
			// 
			this.splitter1.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
			resources.ApplyResources(this.splitter1, "splitter1");
			this.splitter1.Name = "splitter1";
			this.splitter1.TabStop = false;
			// 
			// splitter2
			// 
			this.splitter2.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
			resources.ApplyResources(this.splitter2, "splitter2");
			this.splitter2.Name = "splitter2";
			this.splitter2.TabStop = false;
			// 
			// checkedListBox1
			// 
			this.checkedListBox1.CheckOnClick = true;
			this.checkedListBox1.DataBindings.Add(new System.Windows.Forms.Binding("Size", global::Satolist.Properties.Settings.Default, "SurfaceViewerBindSize", true, System.Windows.Forms.DataSourceUpdateMode.OnPropertyChanged));
			resources.ApplyResources(this.checkedListBox1, "checkedListBox1");
			this.checkedListBox1.FormattingEnabled = true;
			this.checkedListBox1.Name = "checkedListBox1";
			this.checkedListBox1.Size = global::Satolist.Properties.Settings.Default.SurfaceViewerBindSize;
			this.checkedListBox1.ItemCheck += new System.Windows.Forms.ItemCheckEventHandler(this.checkedListBox1_ItemCheck);
			this.checkedListBox1.Click += new System.EventHandler(this.checkedListBox1_Click);
			// 
			// listBox1
			// 
			this.listBox1.DataBindings.Add(new System.Windows.Forms.Binding("Size", global::Satolist.Properties.Settings.Default, "SurfaceViewerListSize1", true, System.Windows.Forms.DataSourceUpdateMode.OnPropertyChanged));
			resources.ApplyResources(this.listBox1, "listBox1");
			this.listBox1.FormattingEnabled = true;
			this.listBox1.Name = "listBox1";
			this.listBox1.Size = global::Satolist.Properties.Settings.Default.SurfaceViewerListSize1;
			this.listBox1.SelectedIndexChanged += new System.EventHandler(this.listBox1_SelectedIndexChanged);
			this.listBox1.DoubleClick += new System.EventHandler(this.listBox1_DoubleClick);
			// 
			// pictureBox1
			// 
			resources.ApplyResources(this.pictureBox1, "pictureBox1");
			this.pictureBox1.Name = "pictureBox1";
			this.pictureBox1.TabStop = false;
			this.pictureBox1.Click += new System.EventHandler(this.pictureBox1_Click);
			this.pictureBox1.DoubleClick += new System.EventHandler(this.listBox1_DoubleClick);
			this.pictureBox1.MouseClick += new System.Windows.Forms.MouseEventHandler(this.pictureBox1_MouseClick);
			this.pictureBox1.MouseDoubleClick += new System.Windows.Forms.MouseEventHandler(this.pictureBox1_MouseDoubleClick);
			this.pictureBox1.MouseDown += new System.Windows.Forms.MouseEventHandler(this.pictureBox1_MouseDown);
			this.pictureBox1.MouseMove += new System.Windows.Forms.MouseEventHandler(this.pictureBox1_MouseMove);
			this.pictureBox1.MouseUp += new System.Windows.Forms.MouseEventHandler(this.pictureBox1_MouseUp);
			// 
			// SurfaceViewer
			// 
			this.AllowDrop = true;
			resources.ApplyResources(this, "$this");
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.Controls.Add(this.pictureBox1);
			this.Controls.Add(this.splitter2);
			this.Controls.Add(this.checkedListBox1);
			this.Controls.Add(this.splitter1);
			this.Controls.Add(this.textBox1);
			this.Controls.Add(this.listBox1);
			this.Controls.Add(this.menuStrip1);
			this.MainMenuStrip = this.menuStrip1;
			this.Name = "SurfaceViewer";
			this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.SurfaceViewer_FormClosing);
			this.FormClosed += new System.Windows.Forms.FormClosedEventHandler(this.SurfaceViewer_FormClosed);
			this.DragDrop += new System.Windows.Forms.DragEventHandler(this.SurfaceViewer_DragDrop);
			this.DragEnter += new System.Windows.Forms.DragEventHandler(this.SurfaceViewer_DragEnter);
			this.KeyDown += new System.Windows.Forms.KeyEventHandler(this.SurfaceViewer_KeyDown);
			this.menuStrip1.ResumeLayout(false);
			this.menuStrip1.PerformLayout();
			((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).EndInit();
			this.ResumeLayout(false);
			this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.ListBox listBox1;
        private System.Windows.Forms.TextBox textBox1;
		private System.Windows.Forms.CheckedListBox checkedListBox1;
		private System.Windows.Forms.Splitter splitter2;
        private System.Windows.Forms.MenuStrip menuStrip1;
        private System.Windows.Forms.ToolStripMenuItem シェルを選択ToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem ベータ版についてToolStripMenuItem;
        private System.Windows.Forms.Splitter splitter1;
		
		private System.Windows.Forms.ToolStripMenuItem オプションToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem リストをダブルクリックでサーフェス変更を挿入ToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem 里々形式のサーフェス変更を使用するToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem シェルを読み込むToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem シェルをリロードToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem シェルのさわり判定のみ再読み込みToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem さわり判定箇所の表示ToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem 定義のないサーフェスも表示するリロードが必要ToolStripMenuItem;
		private System.Windows.Forms.PictureBox pictureBox1;
		private System.Windows.Forms.ToolStripMenuItem 着せ替えリストを表示ToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem ツールToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem surfacestxtの座標倍率を変化ToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem 表示倍率ToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem x01ToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem x025ToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem x05ToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem x075ToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem x10ToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem x125ToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem x15ToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem x175ToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem x20ToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem 読み込みログToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem アニメーションを再生を有効にするToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem スコープToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem sakuraToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem keroToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem toolStripMenuItem2;
		private System.Windows.Forms.ToolStripMenuItem toolStripMenuItem3;
		private System.Windows.Forms.ToolStripMenuItem toolStripMenuItem4;
		private System.Windows.Forms.ToolStripMenuItem toolStripMenuItem5;
		private System.Windows.Forms.ToolStripMenuItem toolStripMenuItem6;
		private System.Windows.Forms.ToolStripMenuItem toolStripMenuItem7;
		private System.Windows.Forms.ToolStripMenuItem toolStripMenuItem8;
		private System.Windows.Forms.ToolStripMenuItem toolStripMenuItem9;
		private System.Windows.Forms.ToolStripSeparator toolStripSeparator1;
		private System.Windows.Forms.ToolStripMenuItem surfacestxtによるスコープ指定を優先ToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem 描画領域の調整ToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem sERIKO定義を旧定義から新定義に置換ToolStripMenuItem;
		private System.Windows.Forms.ToolStripSeparator toolStripSeparator2;
		private System.Windows.Forms.ToolStripMenuItem 更新ファイル作成ToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem narファイル作成ToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem 更新ファイルnar作成ToolStripMenuItem;
		private System.Windows.Forms.ToolStripSeparator toolStripSeparator3;
		private System.Windows.Forms.ToolStripMenuItem 更新ファイルを作成してアップロードToolStripMenuItem;
		private System.Windows.Forms.ToolStripSeparator toolStripSeparator4;
		private System.Windows.Forms.ToolStripMenuItem シェルをエクスプローラで開くToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem 撮影ToolStripMenuItem;
		private System.Windows.Forms.ToolStripSeparator toolStripSeparator5;
	}
}
