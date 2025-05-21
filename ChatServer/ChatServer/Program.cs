using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace ChatServer
{
	class Program
	{
		static List<StreamWriter> clients = new List<StreamWriter>();

		static void Main()
		{
			TcpListener server = new TcpListener(IPAddress.Any, 5000);
			server.Start();

			// Display all local IPv4 addresses
			string hostName = Dns.GetHostName();
			IPAddress[] addresses = Dns.GetHostAddresses(hostName);
			foreach (var addr in addresses)
			{
				if (addr.AddressFamily == AddressFamily.InterNetwork)
				{
					Console.WriteLine($"Server IP Address: {addr}");
				}
			}

			Console.WriteLine("Server started.....");
			while (true)
			{
				TcpClient client = server.AcceptTcpClient();
				Thread thread = new Thread(() => HandleClient(client));
				thread.Start();
			}
		}

		static void HandleClient(TcpClient tcpClient)
		{
			StreamReader reader = new StreamReader(tcpClient.GetStream());
			StreamWriter writer = new StreamWriter(tcpClient.GetStream()) { AutoFlush = true };
			clients.Add(writer);

			try
			{
				string message;
				while ((message = reader.ReadLine()) != null)
				{
					foreach (var client in clients)
					{
						try { client.WriteLine(message); } catch { }
					}
				}
			}
			catch { }

			clients.Remove(writer);
			tcpClient.Close();
		}
	}
}
