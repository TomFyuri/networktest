using System;
using System.Text;
using System.IO;
//using System.Text.Encoding;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Diagnostics;
using System.Collections.Generic;

namespace NetworkTest
{
	public class Client
	{
		private static Thread threadConnectRec;
		private static Thread threadConnectSend;
		private static TcpClient client = new TcpClient();
		private static List<string> msg_to_send = new List<string>();
		private static Object thisLock = new Object();
		private static int SendMessage(string msg)
		{
			if (msg.Length <= 1024) {
				msg_to_send.Add (msg);
				return 0;
			} else
				return 1;
		}
		public static void SendMessages(NetworkStream stream)
		{
			lock (thisLock) {
				int z = 0;
				for (int i = 0; i < msg_to_send.Count; i++) {
					z = i;
					try {
						ASCIIEncoding asen = new ASCIIEncoding ();
						byte[] ba = asen.GetBytes (msg_to_send [i]);
						stream.Write (ba, 0, ba.Length);
					} catch {
					}
				}
				for (int i = 0; i <= z; i++) {
					msg_to_send.RemoveAt (0);
				}
			}
		}
		public Client ()
		{
			try {
				Console.WriteLine("What's the IP (enter nothing for localhost): ");
				string IP = Console.ReadLine();
				if (IP.Length < 1) IP = "127.0.0.1";

				Console.WriteLine("What's your name: ");
				string myname = Console.ReadLine();

				Console.WriteLine("Connecting.....");

				while (!client.Connected)
				{
					try{
						client.Connect(IP,8000); 
					}
					catch {}
				}

				if (myname.Length > 0)
				{
					SendMessage("/nickname "+myname);
					//SendMessage("/users");
				}
				Console.WriteLine("Connected...");

				threadConnectRec = new Thread(ConnectedRec);
				threadConnectRec.Start();
				threadConnectSend = new Thread(ConnectedSend);
				threadConnectSend.Start();

				while(true)
				{
					String str = Console.ReadLine();
					if (str == "/quit")
					{
						break;
					}
					SendMessage(str);
				}
				try {
					threadConnectRec.Abort();
				}
				catch (ThreadAbortException tae) {
					Console.WriteLine(tae.ToString() );
				}
				try {
					threadConnectSend.Abort();
				}
				catch (ThreadAbortException tae) {
					Console.WriteLine(tae.ToString() );
				}
				Console.WriteLine("Disconnecting. . .");
				client.Close();
			}
			catch(Exception e) {
				Console.WriteLine ("Error..... " + e.StackTrace);
			}
		}
		private static void ConnectedSend() {
			Stopwatch stopWatch = new Stopwatch ();
			stopWatch.Start (); 
			NetworkStream tcpStream = client.GetStream();
			while (client.Connected) {
				try {
					if (msg_to_send.Count > 0)
					{
						SendMessages(tcpStream);
					}
					if (stopWatch.ElapsedMilliseconds >= 60000)
					{
						if (client.Connected) { // try to reply, if can't possibly lost connection
							try {
								Byte[] sendBytes = Encoding.UTF8.GetBytes ("/ping");
								tcpStream.Write (sendBytes, 0, sendBytes.Length);
							} catch {
							}
						}
						stopWatch.Reset();
					}
				}
				catch {
				}
				Thread.Sleep (100);
			}
		}
		private static void ConnectedRec() {
			NetworkStream tcpStream = client.GetStream();
			while (client.Connected) {
				try {
					if (tcpStream.CanRead) {
						byte[] bytes = new byte[client.ReceiveBufferSize];

						int bytesRead = tcpStream.Read (bytes, 0, (int)client.ReceiveBufferSize);

						if (bytesRead > 0) {
							string returndata = Encoding.UTF8.GetString (bytes);
							returndata = returndata.Substring(0,bytesRead);
							if (returndata != "/ping")
							{
								Console.Write(returndata);
							}
						}
					}
				}
				catch {
				}
				Thread.Sleep (100);
			}
		}
	}
}

