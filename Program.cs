using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Telegram.Bots.Types;

namespace TimeTable_Project
{
    internal class Program
    {
        public static string timeForNotification = "11:00";

        static async Task Main()
        {         

            TelegramBOT Bot = new TelegramBOT();
            await Bot.Main();
        }


    }
}
