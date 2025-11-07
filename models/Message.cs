using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace MiniMessenger.models
{

    public enum TypeMessage
    {
        Text,
        Connection,
        Disconnection,
        UserList
    }
    [Serializable]
    public class Message
    {
        public string Author { get; set; } = string.Empty;
        public string Text { get; set; } = string.Empty;
        public DateTime CreateTime { get; set; }
        public TypeMessage MessageType { get; set; }

        public string ToJson()
        {
            return JsonSerializer.Serialize(this);
        }

        public static Message FromJson(string json)
        {
            return JsonSerializer.Deserialize<Message>(json) ?? new Message();
        }

        public override string ToString()
        {
            return $"[{CreateTime:HH:mm:ss}] {Author}: {Text}";
        }
    }
}
