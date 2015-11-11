using System;
using System.IO;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Threading;
using System.Data.SQLite;
using System.Net;
using MetroFramework.Forms;
using MetroFramework;

namespace Jahvahnbot
{
	public partial class mainForm : MetroForm
	{
		public mainForm()
		{
			InitializeComponent();
			backgroundWorker2.RunWorkerAsync(); 
			this.metroTabControl1.SelectedTab = this.Main;
			this.Theme = MetroThemeStyle.Dark;
		}
		#region Declares
		public static string auth { get; set; }
		private IRCConfig conf = new IRCConfig();
		private IRCBot bot;
		private PConfig pconf = new PConfig();
		private Poll poll;
		private int next = 0;
		private Dictionary<string, int> pts = new Dictionary<string, int>();
		private bool d = false, d2 = false;
		#endregion
		#region Connection
		private void Form1_Load(object sender, EventArgs e)
		{
			if (Jahvahnbot.Properties.Settings.Default.enableosu)
				metroToggle1.Checked = true;
			else
				metroToggle1.Checked = false;
			this.metroTextBox15.Text = Jahvahnbot.Properties.Settings.Default.nppath;
			this.metroLabel3.ForeColor = MetroFramework.MetroColors.Red;
			this.metroTextBox11.Text = Jahvahnbot.Properties.Settings.Default.fukit; //autoMsg
			this.metroTextBox13.Text = Jahvahnbot.Properties.Settings.Default.byemsg;
			this.metroTextBox1.Text = "" + Jahvahnbot.Properties.Settings.Default.sostupid; //autoMsg cooldown
			this.metroTextBox12.Text = Jahvahnbot.Properties.Settings.Default.connectmsg; //connectMsg
			this.metroTextBox5.Text = Jahvahnbot.Properties.Settings.Default.pploss;
			this.metroTextBox4.Text = Jahvahnbot.Properties.Settings.Default.ppgain;
			this.metroTextBox10.Text = Jahvahnbot.Properties.Settings.Default.osupath;
			this.metroTextBox3.Text = (Jahvahnbot.Properties.Settings.Default.duration / 1000).ToString(); //osu stuff
			metroCheckBox3.Checked = true;
			metroRadioButton1.Checked = true; //Commands
		} //new GUI stuff
		public void startBot() //botStart
		{
			conf.oauth = "oauth:" + auth;
			conf.nick = "harubot";
			conf.port = 6667;
			conf.channel = "#jahvahn";
			conf.server = "irc.twitch.tv";
			conf.debug = false;
			conf.joined = false;
			bot = new IRCBot(conf);
			bot.startUp();
			bot.Connect();
		}
		private void startBotDebug()
		{
			conf.oauth = "oauth:" + auth;
			conf.nick = "hail_zaros";
			conf.port = 6667;
			conf.channel = "#hail_zaros";
			conf.server = "irc.twitch.tv";
			conf.debug = true;
			conf.joined = false;
			bot = new IRCBot(conf);
			bot.startUp();
			bot.Connect();
			//bot.IRCWork();
		} //no longer in use.
		private void metroButton1_Click(object sender, EventArgs e)
		{
			WebPage g = new WebPage(); g.FormClosing += new FormClosingEventHandler(this.WebPage_FormClosed); g.Show();
		} //Connect
		private void metroButton9_Click(object sender, EventArgs e)
		{
			WebPage g = new WebPage(); g.FormClosing += new FormClosingEventHandler(this.WebPage_FormClosed1); g.Show();
		}
		private void WebPage_FormClosed(object sender, FormClosingEventArgs e)
		{
			timer1.Start();
			startBot();
			d = true;
			this.metroLabel3.ForeColor = MetroFramework.MetroColors.Teal;
			this.metroLabel3.Text = "Connecting...";
		} //Connect
		private void WebPage_FormClosed1(object sender, FormClosingEventArgs e)
		{
			timer1.Start();
			startBotDebug();
			d = true;
			this.metroLabel3.ForeColor = MetroFramework.MetroColors.Teal;
			this.metroLabel3.Text = "Connecting...";
		} //debugConnect
		#endregion
		#region Everything else
		private void timer1_Tick(object sender, EventArgs e) //progressBar
		{
			metroProgressBar1.Increment(3);
			this.metroButton1.Cursor = Cursors.Default;
			if (metroProgressBar1.Value > 99)
			{
				this.metroLabel3.ForeColor = MetroFramework.MetroColors.Green;
				this.metroLabel3.Text = "Connected";
				this.metroTextBox9.Enabled = true;
				this.metroTile9.Enabled = true;
				timer1.Stop();
			}
		}
		private void sM(object sender, KeyPressEventArgs e)
		{
			if (e.KeyChar == (char)13)
			{
				bot.sM(this.metroTextBox9.Text);
				this.metroTextBox9.Text = "";
				e.Handled = true;
			}
		}
		private void ClosingForm(object sender, FormClosingEventArgs e) //Useless
		{
			if (d)
				bot.sM(Jahvahnbot.Properties.Settings.Default.byemsg);
			Environment.Exit(0);
		}
		#endregion
		#region Debug
		private void metroButton12_Click(object sender, EventArgs e)
		{
			//Null
		}
		private void metroButton13_Click(object sender, EventArgs e)
		{
			osu_np d = new osu_np();
			MessageBox.Show(d.getsongs(this.metroTextBox14.Text));
		}
		private void metroProgressBar1_Click(object sender, EventArgs e)
		{
			if (!d2)
			{
				this.metroTabControl1.Controls.Add(this.metroTabPage7);
				this.metroTabPage7.Controls.Add(this.metroLabel26);
				this.metroTabPage7.Controls.Add(this.metroLabel27);
				this.metroTabPage7.Controls.Add(this.metroLabel25);
				d2 = true;
			}
		}
		private void metroButton6_Click(object sender, EventArgs e)
		{
			System.Collections.Specialized.NameValueCollection Data = new System.Collections.Specialized.NameValueCollection();
			Data["api_paste_name"] = "Haru Bot commands as of: " + DateTime.Now.ToString("HH:mm:ss") + " Local bot time!";
			Data["api_paste_expire_date"] = "1D";
			Data["api_paste_code"] = getCommands();
			Data["api_paste_private"] = "1";
			Data["api_dev_key"] = "81a709fa0ea892447568ed8627095bf0";
			Data["api_option"] = "paste";
			Data["api_paste_format"] = "php";
			if (!(Data["api_paste_code"] == ""))
			{
				WebClient wb = new WebClient();
				byte[] bytes = wb.UploadValues("http://pastebin.com/api/api_post.php", Data);
				string response;
				using (MemoryStream ms = new MemoryStream(bytes))
				using (StreamReader reader = new StreamReader(ms))
					response = reader.ReadToEnd();
				if (response.StartsWith("Bad API request"))
				{
					Console.WriteLine(response);
				}
				else
				{
					Clipboard.SetText(response);
					Console.WriteLine(response + " is now on your clipboard.");
				}
			}
			else
			{
				Jahvahnbot.Properties.Settings.Default.link = "ERROR FETCHING COMMANDS";
				Jahvahnbot.Properties.Settings.Default.Save();
			}
		}
		#endregion
		#region GUI Buttons
		private void metroButton2_Click(object sender, EventArgs e)
		{
			if (d)
				bot.sM(Jahvahnbot.Properties.Settings.Default.byemsg);
			d = false;
			Environment.Exit(0);
		} //Shutdown
		private void metroButton14_Click(object sender, EventArgs e)
		{
			var m = new Random();
			int next = m.Next(0, 13);
			this.metroStyleManager1.Style = (MetroColorStyle)next;
		}
		private void metroButton15_Click(object sender, EventArgs e)
		{
			metroStyleManager1.Theme = metroStyleManager1.Theme == MetroThemeStyle.Light ? MetroThemeStyle.Dark : MetroThemeStyle.Light;
		}
		private void metroTile1_Click(object sender, EventArgs e)
		{
			int x = 0;
			string error = "";
			if (Int32.TryParse(this.metroTextBox3.Text.ToString(), out x))
				Jahvahnbot.Properties.Settings.Default.duration = x * 1000;
			else
				error += " Error with Update freq.";
			if (this.metroTextBox4.Text != null)
				Jahvahnbot.Properties.Settings.Default.ppgain = this.metroTextBox4.Text;
			else
				error += " Error with ppgain emote.";
			if (this.metroTextBox5.Text != null)
				Jahvahnbot.Properties.Settings.Default.pploss = this.metroTextBox5.Text;
			else
				error += " Error with pploss emote.";
			Jahvahnbot.Properties.Settings.Default.osupath = this.metroTextBox10.Text;
			if (this.metroTextBox15.Text.EndsWith("txt"))
				Jahvahnbot.Properties.Settings.Default.nppath = this.metroTextBox15.Text;
			else
				error += " Error with NP file path.";

			if (error != "")
				MetroMessageBox.Show(this, error);
			error = "";
			metroLabel14.Text = "Updated!";
			Jahvahnbot.Properties.Settings.Default.Save();
		}
		private void metroTile2_Click(object sender, EventArgs e)
		{
			Jahvahnbot.Properties.Settings.Default.connectmsg = metroTextBox12.Text;
			Jahvahnbot.Properties.Settings.Default.byemsg = metroTextBox13.Text;
			Jahvahnbot.Properties.Settings.Default.Save();
			this.metroLabel23.Text = "Done.";
		}
		private void metroTile3_Click(object sender, EventArgs e)
		{
			Jahvahnbot.Properties.Settings.Default.fukit = this.metroTextBox11.Text;
			if (this.metroTextBox1.Text != null)
				Jahvahnbot.Properties.Settings.Default.sostupid = Int32.Parse(this.metroTextBox1.Text);
			Jahvahnbot.Properties.Settings.Default.Save();
			this.metroLabel24.Text = "Done.";
		}
		private void metroTile4_Click(object sender, EventArgs e)
		{
			conf.channel = "#jahvahn";
			bot = new IRCBot(conf);
			int _enabled = 1, _level = 0;
			bool _work;
			if (!metroCheckBox3.Checked)
				_enabled = 0;
			if (metroCheckBox2.Checked)
				_level = 1;
			if (metroRadioButton1.Checked)
			{
				_work = bot.addCom(conf.channel, this.metroTextBox7.Text, this.metroTextBox8.Text, _enabled, _level, Int32.Parse(this.metroTextBox6.Text));
				if (_work)
					this.metroLabel16.Text = "Command " + this.metroTextBox8.Text + " added";
				else
					this.metroLabel16.Text = "Unable to add command " + this.metroTextBox8.Text;
			}
			else if (metroRadioButton2.Checked)
			{
				_work = bot.updCom(conf.channel, this.metroTextBox7.Text, this.metroTextBox8.Text, _enabled, _level, Int32.Parse(this.metroTextBox6.Text));
				if (_work)
					this.metroLabel16.Text = "Command " + this.metroTextBox8.Text + " updated";
				else
					this.metroLabel16.Text = "Unable to update command " + this.metroTextBox8.Text;
			}
			else if (metroRadioButton3.Checked)
			{
				_work = bot.delCom(conf.channel, this.metroTextBox8.Text);
				if (_work)
					this.metroLabel16.Text = "Command " + this.metroTextBox8.Text + " deleted";
				else
					this.metroLabel16.Text = "Unable to delete command " + this.metroTextBox8.Text;
			}
		}
		private void metroTile5_Click(object sender, EventArgs e)
		{
			try
			{
				bot.addBlackList(conf.channel, this.metroTextBox2.Text);
				this.metroLabel9.Text = this.metroTextBox2.Text + " added to blacklist!";
			}
			catch
			{
				this.metroLabel9.Text = "Error connecting to Database.";
			}
		}
		private void metroTile6_Click(object sender, EventArgs e)
		{
			try
			{
				bot.delBlackList(conf.channel, this.metroTextBox2.Text);
				this.metroLabel9.Text = this.metroTextBox2.Text + " removed from blacklist!";
			}
			catch
			{
				this.metroLabel9.Text = "Error connecting to Database.";
			}
		}
		private void metroTile8_Click(object sender, EventArgs e)
		{
			this.metroStyleManager1.Style = (MetroColorStyle)next;
			next++;
			if (next == 14)
				next = 0;
		}
		private void metroTile9_Click(object sender, EventArgs e)
		{
			bot.sM(this.metroTextBox9.Text);
			this.metroTextBox9.Text = "";
		}
		#endregion
		#region GUI Toggles
		private void metroToggle1_CheckedChanged(object sender, EventArgs e)
		{
			if (metroToggle1.Checked)
			{
				Jahvahnbot.Properties.Settings.Default.enableosu = true;
			}
			else
			{
				Jahvahnbot.Properties.Settings.Default.enableosu = false;
			}
			Jahvahnbot.Properties.Settings.Default.Save();
		}
		private void metroComboBox1_SelectedIndexChanged(object sender, EventArgs e)
		{
			using (SQLiteConnection dbCon = new SQLiteConnection("Data Source=Database.sqlite;Version=3;"))
			{
				dbCon.Open();
				string qSelect = "SELECT * FROM commands WHERE command = '" + metroComboBox1.SelectedItem.ToString() + "'";
				SQLiteCommand command2 = new SQLiteCommand(qSelect, dbCon);
				SQLiteDataReader reader = command2.ExecuteReader();
				while (reader.Read())
				{
					this.metroTextBox6.Text = (string)reader["cooldown"];
					this.metroTextBox7.Text = (string)reader["text"];
					this.metroTextBox8.Text = (string)reader["command"];
					if (Int32.Parse((string)reader["enabled"]) == 1)
						this.metroCheckBox3.Checked = true;
					else
						this.metroCheckBox3.Checked = false;
					if (Int32.Parse((string)reader["level"]) == 1)
						this.metroCheckBox2.Checked = true;
					else
						this.metroCheckBox2.Checked = false;
				}
				dbCon.Close();
			}
		}
		private void metroComboBox2_SelectedIndexChanged(object sender, EventArgs e)
		{
			using (SQLiteConnection dbCon = new SQLiteConnection("Data Source=Database.sqlite;Version=3;"))
			{
				dbCon.Open();
				SQLiteCommand command2 = dbCon.CreateCommand();
				command2.CommandText = String.Format("SELECT * FROM blacklist WHERE text = @text");
				command2.Parameters.Add(new SQLiteParameter("@text", metroComboBox2.SelectedItem));
				SQLiteDataReader reader = command2.ExecuteReader();
				while (reader.Read())
				{
					this.metroTextBox2.Text = (string)reader["text"];
				}
				dbCon.Close();
			}
		}
		private void metroComboBox3_SelectedIndexChanged(object sender, EventArgs e)
		{
			using (SQLiteConnection dbCon = new SQLiteConnection("Data Source=NerdTracker.sqlite;Version=3;"))
			{
				dbCon.Open();
				SQLiteCommand command2 = dbCon.CreateCommand();
				command2.CommandText = String.Format("SELECT * FROM points WHERE user = @text");
				command2.Parameters.Add(new SQLiteParameter("@text", metroComboBox3.SelectedItem));
				SQLiteDataReader reader = command2.ExecuteReader();
				while (reader.Read())
				{
					this.metroTextBox16.Text = (string)reader["amount"];
				}
				dbCon.Close();
			}
		}
		private void metroTabControl1_SelectedIndexChanged(object sender, EventArgs e)
		{
			this.metroLabel14.Text = "";
			this.metroLabel23.Text = "";
			this.metroLabel24.Text = "";
			this.metroLabel16.Text = "";
			this.metroLabel9.Text = "";
			this.metroTextBox2.Text = "";
			this.metroTextBox6.Text = "";
			this.metroTextBox7.Text = "";
			this.metroTextBox8.Text = "";
			this.metroTextBox16.Text = "";
			this.metroTextBox17.Text = "";
			this.metroTextBox18.Text = "";
			if (metroTabControl1.SelectedIndex == 1)
			{
				metroComboBox1.Items.Clear();
				using (SQLiteConnection dbCon = new SQLiteConnection("Data Source=Database.sqlite;Version=3;"))
				{
					dbCon.Open();
					string qSelect = "SELECT * FROM commands";
					SQLiteCommand command2 = new SQLiteCommand(qSelect, dbCon);
					SQLiteDataReader reader = command2.ExecuteReader();
					while (reader.Read())
					{
						metroComboBox1.Items.Add((string)reader["command"]);
					}
					dbCon.Close();
				}
			}
			if (metroTabControl1.SelectedIndex == 2)
			{
				metroComboBox2.Items.Clear();
				using (SQLiteConnection dbCon = new SQLiteConnection("Data Source=Database.sqlite;Version=3;"))
				{
					dbCon.Open();
					string qSelect = "SELECT * FROM blacklist";
					SQLiteCommand command2 = new SQLiteCommand(qSelect, dbCon);
					SQLiteDataReader reader = command2.ExecuteReader();
					while (reader.Read())
					{
						metroComboBox2.Items.Add((string)reader["text"]);
					}
					dbCon.Close();
				}
			}
			if (metroTabControl1.SelectedIndex == 6)
			{
				metroComboBox4.Items.Clear();
				using (SQLiteConnection dbCon = new SQLiteConnection("Data Source=Quotes.sqlite;Version=3;"))
				{
					dbCon.Open();
					string qSelect = "SELECT * FROM quotes";
					SQLiteCommand command2 = new SQLiteCommand(qSelect, dbCon);
					SQLiteDataReader reader = command2.ExecuteReader();
					while (reader.Read())
					{
						metroComboBox4.Items.Add((string)reader["quote"]);
					}
					dbCon.Close();
				}
			}
			if (metroTabControl1.SelectedIndex == 8)
			{
				try
				{
					listBox1.Items.Clear();
					using (StreamReader rdr = new StreamReader(@"Highlights.txt"))
					{
						string g = "";
						while ((g = rdr.ReadLine()) != null)
						{
							listBox1.Items.Add(g);
						}
					}
				}
				catch (Exception g)
				{
					MessageBox.Show(g.ToString());
				}
			}
			/*if (metroTabControl1.SelectedIndex == 6)
			{
				metroComboBox3.Items.Clear();
				pts.Clear();
				int num = 1;
				using (SQLiteConnection dbCon = new SQLiteConnection("Data Source=NerdTracker.sqlite;Version=3;"))
				{
					dbCon.Open();
					string qSelect = "SELECT * FROM points";
					SQLiteCommand command2 = new SQLiteCommand(qSelect, dbCon);
					SQLiteDataReader reader = command2.ExecuteReader();
					while (reader.Read())
					{
						metroComboBox3.Items.Add((string)reader["user"]);
						pts[(string)reader["user"]] = Int32.Parse((string)reader["amount"]);
					}
					var top5 = pts.OrderByDescending(pair => pair.Value).Take(5)
			   .ToDictionary(pair => pair.Key, pair => pair.Value);
					foreach (KeyValuePair<string, int> top in top5)
					{
						switch (num)
						{
							case 1:
								this.metroLabel36.Text = top.Key + ": " + top.Value;
								num++;
								break;
							case 2:
								this.metroLabel37.Text = top.Key + ": " + top.Value;
								num++;
								break;
							case 3:
								this.metroLabel38.Text = top.Key + ": " + top.Value;
								num++;
								break;
							case 4:
								this.metroLabel39.Text = top.Key + ": " + top.Value;
								num++;
								break;
							case 5:
								this.metroLabel40.Text = top.Key + ": " + top.Value;
								num = 1;
								break;
							default:
								num = 1;
								break;
						}
					}
					dbCon.Close();
				}
			}*/
		}
		#endregion
		#region Pastebin
		public string getCommands()
		{
			using (SQLiteConnection dbCon = new SQLiteConnection("Data Source=Database.sqlite;Version=3;"))
			{
				string output = "";
				dbCon.Open();
				string qSelect = "SELECT * FROM commands";
				SQLiteCommand command2 = new SQLiteCommand(qSelect, dbCon);
				SQLiteDataReader reader = command2.ExecuteReader();
				while (reader.Read())
				{
					output += (string)reader["command"] + " - " + "\"" + (string)reader["text"] + "\"\n";
				}
				dbCon.Close();
				return output;
			}
		}
		public void upload()
		{
			System.Collections.Specialized.NameValueCollection Data = new System.Collections.Specialized.NameValueCollection();

			Data["api_paste_name"] = "Haru Bot commands as of: " + DateTime.Now.ToString("HH:mm:ss") + " Local bot time!";
			Data["api_paste_expire_date"] = "1D";
			Data["api_paste_code"] = getCommands();
			Data["api_paste_private"] = "1";
			Data["api_dev_key"] = Jahvahnbot.Properties.Settings.Default.PastebinAPI;
			Data["api_option"] = "paste";
			Data["api_paste_format"] = "php";
			if (!(Data["api_paste_code"] == ""))
			{
				WebClient wb = new WebClient();
				byte[] bytes = wb.UploadValues("http://pastebin.com/api/api_post.php", Data);
				string response;
				using (MemoryStream ms = new MemoryStream(bytes))
				using (StreamReader reader = new StreamReader(ms))
					response = reader.ReadToEnd();
				if (response.StartsWith("Bad API request"))
				{
					Jahvahnbot.Properties.Settings.Default.link = "ERROR FETCHING URL";
					Jahvahnbot.Properties.Settings.Default.Save();
				}
				else
				{
					Jahvahnbot.Properties.Settings.Default.link = response;
					Jahvahnbot.Properties.Settings.Default.Save();
				}
			}
			else
			{
				Jahvahnbot.Properties.Settings.Default.link = "ERROR FETCHING COMMANDS";
				Jahvahnbot.Properties.Settings.Default.Save();
			}
		}
		private void metroButton3_Click(object sender, EventArgs e)
		{
			upload();
		}
		#endregion
		#region Quotes
		private static DialogResult ShowInputDialog(ref string input, string title)
		{
			System.Drawing.Size size = new System.Drawing.Size(200, 70);
			Form inputBox = new Form();

			inputBox.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
			inputBox.ClientSize = size;
			inputBox.Text = title;

			System.Windows.Forms.TextBox textBox = new TextBox();
			textBox.Size = new System.Drawing.Size(size.Width - 10, 23);
			textBox.Location = new System.Drawing.Point(5, 5);
			textBox.Text = input;
			inputBox.Controls.Add(textBox);

			Button okButton = new Button();
			okButton.DialogResult = System.Windows.Forms.DialogResult.OK;
			okButton.Name = "okButton";
			okButton.Size = new System.Drawing.Size(75, 23);
			okButton.Text = "&OK";
			okButton.Location = new System.Drawing.Point(size.Width - 80 - 80, 39);
			inputBox.Controls.Add(okButton);

			Button cancelButton = new Button();
			cancelButton.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			cancelButton.Name = "cancelButton";
			cancelButton.Size = new System.Drawing.Size(75, 23);
			cancelButton.Text = "&Cancel";
			cancelButton.Location = new System.Drawing.Point(size.Width - 80, 39);
			inputBox.Controls.Add(cancelButton);

			inputBox.AcceptButton = okButton;
			inputBox.CancelButton = cancelButton;
			inputBox.StartPosition = FormStartPosition.CenterParent;

			DialogResult result = inputBox.ShowDialog();
			input = textBox.Text;
			return result;
		}
		private void metroComboBox4_SelectedIndexChanged(object sender, EventArgs e)
		{
			using (SQLiteConnection dbCon = new SQLiteConnection("Data Source=Quotes.sqlite;Version=3;"))
			{
				dbCon.Open();
				SQLiteCommand command2 = new SQLiteCommand(dbCon);
				command2.CommandText = "SELECT * FROM quotes WHERE quote = @r";
				command2.Parameters.Add(new SQLiteParameter("@r", metroComboBox4.SelectedItem));
				SQLiteDataReader reader = command2.ExecuteReader();
				while (reader.Read())
				{
					this.metroTextBox17.Text = (string)reader["quote"];
					this.metroTextBox18.Text = (string)reader["date"];
				}
				dbCon.Close();
				this.metroButton5.Enabled = true;
			}
		}
		private void metroButton5_Click(object sender, EventArgs e)
		{
			try
			{
				string d = "";
				if (ShowInputDialog(ref d, "Enter Quote:") == DialogResult.OK)
				{
					using (SQLiteConnection dbCon = new SQLiteConnection("Data Source=Quotes.sqlite;Version=3;"))
					{
						dbCon.Open();
						bool _xist = false;
						SQLiteCommand command2 = new SQLiteCommand(dbCon);
						command2.CommandText = "SELECT * FROM quotes WHERE quote = @text";
						command2.Parameters.Add(new SQLiteParameter("@text", d));
						SQLiteDataReader reader = command2.ExecuteReader();
						while (reader.Read())
						{
							_xist = true;
						}
						if (!_xist)
						{
							SQLiteCommand run = new SQLiteCommand(dbCon);
							run.CommandText = "INSERT INTO quotes (quote, date) VALUES (@text, @date)";
							run.Parameters.Add(new SQLiteParameter("@text", d));
							run.Parameters.Add(new SQLiteParameter("@date", DateTime.Now.ToString("MMM dd, yyy")));
							run.ExecuteNonQuery();
							MessageBox.Show(this, "\"" + d + "\" has been added as a quote.");
							bot.fillQuoteList();
							Console.WriteLine("Added quote \"{1}\" at: {1}", d, DateTime.Now.ToString("MMM dd, yyy"));
						}
						else
						{
							MessageBox.Show(this, "That quote already exists.");
							Console.WriteLine("Quote already exists.");
						}
						dbCon.Close();
					}
				}
			}
			catch
			{
				MessageBox.Show("Connect the bot before changing settings!");
			}
		}
		private void metroButton11_Click(object sender, EventArgs e)
		{
			try
			{
				using (SQLiteConnection dbCon = new SQLiteConnection("Data Source=Quotes.sqlite;Version=3;"))
				{
					dbCon.Open();
					SQLiteCommand command2 = new SQLiteCommand(dbCon);
					command2.CommandText = "UPDATE quotes SET quote = @r, date = @w WHERE quote = @t";
					command2.Parameters.Add(new SQLiteParameter("@r", this.metroTextBox17.Text));
					command2.Parameters.Add(new SQLiteParameter("@w", this.metroTextBox18.Text));
					command2.Parameters.Add(new SQLiteParameter("@t", metroComboBox4.SelectedItem.ToString()));
					command2.ExecuteNonQuery();
					dbCon.Close();
					bot.fillQuoteList();
					MessageBox.Show(this, "The quote has been updated.");
				}
			}
			catch
			{
				MessageBox.Show("Connect the bot before changing settings!");
			}
		}
		private void metroButton10_Click(object sender, EventArgs e)
		{
			try
			{
				using (SQLiteConnection dbCon = new SQLiteConnection("Data Source=Quotes.sqlite;Version=3;"))
				{
					dbCon.Open();
					SQLiteCommand command2 = new SQLiteCommand(dbCon);
					command2.CommandText = "DELETE FROM quotes WHERE quote = @r";
					command2.Parameters.Add(new SQLiteParameter("@r", this.metroTextBox17.Text));
					command2.ExecuteNonQuery();
					dbCon.Close();
					bot.fillQuoteList();
					MessageBox.Show(this, "The quote has been deleted.");
				}
			}
			catch
			{
				MessageBox.Show("Connect the bot before changing settings!");
			}
		}
		#endregion
		#region Polls
		public void vote(string p)
		{
			try
			{
				if (poll.isRunning())
					poll.vote(p);
			}
			catch
			{
				if (bot != null)
					bot.sM("There's no poll currently running!");
			}
		}
		private void metroButton7_Click(object sender, EventArgs e)
		{
			try
			{
				if (poll == null)
				{
					Dictionary<string, int> g = new Dictionary<string, int>();
					g.Clear();
					string[] gg = this.metroTextBox20.Text.Split(new char[] { ';' }, 10);
					foreach (string f in gg)
					{
						if (f != "")
							g.Add(f.ToLower(), 0);
					}
					pconf.ans = g;
					pconf.duration = Int32.Parse(this.metroTextBox21.Text);
					poll = new Poll(pconf);
					bot.sM(poll.startPoll(this.metroTextBox19.Text));
				}
			}
			catch (Exception ed)
			{
				MetroMessageBox.Show(this, "Fatal error attempting to initiate poll. Loading error debugger..", "", MessageBoxButtons.OK, MessageBoxIcon.Error);
				MessageBox.Show(ed.ToString());
			}
		}
		public void endPoll(string results)
		{
			if (poll.isRunning())
			{
				bot.sM(poll.endPoll());
				if (results != null)
				{
					this.metroTextBox22.Text = poll.getPollResults();
					poll = null;
				}
			}
		}
		private void metroButton8_Click(object sender, EventArgs e)
		{
			endPoll("form");
		}
		#endregion
		#region Feature Requests
		private void metroButton16_Click(object sender, EventArgs e)
		{
			if (Directory.Exists(@"Feature Requests"))
				metroTextBox25.Text += "Skipping setup.\r\n";
			else
			{
				Directory.CreateDirectory(@"Feature Requests");
				metroTextBox25.Text += "Setup Succesful.\r\n";
			}
			metroProgressSpinner1.Visible = true;
			metroProgressSpinner1.EnsureVisible = true;
			metroButton16.Text = "Working...";
			if (metroCheckBox1.Checked)
			{
				if (!File.Exists(@"Feature Requests\" + metroTextBox23.Text + ".txt"))
				{
					using (TextWriter streamWriter = new StreamWriter(@"Feature Requests\" + metroTextBox23.Text + ".txt", true))
					{
						streamWriter.WriteLine(metroTextBox24.Text);
						streamWriter.Close();
					}
					metroTextBox25.Text += "Local file " + metroTextBox23.Text + ".txt has been created.\r\n";
				}
				else
				{
					metroTextBox25.Text += "Skipping Local save - File already exists.\r\n";
					metroTextBox25.Text += "";
				}
			}
			metroTextBox25.Text += "Starting upload...\r\n";
			backgroundWorker1.RunWorkerAsync();
		}
		private void backgroundWorker1_DoWork(object sender, DoWorkEventArgs e)
		{
			System.Net.Mail.MailMessage message = new System.Net.Mail.MailMessage();
			message.To.Add("hailzaros@gmail.com");
			message.Subject = "[" + DateTime.Today + "] HaruRequest - " + metroTextBox23.Text;
			message.From = new System.Net.Mail.MailAddress("harurequests@gmail.com");
			message.Body = metroTextBox24.Text;
			System.Net.Mail.SmtpClient SmtpServer = new System.Net.Mail.SmtpClient("smtp.gmail.com");
			SmtpServer.Port = 587;
			SmtpServer.Credentials = new System.Net.NetworkCredential("harurequests", Jahvahnbot.Properties.Settings.Default.mailPassword);
			SmtpServer.EnableSsl = true;
			SmtpServer.Send(message);
		}
		private void backgroundWorker1_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
		{
			this.metroButton16.Text = "Upload";
			this.metroProgressSpinner1.Visible = false;
			this.metroProgressSpinner1.EnsureVisible = false;
			this.metroTextBox25.Text += "Upload Successful.\r\n";
			this.metroTextBox25.Text += "";
		}
		#endregion
		#region Updates
		private void backgroundWorker2_DoWork(object sender, DoWorkEventArgs e)
		{
			WebClient client = new WebClient();
			client.DownloadFile(new Uri("https://www.dropbox.com/s/chq75aua3nzxmqd/ver.txt?dl=1"), "upd\\ver.txt");
		}
		private void backgroundWorker2_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
		{
			if (File.Exists(@"ver.txt") && File.Exists(@"upd\\ver.txt"))
			{
				using (StreamReader s = new StreamReader(@"upd\\ver.txt"))
				{
					StreamReader s2 = new StreamReader(@"ver.txt");
					if (s.ReadToEnd() != s2.ReadToEnd())
						metroButton4.Visible = true;
					else if (s.ReadToEnd() == "upd")
						new WebClient().DownloadFileAsync(new Uri("https://www.dropbox.com/s/1yyvb2hzl0xi32d/updater.exe?dl=1"), "updater.exe");
					s.Close(); s2.Close();
				}
			}
			else
			{
				MetroMessageBox.Show(this, "Unable to find one of the files: \"ver.txt\", \"upd\\ver.txt\". \nPlease them re-create if they don't exist.", "Error while attempting to fetch updates", MessageBoxButtons.OK, MessageBoxIcon.Asterisk, MessageBoxDefaultButton.Button1);
			}
		}
		private void metroButton4_Click(object sender, EventArgs e)
		{
			System.Diagnostics.Process.Start(@"updater.exe");
			Environment.Exit(0);
		}
		private void metroTile7_Click(object sender, EventArgs e)
		{
			backgroundWorker2.RunWorkerAsync();
		}
		private void timer2_Tick(object sender, EventArgs e)
		{
			if (!metroButton4.Visible)
			{
				backgroundWorker2.RunWorkerAsync();
			}
		} //1h timer
		#endregion
	}
}
