using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Telegram.Bot.TelegramBotClient;
using Telegram.Bot.Polling;
using Telegram.Bot.Types.Enums;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;
using Telegram.Bot.Exceptions;
using static System.Net.Mime.MediaTypeNames;
using System.Xml.Linq;
using System.Threading;
using Microsoft.Extensions.DependencyInjection;
using Telegram.Bots.Http;
using System.Globalization;
using System.Net.Http.Json;
using Telegram.Bots;
using System.Collections;
using Telegram.BotBuilder.CalendarPicker;
using System.Runtime.InteropServices;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using System.Diagnostics.Eventing.Reader;






namespace TimeTable_Project
{
    public class TelegramBOT
    {

        static readonly HttpClient client = new HttpClient();

        static Dictionary<long, Request> requestsHolder = new();

        static Dictionary<long, List<Request>> requestsHolderList = new();

        static Dictionary<long, Request> requestsHolderForAgreement = new();

        public async Task Main()
        {


            var confBuilder = new ConfigurationBuilder().AddJsonFile("appsettings.json", true, true).AddUserSecrets<Program>();
            var config = confBuilder.Build();

            string? myToken = config.GetValue<string>("MyToken");

            using var cts = new CancellationTokenSource();
            var botClient = new TelegramBotClient(myToken);
            var me = await botClient.GetMe(cancellationToken: cts.Token);
            Console.WriteLine($"Start Bot, {me.FirstName} {me.LastName} with {me.Username} and id {me.Id}!");

            botClient.StartReceiving(UpdateRecieved,
            ErrorHandler,
            new ReceiverOptions()
            {
                AllowedUpdates = [UpdateType.Message,
                                  UpdateType.CallbackQuery]
            },
            cts.Token);


            Timer timer = new Timer(async state =>
            {
                await SendNotificationIfNecessaryAsync(botClient);
            }, null, TimeSpan.Zero, TimeSpan.FromHours(1));


            string taskCommand = Console.ReadLine();
            await cts.CancelAsync();


        }


        private static Task ErrorHandler(ITelegramBotClient botClient, Exception eX, CancellationToken token)

        {
            var ErrorMessage = eX switch
            {
                ApiRequestException apiRequestException
                    => $"Telegram API Error:\n[{apiRequestException.ErrorCode}]\n{apiRequestException.Message}",
                _ => eX.ToString()
            };

            Console.WriteLine(ErrorMessage);
            return Task.CompletedTask;

        }

