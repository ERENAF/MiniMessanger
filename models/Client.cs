using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace MiniMessenger.models
{
    public class Client
    {
        private string username = string.Empty;
        private TcpClient tcpClient;
        private NetworkStream stream;
        private StreamReader reader;
        private StreamWriter writer;
        private bool isConnected = false;

        public event Action<MessageClass> MessageReceived;
        public event Action<List<String>> UserListUpdated;
        public event Action<bool> ConnectionStatusChanged;

        public async Task Connect(string serverIP, int port, string username)
        {
            try
            {
                tcpClient = new TcpClient();
                await tcpClient.ConnectAsync(serverIP,port);

                stream = tcpClient.GetStream();
                reader = new StreamReader(stream);
                writer = new StreamWriter(stream) { AutoFlush = true };

                this.username = username;
                isConnected = true;

                ConnectionStatusChanged?.Invoke(true);

                var connectionMessage = new MessageClass
                {
                    Author = username,
                    MessageType = TypeMessage.Connection,
                    CreateTime = DateTime.Now
                };
                await SendMessage(connectionMessage);

                _ = Task.Run(ReceiveMessage);
            }
            catch (Exception ex)
            {
                throw new Exception($"Connection failed: {ex.Message}");
            }
        }

        public void Disconnect()
        {
            if (!isConnected) return;
            isConnected = false;
            try
            {
                var disconnectMessage = new MessageClass
                {
                    Author = username,
                    MessageType = TypeMessage.Disconnection,
                    CreateTime = DateTime.Now
                };
                writer?.WriteLine(disconnectMessage.ToJson());
            }
            catch { }
            finally
            {
                CloseNetworkResources();
                ConnectionStatusChanged?.Invoke(false);
            }
        }

        public async Task SendMessage(MessageClass message)
        {
            if (!isConnected) return;
            try
            {
                await writer.WriteLineAsync(message.ToJson());
            }
            catch
            {
                Disconnect();   
            }
        }

        private async Task ReceiveMessage()
        {
            try
            {
                string line;
                while (isConnected && (line = await reader.ReadLineAsync()) != null)
                {
                    var message = MessageClass.FromJson(line);

                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Receive error: {ex.Message}");
            }
            finally
            {
                Disconnect();
            }
        }

        private void ProcessReceivedMessage(MessageClass message)
        {
            switch (message.MessageType)
            {
                case TypeMessage.UserList:
                    var users = new List<string>(message.Text.Split(','));
                    UserListUpdated?.Invoke(users);
                    break;
                default:
                    MessageReceived?.Invoke(message);
                    break;
            }
        }

        private void CloseNetworkResources()
        {
            try
            {
                reader?.Close();
                writer?.Close();
                stream?.Close();
                tcpClient?.Close();
            }
            catch (Exception ex) 
            { 
                Console.WriteLine($"Error closing resources: {ex.Message}");
            }
        }
    }
}
