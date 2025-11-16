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
        private List<ClientHandler> chatManagers = new List<ClientHandler>();
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
                        var handler = new ClientHandler(client);

                        // ДОБАВЛЯЕМ клиента в список сразу при подключении
                        chatManagers.Add(handler);

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

        private void OnMessageReceived(MessageClass message, ClientHandler sender)
        {
            MessageReceived?.Invoke(message);

            switch (message.MessageType)
            {
                case TypeMessage.Text:
                    BroadcastMessage(message);
                    break;
                case TypeMessage.Connection:
                    UpdateUserList();
                    var notificationMessage = new MessageClass
                    {
                        Author = "Server",
                        Text = $"{message.Author} joined the chat",
                        MessageType = TypeMessage.Text,
                        CreateTime = DateTime.Now
                    };
                    BroadcastMessage(notificationMessage);
                    break;
                case TypeMessage.Disconnection:
                    UpdateUserList();
                    var disconnectMessage = new MessageClass
                    {
                        Author = "Server",
                        Text = $"{message.Author} left the chat",
                        MessageType = TypeMessage.Text,
                        CreateTime = DateTime.Now
                    };
                    BroadcastMessage(disconnectMessage);
                    break;
                case TypeMessage.File:
                    BroadcastMessage(message);
                    break;
            }
        }

        private void OnClientDisconnected(ClientHandler client, string username)
        {
            chatManagers.Remove(client);
            UpdateUserList();

            var notificationMessage = new MessageClass
            {
                Author = "Server",
                Text = $"{username} left the chat",
                CreateTime = DateTime.Now,
                MessageType = TypeMessage.Text
            };
            BroadcastMessage(notificationMessage);
            MessageReceived?.Invoke(notificationMessage);
        }

        private async void BroadcastMessage(MessageClass message)
        {
            var tasks = new List<Task>();
            foreach (var client in chatManagers.ToList())
            {
                try
                {
                    tasks.Add(client.SendMessageAsync(message));
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[SERVER] Error sending to client {client.Username}: {ex.Message}");
                }
            }
        }

        private void UpdateUserList()
        {
            var users = chatManagers
                .Where(client => !string.IsNullOrEmpty(client.Username))
                .Select(client => client.Username)
                .ToList();

            UserListUpdated?.Invoke(users);

            var userListMessage = new MessageClass
            {
                MessageType = TypeMessage.UserList,
                Text = string.Join(",", users), 
                CreateTime = DateTime.Now,
            };

            foreach (var client in chatManagers.ToList())
            {
                try
                {
                    _ = client.SendMessageAsync(userListMessage);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error sending user list: {ex.Message}");
                }
            }
        }

        public void StopServer()
        {
            isRunning = false;
            listener?.Stop();
            foreach (var client in chatManagers.ToList())
            {
                client.Disconnect();
            }
            chatManagers.Clear();
        }
    }
}
