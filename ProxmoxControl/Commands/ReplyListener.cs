using Telegram.BotAPI.AvailableTypes;

namespace ProxmoxControl.Commands
{
    public class ReplyListener : MessageListener
    {
        public Message Message { get; }
        public ReplyListener(Message message, string listenerID) : base(listenerID)
        {
            Message = message;
        }

        public override bool Handles(Message message)
        {
            return message.Chat.Id == Message.Chat.Id
                && message.ReplyToMessage?.MessageId == Message.MessageId;
        }
    }
}
