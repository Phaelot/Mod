namespace SovereigntyTK
{
	public partial class SendForm : global::System.Windows.Forms.Form
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
			this.button3 = new global::System.Windows.Forms.Button();
			this.button2 = new global::System.Windows.Forms.Button();
			this.button1 = new global::System.Windows.Forms.Button();
			this.label2 = new global::System.Windows.Forms.Label();
			this.label1 = new global::System.Windows.Forms.Label();
			base.SuspendLayout();
			this.button3.Location = new global::System.Drawing.Point(223, 45);
			this.button3.Name = "button3";
			this.button3.Size = new global::System.Drawing.Size(75, 23);
			this.button3.TabIndex = 9;
			this.button3.Text = "Close";
			this.button3.UseVisualStyleBackColor = true;
			this.button3.Click += new global::System.EventHandler(this.button3_Click_1);
			this.button2.Location = new global::System.Drawing.Point(142, 45);
			this.button2.Name = "button2";
			this.button2.Size = new global::System.Drawing.Size(75, 23);
			this.button2.TabIndex = 8;
			this.button2.Text = "Retry";
			this.button2.UseVisualStyleBackColor = true;
			this.button2.Click += new global::System.EventHandler(this.button2_Click_1);
			this.button1.Location = new global::System.Drawing.Point(61, 45);
			this.button1.Name = "button1";
			this.button1.Size = new global::System.Drawing.Size(75, 23);
			this.button1.TabIndex = 7;
			this.button1.Text = "Cancel";
			this.button1.UseVisualStyleBackColor = true;
			this.button1.Click += new global::System.EventHandler(this.button1_Click_1);
			this.label2.Location = new global::System.Drawing.Point(130, 7);
			this.label2.Name = "label2";
			this.label2.Size = new global::System.Drawing.Size(212, 35);
			this.label2.TabIndex = 6;
			this.label2.Text = "label2";
			this.label1.AutoSize = true;
			this.label1.Location = new global::System.Drawing.Point(9, 7);
			this.label1.Name = "label1";
			this.label1.Size = new global::System.Drawing.Size(115, 13);
			this.label1.TabIndex = 5;
			this.label1.Text = "Sending Error Report...";
			base.AutoScaleMode = global::System.Windows.Forms.AutoScaleMode.None;
			base.ClientSize = new global::System.Drawing.Size(360, 77);
			base.ControlBox = false;
			base.Controls.Add(this.button3);
			base.Controls.Add(this.button2);
			base.Controls.Add(this.button1);
			base.Controls.Add(this.label2);
			base.Controls.Add(this.label1);
			base.FormBorderStyle = global::System.Windows.Forms.FormBorderStyle.FixedToolWindow;
			base.MaximizeBox = false;
			base.MinimizeBox = false;
			base.Name = "SendForm";
			this.Text = "Sending Report";
			base.ResumeLayout(false);
			base.PerformLayout();
		}

		private global::System.ComponentModel.IContainer components;

		private global::System.Windows.Forms.Button button3;

		private global::System.Windows.Forms.Button button2;

		private global::System.Windows.Forms.Button button1;

		private global::System.Windows.Forms.Label label2;

		private global::System.Windows.Forms.Label label1;
	}
}
