using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jahvahnbot
{
	class osu_np
	{
		public List<string> songs = new List<string>();
		public List<string> acts = new List<string>();
		public List<string> ids = new List<string>();
		public void fillSongs(string song)
		{
			int c = 0;
			string path = Jahvahnbot.Properties.Settings.Default.osupath + "\\Songs\\";
			foreach (string s in Directory.GetDirectories(path))
			{
				string[] x = (s.Remove(0, path.Length)).Split(new char[] { ' ' }, 2);
				foreach (string file in Directory.GetFiles(s, "*.osu"))
				{
					string noExt = Path.GetFileNameWithoutExtension(file).Replace(".", "");
					if (x.Length > 1)
					{
						string songName = noExt.Replace(x[1], "");
						if (songName.Contains("["))
							songs.Add(x[0] + " " + noExt.Replace(songName.Remove((songName.LastIndexOf("["))), " "));
						else
							songs.Add(x[0]);
					}
					else
						songs.Add(x[0]);
					string[] extractedName = songs[c].Split(new char[] { ' ' }, 2);
					if (extractedName.Length > 1)
					{
						acts.Add(extractedName[1]);
						ids.Add(extractedName[0]);
					}
					else
					{
						acts.Add(extractedName[0]);
						ids.Add("0");
					}
					c++;
				}
			}
		}
		public string getsongs(string song)
		{
			if (song != "")
			{
				for (int i = 0; i < songs.Count; i++)
				{
					if (song == acts[i])
					{
						return ids[i];
					}
				}
				return "null";
			}
			return "nosong";
		}
	}
}
