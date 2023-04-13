namespace Satolist
{
	partial class OpeningGhostDialog
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(OpeningGhostDialog));
            this.progressBar1 = new System.Windows.Forms.ProgressBar();
            this.progressDescript = new System.Windows.Forms.Label();
            this.loadingGhostName = new System.Windows.Forms.Label();
            this.progressInfo = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // progressBar1
            // 
            this.progressBar1.Location = new System.Drawing.Point(12, 48);
            this.progressBar1.Maximum = 1;
            this.progressBar1.Name = "progressBar1";
            this.progressBar1.Size = new System.Drawing.Size(445, 26);
            this.progressBar1.Step = 1;
            this.progressBar1.Style = System.Windows.Forms.ProgressBarStyle.Continuous;
            this.progressBar1.TabIndex = 0;
            // 
            // progressDescript
            // 
            this.progressDescript.AutoSize = true;
            this.progressDescript.Location = new System.Drawing.Point(12, 21);
            this.progressDescript.Name = "progressDescript";
            this.progressDescript.Size = new System.Drawing.Size(275, 12);
            this.progressDescript.TabIndex = 1;
            this.progressDescript.Text = "サーフェスパレットの作成中です。もうしばらくお待ち下さい。";
            // 
            // loadingGhostName
            // 
            this.loadingGhostName.AutoSize = true;
            this.loadingGhostName.Location = new System.Drawing.Point(12, 9);
            this.loadingGhostName.Name = "loadingGhostName";
            this.loadingGhostName.Size = new System.Drawing.Size(119, 12);
            this.loadingGhostName.TabIndex = 2;
            this.loadingGhostName.Text = "あおいリボンのあおいさん";
            this.loadingGhostName.Visible = false;
            // 
            // progressInfo
            // 
            this.progressInfo.AutoSize = true;
            this.progressInfo.Location = new System.Drawing.Point(10, 77);
            this.progressInfo.Name = "progressInfo";
            this.progressInfo.Size = new System.Drawing.Size(407, 12);
            this.progressInfo.TabIndex = 3;
            this.progressInfo.Text = "-------------------------------------------------------------------";
            // 
            // OpeningGhostDialog
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(469, 95);
            this.ControlBox = false;
            this.Controls.Add(this.progressInfo);
            this.Controls.Add(this.loadingGhostName);
            this.Controls.Add(this.progressDescript);
            this.Controls.Add(this.progressBar1);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "OpeningGhostDialog";
            this.Text = "準備しています...";
            this.ResumeLayout(false);
            this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.ProgressBar progressBar1;
		private System.Windows.Forms.Label progressDescript;
		private System.Windows.Forms.Label loadingGhostName;
		private System.Windows.Forms.Label progressInfo;
	}
}