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
    public class CommandStart : IBotCommand
    {
        public string Name { get; set; } = "/start";
        public string Text { get; set; }
        public StaffPerson? CurrentUser { get; set; }
        public async Task SendAnswer(ITelegramBotClient botClient, Message message)
        {
            var chat = message.Chat;

            var user = message.From;
            long TelegramId = user.Id;

            Text = $"{CurrentUser.name}, выберите, пожалуйста, действие";

            if (CurrentUser.rule == StaffRules.staff)
            {
                
                var Buttons = new ReplyKeyboardMarkup(new[]
                {
                   new[]
                {
                   new KeyboardButton ("Мой статус на сегодня"),
                   new KeyboardButton ("Создать заявку на отпуск"),
                },
                   new[]
                {
                   new KeyboardButton ("Мой отпуск плановый")
                   
                },
                   new[]
                {
                 new KeyboardButton ("Выйти")
                }

                });

                Buttons.ResizeKeyboard = true;

                await botClient.SendMessage(chat.Id, Text, ParseMode.None, message.MessageId, Buttons);
            }
            else if (CurrentUser.rule == StaffRules.boss|| CurrentUser.rule == StaffRules.admin)
            {
                
                var Buttons = new ReplyKeyboardMarkup(new[]
                {
                   new[]
                {
                   new KeyboardButton ("Заявки"),
                   new KeyboardButton ("Мой статус на сегодня"),
                   new KeyboardButton ("Отпуска")

                },
                   new[]
                {
                   new KeyboardButton ("Управление сотрудниками"),
                   new KeyboardButton ("Отчет по явке сотрудников"),

                },
                   new[]
                {
                 new KeyboardButton ("Выйти")
                }

                });

                Buttons.ResizeKeyboard = true;

                await botClient.SendMessage(chat.Id, Text, ParseMode.None, message.MessageId, Buttons);
            }
            else if (CurrentUser.rule == StaffRules.super_staff)
            {
                var Buttons = new ReplyKeyboardMarkup(new[]
                {
                   new[]
                {
                   new KeyboardButton ("Мой статус на сегодня"),
                   new KeyboardButton ("Заявки"),
                },
                   new[]
                {
                   new KeyboardButton ("Отпуска"),
                   new KeyboardButton ("Отчет по явке сотрудников")

                },
                   new[]
                {
                 new KeyboardButton ("Выйти")
                }

                });

                Buttons.ResizeKeyboard = true;

                await botClient.SendMessage(chat.Id, Text, ParseMode.None, message.MessageId, Buttons);


            }


        }
    }
}
