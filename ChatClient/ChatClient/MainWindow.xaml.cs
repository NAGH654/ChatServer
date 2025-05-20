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
            SetChatControlsEnabled(false);
        }

        private void SetChatControlsEnabled(bool enabled)
        {
            MessageInput.IsEnabled = enabled;
            foreach (var child in ((StackPanel)MessageInput.Parent).Children)
            {
                if (child is Button btn)
                    btn.IsEnabled = enabled;
            }
        }

        private void ConnectButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string ip = IpBox.Text.Trim();
                if (!int.TryParse(PortBox.Text.Trim(), out int port))
                {
                    MessageBox.Show("Port phải là số nguyên.");
                    return;
                }
                ConnectToServer(ip, port);
                new Thread(ReceiveMessages) { IsBackground = true }.Start();
                ConnectButton.IsEnabled = false;
                IpBox.IsEnabled = false;
                PortBox.IsEnabled = false;
                SetChatControlsEnabled(true);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Kết nối thất bại: " + ex.Message);
            }
        }

        void ConnectToServer(string ip, int port)
        {
            client = new TcpClient(ip, port);
            var stream = client.GetStream();
            writer = new StreamWriter(stream) { AutoFlush = true };
            reader = new StreamReader(stream);
        }

        void ReceiveMessages()
        {
            string message;
            try
            {
                while ((message = reader.ReadLine()) != null)
                {
                    // Kiểm tra nếu là file (có 4 phần: sender|[file]filename|base64|timestamp)
                    var parts = message.Split('|');
                    if (parts.Length == 4 && parts[1].StartsWith("[file]"))
                    {
                        string sender = parts[0];
                        string fileName = parts[1].Substring(6);
                        string base64 = parts[2];
                        string time = parts[3];

                        Dispatcher.Invoke(() =>
                        {
                            string displayName = sender == username ? "Me" : sender;
                            Button saveBtn = new Button
                            {
                                Content = $"Lưu {fileName}",
                                Tag = base64,
                                Margin = new Thickness(0, 2, 0, 2)
                            };
                            saveBtn.Click += (s, e) =>
                            {
                                var sfd = new SaveFileDialog { FileName = fileName };
                                if (sfd.ShowDialog() == true)
                                {
                                    File.WriteAllBytes(sfd.FileName, Convert.FromBase64String(base64));
                                    MessageBox.Show("Đã lưu file!");
                                }
                            };
                            //xử lí video
                            if (fileName.EndsWith(".mp4") || fileName.EndsWith(".mp3") || fileName.EndsWith(".wav") ||
                                fileName.EndsWith(".avi") || fileName.EndsWith(".mov") || fileName.EndsWith(".wmv"))
                            {
                                string tempPath = System.IO.Path.GetTempFileName() + System.IO.Path.GetExtension(fileName);
                                File.WriteAllBytes(tempPath, Convert.FromBase64String(base64));
                                var playButton = new Button
                                {
                                    Content = "▶ Xem video",
                                    Tag = tempPath,
                                    Margin = new Thickness(0, 2, 0, 2)
                                };

                                MediaElement media = new MediaElement
                                {
                                    Width = 200,
                                    Height = 120,
                                    LoadedBehavior = MediaState.Manual,
                                    UnloadedBehavior = MediaState.Manual,
                                    Visibility = Visibility.Collapsed
                                };

                                playButton.Click += (s, e) =>
                                {
                                    if (media.Visibility == Visibility.Collapsed)
                                    {
                                        media.Source = new Uri(tempPath);
                                        media.Visibility = Visibility.Visible;
                                        media.Play();
                                        playButton.Content = "⏸ Dừng video";
                                    }
                                    else
                                    {
                                        media.Pause();
                                        media.Visibility = Visibility.Collapsed;
                                        playButton.Content = "▶ Xem video";
                                    }
                                };

                                ChatBox.Items.Add($"{displayName} [{time}]: {fileName}");
                                ChatBox.Items.Add(playButton);
                                ChatBox.Items.Add(media);
                            }
                            else
                            {
                                ChatBox.Items.Add($"{displayName} [{time}]: {fileName}");
                                ChatBox.Items.Add(saveBtn);
                            }
                        });
                    }
                    // Xử lý ảnh như cũ
                    else if (parts.Length == 3)
                    {
                        string sender = parts[0];
                        string content = parts[1];
                        string time = parts[2];

                        Dispatcher.Invoke(() =>
                        {
                            string displayName = sender == username ? "Me" : sender;

                            if (content.StartsWith("[image]"))
                            {
                                try
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
                                catch
                                {
                                    ChatBox.Items.Add($"{displayName} [{time}]: [Lỗi ảnh]");
                                }
                            }
                            else
                            {
                                ChatBox.Items.Add($"{displayName} [{time}]: {content}");
                            }
                        });
                    }
                }
            }
            catch
            {
                        Dispatcher.Invoke(() =>
                {
                    MessageBox.Show("Mất kết nối tới server.");
                    SetChatControlsEnabled(false);
                    ConnectButton.IsEnabled = true;
                    IpBox.IsEnabled = true;
                    PortBox.IsEnabled = true;
                });
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
            if (writer == null) return;
            string text = MessageInput.Text.Trim();
            if (string.IsNullOrEmpty(text)) return;
            string timestamp = DateTime.Now.ToString("HH:mm:ss");
            string fullMessage = $"{username}|{text}|{timestamp}";
            writer.WriteLine(fullMessage);
            MessageInput.Clear();
        }

        private void SendImage_Click(object sender, RoutedEventArgs e)
        {
            if (writer == null) return;
            var dialog = new OpenFileDialog { Filter = "Image Files (*.png;*.jpg)|*.png;*.jpg" };
            if (dialog.ShowDialog() == true)
            {
                try
                {
                    byte[] imgBytes = File.ReadAllBytes(dialog.FileName);
                    string base64Img = Convert.ToBase64String(imgBytes);
                    string timestamp = DateTime.Now.ToString("HH:mm:ss");
                    string fullMessage = $"{username}|[image]{base64Img}|{timestamp}";
                    writer.WriteLine(fullMessage);
                    ChatBox.Items.Add($"Me [{timestamp}]: [Hình ảnh đã gửi]");
                }
                catch
                {
                    MessageBox.Show("Không thể gửi ảnh này.");
                }
            }
        }

        private void SendFile_Click(object sender, RoutedEventArgs e)
        {
            if (writer == null) return;
            var dialog = new OpenFileDialog
            {
                Filter = "All Files|*.*",
                Title = "Chọn file để gửi"
            };
            if (dialog.ShowDialog() == true)
            {
                try
                {
                    byte[] fileBytes = File.ReadAllBytes(dialog.FileName);
                    string base64File = Convert.ToBase64String(fileBytes);
                    string fileName = System.IO.Path.GetFileName(dialog.FileName);
                    string timestamp = DateTime.Now.ToString("HH:mm:ss");
                    string fullMessage = $"{username}|[file]{fileName}|{base64File}|{timestamp}";
                    writer.WriteLine(fullMessage);
                    ChatBox.Items.Add($"Me [{timestamp}]: [Đã gửi file] {fileName}");
                }
                catch
                {
                    MessageBox.Show("Không thể gửi file này.");
                }
            }
        }
    }
}
