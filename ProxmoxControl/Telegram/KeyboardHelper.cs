using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telegram.BotAPI.AvailableTypes;

namespace ProxmoxControl.Telegram
{
    public class KeyboardHelper
    {
        public const string ArrowRight = "▶️";
        public const string ArrowLeft = "◀️";

        public static ReplyKeyboardMarkup GetReplyMarkupPage(IEnumerable<string> elements, int page)
        {
            if (page < 0) page = 0;
            List<List<KeyboardButton>> rows = new();
            for (int i = page * MessageHelper.ItemsPerPage; i < (page + 1) * MessageHelper.ItemsPerPage; i++)
            {
                if (i >= elements.Count()) break;
                rows.Add(new List<KeyboardButton> { new KeyboardButton(elements.ElementAt(i)) });
            }
            List<KeyboardButton> arrows = new();
            if (page > 0)
            {
                arrows.Add(new KeyboardButton(ArrowLeft));
            }
            if (elements.Count() >= (page + 1) * MessageHelper.ItemsPerPage)
            {
                arrows.Add(new KeyboardButton(ArrowRight));
            }
            if (arrows.Count > 0) rows.Add(arrows);
            return new(rows) { OneTimeKeyboard = true };
        }
    }
}
