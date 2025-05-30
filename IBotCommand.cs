using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;


namespace TimeTable_Project
{
    public interface IBotCommand
    {
        public string Name { get; set; }

        public string Text { get; set; }



        public async Task SendAnswer(ITelegramBotClient botClient, Message message,ReplyMarkup? replyMarkup)
        {
            var chat = message.Chat;

            var user = message.From;
            long TelegramId = user.Id;

            await botClient.SendMessage(chat.Id, Text, ParseMode.None, message.MessageId, replyMarkup);

        }

    }
}
