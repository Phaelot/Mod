using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace SovereigntyTK
{
	public partial class ReportDialog : Form
	{
		public ReportDialog()
		{
			this.InitializeComponent();
		}

		private void button1_Click(object sender, EventArgs e)
		{
			base.Close();
		}

		private void button2_Click(object sender, EventArgs e)
		{
			string text = "";
			text += "Bug Report\r\n";
			text += "==========\r\n";
			object obj = text;
			text = string.Concat(new object[]
			{
				obj,
				"Version: ",
				GlobalData.VERSION_MAJOR,
				".",
				GlobalData.VERSION_MINOR,
				".",
				GlobalData.VERSION_REVISION,
				" Build: ",
				GlobalData.VERSION_BUILD,
				"\r\n"
			});
			text = text + "User ID: " + GlobalData.UserID + "\r\n";
			text += "==========\r\n";
			text += this.textBox1.Text;
			SendForm sendForm = new SendForm();
			sendForm.SeterrorText(text);
			sendForm.ShowDialog();
		}
	}
}
