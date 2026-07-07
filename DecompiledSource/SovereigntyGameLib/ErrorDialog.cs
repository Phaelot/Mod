using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using SovereigntyTK.Utility;

namespace SovereigntyTK
{
	public partial class ErrorDialog : Form
	{
		public ErrorDialog(string ErrorMessage, string StackTrace, Sovereignty Game)
		{
			this.InitializeComponent();
			string text = "An error has occurred\r\n\r\n";
			text = text + "User ID: " + GlobalData.UserID + "\r\n";
			object obj = text;
			text = string.Concat(new object[]
			{
				obj,
				"Build Number: ",
				GlobalData.VERSION_BUILD,
				"\r\n"
			});
			if (Game == null)
			{
				text += "Game was not initialized\r\n";
			}
			else if (Game.ActiveMods == null)
			{
				text += "Mods were not yet loaded\r\n";
			}
			else if (Game.ActiveMods.Count == 0)
			{
				text += "Mods Active: None\r\n";
			}
			else
			{
				text += "Mods Active: ";
				int num = 0;
				foreach (ModData modData in Game.ActiveMods)
				{
					text += modData.Name;
					if (num < Game.ActiveMods.Count - 1)
					{
						text += ", ";
					}
					num++;
				}
			}
			text = text + ErrorMessage + "\r\n\r\n";
			text += StackTrace;
			this.textBox1.Text = text;
		}

		private void button1_Click(object sender, EventArgs e)
		{
			base.Close();
		}

		private void button2_Click(object sender, EventArgs e)
		{
			SendForm sendForm = new SendForm();
			sendForm.SeterrorText("Comments:\r\n" + this.textBox2.Text + "\r\n\r\n" + this.textBox1.Text);
			sendForm.ShowDialog();
		}
	}
}
