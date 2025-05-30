using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.BotBuilder.CalendarPicker.Interfaces;

namespace TimeTable_Project
{
    
    public class Calendar : ICalendarHandler
    {
        public async Task HandlePickedDateAsync(
            ITelegramBotClient context,
            Message message,
            DateTime pickedDate,
            CancellationToken cancellationToken
            )
        {
            await context.SendMessage(
                message.Chat,
                $"PickedDate: {pickedDate:d}",
                cancellationToken: cancellationToken
                );
        }

        
    }
}
