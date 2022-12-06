using ProxmoxControl.Commands;
using Telegram.BotAPI;
using Telegram.BotAPI.AvailableMethods;
using Telegram.BotAPI.AvailableTypes;
using Telegram.BotAPI.GettingUpdates;

namespace ProxmoxControl
{
    public static class Program
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
        private static readonly Func<MessageEntity, bool> isBotCommand = entity => entity.GetEntityType() == MessageEntityType.BotCommand;

        private static readonly List<MessageListener> updateListeners = new();
        private static string botUsername = "UNAUTHORIZED";

        public static async Task Main(string[] args)
        {
            if (args.Length < 1)
            {
                Logger.Error("Please provide a telegram bot API token.");
                return;
            }
            string token = args[0];
            Logger.Info("Initializing bot connection...");
            BotClient tg = new(token);
            User me = await tg.GetMeAsync();
            botUsername = me.Username;
            Logger.Info("Successfully connected to bot with username {0}.", botUsername);
            Logger.Info("Loading commands...");
            BotCommands.Init();
            Logger.Info("Loading complete!");
            Logger.Info("Starting to listen for updates!");

            var updates = await tg.GetUpdatesAsync();
            while (true)
            {
                if (updates.Any())
                {
                    foreach (var update in updates)
                    {
                        HandleUpdate(tg, update);
                    }
                    var offset = updates.Last().UpdateId + 1;
                    updates = await tg.GetUpdatesAsync(offset);
                }
                else
                {
                    updates = await tg.GetUpdatesAsync();
                }
            }
        }

        public static void AddListener(MessageListener listener)
        {
            updateListeners.Add(listener);
        }

        private static void HandleUpdate(BotClient tg, Update update)
        {
            try
            {
                switch (update.Type)
                {
                    case UpdateType.Message:
                        HandleMessage(tg, update.Message);
                        break;
                }
            } catch (Exception ex)
            {
                Logger.Error(ex);
            }
        }

        private static void HandleMessage(BotClient tg, Message message)
        {
            foreach (var listener in updateListeners.Where(listener => listener.Handles(message)).ToList())
            {
                if (BotCommands.HandleListener(listener.ListenerID, message, tg))
                {
                    updateListeners.Remove(listener);
                }
            }
            if (message.Text != null)
            {
                if (message.Entities?.Count() > 0 && message.Entities.Any(isBotCommand))
                {
                    for (int i = 0; i < message.Entities.Count(); i++)
                    {
                        MessageEntity entity = message.Entities.ElementAt(i);
                        if (isBotCommand(entity))
                        {
                            string value = message.Text.Substring(entity.Offset, entity.Length);
                            if (value.EndsWith($"@{botUsername}")) value = value.Remove(value.LastIndexOf($"@{botUsername}"));
                            BotCommands.HandleCommand(value, message, tg);
                        }
                    }
                }
            }
        }
    }
}
