using MiniMessenger.models;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Forms;
namespace MiniMessenger.forms
{
    public partial class ClientForm : Form
    {
        private Client client;
        private List<MessageClass> mHistory = new List<MessageClass>();
        private bool isConnected = false;

        private Panel connectionPanel;
        private Label serverIPLabel;
        private TextBox serverIPTextBox;
        private Label portLabel;
        private NumericUpDown portTextBox;
        private Label usernameLabel;
        private TextBox usernameTextBox;
        private Button connectButton;
        private Button disconnectButton;

        private Panel messagePanel;
        private TextBox messageTextBox;
        private Button sendButton;
        private Button attachButton;

        private SplitContainer splitContainer;
        private TextBox chatHistory;
        private ListBox userList;
        public ClientForm()
        {
            InitializeComponent();
            client = new Client();
            client.MessageReceived += OnMessageReceived;
            client.UserListUpdated += OnUserListUpdated;
            client.ConnectionStatusChanged += OnConnectionStatusChanged;
            LoadChatHistory();
        }

        private void InitializeComponent()
        {
            this.Text = "Чат";
            this.Size = new Size(1280, 720);
            this.StartPosition = FormStartPosition.CenterParent;

            connectionPanel = new Panel { Dock = DockStyle.Top, Height = 120 };
            serverIPLabel = new Label { Text = "Server IP:", Location = new Point(10, 15), Size = new Size(60, 20) };
            serverIPTextBox = new TextBox { Location = new Point(75, 12), Size = new Size(60, 20) };
            portLabel = new Label { Text = "Port:", Location = new Point(185, 15), Size = new Size(30, 20) };
            portTextBox = new NumericUpDown { Location = new Point(220, 12), Size = new Size(60, 20), Minimum = 49152, Maximum = 65535 };
            usernameLabel = new Label { Text = "Username:", Location = new Point(290, 15), Size = new Size(60, 20) };
            usernameTextBox = new TextBox { Text = "User", Location = new Point(355, 12), Size = new Size(100, 20) };
            connectButton = new Button { Text = "Connect", Location = new Point(465, 10), Size = new Size(80, 25) };
            disconnectButton = new Button { Text = "Disconnect", Location = new Point(555, 10), Size = new Size(80, 25) };

            connectionPanel.Controls.AddRange(new Control[] {
                serverIPLabel, serverIPTextBox, portLabel, portTextBox,
                usernameLabel, usernameTextBox, connectButton, disconnectButton
            });

            messagePanel = new Panel { Dock = DockStyle.Bottom, Height = 80 };
            messageTextBox = new TextBox { Location = new Point(10, 10), Size = new Size(400, 40), Multiline = true };
            sendButton = new Button { Text = "Отправить", Location = new Point(420, 10), Size = new Size(80, 40) };
            attachButton = new Button { Text = "Прикрепить", Location = new Point(420, 60), Size = new Size(80, 40) };

            messagePanel.Controls.AddRange(new Control[] { messageTextBox, sendButton, attachButton });

            splitContainer = new SplitContainer { Dock = DockStyle.Fill };
            chatHistory = new TextBox
            {
                Multiline = true,
                ScrollBars = ScrollBars.Vertical,
                Dock = DockStyle.Fill,
                ReadOnly = true
            };
            userList = new ListBox
            {
                Dock = DockStyle.Fill
            };

            splitContainer.Panel1.Controls.Add(chatHistory);
            splitContainer.Panel2.Controls.Add(userList);
            splitContainer.SplitterDistance = 600;

            this.Controls.AddRange(new Control[] { splitContainer, messagePanel, connectionPanel });

            connectButton.Click += (s, e) => ConnectToServer();
            disconnectButton.Click += (s, e) => Disconnect();
            sendButton.Click += (s, e) => SendMessage();
            attachButton.Click += (s, e) => AttachFile();
            messageTextBox.KeyPress += (s, e) =>
            {
                if (e.KeyChar == (char)Keys.Enter)
                {
                    e.Handled = true;
                    SendMessage();
                }
            };

            LoadChatHistory();
        }

        private async void ConnectToServer()
        {
            if (isConnected) return;

            try
            {
                await client.Connect(serverIPTextBox.Text, (int)portTextBox.Value, usernameTextBox.Text);
                connectButton.Enabled = false;
                disconnectButton.Enabled = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Connection failed: {ex.Message}");
            }
        }

