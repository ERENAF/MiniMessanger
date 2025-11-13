using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace MiniMessenger.models
{
    public class Server
    {
        private TcpListener listener;
        private List<CLientHadler> chatManagers = new List<CLientHadler>();
        private bool isRunning = false;

        public event Action<MessageClass> MessageReceived;
        public event Action<List<string>> UserListUpdated;

        public async Task StartServer(IPAddress ipAddress, int port)
        {
            listener = new TcpListener(ipAddress, port);
            listener.Start();
            isRunning = true;


            _ = Task.Run(async () =>

            {
                while (isRunning)
                {
                    try
                    {
                        var client = await listener.AcceptTcpClientAsync();
                        var handler = new CLientHadler(client);
                        handler.MessageReceived += OnMessageReceived;
                        handler.ClientDisconnected += OnClientDisconnected;
                        _ = handler.HandleClientAsync();
                    }
                    catch (ObjectDisposedException)
                    {
                        break;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error accepting client: {ex.Message}");
                    }
                }
            });
        }
        public void StopServer()
        {
            isRunning = false;
            listener?.Stop();
            foreach (var client in chatManagers.ToArray())
            {
                client.Disconnect();
            }
            chatManagers.Clear();
        }

        private void OnMessageReceived(MessageClass message, CLientHadler sender)
        {
            MessageReceived?.Invoke(message);

            switch (message.MessageType)
            {
                case TypeMessage.Text:
                    BroadcastMessage(message, sender);
                    break;
                case TypeMessage.Connection:
                    UpdateUserList();
                    BroadcastMessage(new MessageClass
                    {
                        Author = "Server",
                        Text = $"{message.Author} joined the chat",
                        MessageType = TypeMessage.Text,
                        CreateTime = DateTime.Now
                    });
                    break;
            }
        }

        private void OnClientDisconnected(CLientHadler client,string username)
        {
            chatManagers.Remove(client);
            UpdateUserList();
            BroadcastMessage(new MessageClass
            {
                Author = "Server",
                Text = $"{username} left the chat",
                CreateTime = DateTime.Now,
                MessageType = TypeMessage.Text
            });
        }
        private void BroadcastMessage(MessageClass message, CLientHadler excludeSender = null)
        {
            foreach (var client in chatManagers)
            {
                if (client != excludeSender) _ = client.SendMessageAsync(message);
            }
        }

        private void UpdateUserList()
        {
            var users = new List<string>();
            foreach (var user in chatManagers)
            {
                if (!string.IsNullOrEmpty(user.Username))
                {
                    users.Add(user.Username);
                }
            }
            UserListUpdated?.Invoke(users);

            var userListMessage = new MessageClass
            {
                MessageType = TypeMessage.UserList,
                Text = string.Join(", ", users),
                CreateTime = DateTime.Now,
            };
            foreach (var user in chatManagers)
            {
                _ = user.SendMessageAsync(userListMessage);
            }
        }
    }
}
