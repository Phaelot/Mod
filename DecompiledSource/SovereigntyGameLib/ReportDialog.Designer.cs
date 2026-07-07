namespace SovereigntyTK
{
	public partial class ReportDialog : global::System.Windows.Forms.Form
	{
		protected override void Dispose(bool disposing)
		{
			if (disposing && this.components != null)
			{
				this.components.Dispose();
			}
			base.Dispose(disposing);
		}

		private void InitializeComponent()
		{
			global::System.ComponentModel.ComponentResourceManager componentResourceManager = new global::System.ComponentModel.ComponentResourceManager(typeof(global::SovereigntyTK.ReportDialog));
			this.button1 = new global::System.Windows.Forms.Button();
			this.button2 = new global::System.Windows.Forms.Button();
			this.label1 = new global::System.Windows.Forms.Label();
			this.label3 = new global::System.Windows.Forms.Label();
			this.textBox1 = new global::System.Windows.Forms.TextBox();
			base.SuspendLayout();
			this.button1.Location = new global::System.Drawing.Point(451, 621);
			this.button1.Margin = new global::System.Windows.Forms.Padding(4, 5, 4, 5);
			this.button1.Name = "button1";
			this.button1.Size = new global::System.Drawing.Size(112, 35);
			this.button1.TabIndex = 1;
			this.button1.Text = "Close";
			this.button1.UseVisualStyleBackColor = true;
			this.button1.Click += new global::System.EventHandler(this.button1_Click);
			this.button2.Location = new global::System.Drawing.Point(572, 621);
			this.button2.Margin = new global::System.Windows.Forms.Padding(4, 5, 4, 5);
			this.button2.Name = "button2";
			this.button2.Size = new global::System.Drawing.Size(112, 35);
			this.button2.TabIndex = 2;
			this.button2.Text = "Send";
			this.button2.UseVisualStyleBackColor = true;
			this.button2.Click += new global::System.EventHandler(this.button2_Click);
			this.label1.Font = new global::System.Drawing.Font("Microsoft Sans Serif", 12f, global::System.Drawing.FontStyle.Bold, global::System.Drawing.GraphicsUnit.Point, 0);
			this.label1.Location = new global::System.Drawing.Point(16, 18);
			this.label1.Margin = new global::System.Windows.Forms.Padding(4, 0, 4, 0);
			this.label1.Name = "label1";
			this.label1.Size = new global::System.Drawing.Size(119, 30);
			this.label1.TabIndex = 8;
			this.label1.Text = "Report a Bug";
			this.label3.Font = new global::System.Drawing.Font("Microsoft Sans Serif", 9.75f, global::System.Drawing.FontStyle.Regular, global::System.Drawing.GraphicsUnit.Point, 0);
			this.label3.Location = new global::System.Drawing.Point(18, 48);
			this.label3.Margin = new global::System.Windows.Forms.Padding(4, 0, 4, 0);
			this.label3.Name = "label3";
			this.label3.Size = new global::System.Drawing.Size(666, 69);
			this.label3.TabIndex = 11;
			this.label3.Text = componentResourceManager.GetString("label3.Text");
			this.textBox1.Location = new global::System.Drawing.Point(12, 109);
			this.textBox1.Multiline = true;
			this.textBox1.Name = "textBox1";
			this.textBox1.ScrollBars = global::System.Windows.Forms.ScrollBars.Both;
			this.textBox1.Size = new global::System.Drawing.Size(672, 504);
			this.textBox1.TabIndex = 12;
			base.AutoScaleMode = global::System.Windows.Forms.AutoScaleMode.None;
			base.ClientSize = new global::System.Drawing.Size(699, 662);
			base.ControlBox = false;
			base.Controls.Add(this.textBox1);
			base.Controls.Add(this.label3);
			base.Controls.Add(this.label1);
			base.Controls.Add(this.button2);
			base.Controls.Add(this.button1);
			base.FormBorderStyle = global::System.Windows.Forms.FormBorderStyle.FixedToolWindow;
			base.Margin = new global::System.Windows.Forms.Padding(4, 5, 4, 5);
			base.Name = "ReportDialog";
			this.Text = "Bug Report";
			base.ResumeLayout(false);
			base.PerformLayout();
		}

		private global::System.ComponentModel.IContainer components;

		private global::System.Windows.Forms.Button button1;

		private global::System.Windows.Forms.Button button2;

		private global::System.Windows.Forms.Label label1;

		private global::System.Windows.Forms.Label label3;

		private global::System.Windows.Forms.TextBox textBox1;
	}
}