        public async Task UpdateRecieved(ITelegramBotClient botClient, Update update, CancellationToken token)
        {

            try
            {

                switch (update.Type)
                {
                    case UpdateType.Message:
                        {

                            if (update.Message.Type == MessageType.Text)

                            {
                                var message = update.Message;
                                var chat = message.Chat;
                                var user = message.From;
                                long TelegramId = user.Id;

                                string newText = "";

                                StaffPerson currentUser = StaffList.CheckUserForTelegramID(TelegramId);
                                int TableNumber = 0;
                                string NameUser = "";
                                string SurnameUser = "";
                                string PatronymicUser = "";


                                if (currentUser != null)
                                {
                                    if (currentUser.AuthenticationIsComplited())

                                    {
                                        var lastBotCommand = StaffList.GetLastBotCommand(TelegramId);

                                        if (lastBotCommand.Date < DateTime.Today)
                                        {
                                            newText = $"Здравствуйте, {currentUser.name}!";
                                            Console.WriteLine(newText);
                                            await botClient.SendMessage(
                                            chat.Id,
                                            newText, ParseMode.None,
                                            message.MessageId
                                            );
                                            StaffList.UpdateLastBotCommand(TelegramId, Bot_command_types.authentication_complete);
                                        }

                                        if (lastBotCommand.Type != Bot_command_types.authentication_complete)
                                        {
                                            StaffList.UpdateLastBotCommand(TelegramId, Bot_command_types.authentication_complete);

                                        }


                                        switch (message.Text)
                                        {
                                            case "Заявки на согласование":
                                               
                                                await HandlerRequestsAgreement(botClient, message, TelegramId);

                                                break;
                                            case "Заявки":
                                                string textAnswer = "Здесь можно создать свою заявку или посмотреть на все заявки";
                                                Console.WriteLine(textAnswer);

                                                await botClient.SendMessage(
                                                chat.Id,
                                                textAnswer, ParseMode.None,
                                                message.MessageId, GetKeyboard(message.Text)
                                                );
                                                break;
                                            case "Мой статус на сегодня":
                                                textAnswer = currentUser.GetMyStatus();

                                                Console.WriteLine(textAnswer);

                                                await botClient.SendMessage(
                                                chat.Id,
                                                textAnswer, ParseMode.None,
                                                message.MessageId, GetKeyboard(message.Text)
                                                );

                                                break;
                                            case "Изменить статус":

                                                textAnswer = "Выберите подходящий статус на сегодня:" +
                                                    "\n /Office - В офисе" +
                                                    "\n /RemoteWork - На удаленке" +
                                                    "\n /Vacation - В oтпуске oфициально" +
                                                    "\n /Family - Отсутствую по семейным обстоятельствам" +
                                                    "\n /Medical - На больничном" +
                                                    "\n /BusinessTrip - В командировке" +
                                                    "\n /OnStudy - На учебе";

                                                Console.WriteLine(textAnswer);

                                                await botClient.SendMessage(
                                                chat.Id,
                                                textAnswer, ParseMode.None,
                                                message.MessageId, GetKeyboard(message.Text)
                                                );

                                                StaffList.UpdateLastBotCommand(TelegramId, Bot_command_types.enter_status);

                                                break;
                                            case "/Office":

                                                if (lastBotCommand.Type == Bot_command_types.enter_status)
                                                {

                                                    textAnswer = SetStatusOffice(currentUser, message.Text);
                                                    await botClient.SendMessage(
                                                        chat.Id,
                                                        textAnswer, ParseMode.None,
                                                        message.MessageId, GetKeyboard(message.Text)
                                                        );
                                                }

                                                break;
                                            case "/RemoteWork":
                                            case "/Family":
                                            case "/OnStudy":
                                            case "/BusinessTrip":
                                            case "/Vacation":
                                                if (lastBotCommand.Type == Bot_command_types.enter_status)
                                                {
                                                    var vacationType = currentUser.UpdateStatus(message.Text);
                                                    StaffList.UpdateLastBotCommand(TelegramId, Bot_command_types.authentication_complete);
                                                    if (vacationType != null)
                                                    {
                                                        textAnswer = $"Удачи! \nЗаписал новый статус: {vacationType.name} ({vacationType.table_code.ToString()})";
                                                        await botClient.SendMessage(
                                                        chat.Id,
                                                        textAnswer, ParseMode.None,
                                                        message.MessageId, GetKeyboard(message.Text)
                                                        );
                                                    }
                                                }
                                                else if (lastBotCommand.Type == Bot_command_types.enter_request_type)
                                                {

                                                    int vacationTypeId = VacationType.GetVacationType(message.Text);
                                                    Request newRequest = new Request() { staff_table_number = currentUser.table_number, vacation_type_id = vacationTypeId };
                                                    requestsHolder[TelegramId] = newRequest;

                                                    StaffList.UpdateLastBotCommand(TelegramId, Bot_command_types.enter_request_datastart);

                                                    textAnswer = $"Хорошо. Теперь введите дату начала отпуска (формат даты: 01.05.2025)";
                                                    await botClient.SendMessage(
                                                    chat.Id,
                                                    textAnswer, ParseMode.None,
                                                    message.MessageId, GetKeyboard("Создать заявку на отпуск")
                                                    );
                                                }

                                                break;

                                            case "/Medical":
                                                if (lastBotCommand.Type == Bot_command_types.enter_status)
                                                {
                                                    var vacationType = currentUser.UpdateStatus(message.Text);
                                                    StaffList.UpdateLastBotCommand(TelegramId, Bot_command_types.authentication_complete);
                                                    if (vacationType != null)
                                                    {
                                                        textAnswer = $"Поправлйтесь! Не забудьте про больничный лист. \nЗаписал новый статус: {vacationType.name} ({vacationType.table_code.ToString()})";
                                                        await botClient.SendMessage(
                                                        chat.Id,
                                                        textAnswer, ParseMode.None,
                                                        message.MessageId, GetKeyboard(message.Text)
                                                        );
                                                    }
                                                }
                                                else if (lastBotCommand.Type == Bot_command_types.enter_request_type)
                                                {

                                                    int vacationTypeId = VacationType.GetVacationType(message.Text);
                                                    Request newRequest = new Request() { staff_table_number = currentUser.table_number, vacation_type_id = vacationTypeId };
                                                    requestsHolder[TelegramId] = newRequest;

                                                    StaffList.UpdateLastBotCommand(TelegramId, Bot_command_types.enter_request_datastart);

                                                    textAnswer = $"Хорошо. Теперь введите дату начала отпуска (формат даты: 01.05.2025)";
                                                    await botClient.SendMessage(
                                                    chat.Id,
                                                    textAnswer, ParseMode.None,
                                                    message.MessageId, GetKeyboard("Создать заявку на отпуск")
                                                    );
                                                }

                                                break;
                                            case "Управление сотрудниками":
                                                textAnswer = "Тут можно посмотреть на весь список сотрудников и указать ОСОБЕННЫХ, кому будет ежедневно приходить напоминание, чтобы он отметился)";

                                                Console.WriteLine(textAnswer);

                                                await botClient.SendMessage(
                                                chat.Id,
                                                textAnswer, ParseMode.None,
                                                message.MessageId, GetKeyboard(message.Text)
                                                );
                                                break;
                                            case "Список сотрудников":
                                                textAnswer = "Вывожу список сотрудников отдела";
                                                var staffList = StaffList.GetStaff();


                                                textAnswer = textAnswer + '\n' + String.Join(";", staffList.Select(b => "\n" + "-" + b.surname + " " + b.name + " " + b.patronymic + ", Таб. номер: " + b.table_number + ", Роль: " + b.rule + ", Напоминание присылать: " + b.reminder_is_necessary));


                                                Console.WriteLine(textAnswer);

                                                await botClient.SendMessage(
                                                chat.Id,
                                                textAnswer, ParseMode.None,
                                                message.MessageId, GetKeyboard(message.Text)
                                                );

                                                break;
                                            case "Установить напоминания сотрудникам":

                                                await HandleNotificationIsNecessaryCommandAsync(botClient, message, TelegramId);

                                                break;
                                            case "Снять напоминания сотрудникам":

                                                await HandleNotificationIsUnnecessaryCommandAsync(botClient, message, TelegramId);

                                                break;
                                            case "Отчет по явке сотрудников":
                                                textAnswer = "Выберите период";

                                                Console.WriteLine(textAnswer);

                                                await botClient.SendMessage(
                                                chat.Id,
                                                textAnswer, ParseMode.None,
                                                message.MessageId, GetKeyboard(message.Text)
                                                );
                                                break;
                                            case "Создать заявку на отпуск":
                                                await HandleRequestCreationCommandAsync(botClient, message, TelegramId);
                                                break;
                                            case "Отпуска":
                                                textAnswer = "Тут можно посмотреть на свой плановый отпуск или на список плановых отпусков сотрудников";

                                                Console.WriteLine(textAnswer);

                                                await botClient.SendMessage(
                                                chat.Id,
                                                textAnswer, ParseMode.None,
                                                message.MessageId, GetKeyboard(message.Text)
                                                );
                                                break;
                                            case "Мой отпуск плановый":

                                                int? holidayDays = currentUser.GetMyHolidayDaysCount();
                                                holidayDays = holidayDays.GetValueOrDefault();

                                                if (holidayDays > 0)

                                                {
                                                    textAnswer = $"Ура! В этом году еще есть деньки отпуска: {holidayDays}";

                                                    Console.WriteLine(textAnswer);

                                                }
                                                else
                                                {
                                                    textAnswer = $"Увы, в этом году дней отпуска уже не осталось";

                                                    Console.WriteLine(textAnswer);

                                                }

                                                var vacationList = currentUser.GetMyHolidayDays();

                                                if (vacationList != null && vacationList.Count > 0)
                                                {
                                                    string addAnswer = $"\nВот ваши отпуска на {DateTime.Now.ToString("yyyy", CultureInfo.InvariantCulture)} год:";
                                                    foreach (var vacation in vacationList)
                                                    {
                                                        addAnswer = addAnswer + $"\n {vacation.date_start.ToString("dd.MM.yyyy", CultureInfo.InvariantCulture)} - {vacation.date_end.ToString("dd.MM.yyyy", CultureInfo.InvariantCulture)} Дней: {vacation.days}";
                                                    }
                                                    textAnswer += addAnswer;
                                                }

                                                await botClient.SendMessage(
                                                    chat.Id,
                                                    textAnswer, ParseMode.None,
                                                    message.MessageId, GetKeyboard(message.Text)
                                                    );

                                                break;
                                            case "Отпуска отдела":

                                                vacationList = StaffList.GetAllVacations();

                                                if (vacationList != null && vacationList.Count > 0)
                                                {
                                                    textAnswer = $"\nВот все отпуска отдела на {DateTime.Now.ToString("yyyy", CultureInfo.InvariantCulture)} год:";
                                                    int curentTabNum = 0;
                                                    foreach (var vacation in vacationList)
                                                    {
                                                        if (curentTabNum != vacation.staff_table_number)
                                                        {
                                                            curentTabNum = vacation.staff_table_number;
                                                            textAnswer = textAnswer + $"\n{vacation.Surname} {vacation.Name} Таб. номер {vacation.staff_table_number}";
                                                        }
                                                        textAnswer = textAnswer + $"\n  {vacation.date_start.ToString("dd.MM.yyyy", CultureInfo.InvariantCulture)} - {vacation.date_end.ToString("dd.MM.yyyy", CultureInfo.InvariantCulture)} Дней: {vacation.days}";
                                                    }

                                                }
                                                else { textAnswer = $"Пока в базу не занесено ни одного планового отпуска на {DateTime.Now.ToString("yyyy", CultureInfo.InvariantCulture)} год"; }

                                                await botClient.SendMessage(
                                                    chat.Id,
                                                    textAnswer, ParseMode.None,
                                                    message.MessageId, GetKeyboard(message.Text)
                                                    );

                                                break;

                                            case "Предыдущая":

                                                var requestsList = requestsHolderList[TelegramId];
                                                var currentRequest = requestsHolderForAgreement[TelegramId];



                                                if (requestsList.Count > 0)
                                                {
                                                    int curIndex = requestsList.IndexOf(currentRequest);

                                                    if (curIndex - 1 > -1)
                                                    {
                                                        Request nextRequest = requestsList[curIndex - 1];

                                                        await HandlerRequestsAgreement(botClient, message, TelegramId, curIndex - 1);

                                                    }

                                                    else
                                                    {
                                                        textAnswer = "Ранее заявок на согласование не найдено";
                                                        await botClient.SendMessage(
                                                        chat.Id,
                                                        textAnswer, ParseMode.None,
                                                        message.MessageId, GetKeyboard(message.Text)
                                                        );

                                                        await HandlerRequestsAgreement(botClient, message, TelegramId);
                                                    }
                                                }

                                                break;
                                            case "Следующая":

                                                requestsList = requestsHolderList[TelegramId];
                                                currentRequest = requestsHolderForAgreement[TelegramId];


                                                if (requestsList.Count > 0)
                                                {
                                                    int curIndex = requestsList.IndexOf(currentRequest);

                                                    if (curIndex + 1 < requestsList.Count)
                                                    {
                                                        Request nextRequest = requestsList[curIndex + 1];

                                                        await HandlerRequestsAgreement(botClient, message, TelegramId, curIndex + 1);

                                                    }

                                                    else
                                                    {
                                                        textAnswer = "Далее заявок на согласование не найдено";
                                                        await botClient.SendMessage(
                                                        chat.Id,
                                                        textAnswer, ParseMode.None,
                                                        message.MessageId, GetKeyboard(message.Text)
                                                        );

                                                        await HandlerRequestsAgreement(botClient, message, TelegramId);
                                                    }
                                                }
                                                break;
                                            case "Принять":

                                                currentRequest = requestsHolderForAgreement[TelegramId];

                                                if (currentRequest != null)
                                                {

                                                    currentRequest.UpdateRequest("done");

                                                    requestsHolderList[TelegramId].Remove(currentRequest);

                                                    textAnswer = "Заявка принята";

                                                    await botClient.SendMessage(
                                                    chat.Id,
                                                    textAnswer, ParseMode.None,
                                                    message.MessageId, GetKeyboard(message.Text)
                                                    );


                                                    textAnswer = $"Ваша заявка принята. " +
                                                           $"\n {currentRequest.VacationTypeString}" +
                                                           $"\n Даты: {currentRequest.date_start.GetValueOrDefault().ToString("dd.MM.yyyy", CultureInfo.InvariantCulture)} - {currentRequest.date_end.GetValueOrDefault().ToString("dd.MM.yyyy", CultureInfo.InvariantCulture)}";

                                                    var staffPerson = StaffList.GetStaffPerson($"table_number={currentRequest.staff_table_number}");

                                                    if (staffPerson != null)
                                                    {

                                                        await botClient.SendMessage(
                                                        staffPerson.telegram_id,
                                                        textAnswer, ParseMode.None,
                                                        message.MessageId
                                                        );
                                                    }

                                                }

                                                await HandlerRequestsAgreement(botClient, message, TelegramId);

                                                break;
                                            case "Отказать":
                                                currentRequest = requestsHolderForAgreement[TelegramId];

                                                if (currentRequest != null)
                                                {

                                                    currentRequest.UpdateRequest("cancelled");

                                                    requestsHolderList[TelegramId].Remove(currentRequest);

                                                    StaffList.UpdateLastBotCommand(TelegramId, Bot_command_types.enter_reason_of_cancell_request);

                                                    textAnswer = "В заявке отказано. Введите причину отказа.";

                                                    await botClient.SendMessage(
                                                    chat.Id,
                                                    textAnswer, ParseMode.None,
                                                    message.MessageId, GetKeyboard("Ввод заявки")
                                                    );
                                                }

                                                
                                                break;
                                            case "За сегодня":

                                                var cts = new CancellationTokenSource();
                                                var todayIsWeekDay = await client.GetFromJsonAsync<int>("https://isdayoff.ru/today", cts.Token);

                                                if (todayIsWeekDay != 0)
                                                {
                                                    await botClient.SendMessage(
                                                        chat.Id,
                                                        "Сегодня выходной, так что все отдыхают", ParseMode.None,
                                                        message.MessageId, GetKeyboard(message.Text)
                                                        );
                                                    return;
                                                }

                                                var reportList = StaffList.GetAllStatus();

                                                if (reportList != null && reportList.Count > 0)
                                                {
                                                    textAnswer = $"\nВот все статусы сотрудников на {DateTime.Now.ToString("dd.MM.yyyy", CultureInfo.InvariantCulture)}:";

                                                    foreach (var report in reportList)
                                                    {

                                                        textAnswer = textAnswer + "\n" + report;
                                                    }

                                                    await botClient.SendMessage(
                                                        chat.Id,
                                                        textAnswer, ParseMode.None,
                                                        message.MessageId, GetKeyboard(message.Text)
                                                        );
                                                }

                                                break;
                                            case "За неделю":

                                                reportList = StaffList.GetAllStatusByPeriod();

                                                if (reportList != null && reportList.Count > 0)
                                                {
                                                    textAnswer = $"\nВот все статусы сотрудников с начала этой недели:";

                                                    foreach (var report in reportList)
                                                    {

                                                        textAnswer = textAnswer + "\n" + report;
                                                    }

                                                    await botClient.SendMessage(
                                                        chat.Id,
                                                        textAnswer, ParseMode.None,
                                                        message.MessageId, GetKeyboard(message.Text)
                                                        );
                                                }

                                                break;
                                            case "За месяц":
                                                reportList = StaffList.GetAllStatusByPeriod("month");

                                                if (reportList != null && reportList.Count > 0)
                                                {
                                                    textAnswer = $"\nВот все статусы сотрудников с начала этого месяца:";

                                                    foreach (var report in reportList)
                                                    {

                                                        textAnswer = textAnswer + "\n" + report;
                                                    }

                                                    await botClient.SendMessage(
                                                        chat.Id,
                                                        textAnswer, ParseMode.None,
                                                        message.MessageId, GetKeyboard(message.Text)
                                                        );
                                                }
                                                break;
                                            case "Выйти":


                                                if (lastBotCommand.Type == Bot_command_types.enter_reason_of_cancell_request)
                                                {
                                                    
                                                    await HandleEnterReasonOfCancellRequest(botClient, message, TelegramId);
                                                }
                                                
                                                StaffList.UpdateLastBotCommand(TelegramId, Bot_command_types.authentication_complete);
                                                var startCom = new CommandStart() { CurrentUser = currentUser };
                                                await startCom.SendAnswer(botClient, message);
                                                break;
                                            case "Да, я в офисе":

                                                textAnswer = SetStatusOffice(currentUser, "/Office");
                                                await botClient.SendMessage(
                                                    chat.Id,
                                                    textAnswer, ParseMode.None,
                                                    message.MessageId, GetKeyboard("Отметка статуса")
                                                    );
                                                break;
                                            case "Сегодня":
                                                if (lastBotCommand.Type == Bot_command_types.enter_request_datastart)
                                                {
                                                    bool res = requestsHolder.TryGetValue(TelegramId, out var value);
                                                    if (res)
                                                    {
                                                        requestsHolder[TelegramId].date_start = DateTime.Now;
                                                        StaffList.UpdateLastBotCommand(TelegramId, Bot_command_types.enter_request_days);

                                                        textAnswer = $"Отлично. Теперь введите, на какое количество дней берете отпуск (числом)";
                                                        await botClient.SendMessage(
                                                        chat.Id,
                                                        textAnswer, ParseMode.None,
                                                        message.MessageId, GetKeyboard("Ввод заявки")
                                                        );
                                                    }

                                                }
                                                break;
                                            default:

                                                if (lastBotCommand.Type == Bot_command_types.enter_notification_necessary)
                                                {
                                                    string columnsAndValues = $"reminder_is_necessary=true";

                                                    textAnswer = message.Text.Replace("/", "");
                                                    bool res = int.TryParse(textAnswer, out int tabNum);

                                                    if (res)
                                                    {

                                                        StaffList.UpdateStaffMultiple(columnsAndValues, tabNum);
                                                        StaffList.UpdateLastBotCommand(TelegramId, Bot_command_types.authentication_complete);

                                                        textAnswer = $"Сотруднику с табельным номером {tabNum} включены напоминания";

                                                        await botClient.SendMessage(
                                                        chat.Id,
                                                        textAnswer, ParseMode.None,
                                                        message.MessageId, GetKeyboard("Управление сотрудниками")
                                                        );

                                                        var listStaff = StaffList.GetStaff($"Where table_number={tabNum}");

                                                        if (listStaff != null && listStaff.Count > 0)
                                                        {
                                                            if (listStaff[0].telegram_id != null)
                                                            {
                                                                newText = "Если Вы сегодня не в офисе, введите свой статус, пожалуйста";

                                                                Console.WriteLine($"{DateTime.Now.ToString("dd.MM.yyy HH:mm:ss", CultureInfo.InvariantCulture)} Отправляю напоминание сотруднику {listStaff[0].surname} {listStaff[0].name} (таб. номер: {listStaff[0].table_number}) для ввода своего сегодняшнего статуса");

                                                                await botClient.SendMessage(
                                                                listStaff[0].telegram_id
                                                                ,
                                                                newText, ParseMode.None,
                                                                message.MessageId, GetKeyboard("Отметка статуса")
                                                                );
                                                            }
                                                        }
                                                    }
                                                    else
                                                    {
                                                        textAnswer = $"Введен не верный табельный номер {textAnswer}";

                                                        await botClient.SendMessage(
                                                        chat.Id,
                                                        textAnswer, ParseMode.None,
                                                        message.MessageId, GetKeyboard("Управление сотрудниками")
                                                        );

                                                        await HandleNotificationIsNecessaryCommandAsync(botClient, message, TelegramId);


                                                    }



                                                }
                                                else if (lastBotCommand.Type == Bot_command_types.enter_notification_unnecessary)
                                                {
                                                    string columnsAndValues = $"reminder_is_necessary=false";

                                                    textAnswer = message.Text.Replace("/", "");
                                                    bool res = int.TryParse(textAnswer, out int tabNum);

                                                    if (res)
                                                    {

                                                        StaffList.UpdateStaffMultiple(columnsAndValues, tabNum);
                                                        StaffList.UpdateLastBotCommand(TelegramId, Bot_command_types.authentication_complete);

                                                        textAnswer = $"Сотруднику с табельным номером {tabNum} отключены напоминания";

                                                        await botClient.SendMessage(
                                                        chat.Id,
                                                        textAnswer, ParseMode.None,
                                                        message.MessageId, GetKeyboard("Управление сотрудниками")
                                                        );
                                                    }
                                                    else
                                                    {
                                                        textAnswer = $"Введен не верный табельный номер {textAnswer}";

                                                        await botClient.SendMessage(
                                                        chat.Id,
                                                        textAnswer, ParseMode.None,
                                                        message.MessageId, GetKeyboard("Управление сотрудниками")
                                                        );

                                                        await HandleNotificationIsUnnecessaryCommandAsync(botClient, message, TelegramId);
                                                    }

                                                }
                                                else if (lastBotCommand.Type == Bot_command_types.enter_request_datastart)
                                                {
                                                    bool res = requestsHolder.TryGetValue(TelegramId, out var value);
                                                    if (res)
                                                    {
                                                        res = DateTime.TryParseExact(message.Text, "dd.MM.yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime dateStart);
                                                        if (res)
                                                        {
                                                            if (dateStart < DateTime.Now.Date)
                                                            {
                                                                textAnswer = $"Задним числом нельзя вводить заявки";

                                                                await botClient.SendMessage(
                                                                chat.Id,
                                                                textAnswer, ParseMode.None,
                                                                message.MessageId, GetKeyboard("Создать заявку на отпуск")
                                                                );
                                                            }
                                                            else
                                                            {
                                                                requestsHolder[TelegramId].date_start = dateStart;
                                                                StaffList.UpdateLastBotCommand(TelegramId, Bot_command_types.enter_request_days);

                                                                textAnswer = $"Отлично. Теперь введите, на какое количество дней берете отпуск (числом)";
                                                                await botClient.SendMessage(
                                                                chat.Id,
                                                                textAnswer, ParseMode.None,
                                                                message.MessageId, GetKeyboard("Ввод заявки")
                                                                );
                                                            }
                                                        }
                                                        else
                                                        {
                                                            textAnswer = $"Неверный формат даты";
                                                            await botClient.SendMessage(
                                                            chat.Id,
                                                            textAnswer, ParseMode.None,
                                                            message.MessageId
                                                            );
                                                            await HandleRequestCreationCommandAsync(botClient, message, TelegramId);
                                                        }
                                                    }
                                                }
                                                else if (lastBotCommand.Type == Bot_command_types.enter_request_days)
                                                {
                                                    bool res = requestsHolder.TryGetValue(TelegramId, out var valueRequest);
                                                    if (res)
                                                    {
                                                        textAnswer = message.Text.Replace("/", "");
                                                        res = int.TryParse(textAnswer, out int daysQuantity);

                                                        if (res)
                                                        {
                                                            valueRequest.days_quantity = daysQuantity;

                                                            string createdRequest = StaffList.CreateNewRequest(valueRequest);
                                                            StaffList.UpdateLastBotCommand(TelegramId, Bot_command_types.authentication_complete);

                                                            textAnswer = $"Здорово! Заявка создана и отправлена начальнику на согласование. Вам придет уведомление о его решении. Ожидайте." +
                                                                $"\n{createdRequest}";
                                                            await botClient.SendMessage(
                                                            chat.Id,
                                                            textAnswer, ParseMode.None,
                                                            message.MessageId, GetKeyboard("Ввод заявки")
                                                            );

                                                            var bossList = StaffList.GetBosses();

                                                            if (bossList.Count > 0)
                                                            {
                                                                newText = $"Вам поступила новая заявка на отпуск от {currentUser.surname} {currentUser.name} ({currentUser.table_number})" +
                                                                    $"\n{createdRequest}" +
                                                                    $"\nПринять или отклонить можно в меню Заявки на согласование";
                                                                foreach (var boss in bossList)
                                                                {

                                                                    Console.WriteLine(newText);
                                                                    await botClient.SendMessage(
                                                                    boss.telegram_id,
                                                                    newText, ParseMode.None
                                                                    );
                                                                }
                                                            }

                                                        }
                                                        else
                                                        {
                                                            textAnswer = $"Ошибка. Вводить можно только целые числа";

                                                            await botClient.SendMessage(
                                                            chat.Id,
                                                            textAnswer, ParseMode.None,
                                                            message.MessageId, GetKeyboard("Ввод заявки")
                                                            );
                                                            await HandleRequestCreationCommandAsync(botClient, message, TelegramId);
                                                        }



                                                    }
                                                }
                                                else if (lastBotCommand.Type == Bot_command_types.enter_reason_of_cancell_request)
                                                {
                                                    await HandleEnterReasonOfCancellRequest(botClient, message, TelegramId, message.Text);

                                                    await HandlerRequestsAgreement(botClient, message, TelegramId);
                                                }
                                                else
                                                {

                                                    if (message.Text.Contains('?'))
                                                    {
                                                        textAnswer = $"Я пока не умею отвечать на вопросы {char.ConvertFromUtf32(0x1F601)}  Может Яндекс поможет...";
                                                        var hyperLinkKeyboard = new InlineKeyboardMarkup(InlineKeyboardButton.WithUrl("Яндекс.ру", "https://ya.ru/"));
                                                        await botClient.SendMessage(message.Chat, textAnswer, ParseMode.Markdown, message.MessageId, replyMarkup: hyperLinkKeyboard);

                                                    }
                                                    else
                                                    {
                                                        textAnswer = $"Однажды мой хозяин запрограммирует возможность общаться на разные темы.\n" +
                                                            $"А пока лови сердечко {char.ConvertFromUtf32(0x0001F4A9)}" +
                                                            $"\n Ой! Не то!{char.ConvertFromUtf32(0x0001F648)} Вот оно {char.ConvertFromUtf32(0x0001F496)}";

                                                        await botClient.SendMessage(message.Chat, textAnswer, ParseMode.Markdown, message.MessageId);
                                                    }

                                                    var startCommand = new CommandStart() { CurrentUser = currentUser };
                                                    await startCommand.SendAnswer(botClient, message);

                                                }

                                                break;

                                        }

                                    }
                                    else if (currentUser.table_number.GetValueOrDefault() == 0)
                                    {


                                        var lastBotCommand = StaffList.GetLastBotCommand(TelegramId);
                                        if (lastBotCommand.Type != Bot_command_types.enter_table_number)
                                        {
                                            newText = $"Здравствуйте, Коллега!\nДавайте познакомимся?\nВведите свой табельный номер (только числа), попробую идентифицировать Вас по нему";
                                            Console.WriteLine(newText);
                                            await botClient.SendMessage(
                                            message.Chat.Id,
                                            newText, ParseMode.None);

                                            StaffList.UpdateLastBotCommand(TelegramId, Bot_command_types.enter_table_number);

                                        }

                                        await HandleTableNumberCommand(botClient, message);


                                    }

                                    else if (string.IsNullOrEmpty(currentUser.name))
                                    {
                                        await HandleNameUserCommand(botClient, message, currentUser);
                                    }

                                }
                                else
                                {
                                    var lastBotCommand = StaffList.GetLastBotCommand(message.Chat.Id);
                                    if (lastBotCommand.Type != Bot_command_types.enter_table_number && lastBotCommand.Type != Bot_command_types.enter_table_number_error)
                                    {
                                        newText = $"Здравствуйте, Коллега!\nДавайте познакомимся?\nВведите свой табельный номер (только числа), попробую идентифицировать Вас по нему";
                                        Console.WriteLine(newText);
                                        await botClient.SendMessage(
                                        message.Chat.Id,
                                        newText, ParseMode.None);

                                        StaffList.UpdateLastBotCommand(message.Chat.Id, Bot_command_types.enter_table_number);

                                    }

                                    await HandleTableNumberCommand(botClient, message);



                                }


                                Console.WriteLine($"{user.FirstName} {user.LastName} ({user.Id}) написал сообщение: {message.Text}");

                            }

                            break;

                        }
                }




            }
            catch (OperationCanceledException ex)
            {
                Console.WriteLine("Отменяю задачу");
                await ErrorHandler(
                           botClient,
                ex,
                           token
                       );
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                await ErrorHandler(
                           botClient,
                ex,
                           token
                       );
            }



        }

        private async Task HandleEnterReasonOfCancellRequest(ITelegramBotClient botClient, Message message, long telegramId, string reason="не указана")
        {
            var currentRequest = requestsHolderForAgreement[telegramId];
            if (currentRequest != null)
            {
                string textAnswer = $"В Вашей заявке отказано. " +
                    $"\n {currentRequest.VacationTypeString}" +
                    $"\n Даты: {currentRequest.date_start.GetValueOrDefault().ToString("dd.MM.yyyy", CultureInfo.InvariantCulture)} - {currentRequest.date_end.GetValueOrDefault().ToString("dd.MM.yyyy", CultureInfo.InvariantCulture)}" +
                    $"\n Причина: {reason}";
                var staffPerson = StaffList.GetStaffPerson($"table_number={currentRequest.staff_table_number}");

                if (staffPerson != null)
                {

                    await botClient.SendMessage(
                    staffPerson.telegram_id,
                    textAnswer, ParseMode.None,
                    message.MessageId, GetKeyboard("Заявки на согласование")
                    );
                }
            }
        }

        public async Task HandleTableNumberCommand(ITelegramBotClient botClient, Message message)
        {
            string newText = "";

            if (!string.IsNullOrEmpty(message.Text))
            {


                bool res = int.TryParse(message.Text, out int tabNum);

                if (res)
                {

                    StaffPerson currentUser = StaffList.CheckUserForTableNumber(tabNum);
                    if (currentUser != null)
                    {

                        StaffList.UpdateStaffTelegram_id(message.From.Id, tabNum);
                        currentUser = StaffList.CheckUserForTableNumber(tabNum);

                        newText = $"Нашел Вас в базе. Здравствуйте, {currentUser.name}! Ваш TelegramId: {currentUser.telegram_id}";
                        Console.WriteLine(newText);
                        await botClient.SendMessage(
                        message.Chat.Id,
                        newText, ParseMode.None,
                        message.MessageId, GetKeyboard(newText)
                        );

                        var startCommand = new CommandStart() { CurrentUser = currentUser };
                        await startCommand.SendAnswer(botClient, message);
                    }
                    else
                    {
                        var lastBotCommand = StaffList.GetLastBotCommand(message.From.Id);
                        if (lastBotCommand.Type != Bot_command_types.enter_name)
                        {
                            newText = $"Не нашел Вас в базе. Введите свои ФИО через пробел (отчество не обязательно). Например, Иванов Иван Иванович";
                            Console.WriteLine(newText);
                            await botClient.SendMessage(
                            message.Chat.Id,
                            newText, ParseMode.None,
                            message.MessageId
                            );

                            StaffList.UpdateLastBotCommand(message.From.Id, Bot_command_types.enter_name);

                        }

                        currentUser = StaffList.CreateUser(tabNum, "", "", "", message.From.Id);
                    }

                }
                else
                {

                    var lastBotCommand = StaffList.GetLastBotCommand(message.Chat.Id);
                    if (lastBotCommand.Type != Bot_command_types.enter_table_number)
                    {
                        await botClient.SendMessage(
                        message.Chat.Id,
                        "Вводить можно только число. Попробуйте еще раз"
                        );

                    }
                    StaffList.UpdateLastBotCommand(message.Chat.Id, Bot_command_types.enter_table_number_error);

                }

            }

        }

        public async Task HandleNameUserCommand(ITelegramBotClient botClient, Message message, StaffPerson currentUser)
        {
            string newText = "";

            var lastBotCommand = StaffList.GetLastBotCommand(currentUser.telegram_id);
            if (lastBotCommand.Type != Bot_command_types.enter_name)
            {
                newText = "Введите свои ФИО через пробел (отчество не обязательно). Например, Иванов Иван Иванович";
                Console.WriteLine(newText);
                await botClient.SendMessage(
                message.Chat.Id,
                newText, ParseMode.None);

                StaffList.UpdateLastBotCommand(currentUser.telegram_id, Bot_command_types.enter_name);

            }



            if (!string.IsNullOrEmpty(message.Text))
            {


                string surnameUser = "";
                string nameUser = "";
                string patronymicUser = "";


                if (currentUser != null)
                {

                    string[] wordsFIO = message.Text.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                    string columnsAndValues = "";

                    if (wordsFIO.Length > 2)
                    {
                        surnameUser = wordsFIO[0];
                        nameUser = wordsFIO[1];
                        patronymicUser = wordsFIO[2];

                    }
                    else if (wordsFIO.Length == 2)
                    {
                        surnameUser = wordsFIO[0];
                        nameUser = wordsFIO[1];
                    }
                    else
                    {
                        surnameUser = wordsFIO[0];
                        nameUser = wordsFIO[0];
                    }


                    columnsAndValues = $"name='{nameUser}',surname='{surnameUser}',patronymic='{patronymicUser}'";



                    StaffList.UpdateStaffMultiple(columnsAndValues, currentUser.table_number);
                    currentUser = StaffList.CheckUserForTableNumber(currentUser.table_number);

                    newText = $"Занес Вас в базу. Здравствуйте, {currentUser.name}! Ваш TelegramID: {currentUser.telegram_id}";
                    Console.WriteLine(newText);
                    await botClient.SendMessage(
                    message.Chat.Id,
                    newText, ParseMode.None,
                    message.MessageId
                    );

                    await SendNotificationToBoss(botClient, currentUser);
                }


            }
            else
            {
                await botClient.SendMessage(
                message.Chat.Id,
                "Введите текст"
                );
            }

        }


        public async Task SendNotificationToBoss(ITelegramBotClient botClient, StaffPerson currentUser)
        {
            var bossList = StaffList.GetBosses();

            if (bossList.Count > 0)
            {
                string newText = $"Зарегистрирован новый пользователь: {currentUser.surname} {currentUser.name}! Табельный номер: {currentUser.table_number}";

                foreach (var boss in bossList)
                {

                    Console.WriteLine(newText);
                    await botClient.SendMessage(
                    boss.telegram_id,
                    newText, ParseMode.None
                    );
                }
            }

        }

        public static async Task SendNotificationIfNecessaryAsync(ITelegramBotClient botClient)
        {

            var cts = new CancellationTokenSource();
            
            var todayIsWeekDay = await client.GetFromJsonAsync<int>("https://isdayoff.ru/today", cts.Token);

            if (todayIsWeekDay == 0)
            {

                string nowDateTime = DateTime.Now.ToString("HH:mm", CultureInfo.InvariantCulture);
                DateTime currentTime = DateTime.ParseExact(nowDateTime, "HH:mm", CultureInfo.InvariantCulture);

                DateTime timeForNotification = DateTime.ParseExact(Program.timeForNotification, "HH:mm", CultureInfo.InvariantCulture);

                if (currentTime >= timeForNotification)
                {

                    var staffListForNotification = StaffList.GetStaffIfNotificationIsNecessary();
                    
                    string newText = "Если Вы сегодня не в офисе, введите свой статус, пожалуйста";

                    Console.WriteLine($"{DateTime.Now.ToString("dd.MM.yyy HH:mm:ss", CultureInfo.InvariantCulture)} Отправляю напоминание особым пользователям для ввода своего сегодняшнего статуса");

                    foreach (var staff in staffListForNotification)
                    {

                        Console.WriteLine($"{staff.surname} {staff.name} (таб. номер: {staff.table_number}) {newText}");
                        await botClient.SendMessage(
                        staff.telegram_id,
                        newText, ParseMode.None, null, GetKeyboard("Отметка статуса")
                        );
                        staff.UpdateStatus("Не отметился");


                    }

                    Console.WriteLine("Отправил напоминание");

                    var staffList = StaffList.UpdateAllStatusForUnnecessary();
                    if (staffList.Count > 0)
                    {
                        Console.WriteLine($"{DateTime.Now.ToString("dd.MM.yyy HH:mm:ss", CultureInfo.InvariantCulture)} Устанавливаю всем сотрудникам статус В офисе, которые не в отпуске и не отмечены для самостоятельного указания статуса");
                    }
                    foreach (var staff in staffList)
                    {
                        staff.UpdateStatus("/Office");
                        Console.WriteLine($" {staff.surname} {staff.name} (таб. номер: {staff.table_number})");
                    }
                }

            }
        }


        public static ReplyMarkup? GetKeyboard(string srtCase)

        {
            var Buttons = new ReplyKeyboardMarkup();


            switch (srtCase)
            {
                case "Мой статус на сегодня":

                    Buttons = new ReplyKeyboardMarkup(new[]
                    {
                    new[]
                    {
                        new KeyboardButton ("Изменить статус"),
                        new KeyboardButton ("Выйти")

                    }

                    });

                    Buttons.ResizeKeyboard = true;

                    return Buttons;
                case "Заявки на согласование":

                    Buttons = new ReplyKeyboardMarkup(new[]
                   {
                    new[]
                    {
                        new KeyboardButton ("Предыдущая"),
                        new KeyboardButton ("Следующая"),

                    },
                    new[]
                    {
                         new KeyboardButton ("Принять"),
                         new KeyboardButton ("Отказать"),
                    },
                    new[]
                    {
                         new KeyboardButton ("Выйти")
                    }

                    });

                    Buttons.ResizeKeyboard = true;

                    return Buttons;

                case "Заявки":
                    Buttons = new ReplyKeyboardMarkup(new[]
                   {
                    new[]
                    {
                        new KeyboardButton ("Заявки на согласование"),
                        new KeyboardButton ("Заявки все")

                    },
                    new[]
                    {
                        new KeyboardButton ("Создать заявку на отпуск")
                    },
                    new[]
                    {
                         new KeyboardButton ("Выйти")
                    }

                    });

                    Buttons.ResizeKeyboard = true;

                    return Buttons;

                case "Управление сотрудниками":
                    Buttons = new ReplyKeyboardMarkup(new[]
                  {
                    new[]
                    {
                        new KeyboardButton ("Список сотрудников"),
                        new KeyboardButton ("Установить напоминания сотрудникам"),
                        new KeyboardButton ("Снять напоминания сотрудникам"),
                    },
                    new[]
                    {
                         new KeyboardButton ("Выйти")
                    }

                    });

                    Buttons.ResizeKeyboard = true;

                    return Buttons;
                case "Создать заявку на отпуск":
                    Buttons = new ReplyKeyboardMarkup(new[]
                    {
                    new[]
                    {
                        new KeyboardButton ("Сегодня"),
                        new KeyboardButton ("Выйти")
                    }

                    });

                    Buttons.ResizeKeyboard = true;

                    return Buttons;
                case "Отчет по явке сотрудников":

                    Buttons = new ReplyKeyboardMarkup(new[]
                 {
                    new[]
                    {
                        new KeyboardButton ("За сегодня"),
                        new KeyboardButton ("За неделю"),
                        new KeyboardButton ("За месяц"),

                    },
                    new[]
                    {
                         new KeyboardButton ("Выйти")
                    }

                    });

                    Buttons.ResizeKeyboard = true;

                    return Buttons;
                case "Предыдущая":

                    Buttons = new ReplyKeyboardMarkup(new[]
                   {
                    new[]
                    {
                        new KeyboardButton ("Предыдущая"),
                        new KeyboardButton ("Следующая"),

                    },
                    new[]
                    {
                         new KeyboardButton ("Принять"),
                         new KeyboardButton ("Отказать"),
                    },
                    new[]
                    {
                         new KeyboardButton ("Выйти")
                    }

                    });

                    Buttons.ResizeKeyboard = true;

                    return Buttons;
                case "Следующая":

                    Buttons = new ReplyKeyboardMarkup(new[]
                   {
                    new[]
                    {
                        new KeyboardButton ("Предыдущая"),
                        new KeyboardButton ("Следующая"),

                    },
                    new[]
                    {
                         new KeyboardButton ("Принять"),
                         new KeyboardButton ("Отказать"),
                    },
                    new[]
                    {
                         new KeyboardButton ("Выйти")
                    }

                    });

                    Buttons.ResizeKeyboard = true;

                    return Buttons;
                case "Принять":

                    Buttons = new ReplyKeyboardMarkup(new[]
                   {
                    new[]
                    {
                        new KeyboardButton ("Предыдущая"),
                        new KeyboardButton ("Следующая"),

                    },
                    new[]
                    {
                         new KeyboardButton ("Принять"),
                         new KeyboardButton ("Отказать"),
                    },
                    new[]
                    {
                         new KeyboardButton ("Выйти")
                    }

                    });

                    Buttons.ResizeKeyboard = true;

                    return Buttons;
                case "Отказать":

                    Buttons = new ReplyKeyboardMarkup(new[]
                   {
                    new[]
                    {
                        new KeyboardButton ("Предыдущая"),
                        new KeyboardButton ("Следующая"),

                    },
                    new[]
                    {
                         new KeyboardButton ("Принять"),
                         new KeyboardButton ("Отказать"),
                    },
                    new[]
                    {
                         new KeyboardButton ("Выйти")
                    }

                    });

                    Buttons.ResizeKeyboard = true;

                    return Buttons;
                case "Отпуска":

                    Buttons = new ReplyKeyboardMarkup(new[]
                 {
                    new[]
                    {
                        new KeyboardButton ("Мой отпуск плановый"),
                        new KeyboardButton ("Отпуска отдела")

                    },
                    new[]
                    {
                         new KeyboardButton ("Выйти")
                    }

                    });

                    Buttons.ResizeKeyboard = true;

                    return Buttons;
                case "Ввод заявки":

                    Buttons = new ReplyKeyboardMarkup(new[]
                    {

                    new[]
                    {
                         new KeyboardButton ("Выйти")
                    }

                    });

                    Buttons.ResizeKeyboard = true;

                    return Buttons;
                case "Отметка статуса":

                    Buttons = new ReplyKeyboardMarkup(new[]
                    {
                    new[]
                    {
                         new KeyboardButton ("Да, я в офисе"),
                         new KeyboardButton ("Изменить статус")
                    },
                    new[]
                    {

                        new KeyboardButton ("Выйти")

                    }

                    });

                    Buttons.ResizeKeyboard = true;

                    return Buttons;

                default:
                    return null;

            }



        }

        public async Task HandleNotificationIsUnnecessaryCommandAsync(ITelegramBotClient botClient, Message message, long telegramId)
        {

            string textAnswer = "Вывожу список сотрудников отдела, а Вы выберите табельный номер, кому больше НЕ ПРИСЫЛАТЬ напоминания";
            var staffList = StaffList.GetStaff("WHERE reminder_is_necessary");


            textAnswer = textAnswer + '\n' + String.Join(";", staffList.Select(b => "\n" + "Таб.номер /" + b.table_number + "\n" + b.surname + " " + b.name + " " + b.patronymic + ", Роль: " + b.rule + ", Напоминание присылать: " + b.reminder_is_necessary));

            if (staffList.Count == 0) { textAnswer = textAnswer + '\n' + "Не найдено пользователей, у которых включено напоминание"; }

            Console.WriteLine(textAnswer);

            await botClient.SendMessage(
            message.Chat.Id,
            textAnswer, ParseMode.Markdown,
            message.MessageId, GetKeyboard("Управление сотрудниками")
            );

            StaffList.UpdateLastBotCommand(telegramId, Bot_command_types.enter_notification_unnecessary);




        }

        public async Task HandleNotificationIsNecessaryCommandAsync(ITelegramBotClient botClient, Message message, long telegramId)
        {
            string textAnswer = "Вывожу список сотрудников отдела, а Вы выберите табельный номер, кому придется ПОЛУЧАТЬ напоминания";
            var staffList = StaffList.GetStaff("WHERE not reminder_is_necessary");


            textAnswer = textAnswer + '\n' + String.Join(";", staffList.Select(b => "\n" + "Таб.номер /" + b.table_number + "\n" + b.surname + " " + b.name + " " + b.patronymic + ", Роль: " + b.rule + ", Напоминание присылать: " + b.reminder_is_necessary));
            if (staffList.Count == 0) { textAnswer = textAnswer + '\n' + "Все пользователи уже получают напоминания! Вы можете только отключить его у кого-нибудь из них"; }

            Console.WriteLine(textAnswer);

            await botClient.SendMessage(
            message.Chat.Id,
            textAnswer, ParseMode.Markdown,
            message.MessageId, GetKeyboard(message.Text)
            );

            StaffList.UpdateLastBotCommand(telegramId, Bot_command_types.enter_notification_necessary);


        }

        private async Task HandleRequestCreationCommandAsync(ITelegramBotClient botClient, Message message, long telegramId)
        {

            string textAnswer = "Для создания новой заявки на отпуск укажите тип отпуска:" +
                                                    "\n /RemoteWork - На удаленке" +
                                                    "\n /Vacation - В oтпуске oфициально" +
                                                    "\n /Family - Отсутствую по семейным обстоятельствам" +
                                                    "\n /Medical - На больничном" +
                                                    "\n /BusinessTrip - В командировке" +
                                                    "\n /OnStudy - На учебе";

            Console.WriteLine(textAnswer);

            await botClient.SendMessage(
            message.Chat.Id,
            textAnswer, ParseMode.Markdown,
            message.MessageId, GetKeyboard("Ввод заявки")
            );

            StaffList.UpdateLastBotCommand(telegramId, Bot_command_types.enter_request_type);

        }


        public string SetStatusOffice(StaffPerson currentUser, string messageText)
        {
            var vacationType = currentUser.UpdateStatus(messageText);

            StaffList.UpdateLastBotCommand(currentUser.telegram_id, Bot_command_types.authentication_complete);
            string textAnswer = "";
            if (vacationType != null)
            {
                textAnswer = "Тебе повезло - ты не такой как все! Ты рааааботаешь в ЦКБ!!! (Центральное Красное&Белое)" +
                $"\nЗаписал новый статус: {vacationType.name} ({vacationType.table_code.ToString()})";

            }
            return textAnswer;
        }

        public async Task HandlerRequestsAgreement(ITelegramBotClient botClient, Message message, long telegramId, int indexRequest = 0)
        {
            var requestsList = StaffList.GetRequestsForAgreement();
            requestsHolderList[telegramId] = requestsList;

            string textAnswer = "Заявок на согласование не найдено";
            string KeyboardCode = "Заявки";
            if (requestsList.Count > 0)
            {
                textAnswer = $"Найдено заявок на согласование: {requestsList.Count}" +
                    $"\n Id:{requestsList[indexRequest].id} {requestsList[indexRequest].Surname} {requestsList[indexRequest].Name}" +
                    $"\n {requestsList[indexRequest].VacationTypeString}" +
                    $"\n Даты: {requestsList[indexRequest].date_start.GetValueOrDefault().ToString("dd.MM.yyyy", CultureInfo.InvariantCulture)} - {requestsList[indexRequest].date_end.GetValueOrDefault().ToString("dd.MM.yyyy", CultureInfo.InvariantCulture)}" +
                    $"\n Всего дней: {requestsList[indexRequest].days_quantity}";
                KeyboardCode = message.Text;

                requestsHolderForAgreement[telegramId] = requestsList[indexRequest];
            }


            Console.WriteLine(textAnswer);

            await botClient.SendMessage(
            message.Chat.Id,
            textAnswer, ParseMode.None,
            message.MessageId, GetKeyboard(KeyboardCode)
            );
        }
    }
}
