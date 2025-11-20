using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.VisualBasic;
using MiniMessenger.models;

namespace MiniMessenger.forms
{
    public partial class ServerForm : Form
    {
        public Server server;
        private List<MessageClass> mhistory = new List<MessageClass>();
        private IPAddress ipAddress;

        private TextBox chatHistory;
        private ListBox userList;
        private SplitContainer splitContainer;

        private Panel controlPNL;
        private Button startServerBTN;
        private Button stopServerBTN;
        private Label portLBL;
        private NumericUpDown portNUD;
        private Label ipAddressLBL;
        public ServerForm()
        {
            ipAddress = GetIPAddress();
            InitializeComponent();
            server = new Server();
            server.MessageReceived += OnMessageReceived;
            server.UserListUpdated += OnUserListUpdated;
            LoadChatHistory();
        }

        private void InitializeComponent()
        {
            this.Text = "СЕРВЕР";
            this.Size = new Size(1280, 720);
            this.StartPosition = FormStartPosition.CenterScreen;

            controlPNL = new Panel { Dock = DockStyle.Top, Height = 100 };
            startServerBTN = new Button { Text = "Запустить сервер", Location = new Point(10, 10), Size = new Size(100, 30) };
            stopServerBTN = new Button { Text = "Выключить сервер", Location = new Point(120, 10), Size = new Size(100, 30) };
            portLBL = new Label { Text = "Port:", Location = new Point(230, 15), Size = new Size(40, 20) };
            portNUD = new NumericUpDown {Location = new Point(270,12), Size = new Size(80,20), Minimum = 49152, Maximum = 65535};
            ipAddressLBL = new Label { Text = $"IP: {ipAddress.ToString()}", Location = new Point (10,60), Size = new Size(240,20)};

            startServerBTN.Click += (s, e) => StartServer((int)portNUD.Value);
            stopServerBTN.Click += (s, e) => StopServer();

            controlPNL.Controls.AddRange(new Control[] { startServerBTN, stopServerBTN, portLBL, portNUD, ipAddressLBL});

            chatHistory = new TextBox
            {
                Multiline = true,
                ScrollBars = ScrollBars.Vertical,
                Dock = DockStyle.Fill,
                ReadOnly = true
            };
            userList = new ListBox()
            {
                Dock = DockStyle.Right,
                Width = 200
            };

            splitContainer = new SplitContainer
            {
                Dock = DockStyle.Fill,
                Orientation = Orientation.Vertical,
            };
            splitContainer.Panel1.Controls.Add(chatHistory);
            splitContainer.Panel2.Controls.Add(userList);

            this.Controls.AddRange(new Control[] { splitContainer, controlPNL });
        }

        private async void StartServer(int port)
        {
            try
            {
                await server.StartServer(port);
                AddServerMessage($"Server started on port {ipAddress.ToString()} : {port}");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to start server: {ex.Message}");
            }
        }
        private void StopServer()
        {
            server.StopServer();
            AddServerMessage("Server stopped");
        }
        private void OnMessageReceived(MessageClass message)
        {
            if (InvokeRequired)
            {
                Invoke(new Action<MessageClass>(OnMessageReceived), message);
                return;
            }
            mhistory.Add(message);
            chatHistory.AppendText($"{message}\r\n");
            Task.Run(()=> SaveChatHistory());
        }
        private void OnUserListUpdated(List<String> users)
        {
            if (InvokeRequired)
            {
                Invoke(new Action<List<string>> (OnUserListUpdated), users);
                return;
            }
            userList.Items.Clear();
            foreach (var user in users)
            {
                userList.Items.Add(user);
            }
        }

        private void AddServerMessage(string text)
        {
            var message = new MessageClass()
            {
                Author = "Server",
                Text = text,
                CreateTime = DateTime.Now,
                MessageType = TypeMessage.Text
            };
            OnMessageReceived(message);
        }

        private async Task SaveChatHistory()
        {
            try
            {
                var json = JsonSerializer.Serialize(mhistory, new JsonSerializerOptions { WriteIndented = true });
                await File.WriteAllTextAsync("chat_history.json", json);
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
                if (File.Exists("chat_history.json"))
                {
                    var json = File.ReadAllText("chat_history.json");
                    mhistory = JsonSerializer.Deserialize<List<MessageClass>>(json) ?? new List<MessageClass>();
                    foreach (var message in mhistory)
                    {
                        chatHistory.AppendText($"{message}\r\n");

                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to load chat history: {ex.Message}");
            }
        }

        private IPAddress GetIPAddress()
        {
            var host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (var ip in host.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    return ip;
                }
            }
            throw new Exception("No network adapters with an IPv4 address in the system!");
        }


    }
}
