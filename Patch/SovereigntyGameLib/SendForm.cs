using System;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Net;
using System.Net.Cache;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace SovereigntyTK
{
	public partial class SendForm : Form
	{
		public SendForm()
		{
			this.InitializeComponent();
			base.Shown += this.SendForm_Shown;
		}

		public void SeterrorText(string Text)
		{
			this.ErrorText = Text;
		}

		private void SendForm_Shown(object sender, EventArgs e)
		{
			this.SendReport(this.ErrorText);
		}

		public void SendReport(string ReportText)
		{
			this.ErrorText = ReportText;
			this.label2.Text = "Contacting server";
			ReportText = "Report=" + ReportText;
			this.button1.Enabled = true;
			this.button2.Enabled = false;
			this.button3.Enabled = false;
			Application.DoEvents();
			this.CurrentRequest = (HttpWebRequest)WebRequest.Create("http://illustrious-software.com/Report/report.php");
			this.CurrentRequest.Method = "POST";
			byte[] bytes = Encoding.UTF8.GetBytes(ReportText);
			this.CurrentRequest.ContentLength = (long)bytes.Length;
			this.CurrentRequest.ContentType = "application/x-www-form-urlencoded";
			try
			{
				using (Stream requestStream = this.CurrentRequest.GetRequestStream())
				{
					requestStream.Write(bytes, 0, bytes.Length);
				}
				this.CurrentRequest.Timeout = 15000;
				this.CurrentRequest.CachePolicy = new RequestCachePolicy(RequestCacheLevel.BypassCache);
				IAsyncResult asyncResult = this.CurrentRequest.BeginGetResponse(new AsyncCallback(this.UpdateRequest), null);
				ThreadPool.RegisterWaitForSingleObject(asyncResult.AsyncWaitHandle, new WaitOrTimerCallback(this.ScanTimeoutCallback), null, 30000, true);
			}
			catch (Exception ex)
			{
				this.button1.Enabled = false;
				this.button2.Enabled = true;
				this.button3.Enabled = true;
				this.label2.Text = "Send failed - " + ex.Message;
			}
		}

		private void ScanTimeoutCallback(object state, bool timedOut)
		{
			if (timedOut)
			{
				this.CurrentRequest.Abort();
				this.button1.Enabled = false;
				this.button2.Enabled = true;
				this.button3.Enabled = true;
				this.label2.Text = "Send failed - Timout exceeded.";
			}
		}

		private void UpdateRequest(IAsyncResult result)
		{
			try
			{
				HttpWebResponse httpWebResponse = (HttpWebResponse)this.CurrentRequest.EndGetResponse(result);
				this.button1.Enabled = false;
				this.button2.Enabled = false;
				this.button3.Enabled = true;
				this.label2.Text = "Report has been sent";
			}
			catch (WebException ex)
			{
				this.button1.Enabled = false;
				this.button2.Enabled = true;
				this.button3.Enabled = true;
				this.label2.Text = "Send failed - " + ex.Message;
			}
		}

		private void button2_Click_1(object sender, EventArgs e)
		{
			this.SendReport(this.ErrorText);
		}

		private void button3_Click_1(object sender, EventArgs e)
		{
			base.Close();
		}

		private void button1_Click_1(object sender, EventArgs e)
		{
			this.CurrentRequest.Abort();
		}

		private string ErrorText;

		private HttpWebRequest CurrentRequest;
	}
}
