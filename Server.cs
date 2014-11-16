using System;
using System.Text;
//using System.Text.Encoding;
using System.Collections;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Diagnostics;

namespace NetworkTest
{
	public class Server
	{
		private static Boolean server_run = true;
		private AutoResetEvent connectionWaitHandle = new AutoResetEvent(false);
		private static ClientManager CM = new ClientManager();
		private static void MessageSender(object parameterObject)//(TcpClient client)
		{
			ArrayList oparams = (ArrayList)parameterObject;
			TcpClient client = (TcpClient)oparams [0];
			ConnectedClient cc = (ConnectedClient)oparams [1];
			Stopwatch stopWatch = new Stopwatch ();
			stopWatch.Start ();

			//... Use your TcpClient here
			string remote_ip = client.Client.RemoteEndPoint.ToString();
			NetworkStream tcpStream = client.GetStream();
			while (client.Connected) {
				if (client.Connected) { // if we have data for that client, lets send it...
					if (cc.HaveMessagesForMe ()) {
						try {
							string message = cc.GetMessages ();
							Console.WriteLine (remote_ip + " -> "+message.TrimEnd('\n')+" "+"[" + message.Length + "]");
							Byte[] sendBytes = Encoding.UTF8.GetBytes (message);
							tcpStream.Write (sendBytes, 0, sendBytes.Length);} catch {
						}
					}
				}
				if (stopWatch.ElapsedMilliseconds >= 60000)
				{
					if (client.Connected) { // try to reply, if can't possibly lost connection
						Console.WriteLine (remote_ip + " -> are you alive?");
						try {
							Byte[] sendBytes = Encoding.UTF8.GetBytes ("/ping");
							tcpStream.Write (sendBytes, 0, sendBytes.Length);
						} catch {
						}
					}
					stopWatch.Reset();
				}
				Thread.Sleep(100);
			}

			cc.Working = false;
		}
		private static void MessageReciever(object parameterObject)//(TcpClient client)
		{
			ArrayList oparams = (ArrayList)parameterObject;
			TcpClient client = (TcpClient)oparams [0];
			ConnectedClient cc = (ConnectedClient)oparams [1];
			Stopwatch stopWatch = new Stopwatch ();
			stopWatch.Start ();

			//... Use your TcpClient here
			string remote_ip = client.Client.RemoteEndPoint.ToString();
			NetworkStream tcpStream = client.GetStream();
			while (client.Connected) {
				try {
					if (tcpStream.CanRead) {
						byte[] bytes = new byte[client.ReceiveBufferSize];
						int bytesRead = tcpStream.Read (bytes, 0, (int)client.ReceiveBufferSize);

						if (bytesRead > 0) {
							string returndata = Encoding.UTF8.GetString (bytes);

							Console.WriteLine (remote_ip + " <- "+cc.Nick+": " + returndata.TrimEnd('\n')+" [" + bytesRead + "]");
							try {
								if (returndata.IndexOf ("/ping") == 0) {
									//Console.WriteLine (remote_ip + " -> are you alive?");
									// do nothing
								}
								else if (returndata.IndexOf ("/users") == 0) {
									CM.SendMessage(null, cc, CM.WhoIsOnline(), false);
									// do nothing
								}
								else if (returndata.IndexOf ("/nickname ") == 0) {
									string s1 = returndata.Substring (10, bytesRead - 10);
									CM.SendMessage (cc, null, "User \""+cc.Nick+"\" changed name to \""+s1+"\".", false);
									cc.Nick = s1;
								} else {
									string msg = returndata.Substring(0,bytesRead);
									CM.SendMessage (cc, null, msg, true);
									}
								} catch {
							}
						} else {
							if (client.Connected) { // try to reply, if can't possibly lost connection
								Byte[] sendBytes = Encoding.UTF8.GetBytes ("0");
								tcpStream.Write (sendBytes, 0, sendBytes.Length);
							}
						}
					}
				} catch {
				}
				Thread.Sleep(100);
			}

			cc.Working = false;
		}
		private void HandleAsyncConnection(IAsyncResult result)
		{
			TcpListener listener = (TcpListener)result.AsyncState;
			TcpClient client = listener.EndAcceptTcpClient(result);
			connectionWaitHandle.Set(); //Inform the main thread this connection is now handled

			string remote_ip = client.Client.RemoteEndPoint.ToString();
			Console.WriteLine("Connection accepted from " + remote_ip+".");
			ConnectedClient cc = new ConnectedClient("guest", remote_ip, 0);
			CM.clients.Add (cc);
			CM.SendMessage (cc, null, "New user (\"guest\") connected.", false);

			Thread threadConnectRec = new Thread(MessageReciever);
			threadConnectRec.Start(new ArrayList(){client,cc});
			Thread threadConnectSend = new Thread(MessageSender);
			threadConnectSend.Start(new ArrayList(){client,cc});

			Thread.Sleep(1000);
			CM.SendMessage (null, cc, "Users Online:\n"+CM.WhoIsOnline(), false);
			while (cc.Working) {
				Thread.Sleep(100);
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
			Console.WriteLine("Connection closed from " + remote_ip+".");
			client.Close();
			CM.SendMessage (cc, null, "User (\""+cc.Nick+"\") disconnected.", false);
			CM.clients.Remove(cc);
		}
		public Server ()
		{
			try {
				//IPAddress ipAdress = IPAddress.Parse("127.0.0.1");
				// Initializes the Listener
				//TcpListener myList = new TcpListener(ipAdress,8000);
				TcpListener myList = new TcpListener(IPAddress.Any,8000);

				// Start Listeneting at the specified port
				myList.Start();

				Console.WriteLine("Server running - Port: 8000");    
				Console.WriteLine("Local end point: " + myList.LocalEndpoint );
				Console.WriteLine("Waiting for connections...");

				while(server_run)
				{
					IAsyncResult result = myList.BeginAcceptTcpClient(HandleAsyncConnection, myList);
					connectionWaitHandle.WaitOne(); // Wait until a client has begun handling an event
					connectionWaitHandle.Reset(); // Reset wait handle or the loop goes as fast as it can (after first request)
				}
				myList.Stop();
			}
			catch(Exception e) {
				Console.WriteLine ("Error..... " + e.StackTrace);
			}
		}
	}
}

