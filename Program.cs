using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;
using System.IO;
using System.Threading.Tasks;
using System.Threading;
using System.Diagnostics;
using System.Data.SQLite;
using System.Net;
using System.Windows.Forms;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;


namespace Jahvahnbot
{
	
	internal struct IRCConfig
	{
		public bool joined;
		public string server;
		public int port;
		public string oauth;
		public string nick;
		public string channel;
		public bool debug;
	}

	internal class IRCBot
	{
		#region Variables
		string[] answers = { "It is certain", "It is decidedly so", "Without a doubt", "Yes definitely", 
							 "You may rely on it", "As I see it, yes", "Most likely", "Outlook good", 
							 "Yes", "Signs point to yes", "Reply hazy try again", "Ask again later", 
							 "Better not tell you now", "Cannot predict now", "Concentrate and ask again", 
							 "Don't count on it", "My reply is no", "My sources say no", "Outlook not so good",
							 "Very doubtful" }; //8ball
		string[] abvs = { "august", "exalted", "magnific", "pompus", "sublime", "awesome", "fab", "magnificent",
							"regal", "sumptuous", "ceremonious", "grand", "marvelous", "royal", "superb", "cool", "grandiose",
							"mind-blowing", "smashing", "courtly", "imperial", "monumental", "soveregin",
							"dignified", "imposing", "noble", "stately", "elevated", "lofy", "out of this world",
							"stunning"};
		public osu_np osuSession = new osu_np();
		private TcpClient IRCConnection = null;
		private IRCConfig config;
		private NetworkStream networkStream = null;
		private StreamReader connectionStreamReader = null;
		private StreamWriter connectionStreamWriter = null, errorLog = null;
		private string allowlink, cmdoncd = "", ytID = "", ytResponse = "";
		private HashSet<string> ModList = new HashSet<string>();
		private List<string> waitlist = new List<string>();
		private List<string> quotes = new List<string>();
		private List<string> voted = new List<string>();
		private Dictionary<string, int> ans = new Dictionary<string, int>();
		private Regex linkParser = new Regex(@"^(http|https|ftp|)\://|[a-zA-Z0-9\-\.]+\.[a-zA-Z](:[a-zA-Z0-9]*)?/?([a-zA-Z0-9\-\._\?\,\'/\\\+&amp;%\$#\=~])*[^\.\,\)\(\s]$", RegexOptions.IgnoreCase);
		private string[] blacklist;
		private bool banneduser2 = false, IsWaitlist = false, run2 = true;
		private int globalTimer = 0, globallongTimer = 0;
		#endregion
		#region Connection
		public IRCBot(IRCConfig config)
		{
			this.config = config;
		}
		public void Connect()
		{
			bool pass = true;
			try
			{
				IRCConnection = new TcpClient(config.server, config.port);
			}
			catch
			{
				MessageBox.Show("Unable to connect to twitch servers. (404 not found)");
				pass = false;
			}
			try
			{
				fillList();
				fillQuoteList();
				getModsTemp();
				networkStream = IRCConnection.GetStream();
				connectionStreamReader = new StreamReader(networkStream);
				connectionStreamWriter = new StreamWriter(networkStream);
				errorLog = new StreamWriter(@"ErrorLog.log", true);
				sendData("PASS", config.oauth);
				sendData("NICK", config.nick);
			}
			catch
			{
				MessageBox.Show("Unable to properly initiate: " + config.nick + ".");
				pass = false;
			}
			if (pass)
				new Thread(() => IRCWork()).Start();
			else
				MessageBox.Show("Due to an issue while attempting to login to Twitch Servers as " + config.nick + ", your request to connect has been cancelled.");
		}
		#endregion
		#region GUI Commands
		public void fillList()
		{
			List<string> tmp = new List<string>();
			using (SQLiteConnection dbCon = new SQLiteConnection("Data Source=Database.sqlite;Version=3;"))
			{
				dbCon.Open();
				string qSelect = "SELECT * FROM blacklist WHERE channel = '" + config.channel + "'";
				SQLiteCommand command2 = new SQLiteCommand(qSelect, dbCon);
				SQLiteDataReader reader = command2.ExecuteReader();
				while (reader.Read())
				{
					tmp.Add((string)reader["text"]);
				}
				dbCon.Close();
			}
			blacklist = tmp.ToArray();
		}
		public void getModsTemp()
		{
			StreamReader rd = new StreamReader(@"mods.txt");
			string g = "";
			while ((g = rd.ReadLine()) != null)
			{
				ModList.Add(g);
				Console.WriteLine("Added: {0}", g);
			}
			rd.Close();
		}
		public bool delCom(string _channel, string _name)
		{
			bool diditwork = false;
			bool _xist = false;
			try
			{
				using (SQLiteConnection dbCon = new SQLiteConnection("Data Source=Database.sqlite;Version=3;"))
				{
					dbCon.Open();
					string qSelect = "SELECT * FROM commands WHERE command = '" + _name + "' AND channel = '" + _channel + "'";
					SQLiteCommand command2 = new SQLiteCommand(qSelect, dbCon);
					SQLiteDataReader reader = command2.ExecuteReader();
					while (reader.Read() && _xist == false)
					{
						_xist = true;
					}
					if (_xist)
					{
						string qUpdate = "";
						qUpdate = "DELETE FROM commands WHERE channel = '" + _channel + "' AND command = '" + _name + "'";
						SQLiteCommand cUpdate = new SQLiteCommand(qUpdate, dbCon);
						cUpdate.ExecuteNonQuery();
						diditwork = true;
					}
					else
					{
						diditwork = false;
					}
					dbCon.Close();
				}
			}
			catch
			{
				sM("Error accessing Database. Please try again.");
				Console.WriteLine("Error accessing Database.");
				Log("Error accessing Database");
			}
			finally { }
			return diditwork;
		}
		public bool delMessage(string _message, string _channel)
		{
			bool diditwork = false;
			bool _xist = false;
			using (SQLiteConnection dbCon = new SQLiteConnection("Data Source=Database.sqlite;Version=3;"))
			{
				dbCon.Open();
				string qSelect = "SELECT * FROM automsg WHERE text = '" + _message + "' AND channel = '" + _channel + "'";
				SQLiteCommand command2 = new SQLiteCommand(qSelect, dbCon);
				SQLiteDataReader reader = command2.ExecuteReader();
				while (reader.Read() && _xist == false)
				{
					_xist = true;
				}
				if (_xist)
				{
					string qUpdate = "";
					qUpdate = "DELETE FROM automsg WHERE channel = '" + _channel + "' AND text = '" + _message + "'";
					SQLiteCommand cUpdate = new SQLiteCommand(qUpdate, dbCon);
					cUpdate.ExecuteNonQuery();
					diditwork = true;
				}
				else
				{
					diditwork = false;
				}
				dbCon.Close();
			}
			return diditwork;
		}
		public bool addCom(string _channel, string _text, string _name, int _enabled, int _level, int _cooldown)
		{
			bool diditwork = false;
			try
			{
				using (SQLiteConnection dbCon = new SQLiteConnection("Data Source=Database.sqlite;Version=3;"))
				{
					dbCon.Open();
					string qSelect = "SELECT * FROM commands WHERE command = '" + _name + "' AND channel = '" + _channel + "'";
					SQLiteCommand command2 = new SQLiteCommand(qSelect, dbCon);
					SQLiteDataReader reader = command2.ExecuteReader();
					bool _xist = false;
					while (reader.Read() && _xist == false)
					{
						_xist = true;
					}
					if (!_xist)
					{
						SQLiteCommand cInsert = dbCon.CreateCommand();
						cInsert.CommandText = String.Format("INSERT INTO commands (command, text, channel, level, enabled, cooldown, oncooldown, cmdoncooldown) VALUES (@name, @text, @channel, '" + _level + "', '" + _enabled + "','" + _cooldown + "','0',@name)");
						cInsert.Parameters.Add(new SQLiteParameter("@name", _name));
						cInsert.Parameters.Add(new SQLiteParameter("@text", _text));
						cInsert.Parameters.Add(new SQLiteParameter("@channel", _channel));
						cInsert.ExecuteNonQuery();
						diditwork = true;
					}
					else
					{
						diditwork = false;
					}
					dbCon.Close();
				}
			}
			catch { diditwork = false; }
			finally { }
			return diditwork;
		}
		public bool updCom(string _channel, string _text, string _name, int _enabled, int _level, int _cooldown)
		{
			bool diditwork = false;
			int tmp = 0, tmp2 = 0, tmp4 = 0;
			string tmp3 = "";
			try
			{
				using (SQLiteConnection dbCon = new SQLiteConnection("Data Source=Database.sqlite;Version=3;"))
				{
					dbCon.Open();
					bool _xist = false;
					string qSelect = "SELECT * FROM commands WHERE command = '" + _name + "' AND channel = '" + _channel + "'";
					SQLiteCommand command2 = new SQLiteCommand(qSelect, dbCon);
					SQLiteDataReader reader = command2.ExecuteReader();
					while (reader.Read() && _xist == false)
					{
						_xist = true;
						tmp2 = Int32.Parse((string)reader["enabled"]);
						tmp = Int32.Parse((string)reader["level"]);
						tmp3 = (string)reader["text"];
						tmp4 = Int32.Parse((string)reader["cooldown"]);
					}
					if (_xist)
					{
						SQLiteCommand cmd = dbCon.CreateCommand();
						cmd.CommandText = String.Format("UPDATE commands SET enabled = '" + _enabled + "', level = '" + _level + "', text = @text, cooldown = '" + _cooldown + "' WHERE channel = @channel AND command = @cmd");
						cmd.Parameters.Add(new SQLiteParameter("@cmd", _name));
						cmd.Parameters.Add(new SQLiteParameter("@text", _text));
						cmd.Parameters.Add(new SQLiteParameter("@channel", _channel));
						cmd.ExecuteNonQuery();
						diditwork = true;
					}
					else
					{
						diditwork = false;
					}
					dbCon.Close();
				}
			}
			catch { diditwork = false; }
			finally { }
			return diditwork;
		}
		public bool addBlackList(string _channel, string _text)
		{
			bool diditwork = false;
			using (SQLiteConnection dbCon = new SQLiteConnection("Data Source=Database.sqlite;Version=3;"))
			{
				dbCon.Open();
				bool _xist = false;
				SQLiteCommand command2 = dbCon.CreateCommand();
				command2.CommandText = "SELECT * FROM blacklist WHERE text = @text AND channel = '" + _channel + "'";
				command2.Parameters.Add(new SQLiteParameter("@text", _text));
				SQLiteDataReader reader = command2.ExecuteReader();
				while (reader.Read() && _xist == false)
				{
					_xist = true;
				}
				if (!_xist)
				{
					SQLiteCommand cInsert = dbCon.CreateCommand();
					cInsert.CommandText = "INSERT INTO blacklist (text, channel) VALUES (@text, '" + _channel + "')";
					cInsert.Parameters.Add(new SQLiteParameter("@text", _text));
					cInsert.ExecuteNonQuery();
					diditwork = true;
				}
				else
				{
					diditwork = false;
				}
				dbCon.Close();
			}
			return diditwork;
		}
		public bool delBlackList(string _channel, string _text)
		{
			bool diditwork = false;
			using (SQLiteConnection dbCon = new SQLiteConnection("Data Source=Database.sqlite;Version=3;"))
			{
				dbCon.Open();
				bool _xist = false;
				SQLiteCommand command2 = dbCon.CreateCommand();
				command2.CommandText = "SELECT * FROM blacklist WHERE text = @text AND channel = '" + _channel + "'";
				command2.Parameters.Add(new SQLiteParameter("@text", _text));
				SQLiteDataReader reader = command2.ExecuteReader();
				while (reader.Read() && _xist == false)
				{
					_xist = true;
				}
				if (_xist)
				{
					SQLiteCommand cInsert = dbCon.CreateCommand();
					cInsert.CommandText = "DELETE FROM blacklist  WHERE channel = '" + _channel + "' AND text = @txt";
					cInsert.Parameters.Add(new SQLiteParameter("@txt", _text));
					cInsert.ExecuteNonQuery();
					diditwork = true;
				}
				else
				{
					diditwork = false;
				}
				dbCon.Close();
			}
			return diditwork;
		}
		#endregion
		#region Commands
		public void sendData(string cmd, string param)
		{
			if (param == null)
			{
				connectionStreamWriter.WriteLine(cmd);
				connectionStreamWriter.Flush();
			}
			else
			{
				connectionStreamWriter.WriteLine(cmd + " " + param);
				connectionStreamWriter.Flush();
			}
		}
		#region not used anywhere
		private void getPoints(string user, int opt)
		{
			try
			{
				using (SQLiteConnection dbCon = new SQLiteConnection("Data Source=NerdTracker.sqlite;Version=3;"))
				{
					dbCon.Open();
					bool _xist = false;
					string coins = "0";
					SQLiteCommand command2 = new SQLiteCommand(dbCon);
					command2.CommandText = "SELECT * FROM points WHERE user = @text";
					command2.Parameters.Add(new SQLiteParameter("@text", user));
					SQLiteDataReader reader = command2.ExecuteReader();
					while (reader.Read())
					{
						_xist = true;
						coins = (string)reader["amount"];
					}
					if (_xist)
					{
						if (opt == 1)
							sM(user + " currently has " + coins + " points.");
						else
							sM(user + " -> You currently have " + coins + " points.");
					}
					else
					{
						if (opt == 1)
							sM(user + " currently has 0 points.");
						else
							sM(user + " -> You currently have 0 points.");
					}
					dbCon.Close();
				}
			}
			catch { Console.WriteLine("Not a valid user?"); }
			finally { }
		}
		private void takePoints(string user, int amount)
		{
			try
			{
				using (SQLiteConnection dbCon = new SQLiteConnection("Data Source=NerdTracker.sqlite;Version=3;"))
				{
					dbCon.Open();
					bool _xist = false;
					int coins = 0;
					SQLiteCommand command2 = new SQLiteCommand(dbCon);
					command2.CommandText = "SELECT * FROM points WHERE user = @text";
					command2.Parameters.Add(new SQLiteParameter("@text", user));
					SQLiteDataReader reader = command2.ExecuteReader();
					while (reader.Read())
					{
						_xist = true;
						coins = Int32.Parse((string)reader["amount"]);
					}
					if (_xist)
					{
						if (!(amount > coins))
						{
							SQLiteCommand command = new SQLiteCommand(dbCon);
							command.CommandText = "UPDATE points SET amount = @amt WHERE user = @usr";
							command.Parameters.Add(new SQLiteParameter("@usr", user));
							command.Parameters.Add(new SQLiteParameter("@amt", (coins - amount)));
							command.ExecuteNonQuery();
							sM(user + " now has " + (coins - amount) + " points.");
						}
						else
						{
							sM("You tried taking more points than the user has.");
						}
					}
					else
					{
						sM(user + " doesn't even have any points!");
					}
					dbCon.Close();
				}
			}
			catch (Exception e)
			{
				sM("You tried taking " + amount + " points from " + user + ".. but I failed ;-;");
				Console.WriteLine(e);
			}
			finally { }
		}
		private void setPoints(string user, int amount)
		{
			try
			{
				using (SQLiteConnection dbCon = new SQLiteConnection("Data Source=NerdTracker.sqlite;Version=3;"))
				{
					dbCon.Open();
					bool _xist = false;
					int coins = 0;
					SQLiteCommand command2 = new SQLiteCommand(dbCon);
					command2.CommandText = "SELECT * FROM points WHERE user = @text";
					command2.Parameters.Add(new SQLiteParameter("@text", user));
					SQLiteDataReader reader = command2.ExecuteReader();
					while (reader.Read())
					{
						_xist = true;
						coins = Int32.Parse((string)reader["amount"]);
					}
					if (_xist)
					{
						SQLiteCommand command = new SQLiteCommand(dbCon);
						command.CommandText = "UPDATE points SET amount = @amt WHERE user = @usr";
						command.Parameters.Add(new SQLiteParameter("@usr", user));
						command.Parameters.Add(new SQLiteParameter("@amt", amount));
						command.ExecuteNonQuery();
						sM(user + " now has " + amount + " points.");
					}
					else
					{
						sM("That user has 0 points, so I can't set his points right now.");
					}
					dbCon.Close();
				}
			}
			catch (Exception e)
			{
				sM("You tried setting " + amount + " points to poor " + user + ".. but I failed ;-;");
				Console.WriteLine(e);
				Log("Error: " + e);
			}
			finally { }
		}
		public string setPts(string user, int amount)
		{
			string response = "Error accessing Database (?)";
			try
			{
				using (SQLiteConnection dbCon = new SQLiteConnection("Data Source=NerdTracker.sqlite;Version=3;"))
				{
					dbCon.Open();
					bool _xist = false;
					int coins = 0;
					SQLiteCommand command2 = new SQLiteCommand(dbCon);
					command2.CommandText = "SELECT * FROM points WHERE user = @text";
					command2.Parameters.Add(new SQLiteParameter("@text", user));
					SQLiteDataReader reader = command2.ExecuteReader();
					while (reader.Read())
					{
						_xist = true;
						coins = Int32.Parse((string)reader["amount"]);
					}
					if (_xist)
					{
						SQLiteCommand command = new SQLiteCommand(dbCon);
						command.CommandText = "UPDATE points SET amount = @amt WHERE user = @usr";
						command.Parameters.Add(new SQLiteParameter("@usr", user));
						command.Parameters.Add(new SQLiteParameter("@amt", amount));
						command.ExecuteNonQuery();
						response = user + " now has " + amount + " points.";
					}
					else
					{
						response = "That user has 0 points, so I can't set his points right now.";
					}
					dbCon.Close();
					return response;
				}
			}
			catch (Exception e)
			{
				Console.WriteLine(e);
				Log("Error: " + e);
				return response;
			}
			finally { }
		}
		private void givePoints(string user, int amount)
		{
			try
			{
				using (SQLiteConnection dbCon = new SQLiteConnection("Data Source=NerdTracker.sqlite;Version=3;"))
				{
					dbCon.Open();
					bool _xist = false;
					int coins = 0;
					SQLiteCommand command2 = new SQLiteCommand(dbCon);
					command2.CommandText = "SELECT * FROM points WHERE user = @text";
					command2.Parameters.Add(new SQLiteParameter("@text", user));
					SQLiteDataReader reader = command2.ExecuteReader();
					while (reader.Read())
					{
						_xist = true;
						coins = Int32.Parse((string)reader["amount"]);
					}
					if (_xist)
					{
						SQLiteCommand command = new SQLiteCommand(dbCon);
						command.CommandText = "UPDATE points SET amount = @amt WHERE user = @usr";
						command.Parameters.Add(new SQLiteParameter("@usr", user));
						command.Parameters.Add(new SQLiteParameter("@amt", (coins + amount)));
						command.ExecuteNonQuery();
						sM(user + " now has " + (coins + amount) + " points.");
					}
					else
					{
						SQLiteCommand command = new SQLiteCommand(dbCon);
						command.CommandText = "INSERT INTO points (user, amount) VALUES (@usr, @amt)";
						command.Parameters.Add(new SQLiteParameter("@usr", user));
						command.Parameters.Add(new SQLiteParameter("@amt", amount));
						command.ExecuteNonQuery();
						sM(user + " now has " + (coins + amount) + " points.");
					}
					dbCon.Close();
				}
			}
			catch (Exception e)
			{
				sM("You tried adding " + amount + " of points.. but I failed ;-;");
				Console.WriteLine(e);
				Log("Error: " + e);
			}
			finally { }
		}
		public void ptstimer()
		{
			while (true)
			{
				Thread.Sleep(60000);/*
				using (SQLiteConnection dbCon = new SQLiteConnection("Data Source=NerdTracker.sqlite;Version=3;"))
				{
					try
					{
						using (var w = new WebClient())
						{
							string jsonData = w.DownloadString(string.Format("http://tmi.twitch.tv/group/user/jahvahn/chatters"));
							var stream = JObject.Parse(jsonData);
							var moderators = stream.SelectToken("chatters").SelectToken("moderators").Select(s => (string)s).ToArray();
							var staff = stream.SelectToken("chatters").SelectToken("staff").Select(s => (string)s).ToArray();
							var admins = stream.SelectToken("chatters").SelectToken("admins").Select(s => (string)s).ToArray();
							var global_mods = stream.SelectToken("chatters").SelectToken("global_mods").Select(s => (string)s).ToArray();
							var viewers = stream.SelectToken("chatters").SelectToken("viewers").Select(s => (string)s).ToArray();
							#region Add to Database
							if (!(moderators.Length < 1))
							{
								foreach (string g in moderators)
								{
									dbCon.Open();
									SQLiteCommand command2 = new SQLiteCommand(dbCon);
									command2.CommandText = "INSERT OR IGNORE INTO points (user, amount) VALUES (@usr, '1')";
									command2.Parameters.Add(new SQLiteParameter("@usr", g));
									command2.ExecuteNonQuery();
									dbCon.Close();
								}
							}
							if (!(staff.Length < 1))
							{
								foreach (string g in staff)
								{
									dbCon.Open();
									SQLiteCommand command2 = new SQLiteCommand(dbCon);
									command2.CommandText = "INSERT OR IGNORE INTO points (user, amount) VALUES (@usr, '1')";
									command2.Parameters.Add(new SQLiteParameter("@usr", g));
									command2.ExecuteNonQuery();
									dbCon.Close();
								}
							}
							if (!(admins.Length < 1))
							{
								foreach (string g in admins)
								{
									dbCon.Open();
									SQLiteCommand command2 = new SQLiteCommand(dbCon);
									command2.CommandText = "INSERT OR IGNORE INTO points (user, amount) VALUES (@usr, '1')";
									command2.Parameters.Add(new SQLiteParameter("@usr", g));
									command2.ExecuteNonQuery();
									dbCon.Close();
								}
							}
							if (!(global_mods.Length < 1))
							{
								foreach (string g in global_mods)
								{
									dbCon.Open();
									SQLiteCommand command2 = new SQLiteCommand(dbCon);
									command2.CommandText = "INSERT OR IGNORE INTO points (user, amount) VALUES (@usr, '1')";
									command2.Parameters.Add(new SQLiteParameter("@usr", g));
									command2.ExecuteNonQuery();
									dbCon.Close();
								}
							}
							if (!(viewers.Length < 1))
							{
								foreach (string g in viewers)
								{
									dbCon.Open();
									SQLiteCommand command2 = new SQLiteCommand(dbCon);
									command2.CommandText = "INSERT OR IGNORE INTO points (user, amount) VALUES (@usr, '1')";
									command2.Parameters.Add(new SQLiteParameter("@usr", g));
									command2.ExecuteNonQuery();
									dbCon.Close();
								}
							}
							#endregion
						}
					}
					catch
					{
						Console.WriteLine("Error fetching new viewers, twitch please!");
					}
					finally { }
					dbCon.Open();
					SQLiteCommand command = new SQLiteCommand(dbCon);
					command.CommandText = "UPDATE points SET amount = amount + @amt";
					command.Parameters.Add(new SQLiteParameter("@amt", 1));
					command.ExecuteNonQuery();
					dbCon.Close();
					Console.WriteLine("Points added");
					dbCon.Close();
				}
			}*/
			}
		}
		#endregion
		public bool RemoteFileExists(string url)
		{
			if (ytID != "")
			{
				try
				{
					using (WebClient c = new WebClient())
					{
						string json_data = "[" + c.DownloadString("https://www.googleapis.com/youtube/v3/videos?key="+ Jahvahnbot.Properties.Settings.Default.ytAPI +"&part=snippet,contentDetails&fields=items(snippet(title),contentDetails(duration))&id=" + ytID) + "]";
						JArray a = JArray.Parse(json_data);
						foreach (JObject o in a.Children<JObject>())
						{
							foreach (JProperty p in o.Properties())
							{
								if (p.Name == "items")
								{
									JArray f = JArray.Parse(p.Value.ToString());
									foreach (JObject fo in f.Children<JObject>())
									{
										foreach (JProperty fp in fo.Properties())
										{
											byte[] bytes = Encoding.Default.GetBytes(fp.Value.ToString().Remove(0, 15));
											string vidTitle = Encoding.UTF8.GetString(bytes);
											vidTitle = vidTitle.Remove(vidTitle.Length - 4, 4);
											if (vidTitle.IndexOf(":") == 0 && vidTitle.IndexOf(" ") == 1 &&
												vidTitle.IndexOf("\"") == 2 && vidTitle.IndexOf("P") == 3)
												vidTitle = vidTitle.Remove(0, 3);
											if (vidTitle.StartsWith("P"))
												ytResponse = "[" + System.Xml.XmlConvert.ToTimeSpan(vidTitle) + "]" + ytResponse;
											else
												ytResponse += " - " + vidTitle;
										}
									}
								}
							}
						}
					}
				}
				catch (Exception e)
				{
					Log("YTPostLink error - " + e);
				}
				ytID = "";
				return false;
			}
			else
			{
				if (!url.StartsWith("http://") || !url.StartsWith("https://"))
					url = "http://" + url;
				try
				{
					Uri uriResult;
					return Uri.TryCreate(url, UriKind.Absolute, out uriResult) && uriResult.Scheme == Uri.UriSchemeHttp;
				}
				catch (Exception e)
				{
					MessageBox.Show(e.ToString());
					Log("Link protection message: " + e);
					return false;
				}
			}
		}
		public void putonCooldown()
		{
			using (SQLiteConnection dbCon = new SQLiteConnection("Data Source=Database.sqlite;Version=3;"))
			{
				string command = "";
				int cooldown = 20;
				int oncooldown = 1;
				dbCon.Open();
				string qSelect = "SELECT * FROM commands WHERE oncooldown = '1' AND channel = '" + config.channel + "' AND cmdoncooldown = '" + cmdoncd + "'";
				SQLiteCommand command2 = new SQLiteCommand(qSelect, dbCon);
				SQLiteDataReader reader = command2.ExecuteReader();
				while (reader.Read())
				{
					cooldown = Int32.Parse((string)reader["cooldown"]);
					oncooldown = Int32.Parse((string)reader["oncooldown"]);
					command = (string)reader["cmdoncooldown"];
				}
				dbCon.Close();
				if (oncooldown == 1)
				{
					Thread.Sleep(cooldown * 1000);
					Console.WriteLine("Cooldown time is up for " + command); //Debug
					dbCon.Open();
					string qInsert = "UPDATE commands SET oncooldown = '0' WHERE channel ='" + config.channel + "' AND command = '" + command + "'";
					SQLiteCommand cInsert = new SQLiteCommand(qInsert, dbCon);
					cInsert.ExecuteNonQuery();
					dbCon.Close();
				}
			}
		} //Variable command timer. THIS SHOULD WORK IF timer() IS ACTIVE.
		public void runUptime()
		{
			try
			{
				using (WebClient c = new WebClient())
				{
					string webData = c.DownloadString("http://nightdev.com/hosted/uptime.php?channel=" + config.channel.Replace("#", ""));
					if (webData != "The channel is not live.")
					{
						sM(webData);
						Console.WriteLine("Uptime: {0}", webData);

					}
					else
					{
						sM("Error accessing API.");
						Console.WriteLine("\"The channel is not live\"");
					}
				}
			}
			catch
			{
				sM("Error accessing API.");
				Console.WriteLine("NIGHTDEVPLS");
			}
			finally { }
			globalTimer = 0;
		}
		private void IsOnCooldown(string[] ex)
		{
			using (SQLiteConnection dbCon = new SQLiteConnection("Data Source=Database.sqlite;Version=3;"))
			{
				dbCon.Open();
				bool _xist = false;
				string datetime = "";
				string dt = DateTime.Now.ToString("HH:mm:ss");
				SQLiteCommand command2 = dbCon.CreateCommand();
				command2.CommandText = "SELECT * FROM cooldown WHERE user = @user";
				command2.Parameters.Add(new SQLiteParameter("@user", (ex[0].Substring(ex[0].IndexOf(":"), ex[0].IndexOf("!"))).Replace(":", "")));
				SQLiteDataReader reader = command2.ExecuteReader();
				while (reader.Read() && !_xist)
				{
					_xist = true;
					datetime = (string)reader["DateTime"];
				}
				if (_xist)
				{
					TimeSpan duration = DateTime.Parse(dt).Subtract(DateTime.Parse(datetime));
					if (duration.Minutes >= 15)
					{
						SQLiteCommand cmd = dbCon.CreateCommand();
						cmd.CommandText = "DELETE FROM cooldown WHERE user = @user";
						cmd.Parameters.Add(new SQLiteParameter("@user", (ex[0].Substring(ex[0].IndexOf(":"), ex[0].IndexOf("!"))).Replace(":", "")));
						cmd.ExecuteNonQuery();
						sM((ex[0].Substring(ex[0].IndexOf(":"), ex[0].IndexOf("!"))).Replace(":", "") + " -> You can use request a song now!");
						Console.WriteLine("Cooldown reset for {0}", (ex[0].Substring(ex[0].IndexOf(":"), ex[0].IndexOf("!"))).Replace(":", ""));
					}
					else
					{
						sM((ex[0].Substring(ex[0].IndexOf(":"), ex[0].IndexOf("!"))).Replace(":", "") + " -> You still have to wait " + (15 - duration.Minutes) + " minutes before you can request another song!");
						Console.WriteLine("Cooldown still running for {0}", (ex[0].Substring(ex[0].IndexOf(":"), ex[0].IndexOf("!"))).Replace(":", ""));
					}
				}
				else
				{
					sM((ex[0].Substring(ex[0].IndexOf(":"), ex[0].IndexOf("!"))).Replace(":", "") + "-> You already can !osurequest HaruOno");
					Console.WriteLine("Poor guy never requested anything.");
				}
				dbCon.Close();
			}
		}
		private void AddToCooldown(string[] ex)
		{
			using (SQLiteConnection dbCon = new SQLiteConnection("Data Source=Database.sqlite;Version=3;"))
			{
				dbCon.Open();
				bool _xist = false;
				SQLiteCommand command2 = dbCon.CreateCommand();
				command2.CommandText = "SELECT * FROM cooldown WHERE user = @user";
				command2.Parameters.Add(new SQLiteParameter("@user", (ex[0].Substring(ex[0].IndexOf(":"), ex[0].IndexOf("!"))).Replace(":", "")));
				SQLiteDataReader reader = command2.ExecuteReader();
				while (reader.Read() && !_xist)
				{
					_xist = true;
				}
				if (!_xist)
				{
					SQLiteCommand d = dbCon.CreateCommand();
					d.CommandText = "INSERT INTO cooldown (user, DateTime) VALUES(@user, @datetime)";
					d.Parameters.Add(new SQLiteParameter("@user", (ex[0].Substring(ex[0].IndexOf(":"), ex[0].IndexOf("!")).Replace(":", ""))));
					d.Parameters.Add(new SQLiteParameter("@datetime", DateTime.Now.ToString("HH:mm:ss")));
					d.ExecuteNonQuery();
				}
				else
				{
					SQLiteCommand d = dbCon.CreateCommand();
					d.CommandText = "UPDATE cooldown SET user = @user, DateTime = @datetime WHERE user = @user";
					d.Parameters.Add(new SQLiteParameter("@user", (ex[0].Substring(ex[0].IndexOf(":"), ex[0].IndexOf("!")).Replace(":", ""))));
					d.Parameters.Add(new SQLiteParameter("@datetime", DateTime.Now.ToString("HH:mm:ss")));
					d.ExecuteNonQuery();
				}
				dbCon.Close();
			}
		}
		private void DoWaitlist(string[] ex)
		{
			string user = ex[0].Substring(ex[0].IndexOf(":"), ex[0].IndexOf("!")).Replace(":", "");
			if (ex.Length > 4)
			{
				switch (ex[4])
				{
					case "join":
						if (IsWaitlist)
						{
							waitlist.Add(user);
							sM(user + " has joined the waitlist!");
							Console.WriteLine("{0} joined waitlist.", user);
						}
						break;
					case "leave":
						if (IsWaitlist)
						{
							waitlist.Remove(user);
							sM(user + " has left the waitlist!");
							Console.WriteLine("{0} left waitlist.", user);
						}
						break;
					case "next":
						if (isMod(user) && IsWaitlist)
						{
							sM(waitlist[0] + " -> You're up!");
							Console.WriteLine("It's {0} 's turn.", user);
							waitlist.Remove(waitlist[0]);
						}
						break;
					case "reset":
						if (isMod(user) && IsWaitlist)
						{
							waitlist.Clear();
							sM(user + " has cleared the waitlist!");
							Console.WriteLine("{0} cleared the waitlist.", user);
						}
						break;
					case "whonext":
						if (IsWaitlist)
						{
							if (waitlist.Count >= 2)
							{
								sM("Next up is: " + waitlist[1]);
								Console.WriteLine("Next up: {0}", waitlist[1]);
							}
							else
							{
								sM("No one is next! Type \"!waitlist join\" to join!");
								Console.WriteLine("waitlist() is empty. No one is next?");
							}
						}
						break;
					case "on":
						if (isMod(user) && !IsWaitlist)
						{
							sM("Waitlist is now enabled! Type \"!waitlist join\" to join!");
							Console.WriteLine("Waitlist is now enabled.");
							IsWaitlist = true;
						}
						break;
					case "off":
						if (isMod(user) && IsWaitlist)
						{
							sM("Waitlist is now disabled, and cleared.");
							Console.WriteLine("Waitlist is now disabled.");
							waitlist.Clear();
							IsWaitlist = false;
						}
						break;
				}
			}
			else
			{
				if (IsWaitlist)
				{
					string output = "";
					for (int i = 0; i < waitlist.Count; i++)
					{
						output += waitlist[i] + ", ";
					}
					sM("Current waitlist: " + output);
					Console.WriteLine("Waitlist has been outputted.");
					output = "";
				}
				else
				{
					sM("Waitlist is disabled!");
					Console.WriteLine("Waitlist output requested denied (waitlist disabled)");
				}
			}
			globalTimer = 0;
		}
		public void fillQuoteList()
		{
			quotes.Clear();
			using (SQLiteConnection dbCon = new SQLiteConnection("Data Source=Quotes.sqlite;Version=3;"))
			{
				dbCon.Open();
				SQLiteCommand command2 = new SQLiteCommand("SELECT * FROM quotes", dbCon);
				SQLiteDataReader reader = command2.ExecuteReader();
				while (reader.Read())
				{
					quotes.Add((string)reader["quote"] + " (" + (string)reader["date"] + ")");
				}
				dbCon.Close();
			}
		}
		private void delQuote(string quote)
		{
			using (SQLiteConnection dbCon = new SQLiteConnection("Data Source=Quotes.sqlite;Version=3;"))
			{
				string date = "null";
				dbCon.Open();
				SQLiteCommand command2 = new SQLiteCommand(dbCon);
				command2.CommandText = "SELECT * FROM quotes WHERE quote = @text";
				command2.Parameters.Add(new SQLiteParameter("@text", quote));
				SQLiteDataReader reader = command2.ExecuteReader();
				while (reader.Read())
				{
					date = (string)reader["date"];
				}
				if (date != "null")
				{
					SQLiteCommand run = new SQLiteCommand(dbCon);
					run.CommandText = "DELETE FROM quotes WHERE quote = @text";
					run.Parameters.Add(new SQLiteParameter("@text", quote));
					run.ExecuteNonQuery();
					quotes.Remove(quote + " (" + date + ")");
					sM("\"" + quote + "\" deleted!");
					Console.WriteLine("Quote removed.");
				}
				else
				{
					sM("That quote doesn't even exist!");
					Console.WriteLine("Quote doesn't exist.");
				}
				dbCon.Close();
			}
		}
		public void startUp()
		{
			using (SQLiteConnection dbCon = new SQLiteConnection("Data Source=Database.sqlite;Version=3;"))
			{
				dbCon.Open();
				string qSelect = "DELETE FROM timeout", qSelect2 = "DELETE FROM cooldown", qSelect3 = "UPDATE commands SET oncooldown = '0'";
				SQLiteCommand command = new SQLiteCommand(qSelect, dbCon);
				SQLiteCommand command2 = new SQLiteCommand(qSelect2, dbCon);
				SQLiteCommand command3 = new SQLiteCommand(qSelect3, dbCon);
				command.ExecuteNonQuery();
				command2.ExecuteNonQuery();
				command3.ExecuteNonQuery();
				dbCon.Close();
			}
		}
		public void automatedMessages()
		{
			int _count = 0;
			while (true)
			{
				Thread.Sleep(60000);
				_count++;
				if (_count >= Jahvahnbot.Properties.Settings.Default.sostupid)
				{
					sM(Jahvahnbot.Properties.Settings.Default.fukit);
					Console.WriteLine("Automated message.");
					_count = 0;
				}
				else if (Jahvahnbot.Properties.Settings.Default.sostupid == 0)
					_count = 0;
			}
		}
		public void timeout(string newString)
		{
			Thread.Sleep(500);
			using (SQLiteConnection dbCon = new SQLiteConnection("Data Source=Database.sqlite;Version=3;"))
			{
				dbCon.Open();
				bool _xist = false;
				string qSelect = "SELECT * FROM timeout WHERE times = '0' AND name = '" + newString + "'";
				SQLiteCommand command2 = new SQLiteCommand(qSelect, dbCon);
				SQLiteDataReader reader = command2.ExecuteReader();
				while (reader.Read() && _xist == false)
				{
					_xist = true;
				}
				if (_xist)
				{
					string qInsert = "DELETE FROM timeout WHERE times = '0' AND name = '" + newString + "'";
					SQLiteCommand cInsert = new SQLiteCommand(qInsert, dbCon);
					cInsert.ExecuteNonQuery();
					sendData("PRIVMSG", config.channel + " :/timeout " + newString + " 60");
					sendData("PRIVMSG", config.channel + " :" + newString + "-> Ask a mod to !permit you before sending a link in chat! to see currently allowed links type !allowedlinks (timed out)");
					Console.WriteLine("Timing out: \"{0}\" for 60 seconds. Reason: Link protection", newString);
					sendData("PRIVMSG", config.channel + " :/timeout " + newString + " 60");
				}
				else
				{
					string qInsert = "INSERT INTO timeout (name, times) VALUES('" + newString + "', '0')";
					SQLiteCommand cInsert = new SQLiteCommand(qInsert, dbCon);
					cInsert.ExecuteNonQuery();
					sendData("PRIVMSG", config.channel + " :/timeout " + newString + " 1");
					sendData("PRIVMSG", config.channel + " :" + newString + "-> Ask a mod to !permit you before sending a link in chat! to see currently allowed links type !allowedlinks (purged)");
					Console.WriteLine("Purging \"{0}\" for 60 seconds. Reason: Link protection", newString);
					sendData("PRIVMSG", config.channel + " :/timeout " + newString + " 1");
				}
				dbCon.Close();
			}
		}
		#endregion
		#region Important shit
		public bool isLink(string message)
		{
			string h = "";
			string[] msg = message.Split(new char[] { ' ' });
			string[] chkmsg = message.Split(new char[] { '.' });
			foreach (string i in chkmsg)
			{
				if (i.Length < 2) return false;
			}
			foreach (string g in msg)
			{
				foreach (Match m in linkParser.Matches(g))
				{
					h += m.Value;
				}
				ytID = Regex.Match(g, @"(?:https?:\/\/)?(?:www\.)?youtu(?:.be\/|be\.com\/watch\?v=|be\.com\/v\/)(.{8,})").Groups[1].Value;
			}
			return RemoteFileExists(h);
		}
		public bool isMod(string user)
		{
			if (ModList != null)
			{
				foreach (string x in ModList)
				{
					if (x == user)
					{
						return true;
					}
				}
			}
			return false;
		}
		public void timer()
		{
			while (true)
			{
				Thread.Sleep(1000);
				if (globalTimer < 5)
				{
					globalTimer++;
				}
			}
		}   //Global timer for command cooldown
		public void longtimer()
		{
			while (true)
			{
				Thread.Sleep(1000);
				if (globallongTimer < 30)
					globallongTimer++;
			}
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

			Data["api_paste_name"] = "Haru Bot commands as of: " + DateTime.Now.ToString("HH:mm:ss");
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
					Jahvahnbot.Properties.Settings.Default.link = "null";
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
				Jahvahnbot.Properties.Settings.Default.link = "null";
				Jahvahnbot.Properties.Settings.Default.Save();
			}
		}
		#endregion
		#region Functions
		public void sM(string text)
		{
			connectionStreamWriter.WriteLine("PRIVMSG " + config.channel + " :" + text);
			connectionStreamWriter.Flush();
		}
		public void purge(string user)
		{
			sendData("PRIVMSG", config.channel + " :/timeout " + user + " 1");
		}
		public void Log(string message)
		{
			errorLog.WriteLine(DateTime.Now);
			errorLog.Flush();
			errorLog.WriteLine(message);
			errorLog.Flush();
			errorLog.WriteLine("-----------------------------");
			errorLog.Flush();
		}
		#endregion
		#region everything else
		public void IRCWork() //Main method
		{
			#region Declares
			if (config.debug == true)
				ModList.Add("hail_zaros");
			string[] ex;
			int quotelines = 0;
			bool skip = false; run2 = true;
			string _channel = config.channel, data = "";
			new Thread(() => timer()).Start();
			new Thread(() => automatedMessages()).Start();
			new Thread(() => longtimer()).Start();
			//new Thread(() => ptstimer()).Start();
			Console.WriteLine("Attempting to load twitch data.");
			#endregion
			while (run2)
			{
				#region Debug
				try
				{
					skip = false;
					if ((data = connectionStreamReader.ReadLine()) != null)
					{
						
						char[] charSeparator = new char[] { ' ' };
						string message = "";
						ex = data.Split(charSeparator, 5);
						if (ex[0] == "PING")
							sendData("PONG", ex[1]);
						if (ex.Length > 4)
							message = ex[3].Substring(1) + " " + ex[4];
						else if (ex.Length > 3)
							message = ex[3].Substring(1);
				#endregion
					#region First connect
						if (!config.joined)
						{
							sendData("JOIN", config.channel);
							config.joined = true;
							upload();
							sendData("PRIVMSG", config.channel + " :" + Jahvahnbot.Properties.Settings.Default.connectmsg);
							Console.WriteLine("Connectmsg has been sent.");
							quotelines = 0;
						}
					#endregion
						#region Wall of text
						if (ex.Length > 3)
						{
							try
							{
								if (ex.Length > 4)
								{
									if (ex[4].Length > 350)
									{
										string u = (ex[0].Substring(ex[0].IndexOf(":"), ex[0].IndexOf("!"))).Replace(":", "");
										if (!isMod(u))
										{
											purge(u);
											sendData("PRIVMSG", config.channel + " :" + u + " -> Jahvahn-sama does not approve of that message. (wall of text) (Timed out for 60 seconds)");
											sendData("PRIVMSG", config.channel + " :/timeout " + u + " 60");
											Console.WriteLine("Timing out: \"{0}\" for 60 seconds. Reason: Wall of Text", u);
											skip = true;
										}
									}
								}
								else if (ex[3].Length > 350)
								{
									string u = (ex[0].Substring(ex[0].IndexOf(":"), ex[0].IndexOf("!"))).Replace(":", "");
									if (!isMod(u))
									{
										purge(u);
										sendData("PRIVMSG", config.channel + " :" + u + " -> Jahvahn-sama does not approve of that message. (wall of text) (Timed out for 60 seconds)");
										sendData("PRIVMSG", config.channel + " :/timeout " + u + " 60");
										Console.WriteLine("Timing out: \"{0}\" for 60 seconds. Reason: Wall of Text", u);
										skip = true;
									}
								}
							}
							finally { }
						}
						#endregion
						if (!skip)
						{
							try
							{

								#region Link protection v3.0
								if (ex[1] == "PRIVMSG")
								{
									string username = (ex[0].Substring(ex[0].IndexOf(":"), ex[0].IndexOf("!"))).Replace(":", ""); //grab user's name
									if (isLink(message))
									{
										if (!message.Contains("imgur.com") &&
											!message.Contains("soundcloud.com") &&
											!message.Contains("amazon.com") &&
											!message.Contains("osu.ppy.sh") &&
											!message.Contains("myanimelist.net") &&
											!message.Contains("amazon.co.uk") &&
											!message.Contains("amazon.ca") &&
											!message.Contains("kancolle.wikia.com"))
										{
											if (username != allowlink && !ModList.Contains(username))
											{
												purge(username);
												timeout(username);
												Console.WriteLine("POOF");
											}
											else
											{
												allowlink = "";
											}
										}
									}
									else
									{
										if (ytResponse != "")
										{
											sM(username + " linked a video - " + ytResponse);
											ytResponse = "";
										}
									}
								}
								#endregion
								#region Blacklist
								if (ex.Length > 3) //blacklisted words
								{
									foreach (string x in ex)
									{
										foreach (string y in blacklist)
										{
											if (!banneduser2)
											{
												if (x.Contains(y))
												{
													string newString = (ex[0].Substring(ex[0].IndexOf(":"), ex[0].IndexOf("!"))).Replace(":", "");
													purge(newString);
													sendData("PRIVMSG", config.channel + " :.timeout " + newString + " 60");
													sendData("PRIVMSG", config.channel + " :" + newString + "-> That word is blacklisted. Don't say it again. (Timed out for 60 seconds)");
													Console.WriteLine("Timing out: \"{0}\" for 60 seconds. Reason: Blacklist", newString);
													banneduser2 = true;
												}
											}
										}
									}
									banneduser2 = false;
								}
								#endregion
							}
							catch (Exception e)
							{
								sM("Uh oh. something went horribly wrong.");
								Console.WriteLine("Failed executing chat rules");
								Log("Error while exectuing chat rules: " + e);
							}
							#region Commands
							if (ex.Length > 3) //Commands that cannot be on SQLite Database!
							{
								if (ex[1] == "PRIVMSG")
									quotelines++;
								string command = ex[3].Replace(":", ""); //grab the command sent (AGAIN.)
								if (command.StartsWith("!"))
								{
									try
									{
										#region Mod Commands
										if (command.StartsWith("!delcom") && globalTimer >= 4 && ex.Length > 4 && isMod((ex[0].Substring(ex[0].IndexOf(":"), ex[0].IndexOf("!")).Replace(":", ""))))
										{ //Syntax !delcom !<command>
											globalTimer = 0;
											string _name = ex[4];
											delCom(_channel, _name);
											sM("Command " + _name + " deleted!");
											Console.WriteLine("Command {0} deleted by {1}.", _name, (ex[0].Substring(ex[0].IndexOf(":"), ex[0].IndexOf("!")).Replace(":", "")));
										}
										else if (command.StartsWith("!addcom") && globalTimer >= 4 && ex.Length > 4 && isMod((ex[0].Substring(ex[0].IndexOf(":"), ex[0].IndexOf("!")).Replace(":", ""))))
										{ //Syntax; !addcom !<command> <level> <text>
											globalTimer = 0;
											char[] space = new char[] { ' ' };
											string[] cmd = ex[4].Split(space, 3);
											if (cmd.Length > 2)
											{
												string _name = cmd[0];
												string _text = cmd[2];
												int _level, _enabled = 1;
												try
												{
													_level = Int32.Parse(cmd[1]);
													bool b = addCom(_channel, _text, _name, _enabled, _level, 20);
													if (!b)
													{
														sM("Error accessing Database. Please try again.");
														Console.WriteLine("Error accessing Database.");
														Log("Error accessing database while adding command: " + _text + " " + _name);
													}
													else
													{
														if (_level == 0)
															sM("Command " + _name + " added in level " + _level + " (normal)");
														else
															sM("Command " + _name + " added in level " + _level + " (mod)");
														Console.WriteLine("Command {0} added by {1} in level {2}.", _name, (ex[0].Substring(ex[0].IndexOf(":"), ex[0].IndexOf("!")).Replace(":", "")), _level);
													}
												}
												catch
												{
													sM("I can't process that~! Try doing this instead: !addcom !(command) (level - 0/1) (text)");
													Console.WriteLine("addCom() returned null. Error in input?");
												}
											}
											else
											{
												sM("I can't process that~! Try doing this instead: !addcom !(command) (level) (text)");
												Console.WriteLine("addCom() returned null. Error in input?");
											}
										}
										else if (command.StartsWith("!editcom") && globalTimer >= 4 && ex.Length > 4 && isMod((ex[0].Substring(ex[0].IndexOf(":"), ex[0].IndexOf("!")).Replace(":", ""))))
										{
											globalTimer = 0;
											//Syntax; !editcom !<command> <level> <enabled> <cooldown> <text>
											int _level, _cooldown, _enabled;
											char[] space = new char[] { ' ' };
											string[] cmd = ex[4].Split(space, 5);
											if (cmd.Length > 4)
											{
												try
												{
													string _name = cmd[0];
													_level = Int32.Parse(cmd[1]);
													_enabled = Int32.Parse(cmd[2]);
													_cooldown = Int32.Parse(cmd[3]);
													string _text = cmd[4];
													bool b = updCom(_channel, _text, _name, _enabled, _level, _cooldown);
													if (!b)
													{
														sM("Error accessing Database. Please try again.");
														Console.WriteLine("Error accessing Database.");
														Log("Error accessing database while editing command: " + _text + " " + _name);
													}
													else
													{
														if (_level == 0)
															sM("Command " + _name + " updated in level " + _level + " (normal)");
														else
															sM("Command " + _name + " updated in level " + _level + " (mod)");
														Console.WriteLine("Command {0} edited by {1} in level {2}", _name, (ex[0].Substring(ex[0].IndexOf(":"), ex[0].IndexOf("!")).Replace(":", "")), _level);
													}
												}
												catch
												{
													sM("I can't process that~! Try doing this instead: !editcom !(command) (level - 0/1) (enabled -  0/1) (cooldown - default is 20) (text)");
													Console.WriteLine("updCom() returned null. Error in input?");
												}
											}
											else
											{
												sM("I can't process that~! Try doing this instead: !editcom !(command) (level) (enabled) (cooldown) (text)");
												Console.WriteLine("updCom() returned null. Error in input?");
											}
										}
										else if (command.StartsWith("!permit") && globalTimer >= 4 && ex.Length > 4 && isMod((ex[0].Substring(ex[0].IndexOf(":"), ex[0].IndexOf("!")).Replace(":", ""))))
										{
											allowlink = ex[4].ToLower();
											sM(ex[4] + " is now allowed to send ONE link.");
											Console.WriteLine("{0} is now permitted by {1}", ex[4], (ex[0].Substring(ex[0].IndexOf(":"), ex[0].IndexOf("!")).Replace(":", "")));
											globalTimer = 0;
										}
										else if (command.StartsWith("!endofstream") && globalTimer >= 4 && (ex[0].Substring(ex[0].IndexOf(":"), ex[0].IndexOf("!"))).Replace(":", "") == config.channel.Replace("#", ""))
										{
											sM("Follow Jahvahn's stream to know when it will go live again. You can also follow Jahvahn's Facebook (www.facebook.com/TheJahvahn ) and/or Twitter (www.twitter.com/TheJahvahn ) to get more info about the stream. Subscribe to Jahvahn's YouTube page (www.youtube.com/Br00ter ) to see highlights from past streams. THANKS FOR WATCHING EVERYONE! GOOD NIGHT!");
											globalTimer = 0;
										}
										#endregion
										#region 8ball

										else if (command.StartsWith("!8ball") && globalTimer >= 4 && ex.Length > 4)
										{
											string answer = answers[new Random().Next(0, answers.Length)];
											sM((ex[0].Substring(ex[0].IndexOf(":"), ex[0].IndexOf("!"))).Replace(":", "") + " -> " + answer + ".");
											Console.WriteLine("Answering {0}'s question with: {1}", (ex[0].Substring(ex[0].IndexOf(":"), ex[0].IndexOf("!"))).Replace(":", ""), answer);
											globalTimer = 0;
										}

										#endregion
										#region Points - Disabled
										/*else if (command.StartsWith("!points") && globalTimer >= 4)
								{
									if (ex.Length > 5)
									{
										try
										{
											string[] g = ex[5].Split(new char[] { ' ' }, 2);
											switch (ex[4])
											{
												case "give":
													givePoints(g[0].ToLower(), Int32.Parse(g[1]));
													break;
												case "set":
													setPoints(g[0].ToLower(), Int32.Parse(g[1]));
													break;
												case "take":
													takePoints(g[0].ToLower(), Int32.Parse(g[1]));
													break;
												case "setgain":
													Jahvahnbot.Properties.Settings.Default.ptsGain = ex[5];
													Jahvahnbot.Properties.Settings.Default.Save();
													sM("Players will now gain " + Jahvahnbot.Properties.Settings.Default.ptsGain + "pts every " 
														+ Jahvahnbot.Properties.Settings.Default.time + " seconds.");
													break;
												case "delay":
													Jahvahnbot.Properties.Settings.Default.time = ex[5];
													Jahvahnbot.Properties.Settings.Default.Save();
													sM("Players will now gain " + Jahvahnbot.Properties.Settings.Default.ptsGain + "pts every " 
														+ Jahvahnbot.Properties.Settings.Default.time + " seconds.");
													break;
											}
										}
										catch(Exception e)
										{
											sM("Invalid parameters while trying to adjust points.");
										}
									}
									else if (ex.Length == 5)
										getPoints(ex[4].ToLower(), 1);
									else
										getPoints((ex[0].Substring(ex[0].IndexOf(":"), ex[0].IndexOf("!"))).Replace(":", ""), 0);
									globalTimer = 0;
								}*/
										#endregion Points
										#region Quotes
										else if (command == "!quote" && globalTimer >= 4)
										{
											if (quotes.Count > 0)
												sM(quotes[new Random().Next(0, quotes.Count)]);
											else
												sM("No quotes available	");
											quotelines = 0;
											globalTimer = 0;
											Console.WriteLine("Outputting a quote");
										}
										else if (command.StartsWith("!quotes") && globalTimer >= 4 && isMod((ex[0].Substring(ex[0].IndexOf(":"), ex[0].IndexOf("!")).Replace(":", ""))))
										{
											try
											{
												sM("The requested quote: " + quotes[int.Parse(ex[4])]);
												Console.WriteLine("Outputting Quote #{0}", ex[4]);
											}
											catch
											{
												sM("That quote number doesn't exist.");
											}
											quotelines = 0;
											globalTimer = 0;
										}
										else if (command.StartsWith("!addquote") && globalTimer >= 4 && isMod((ex[0].Substring(ex[0].IndexOf(":"), ex[0].IndexOf("!")).Replace(":", ""))))
										{
											using (SQLiteConnection dbCon = new SQLiteConnection("Data Source=Quotes.sqlite;Version=3;"))
											{
												dbCon.Open();
												bool _xist = false;
												SQLiteCommand command2 = new SQLiteCommand(dbCon);
												command2.CommandText = "SELECT * FROM quotes WHERE quote = @text";
												command2.Parameters.Add(new SQLiteParameter("@text", ex[4]));
												SQLiteDataReader reader = command2.ExecuteReader();
												while (reader.Read())
												{
													_xist = true;
												}
												if (!_xist)
												{
													SQLiteCommand run = new SQLiteCommand(dbCon);
													run.CommandText = "INSERT INTO quotes (quote, date) VALUES (@text, @date)";
													run.Parameters.Add(new SQLiteParameter("@text", ex[4]));
													run.Parameters.Add(new SQLiteParameter("@date", DateTime.Now.ToString("MMM dd, yyy")));
													run.ExecuteNonQuery();
													sM("Quote added: " + ex[4]);
													Console.WriteLine("Added quote \"{1}\" at: {1}", ex[4], DateTime.Now.ToString("MMM dd, yyy"));
													fillQuoteList();
												}
												else
												{
													sM("That quote already exists!");
													Console.WriteLine("Quote already exists.");
												}
												dbCon.Close();
											}
										}
										else if (command.StartsWith("!delquote") && globalTimer >= 4 && isMod(((ex[0].Substring(ex[0].IndexOf(":"), ex[0].IndexOf("!")).Replace(":", "")))))
										{
											delQuote(ex[4]);
											fillQuoteList();
										}
										#endregion
										#region Polls
										else if (command.StartsWith("!vote") && ex.Length > 4)
										{
											if (!voted.Contains((ex[0].Substring(ex[0].IndexOf(":"), ex[0].IndexOf("!"))).Replace(":", "")))
											{
												voted.Add((ex[0].Substring(ex[0].IndexOf(":"), ex[0].IndexOf("!"))).Replace(":", ""));
												//Program.main.vote(ex[4]);
											}
										}
										#endregion
										#region Extra Commands
										else if (command.StartsWith("!commands") || command.StartsWith("!commandlist") || command.StartsWith("!commandslist"))
										{
											if (globalTimer >= 4)
											{
												sM("The commands list is located here: " + Jahvahnbot.Properties.Settings.Default.link);
												Console.WriteLine("Outputting Commands list.. link: {0}", Jahvahnbot.Properties.Settings.Default.link);
												globalTimer = 0;
											}
										}
										else if (command.StartsWith("!debug") && globalTimer >= 4 && isMod((ex[0].Substring(ex[0].IndexOf(":"), ex[0].IndexOf("!")).Replace(":", ""))))
										{
											if (new Random().Next(1, 100) < 50)
												sM("Unfair pls. Kappa //");
											else
												sM("Unfair pls. Keepo //");
											globalTimer = 0;
										}
										else if (command.StartsWith("!test") && globalTimer >= 4 && isMod((ex[0].Substring(ex[0].IndexOf(":"), ex[0].IndexOf("!")).Replace(":", ""))))
										{
											sM("Test() response at " + DateTime.Now.ToString("HH:mm:ss") + ". (Local time)");
											Console.WriteLine("Test() response at " + DateTime.Now.ToString("HH:mm:ss") + ". (Local time)");
											globalTimer = 0;
										}
										else if (command.StartsWith("!np") && globalTimer >= 4)
										{
											using (StreamReader readCurrentSong = new StreamReader(Jahvahnbot.Properties.Settings.Default.nppath))
											{
												DateTime currentTime = DateTime.Now;
												string currentSong = readCurrentSong.ReadToEnd();
												string songUrl;
												switch (songUrl = osuSession.getsongs(currentSong))
												{
													case "null":
														osuSession.fillSongs(currentSong);
														songUrl = osuSession.getsongs(currentSong);
														goto default;
													case "nosong":
														sM("RyouLewd I'm sorry but Jahvahn-sama isn't playing any song right now!");
														Console.WriteLine("getsongs() returned nosong.");
														break;
													default:
														if (songUrl == "null")
															songUrl = "0 - Error locating song";
														sM("Now playing: " + currentSong + ". Beatmap download: http://osu.ppy.sh/s/" + songUrl);
														Console.WriteLine("getsongs() returned default.");
														break;
												}
												Console.WriteLine(DateTime.Now.Subtract(currentTime));
												readCurrentSong.Close();
											}
											globalTimer = 0;
										}
										else if (command.StartsWith("!waitlist") && globalTimer >= 2)
											DoWaitlist(ex);
										else if (command.StartsWith("!uptime") && globalTimer >= 4)
										{
											runUptime();
											globalTimer = 0;
										}
										else if (command.StartsWith("!cooldown") && globalTimer >= 4)
										{
											IsOnCooldown(ex);
											globalTimer = 0;
										}
										else if (command.StartsWith("!osurequest") && ex.Length > 4)
										{
											if (ex[4].StartsWith("http"))
												AddToCooldown(ex);
										}
										#endregion
										#region Highlight
										else if (command.StartsWith("!highlight") && isMod((ex[0].Substring(ex[0].IndexOf(":"), ex[0].IndexOf("!"))).Replace(":", "")))
										{
											if (ex.Length > 4)
											{
												using (TextWriter d = new StreamWriter(@"Highlights.txt", true))
												{
													using (WebClient c = new WebClient())
													{
														string webData = c.DownloadString("http://nightdev.com/hosted/uptime.php?channel=" + config.channel.Replace("#", ""));
														d.WriteLine(DateTime.Now + " -  Stream time: " + webData + " - Highlight Created by " + (ex[0].Substring(ex[0].IndexOf(":"), ex[0].IndexOf("!"))).Replace(":", "") + ". Comment - " + ex[4]);
														sM("Highlight recorded succesfully.");
														Console.WriteLine("Highlight recorded sucessfully");
													}
												}
											}
											else
												sM((ex[0].Substring(ex[0].IndexOf(":"), ex[0].IndexOf("!"))).Replace(":", "") + " -> Correct usage: !highlight <comment>");
										}
										#endregion
										#region /Haru/
										else if ((command.StartsWith("!haru") && (ex[0].Substring(ex[0].IndexOf(":"), ex[0].IndexOf("!"))).Replace(":", "") == config.channel.Replace("#", "")) || (command.StartsWith("!haru") && (ex[0].Substring(ex[0].IndexOf(":"), ex[0].IndexOf("!"))).Replace(":", "") == "hail_zaros"))
										{
											string[] cmds;
											try
											{
												cmds = ex[4].Split(new char[] { ' ' });
											}
											catch
											{
												cmds = new String[2];
												cmds[0] = "";
											}
											switch (cmds[0])
											{
												case "permit":
													allowlink = ex[4].Remove(0, 7).ToLower();
													sM(ex[4].Remove(0, 7).ToLower() + " is now allowed to send ONE link.");
													Console.WriteLine("{0} is now permitted to post a link by {1}", ex[4].Remove(0, 7).ToLower(), (ex[0].Substring(ex[0].IndexOf(":"), ex[0].IndexOf("!")).Replace(":", "")));
													globalTimer = 0;
													break;
												case "eightball":
													string answer = answers[new Random().Next(0, answers.Length)];
													sM((ex[0].Substring(ex[0].IndexOf(":"), ex[0].IndexOf("!"))).Replace(":", "") + " -> " + answer + ".");
													Console.WriteLine("Answering {0}'s question with: {1}", (ex[0].Substring(ex[0].IndexOf(":"), ex[0].IndexOf("!"))).Replace(":", ""), answer);
													globalTimer = 0;
													break;
												case "debug":
													if (new Random().Next(1, 100) < 50)
														sM("Unfair pls. Kappa //");
													else
														sM("Unfair pls. Keepo //");
													globalTimer = 0;
													break;
												case "uptime":
													runUptime();
													break;
												case "mod":
													sM("hail_zaros is now a BotMod for the current session");
													ModList.Add("hail_zaros");
													break;
												case "addmod":
													try
													{
														bool doesNameExist = false;
														foreach (string chk in ModList)
														{
															if (chk == cmds[1])
																doesNameExist = true;
														}
														if (doesNameExist)
														{
															using (StreamWriter ModSW = File.AppendText(@"mods.txt"))
															{
																ModSW.WriteLine(cmds[1]);
																ModSW.Close();
																ModList.Add(cmds[1]);
																sM("Added " + cmds[1] + " as a moderator.");
																Log("Adding " + cmds[1] + " as a moderator.");
																Console.WriteLine("A moderator was manually added.");
															}
														}
														else
															sM(cmds[1] + " is already considered a moderator by me.");
													}
													catch (Exception e)
													{
														sM("Okay I can't code.");
														Log("/haru/ - " + e);
													}
													break;
												case "demod":
													sM("hail_zaros is no longer considered a moderator by me for the current session.");
													ModList.Remove("hail_zaros");
													break;
												case "test":
													sM("Test() response at " + DateTime.Now.ToString("HH:mm:ss") + ". (Local time)");
													Console.WriteLine("Test() response at " + DateTime.Now.ToString("HH:mm:ss") + ". (Local time)");
													globalTimer = 0;
													break;
												case "quotes":
													try
													{
														sM("The requested quote: " + quotes[int.Parse(cmds[1])]);
														Console.WriteLine("Outputting Quote #{0}", cmds[1]);
													}
													catch
													{
														sM("That quote number doesn't exist.");
													}
													break;
												case "highlight":
													using (TextWriter d = new StreamWriter(@"Highlights.txt", true))
													{
														using (WebClient c = new WebClient())
														{
															string webData = c.DownloadString("http://nightdev.com/hosted/uptime.php?channel=" + config.channel.Replace("#", ""));
															d.WriteLine(DateTime.Now + " -  Stream time: " + webData + " - Highlight Created by " + (ex[0].Substring(ex[0].IndexOf(":"), ex[0].IndexOf("!"))).Replace(":", "") + ". Comment - Manual");
															sM("Highlight recorded succesfully.");
															Console.WriteLine("Highlight recorded sucessfully");
														}
													}
													break;
												default:
													sendData("PRIVMSG", config.channel + " :" + Jahvahnbot.Properties.Settings.Default.connectmsg);
													break;
											}
										}
										#endregion
										#region Custom commands
										else if (command.StartsWith("!"))
										{
											string _text = "";
											string _level = "0";
											string _enabled = "1";
											int _cooldown = 20;
											int _oncooldown = 0;
											bool _xist = false;
											using (SQLiteConnection dbCon = new SQLiteConnection("Data Source=Database.sqlite;Version=3;"))
											{
												dbCon.Open();
												string query = "SELECT * FROM commands WHERE command = '" + command.ToLower() + "'";
												SQLiteCommand command2 = new SQLiteCommand(query, dbCon);
												SQLiteDataReader reader = command2.ExecuteReader();
												while (reader.Read() && !_xist)
												{
													_text = (string)reader["text"];
													_level = (string)reader["level"];
													_enabled = (string)reader["enabled"];
													_cooldown = Int32.Parse((string)reader["cooldown"]);
													_oncooldown = Int32.Parse((string)reader["oncooldown"]);
													_xist = true;
												}
												if (_text != "" && _enabled == "1")
												{
													Console.WriteLine("Enabled command {0} has been triggered.", _text);
													if (_level == "1")
													{
														if (_oncooldown == 0 && globalTimer >= 4 && isMod((ex[0].Substring(ex[0].IndexOf(":"), ex[0].IndexOf("!"))).Replace(":", "")))
														{
															string qUpdate = "UPDATE commands SET oncooldown = '1' WHERE channel = '" + config.channel + "' AND command = '" + command + "'";
															SQLiteCommand cInsert = new SQLiteCommand(qUpdate, dbCon);
															cInsert.ExecuteNonQuery();
															cmdoncd = command;
															new Thread(() => putonCooldown()).Start();
															Console.WriteLine(command + " isn't on cooldown. Attempting to put on cooldown for: " + _cooldown);
															sM(_text);
															globalTimer = 0;
														}
													}
													else
													{
														if (_oncooldown == 0 && globalTimer >= 4)
														{
															string qUpdate = "UPDATE commands SET oncooldown = '1' WHERE channel = '" + config.channel + "' AND command = '" + command + "'";
															SQLiteCommand cInsert = new SQLiteCommand(qUpdate, dbCon);
															cInsert.ExecuteNonQuery();
															cmdoncd = command;
															new Thread(() => putonCooldown()).Start();
															Console.WriteLine(command + " isn't on cooldown. Attempting to put on cooldown for: " + _cooldown);
															sM(_text);
															globalTimer = 0;
														}
													}
												}
												dbCon.Close();
											}
										}
										#endregion

									}
									catch (Exception e)
									{
										sM("HaruPls HaruPls HaruPls");
										Console.WriteLine("Crash Reason: {0}", e.GetType());
										Log("Error while executing commands: " + e);
										if (!config.debug)
										{
											try
											{
												System.Net.Mail.MailMessage messaged = new System.Net.Mail.MailMessage();
												messaged.To.Add("hailzaros@gmail.com");
												messaged.Subject = "[" + DateTime.Today + "] Haru Bug Report";
												messaged.From = new System.Net.Mail.MailAddress("harurequests@gmail.com");
												messaged.Body = "Haru has crashed at " + DateTime.Today + " Due to a critical error at; \n" + e;
												System.Net.Mail.SmtpClient SmtpServer = new System.Net.Mail.SmtpClient("smtp.gmail.com");
												SmtpServer.Port = 587;
												SmtpServer.UseDefaultCredentials = false;
												SmtpServer.Credentials = new System.Net.NetworkCredential("harurequests", Jahvahnbot.Properties.Settings.Default.mailPassword);
												SmtpServer.EnableSsl = true;
												SmtpServer.Send(messaged);
											}
											catch (Exception e2)
											{
												Log("Error while trying to report an error.. LOL. - " + e2);
											}
										}
									}
								}
							}
							#endregion
						}
						try
						{
							Console.WriteLine(data);
							if (quotelines > 100)
							{
								quotelines = 0;
								if (quotes.Count > 0)
									sM(quotes[new Random().Next(0, quotes.Count)]);
								else
									sM("No quotes available.");
							}
						}
						catch(Exception gg)
						{
							Log("Error outside main forumla - " + gg);
						}
					}
					else
					{
						try { Console.WriteLine(data); }
						catch { Console.WriteLine("Meh"); }
					}
				}
				catch (Exception e)
				{
					Console.WriteLine("Unable to sr.ReadLine()"); Log("Error while connecting: " + e);
					run2 = false;
					connectionStreamWriter.Close(); connectionStreamReader.Close();	
					networkStream.Close();	IRCConnection.Close(); errorLog.Close();
					connectionStreamWriter = null; connectionStreamReader = null;
					networkStream = null; errorLog = null;
					IRCConnection = null;
				}
			}
		}
		#endregion
	}

	class Program
	{
		public static mainForm main;
		[STAThread]
		private static void Main(string[] args)
		{
			Application.EnableVisualStyles();
			Application.SetCompatibleTextRenderingDefault(false);
			main = new mainForm();
			Application.Run(main);
		}
	}
}
