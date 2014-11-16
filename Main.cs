using System;
using System.Text;

namespace NetworkTest
{
	static class NetworkTest
	{
		public static void Main (string[] args)
		{
			if (args.Length > 0) {
				if (!string.IsNullOrEmpty (args [0])) {
					switch (args [0]) {
					case "server":
						new Server ();
						break;
					default:
						new Client ();
						break;
					}
				}
			} else {
				new Client ();
			}
		}
	}
}

