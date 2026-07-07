namespace SovereigntyTK
{
	public partial class ErrorDialog : global::System.Windows.Forms.Form
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
			global::System.ComponentModel.ComponentResourceManager componentResourceManager = new global::System.ComponentModel.ComponentResourceManager(typeof(global::SovereigntyTK.ErrorDialog));
			this.button1 = new global::System.Windows.Forms.Button();
			this.button2 = new global::System.Windows.Forms.Button();
			this.label2 = new global::System.Windows.Forms.Label();
			this.textBox2 = new global::System.Windows.Forms.TextBox();
			this.textBox1 = new global::System.Windows.Forms.TextBox();
			this.label1 = new global::System.Windows.Forms.Label();
			base.SuspendLayout();
			this.button1.Location = new global::System.Drawing.Point(1016, 638);
			this.button1.Margin = new global::System.Windows.Forms.Padding(4, 5, 4, 5);
			this.button1.Name = "button1";
			this.button1.Size = new global::System.Drawing.Size(112, 35);
			this.button1.TabIndex = 1;
			this.button1.Text = "Close";
			this.button1.UseVisualStyleBackColor = true;
			this.button1.Click += new global::System.EventHandler(this.button1_Click);
			this.button2.Location = new global::System.Drawing.Point(1137, 638);
			this.button2.Margin = new global::System.Windows.Forms.Padding(4, 5, 4, 5);
			this.button2.Name = "button2";
			this.button2.Size = new global::System.Drawing.Size(112, 35);
			this.button2.TabIndex = 2;
			this.button2.Text = "Send";
			this.button2.UseVisualStyleBackColor = true;
			this.button2.Click += new global::System.EventHandler(this.button2_Click);
			this.label2.Location = new global::System.Drawing.Point(18, 638);
			this.label2.Margin = new global::System.Windows.Forms.Padding(4, 0, 4, 0);
			this.label2.Name = "label2";
			this.label2.Size = new global::System.Drawing.Size(710, 55);
			this.label2.TabIndex = 7;
			this.label2.Text = "Note: We do not collect or use any personally identifiable information from these crash reports, any information obtained will be used for the sole purpose of finding and fixing errors in Sovereignty.";
			this.textBox2.Location = new global::System.Drawing.Point(696, 122);
			this.textBox2.Margin = new global::System.Windows.Forms.Padding(4, 5, 4, 5);
			this.textBox2.Multiline = true;
			this.textBox2.Name = "textBox2";
			this.textBox2.ScrollBars = global::System.Windows.Forms.ScrollBars.Both;
			this.textBox2.Size = new global::System.Drawing.Size(552, 510);
			this.textBox2.TabIndex = 10;
			this.textBox2.WordWrap = false;
			this.textBox1.Location = new global::System.Drawing.Point(22, 122);
			this.textBox1.Margin = new global::System.Windows.Forms.Padding(4, 5, 4, 5);
			this.textBox1.Multiline = true;
			this.textBox1.Name = "textBox1";
			this.textBox1.ReadOnly = true;
			this.textBox1.ScrollBars = global::System.Windows.Forms.ScrollBars.Both;
			this.textBox1.Size = new global::System.Drawing.Size(662, 510);
			this.textBox1.TabIndex = 9;
			this.textBox1.WordWrap = false;
			this.label1.Font = new global::System.Drawing.Font("Microsoft Sans Serif", 12f, global::System.Drawing.FontStyle.Regular, global::System.Drawing.GraphicsUnit.Point, 0);
			this.label1.Location = new global::System.Drawing.Point(16, 18);
			this.label1.Margin = new global::System.Windows.Forms.Padding(4, 0, 4, 0);
			this.label1.Name = "label1";
			this.label1.Size = new global::System.Drawing.Size(1233, 98);
			this.label1.TabIndex = 8;
			this.label1.Text = componentResourceManager.GetString("label1.Text");
			base.AutoScaleMode = global::System.Windows.Forms.AutoScaleMode.None;
			base.ClientSize = new global::System.Drawing.Size(1263, 698);
			base.ControlBox = false;
			base.Controls.Add(this.textBox2);
			base.Controls.Add(this.textBox1);
			base.Controls.Add(this.label1);
			base.Controls.Add(this.label2);
			base.Controls.Add(this.button2);
			base.Controls.Add(this.button1);
			base.FormBorderStyle = global::System.Windows.Forms.FormBorderStyle.FixedToolWindow;
			base.Margin = new global::System.Windows.Forms.Padding(4, 5, 4, 5);
			base.Name = "ErrorDialog";
			this.Text = "Error Report";
			base.ResumeLayout(false);
			base.PerformLayout();
		}

		private global::System.ComponentModel.IContainer components;

		private global::System.Windows.Forms.Button button1;

		private global::System.Windows.Forms.Button button2;

		private global::System.Windows.Forms.Label label2;

		private global::System.Windows.Forms.TextBox textBox2;

		private global::System.Windows.Forms.TextBox textBox1;

		private global::System.Windows.Forms.Label label1;
	}
}
