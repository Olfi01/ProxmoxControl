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
        public static ReplyKeyboardMarkup GetReplyMarkupPage(IEnumerable<string> elements, int page)
        {
            List<List<KeyboardButton>> rows = new();
            for (int i = page * MessageHelper.ItemsPerPage; i < (page + 1) * MessageHelper.ItemsPerPage; i++)
            {
                if (i >= elements.Count()) break;
                rows.Add(new List<KeyboardButton> { new KeyboardButton(elements.ElementAt(i)) });
            }
            List<KeyboardButton> arrows = new();
            if (page > 0)
            {
                arrows.Add(new KeyboardButton(":arrow_forward:"));
            }
            if (elements.Count() >= (page + 1) * MessageHelper.ItemsPerPage)
            {
                arrows.Add(new KeyboardButton(":arrow_backward:"));
            }
            if (arrows.Count > 0) rows.Add(arrows);
            return new(rows) { OneTimeKeyboard = true };
        }
    }
}
