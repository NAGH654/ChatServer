using System;
using System.IO;
using System.Net.Sockets;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using Microsoft.Win32;

namespace ChatClient
{
	public partial class MainWindow : Window
	{
		StreamWriter writer;
		StreamReader reader;
		TcpClient client;
		string username;

		public MainWindow()
		{
			InitializeComponent();
			username = Microsoft.VisualBasic.Interaction.InputBox("Nhập tên của bạn:", "Tên người dùng", "User" + new Random().Next(1000));
			ConnectToServer();
			new Thread(ReceiveMessages).Start();
		}

		void ConnectToServer()
		{
			client = new TcpClient("127.0.0.1", 5000);
			var stream = client.GetStream();
			writer = new StreamWriter(stream) { AutoFlush = true };
			reader = new StreamReader(stream);
		}

		void ReceiveMessages()
		{
			string message;
			while ((message = reader.ReadLine()) != null)
			{
				var parts = message.Split('|');
				if (parts.Length == 3)
				{
					string sender = parts[0];
					string content = parts[1];
					string time = parts[2];

					Dispatcher.Invoke(() =>
					{
						string displayName = sender == username ? "Me" : sender;

						if (content.StartsWith("[image]"))
						{
							byte[] imgBytes = Convert.FromBase64String(content.Substring(7));
							var image = new Image
							{
								Source = LoadImage(imgBytes),
								Width = 100,
								Height = 100
							};
							ChatBox.Items.Add($"{displayName} [{time}]: ");
							ChatBox.Items.Add(image);
						}
						else
						{
							ChatBox.Items.Add($"{displayName} [{time}]: {content}");
						}
					});

				}
			}
		}

		BitmapImage LoadImage(byte[] imageData)
		{
			using (var ms = new MemoryStream(imageData))
			{
				var image = new BitmapImage();
				image.BeginInit();
				image.StreamSource = ms;
				image.CacheOption = BitmapCacheOption.OnLoad;
				image.EndInit();
				image.Freeze();
				return image;
			}
		}

		private void SendButton_Click(object sender, RoutedEventArgs e)
		{
			string timestamp = DateTime.Now.ToString("HH:mm:ss");
			string fullMessage = $"{username}|{MessageBox.Text}|{timestamp}";
			writer.WriteLine(fullMessage);
			MessageBox.Clear();
		}


		private void SendImage_Click(object sender, RoutedEventArgs e)
		{
			var dialog = new OpenFileDialog { Filter = "Image Files (*.png;*.jpg)|*.png;*.jpg" };
			if (dialog.ShowDialog() == true)
			{
				byte[] imgBytes = File.ReadAllBytes(dialog.FileName);
				string base64Img = Convert.ToBase64String(imgBytes);
				string timestamp = DateTime.Now.ToString("HH:mm:ss");
				string fullMessage = $"{username}|[image]{base64Img}|{timestamp}";
				writer.WriteLine(fullMessage);
				ChatBox.Items.Add($"Me [{timestamp}]: [Hình ảnh đã gửi]");
			}
		}
	}
}
