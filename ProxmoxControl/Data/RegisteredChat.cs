using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProxmoxControl.Data
{
    [PrimaryKey("ID")]
    public class RegisteredChat
    {
        public long ID { get; }
        public string? ProxmoxHost { get; set; }
        public string? ProxmoxApiToken { get; set; }
        public RegisteredChat() { }
        public RegisteredChat(long id)
        {
            ID = id;
        }
    }
}
