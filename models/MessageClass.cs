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
        UserList,
        File
    }
    [Serializable]
    public class MessageClass
    {
        public string Author { get; set; } = string.Empty;
        public string Text { get; set; } = string.Empty;
        public DateTime CreateTime { get; set; }
        public TypeMessage MessageType { get; set; }
        public byte[] FileData { get; set;  } = Array.Empty<byte>();
        public string FileName { get; set; } = string.Empty;
        public string Recipient { get; set; } = string.Empty;

        public string ToJson()
        {
            return JsonSerializer.Serialize(this);
        }

        public static MessageClass FromJson(string json)
        {
            return JsonSerializer.Deserialize<MessageClass>(json) ?? new MessageClass();
        }

        public override string ToString()
        {
            if (this.Recipient == "") return $"[{CreateTime:HH:mm:ss}] {Author}: {Text}";
            else return $"[{CreateTime:HH:mm:ss}] {Author} to {Recipient}: {Text}";
        }
    }
}