        private void Disconnect()
        {
            client.Disconnect();
            connectButton.Enabled = true;
            disconnectButton.Enabled = false;
            isConnected = false;
        }

        private async void SendMessage()
        {
            if (!isConnected || string.IsNullOrWhiteSpace(messageTextBox.Text))
                return;

            var message = new MessageClass
            {
                Author = usernameTextBox.Text,
                Text = messageTextBox.Text.Trim(),
                CreateTime = DateTime.Now,
                MessageType = TypeMessage.Text
            };

            await client.SendMessage(message);
            messageTextBox.Clear();
        }

        private async void AttachFile()
        {
            var openFileDialog = new OpenFileDialog
            {
                Filter = "All files (*.*)|*.*",
                Title = "Select file to send"
            };

            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                var fileData = new FileInfo(openFileDialog.FileName);
                if (fileData.Length > 10 * 1024 * 1024)
                {
                    MessageBox.Show("Размер файла превышает 10 МБ");
                    return;
                }

                var file =  File.ReadAllBytes(openFileDialog.FileName);
                var message = new MessageClass
                {
                    Author = usernameTextBox.Text,
                    Text = "File attachment",
                    CreateTime = DateTime.Now,
                    MessageType= TypeMessage.File,
                    FileData = file,
                    FileName = fileData.Name
                };
                await client.SendMessage(message);
            }
        }

        private void OnMessageReceived(MessageClass message)
        {
            if (InvokeRequired)
            {
                Invoke(new Action<MessageClass>(OnMessageReceived), message);
                return;
            }

            chatHistory.AppendText(message.ToString() + Environment.NewLine);
            chatHistory.SelectionStart = chatHistory.Text.Length;
            chatHistory.ScrollToCaret();

            mHistory.Add(message);
            if (message.MessageType == TypeMessage.File)
            {
                Task.Run(() => SaveFile(message));
            }

            Task.Run(() => SaveChatHistory());
        }

        private void OnUserListUpdated(List<string> users)
        {
            if (InvokeRequired)
            {
                Invoke(new Action<List<string>>(OnUserListUpdated), users);
                return;
            }
            userList.Items.Clear();
            foreach (var user in users)
            {
                if (user != usernameTextBox.Text)
                {
                    userList.Items.Add(user);
                }
            }
        }

        private void OnConnectionStatusChanged(bool connected)
        {
            if (InvokeRequired)
            {
                Invoke(new Action<bool>(OnConnectionStatusChanged), connected);
                return;
            }

            isConnected = connected;
            connectButton.Enabled = !connected;
            disconnectButton.Enabled = connected;
            sendButton.Enabled = connected;
            messageTextBox.Enabled = connected;

            if (!connected) 
            {
                userList.Items.Clear();
            }
        }

        private async Task SaveFile(MessageClass message)
        {
            try
            {
                var downloadsPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads");
                var filePath = Path.Combine(downloadsPath, message.FileName);

                int counter = 1;
                var originalFileName = Path.GetFileNameWithoutExtension(message.FileName);
                var extension = Path.GetExtension(message.FileName);

                while (File.Exists(filePath))
                {
                    filePath = Path.Combine(downloadsPath, $"{originalFileName} ({counter}){extension}");
                    counter++;
                }

                File.WriteAllBytes(filePath, message.FileData);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to save file: {ex.Message}");
            }
        }


        private async Task SaveChatHistory()
        {
            try
            {
                var json = JsonSerializer.Serialize(mHistory, new JsonSerializerOptions { WriteIndented = true });
                await File.WriteAllTextAsync("client_chat_history.json", json);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to save chat history: {ex.Message}");
            }
        }

        private void LoadChatHistory()
        {
            try
            {
                if (File.Exists("client_chat_history.json"))
                {
                    var json = File.ReadAllText("client_chat_history.json");
                    mHistory = JsonSerializer.Deserialize<List<MessageClass>>(json) ?? new List<MessageClass>();

                    chatHistory.Clear();

                    foreach (var message in mHistory)
                    {

                        chatHistory.AppendText(message.ToString()+Environment.NewLine);
                    }

                    chatHistory.SelectionStart = chatHistory.Text.Length;
                    chatHistory.ScrollToCaret();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to load chat history: {ex.Message}");
            }
        }
    }
}
