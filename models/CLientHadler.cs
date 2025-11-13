using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace MiniMessenger.models
{
    public class CLientHadler
    {
        private TcpClient client;
        private NetworkStream stream;
        private StreamReader reader;
        private StreamWriter writer;

        public string Username { get; set; } = string.Empty;
        public event Action<MessageClass, CLientHadler> MessageReceived;
        public event Action<CLientHadler, string> ClientDisconnected;

        public CLientHadler (TcpClient client)
        {
            this.client = client;
            stream = client.GetStream ();
            reader = new StreamReader (stream);
            writer = new StreamWriter(stream) { AutoFlush = true };
        }

        public async Task HandleClientAsync()
        {
            try
            {
                string line;
                while ((line = await reader.ReadLineAsync()) != null)
                {
                    var message = MessageClass.FromJson(line);

                    if (string.IsNullOrEmpty(Username) && !string.IsNullOrEmpty(message.Author))
                    {
                        Username = message.Author;
                        var connectMessage = new MessageClass
                        {
                            Author = Username,
                            MessageType = TypeMessage.Connection,
                            CreateTime = DateTime.Now
                        };

                        MessageReceived?.Invoke(connectMessage, this);
                    }
                    MessageReceived?.Invoke(message, this);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Client hadling error: {ex.Message}");
            }
            finally
            {
                Disconnect();
            }
        }

        public async Task SendMessageAsync(MessageClass message)
        {
            try
            {
                await writer.WriteLineAsync(message.ToJson());
            }
            catch (Exception ex)
            {
                Disconnect();
            }
        }

        public void Disconnect()
        {
            if (!string.IsNullOrEmpty(Username))
            {
                ClientDisconnected?.Invoke(this, Username);
            }

            reader?.Close();
            writer?.Close();
            stream?.Close();
            client?.Close();
        }
    }
}
