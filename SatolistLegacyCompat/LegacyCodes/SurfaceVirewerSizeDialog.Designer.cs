namespace Satolist
{
	partial class SurfaceVirewerSizeDialog
	{
		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.IContainer components = null;

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		/// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
		protected override void Dispose(bool disposing)
		{
			if (disposing && (components != null))
			{
				components.Dispose();
			}
			base.Dispose(disposing);
		}

		#region Windows Form Designer generated code

		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(SurfaceVirewerSizeDialog));
			this.sizeX = new System.Windows.Forms.NumericUpDown();
			this.sizeY = new System.Windows.Forms.NumericUpDown();
			this.label1 = new System.Windows.Forms.Label();
			this.label2 = new System.Windows.Forms.Label();
			this.label3 = new System.Windows.Forms.Label();
			this.currentSetting = new System.Windows.Forms.Label();
			this.okButton = new System.Windows.Forms.Button();
			this.cancelButton = new System.Windows.Forms.Button();
			((System.ComponentModel.ISupportInitialize)(this.sizeX)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.sizeY)).BeginInit();
			this.SuspendLayout();
			// 
			// sizeX
			// 
			this.sizeX.Location = new System.Drawing.Point(100, 69);
			this.sizeX.Maximum = new decimal(new int[] {
            10000,
            0,
            0,
            0});
			this.sizeX.Minimum = new decimal(new int[] {
            10,
            0,
            0,
            0});
			this.sizeX.Name = "sizeX";
			this.sizeX.Size = new System.Drawing.Size(66, 19);
			this.sizeX.TabIndex = 0;
			this.sizeX.Value = new decimal(new int[] {
            100,
            0,
            0,
            0});
			// 
			// sizeY
			// 
			this.sizeY.Location = new System.Drawing.Point(286, 69);
			this.sizeY.Maximum = new decimal(new int[] {
            10000,
            0,
            0,
            0});
			this.sizeY.Minimum = new decimal(new int[] {
            10,
            0,
            0,
            0});
			this.sizeY.Name = "sizeY";
			this.sizeY.Size = new System.Drawing.Size(62, 19);
			this.sizeY.TabIndex = 1;
			this.sizeY.Value = new decimal(new int[] {
            100,
            0,
            0,
            0});
			// 
			// label1
			// 
			this.label1.AutoSize = true;
			this.label1.Location = new System.Drawing.Point(14, 71);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(80, 12);
			this.label1.TabIndex = 2;
			this.label1.Text = "x(横ピクセル数)";
			// 
			// label2
			// 
			this.label2.AutoSize = true;
			this.label2.Location = new System.Drawing.Point(200, 71);
			this.label2.Name = "label2";
			this.label2.Size = new System.Drawing.Size(80, 12);
			this.label2.TabIndex = 3;
			this.label2.Text = "y(縦ピクセル数)";
			// 
			// label3
			// 
			this.label3.AutoSize = true;
			this.label3.Location = new System.Drawing.Point(14, 11);
			this.label3.Name = "label3";
			this.label3.Size = new System.Drawing.Size(244, 12);
			this.label3.TabIndex = 4;
			this.label3.Text = "サーフェス ビューワの表示領域サイズを変更します。";
			// 
			// currentSetting
			// 
			this.currentSetting.AutoSize = true;
			this.currentSetting.Location = new System.Drawing.Point(14, 34);
			this.currentSetting.Name = "currentSetting";
			this.currentSetting.Size = new System.Drawing.Size(69, 12);
			this.currentSetting.TabIndex = 5;
			this.currentSetting.Text = "現在の設定: ";
			// 
			// okButton
			// 
			this.okButton.Location = new System.Drawing.Point(198, 121);
			this.okButton.Name = "okButton";
			this.okButton.Size = new System.Drawing.Size(97, 26);
			this.okButton.TabIndex = 6;
			this.okButton.Text = "OK";
			this.okButton.UseVisualStyleBackColor = true;
			this.okButton.Click += new System.EventHandler(this.okButton_Click);
			// 
			// cancelButton
			// 
			this.cancelButton.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			this.cancelButton.Location = new System.Drawing.Point(301, 121);
			this.cancelButton.Name = "cancelButton";
			this.cancelButton.Size = new System.Drawing.Size(97, 26);
			this.cancelButton.TabIndex = 7;
			this.cancelButton.Text = "キャンセル";
			this.cancelButton.UseVisualStyleBackColor = true;
			this.cancelButton.Click += new System.EventHandler(this.cancelButton_Click);
			// 
			// SurfaceVirewerSizeDialog
			// 
			this.AcceptButton = this.okButton;
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.CancelButton = this.cancelButton;
			this.ClientSize = new System.Drawing.Size(410, 153);
			this.Controls.Add(this.cancelButton);
			this.Controls.Add(this.okButton);
			this.Controls.Add(this.currentSetting);
			this.Controls.Add(this.label3);
			this.Controls.Add(this.label2);
			this.Controls.Add(this.label1);
			this.Controls.Add(this.sizeY);
			this.Controls.Add(this.sizeX);
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
			this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
			this.Name = "SurfaceVirewerSizeDialog";
			this.Text = "サイズの変更";
			((System.ComponentModel.ISupportInitialize)(this.sizeX)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.sizeY)).EndInit();
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.NumericUpDown sizeX;
		private System.Windows.Forms.NumericUpDown sizeY;
		private System.Windows.Forms.Label label1;
		private System.Windows.Forms.Label label2;
		private System.Windows.Forms.Label label3;
		private System.Windows.Forms.Label currentSetting;
		private System.Windows.Forms.Button okButton;
		private System.Windows.Forms.Button cancelButton;
	}
}