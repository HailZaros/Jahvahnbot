using MetroFramework.Forms;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Jahvahnbot
{
	public partial class WebPage : Form
	{
		public WebPage()
		{
			InitializeComponent();
		}
		public string CalculateMD5Hash(string input)
		{
			// step 1, calculate MD5 hash from input
			System.Security.Cryptography.MD5 md5 = System.Security.Cryptography.MD5.Create();
			byte[] inputBytes = System.Text.Encoding.ASCII.GetBytes(input);
			byte[] hash = md5.ComputeHash(inputBytes);

			// step 2, convert byte array to hex string
			StringBuilder sb = new StringBuilder();
			for (int i = 0; i < hash.Length; i++)
			{
				sb.Append(hash[i].ToString("x2"));
			}
			return sb.ToString();
		}
		private void webBrowser1_DocumentCompleted(object sender, WebBrowserDocumentCompletedEventArgs e)
		{
			Console.WriteLine("Waiting for a successful login..");
			if (webBrowser1.Url.ToString().StartsWith("http://localhost"))
			{
				string[] g = webBrowser1.Url.ToString().Split(new char[] { '=' }, 3);
				mainForm.auth = g[1].Remove(g[1].Length - 6);
				Console.WriteLine("Login Successful.");
				Console.WriteLine("Fetched hashed OAuth ({0})", CalculateMD5Hash(g[1].Remove(g[1].Length - 6)));
				this.Close();
			}
			else
				timer1.Start();
		}

		private void timer1_Tick(object sender, EventArgs e)
		{
			if (webBrowser1.Url.ToString().StartsWith("http://localhost"))
			{
				string[] g = webBrowser1.Url.ToString().Split(new char[] { '=' }, 3);
				mainForm.auth = g[1].Remove(g[1].Length - 6);
				Console.WriteLine("Login Successful.");
				Console.WriteLine("Fetched hashed OAuth ({0})", CalculateMD5Hash(g[1].Remove(g[1].Length - 6)));
				timer1.Stop();
				this.Close();
			}
			else
				Console.WriteLine(webBrowser1.Url.ToString());
		}
	}
}

