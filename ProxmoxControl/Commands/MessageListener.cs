using Telegram.BotAPI.AvailableTypes;

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
