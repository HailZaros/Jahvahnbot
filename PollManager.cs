using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Jahvahnbot
{
	internal struct PConfig
	{
		public Dictionary<string, int> ans;
		public int duration;
	}
	internal class Poll
	{
		private bool PollIsRunning = false;
		private string currentPoll = "";
		private PConfig data;
		public Poll(PConfig config)
		{
			this.data = config;
		}
		public void timer()
		{
			int timepass = 0;
			while (timepass < data.duration)
			{
				Thread.Sleep(1000);
				timepass++;
			}
			endPoll();
		}
		public string startPoll(string cp)
		{
			if (!(data.ans.Count < 2))
			{
				char[] charSeparator = new char[] { ' ' };
				string outg = "";
				foreach (string g in data.ans.Keys)
				{
					outg += g + " / ";
				}
				if (!PollIsRunning)
				{
					outg = outg.Remove(outg.LastIndexOf("/"));
					currentPoll = cp;
					PollIsRunning = true;
					if (data.duration != 0)
						new Thread(() => timer()).Start();
					Console.WriteLine("A new poll has started: \"" + cp + "\" !vote " + outg);
					return "A new poll has started: \"" + cp + "\" !vote " + outg;
				}
			}
			else
			{
				return "Insufficient parameters to initiate a poll.";
			}
			return "Attempting to recover from fatal error while initiating poll..";
		}
		public bool isRunning()
		{
			if (PollIsRunning)
				return true;
			else
				return false;
		}
		public void vote(string vote)
		{
			if (PollIsRunning)
			{
				if (data.ans.Keys.Contains(vote))
				{
					data.ans[vote] += 1;
				}
			}
			else
				Console.WriteLine("There is no poll running at the moment.");
		}
		public string endPoll()
		{
			PollIsRunning = false;
			string results = getPollResults2();
			string output = "The poll \"" + currentPoll + "\" has ended! Poll results:" + results;
			currentPoll = "";
			Console.WriteLine(output);
			return output;
		}
		public string getPollResults()
		{
			int c = -1;
			int totalvotes = 0;
			string results = "";
			foreach (string g in data.ans.Keys)
			{
				totalvotes += data.ans[g];
				Console.WriteLine(g + " " + data.ans[g]);
			}
			var top = data.ans.OrderByDescending(pair => pair.Value).Take(5).ToDictionary(pair => pair.Key, pair => pair.Value);
			foreach (KeyValuePair<string, int> h in top)
			{
				if (h.Value != 0)
					results += "\"" + h.Key + "\" with " + h.Value + " votes (" + (h.Value * 100) / totalvotes + "%) \n";
				else
					results += "\"" + h.Key + "\" with " + h.Value + " votes (0%) \n";
				c++;
			}
			if (results.EndsWith(","))
				results = results.Remove(results.LastIndexOf(",")) + "!";
				return results.Replace("\n", Environment.NewLine);
		}
		public string getPollResults2()
		{
			int c = 0;
			int totalvotes = 0;
			string results = "";
			foreach (string g in data.ans.Keys)
			{
				totalvotes += data.ans[g];
				Console.WriteLine(g + " " + data.ans[g]);
			}
			var top = data.ans.OrderByDescending(pair => pair.Value).Take(5).ToDictionary(pair => pair.Key, pair => pair.Value);
			foreach (KeyValuePair<string, int> h in top)
			{
				if (h.Value != 0)
					results += " \"" + h.Key + "\" with " + h.Value + " votes (" + (h.Value * 100) / totalvotes + "%), ";
				else
					results += " \"" + h.Key + "\" with " + h.Value + " votes (0%), ";
				c++;
			}
			if (results.EndsWith(","))
				results = results.Remove(results.LastIndexOf(",")) + "!";
			return results;
		}
	}
}
