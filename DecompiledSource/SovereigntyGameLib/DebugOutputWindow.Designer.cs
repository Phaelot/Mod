namespace SovereigntyTK
{
	public partial class DebugOutputWindow : global::System.Windows.Forms.Form
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
			this.groupBox1 = new global::System.Windows.Forms.GroupBox();
			this.button1 = new global::System.Windows.Forms.Button();
			this.label3 = new global::System.Windows.Forms.Label();
			this.numericUpDown2 = new global::System.Windows.Forms.NumericUpDown();
			this.numericUpDown1 = new global::System.Windows.Forms.NumericUpDown();
			this.label2 = new global::System.Windows.Forms.Label();
			this.comboBox1 = new global::System.Windows.Forms.ComboBox();
			this.label1 = new global::System.Windows.Forms.Label();
			this.textBox1 = new global::System.Windows.Forms.TextBox();
			this.button2 = new global::System.Windows.Forms.Button();
			this.groupBox1.SuspendLayout();
			((global::System.ComponentModel.ISupportInitialize)this.numericUpDown2).BeginInit();
			((global::System.ComponentModel.ISupportInitialize)this.numericUpDown1).BeginInit();
			base.SuspendLayout();
			this.groupBox1.Controls.Add(this.button2);
			this.groupBox1.Controls.Add(this.button1);
			this.groupBox1.Controls.Add(this.label3);
			this.groupBox1.Controls.Add(this.numericUpDown2);
			this.groupBox1.Controls.Add(this.numericUpDown1);
			this.groupBox1.Controls.Add(this.label2);
			this.groupBox1.Controls.Add(this.comboBox1);
			this.groupBox1.Controls.Add(this.label1);
			this.groupBox1.Controls.Add(this.textBox1);
			this.groupBox1.Location = new global::System.Drawing.Point(12, 12);
			this.groupBox1.Name = "groupBox1";
			this.groupBox1.Size = new global::System.Drawing.Size(727, 422);
			this.groupBox1.TabIndex = 8;
			this.groupBox1.TabStop = false;
			this.groupBox1.Text = "groupBox1";
			this.button1.Location = new global::System.Drawing.Point(506, 135);
			this.button1.Name = "button1";
			this.button1.Size = new global::System.Drawing.Size(103, 23);
			this.button1.TabIndex = 15;
			this.button1.Text = "Show AI Log";
			this.button1.UseVisualStyleBackColor = true;
			this.button1.Click += new global::System.EventHandler(this.button1_Click);
			this.label3.AutoSize = true;
			this.label3.Location = new global::System.Drawing.Point(615, 64);
			this.label3.Name = "label3";
			this.label3.Size = new global::System.Drawing.Size(16, 13);
			this.label3.TabIndex = 14;
			this.label3.Text = "to";
			this.numericUpDown2.Location = new global::System.Drawing.Point(642, 64);
			global::System.Windows.Forms.NumericUpDown numericUpDown = this.numericUpDown2;
			int[] array = new int[4];
			array[0] = 1;
			numericUpDown.Minimum = new decimal(array);
			this.numericUpDown2.Name = "numericUpDown2";
			this.numericUpDown2.Size = new global::System.Drawing.Size(60, 20);
			this.numericUpDown2.TabIndex = 13;
			global::System.Windows.Forms.NumericUpDown numericUpDown2 = this.numericUpDown2;
			int[] array2 = new int[4];
			array2[0] = 1;
			numericUpDown2.Value = new decimal(array2);
			this.numericUpDown1.Location = new global::System.Drawing.Point(549, 64);
			global::System.Windows.Forms.NumericUpDown numericUpDown3 = this.numericUpDown1;
			int[] array3 = new int[4];
			array3[0] = 1;
			numericUpDown3.Minimum = new decimal(array3);
			this.numericUpDown1.Name = "numericUpDown1";
			this.numericUpDown1.Size = new global::System.Drawing.Size(60, 20);
			this.numericUpDown1.TabIndex = 12;
			global::System.Windows.Forms.NumericUpDown numericUpDown4 = this.numericUpDown1;
			int[] array4 = new int[4];
			array4[0] = 1;
			numericUpDown4.Value = new decimal(array4);
			this.label2.AutoSize = true;
			this.label2.Location = new global::System.Drawing.Point(503, 64);
			this.label2.Name = "label2";
			this.label2.Size = new global::System.Drawing.Size(37, 13);
			this.label2.TabIndex = 11;
			this.label2.Text = "Turns:";
			this.comboBox1.DropDownStyle = global::System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.comboBox1.FormattingEnabled = true;
			this.comboBox1.Location = new global::System.Drawing.Point(549, 18);
			this.comboBox1.Name = "comboBox1";
			this.comboBox1.Size = new global::System.Drawing.Size(153, 21);
			this.comboBox1.TabIndex = 10;
			this.label1.AutoSize = true;
			this.label1.Location = new global::System.Drawing.Point(503, 21);
			this.label1.Name = "label1";
			this.label1.Size = new global::System.Drawing.Size(40, 13);
			this.label1.TabIndex = 9;
			this.label1.Text = "Realm:";
			this.textBox1.BackColor = global::System.Drawing.SystemColors.Window;
			this.textBox1.Font = new global::System.Drawing.Font("Courier New", 8.25f, global::System.Drawing.FontStyle.Regular, global::System.Drawing.GraphicsUnit.Point, 0);
			this.textBox1.Location = new global::System.Drawing.Point(6, 19);
			this.textBox1.Multiline = true;
			this.textBox1.Name = "textBox1";
			this.textBox1.ReadOnly = true;
			this.textBox1.ScrollBars = global::System.Windows.Forms.ScrollBars.Both;
			this.textBox1.Size = new global::System.Drawing.Size(490, 398);
			this.textBox1.TabIndex = 8;
			this.button2.Location = new global::System.Drawing.Point(549, 90);
			this.button2.Name = "button2";
			this.button2.Size = new global::System.Drawing.Size(153, 23);
			this.button2.TabIndex = 16;
			this.button2.Text = "Most Recent Turn";
			this.button2.UseVisualStyleBackColor = true;
			this.button2.Click += new global::System.EventHandler(this.button2_Click);
			base.AutoScaleDimensions = new global::System.Drawing.SizeF(6f, 13f);
			base.AutoScaleMode = global::System.Windows.Forms.AutoScaleMode.Font;
			base.ClientSize = new global::System.Drawing.Size(751, 446);
			base.Controls.Add(this.groupBox1);
			base.Name = "DebugOutputWindow";
			this.Text = "DebugOutputWindow";
			this.groupBox1.ResumeLayout(false);
			this.groupBox1.PerformLayout();
			((global::System.ComponentModel.ISupportInitialize)this.numericUpDown2).EndInit();
			((global::System.ComponentModel.ISupportInitialize)this.numericUpDown1).EndInit();
			base.ResumeLayout(false);
		}

		private global::System.ComponentModel.IContainer components;

		private global::System.Windows.Forms.GroupBox groupBox1;

		private global::System.Windows.Forms.Button button1;

		private global::System.Windows.Forms.Label label3;

		private global::System.Windows.Forms.NumericUpDown numericUpDown2;

		private global::System.Windows.Forms.NumericUpDown numericUpDown1;

		private global::System.Windows.Forms.Label label2;

		private global::System.Windows.Forms.ComboBox comboBox1;

		private global::System.Windows.Forms.Label label1;

		private global::System.Windows.Forms.TextBox textBox1;

		private global::System.Windows.Forms.Button button2;
	}
}
