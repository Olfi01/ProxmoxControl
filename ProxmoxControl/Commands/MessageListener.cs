using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telegram.BotAPI.AvailableTypes;
using Telegram.BotAPI.GettingUpdates;

namespace ProxmoxControl.Commands
{
    public abstract class MessageListener
    {
        public string ListenerID { get; }
        public MessageListener(string listenerID)
        {
            ListenerID = listenerID;
        }

        public abstract bool Handles(Message message);
    }
}
