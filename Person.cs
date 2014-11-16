using System;

namespace NetworkTest
{
	public class Person
	{
		string nickname = "guest";
		string remote_point = "unknown";
		public Person (string nick, string remote_point)
		{
			nickname = nick;
			this.remote_point = remote_point;
		}
	}
}

