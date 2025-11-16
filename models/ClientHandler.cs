using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace MiniMessenger.models
{

    public class ClientHandler
    {
        private TcpClient client;
        private NetworkStream stream;
        private StreamReader reader;
        private StreamWriter writer;

        public string Username { get; set; } = string.Empty;
        public event Action<MessageClass, ClientHandler> MessageReceived;
        public event Action<ClientHandler, string> ClientDisconnected;

        public ClientHandler(TcpClient client)
        {
            this.client = client;
            stream = client.GetStream();
            reader = new StreamReader(stream, Encoding.UTF8);
            writer = new StreamWriter(stream, Encoding.UTF8) { AutoFlush = true };
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
                    }

                    MessageReceived?.Invoke(message, this);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Client handling error: {ex.Message}");
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
                Console.WriteLine($"Error sending message: {ex.Message}");
                Disconnect();
            }
        }

        public void Disconnect()
        {
            try
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
            catch (Exception ex)
            {
                Console.WriteLine($"Error during disconnect: {ex.Message}");
            }
        }
    }
}
