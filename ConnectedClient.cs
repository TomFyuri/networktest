using System;
using System.Collections.Generic;

namespace NetworkTest
{
	public class ClientManager
	{
		public List<ConnectedClient> clients = new List<ConnectedClient>();
		public int SendMessage(ConnectedClient cfrom, ConnectedClient to, string msg, Boolean addnick)
		{
			if (to == null) {
				if ((cfrom.Nick.Length + msg.Length + 4) <= 1024) {
					foreach (ConnectedClient cc in clients) {
						if (cfrom != cc) {
							if (addnick) {
								cc.SendMessage (cfrom.Nick + ": " + msg + "\n");
							} else {
								cc.SendMessage (msg + "\n");
							}
						}
					}
					return 0;
				} else
					return 1;
			} else if (cfrom == null) {
				to.SendMessage (msg);
				return 0;
			}
			else return 1;
		}
		public string WhoIsOnline()
		{
			string s1 = "";
			foreach (ConnectedClient cc in clients)
			{
				s1+="  "+cc.Nick+"\n";
			}
			return s1;
		}
	}
	public class ConnectedClient
	{
		private string nickname = "guest";
		private string remote_point = "unknown";
		private int auth = 0;
		private List<string> msg_to_send = new List<string>();
		private Boolean working = true;
		private Object thisLock = new Object();
		public Boolean Working
		{
			get {
				return working;
			}
			set {
				working = value;
			}
		}
		public ConnectedClient (string nick, string remote_point, int auth)
		{
			nickname = nick;
			this.remote_point = remote_point;
			this.auth = auth;
		}
		public string Nick
		{
			get {
				return nickname;
			}
			set {
				nickname = value;
			}
		}
		public int Auth
		{
			get {
				return auth;
			}
			set {
				auth = value;
			}
		}
		public int SendMessage(string msg)
		{
			if (msg.Length <= 1024) {
				lock (thisLock) {
					msg_to_send.Add (msg);
				}
				return 0;
			} else
				return 1;
		}
		public string GetMessages()
		{
			string s1 = "";
			lock (thisLock) {
				int z = 0;
				for (int i = 0; i < msg_to_send.Count; i++) {
					if ((msg_to_send[i].Length + s1.Length) <= 1024) {
						z = i;
						s1 += msg_to_send[i];
					} else {
						break;
					}
				}
				for (int i = 0; i <= z; i++) {
					msg_to_send.RemoveAt (0);
				}
			}
			return s1;
		}
		public bool HaveMessagesForMe()
		{
			//Console.WriteLine ("wtfb " + msg_to_send.Count);
			return (msg_to_send.Count > 0);
		}
	}
}

