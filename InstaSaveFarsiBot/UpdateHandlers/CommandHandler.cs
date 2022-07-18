using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using InstaSaveFarsiBot.States;
using InstaSaveFarsiBot.Utility;
using Newtonsoft.Json.Linq;

namespace InstaSaveFarsiBot.UpdateHandlers;

public static class CommandHandler
{
    static readonly ConversationState state = new ConversationState();

    public static Task PollingErrorHandler(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
    {
        var ErrorMessage = exception switch
        {
            ApiRequestException apiRequestException => $"Telegram API Error:\n[{apiRequestException.ErrorCode}]\n{apiRequestException.Message}",
            _ => exception.ToString()
        };

        Console.WriteLine(ErrorMessage);
        return Task.CompletedTask;
    }

    public static async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
    {
        var handler = update.Type switch
        {
            UpdateType.Message => BotOnMessageReceived(botClient, update.Message!),
            UpdateType.CallbackQuery => BotOnCallbackQueryReceived(botClient, update.CallbackQuery!),
        };



        try
        {
            await handler;
        }
#pragma warning disable CA1031
        catch (Exception exception)
#pragma warning restore CA1031
        {
            await PollingErrorHandler(botClient, exception, cancellationToken);
        }
    }

    private static async Task BotOnMessageReceived(ITelegramBotClient botClient, Message message)
    {
        if (message.Text is not { } messageText)
            return;

        // Removed because the Telegram.Bot library loop cycle default switch expression after state change
        //var action = messageText.Split(' ')[0] switch
        //{
        //    "/start" => onStart(botClient, message),
        //    "/download" => onDownload(botClient, message),
        //    _ => if () { Usage(botClient, message)}
        //};



        if (state.isDownload == true && message.Text != "/download")
        {
            await getPost(botClient, message);
        }
        else
        {
            if (messageText == "/start")
            {
                Message sentMessage = await onStart(botClient, message);
            }
            else if (messageText == "/download")
            {
                Message sentMessage = await onDownload(botClient, message);
            }
            else if (messageText == "/help")
            {
                Message sentMessage = await onHelp(botClient, message);
            }
            else if (state.isDownload != true)
            {
                Message sentMessage = await Usage(botClient, message);

            }
        }


        static async Task<Message> onStart(ITelegramBotClient botClient, Message message)
        {

            InlineKeyboardMarkup inlineKeyboardMarkup = new(
                new[]
                {
                    InlineKeyboardButton.WithCallbackData("دریافت پست" , "/download"),
                    InlineKeyboardButton.WithCallbackData("راهنما" , "/help"),
                }
                );

            return await botClient.SendTextMessageAsync(
                chatId: message.Chat.Id,
                text: "⭕️❗️اینستاگرام دانلودر فارسی❗️⭕ \n\n 🔹به ربات Insta Save Farsi خوش آمدید. \n\n 🔵 @InstaSaveFarsi_bot 🔵 ",
                replyMarkup: inlineKeyboardMarkup
                );
        }

        static async Task<Message> onDownload(ITelegramBotClient botClient, Message message)
        {

            string newMessage = "لطفا لینک پست مورد نظر را ارسال کنید. 🌐";

            state.isDownload = true;

            return await botClient.SendTextMessageAsync(
                chatId: message.Chat.Id,
                text: newMessage
                );

        }

        static async Task<Message> onHelp(ITelegramBotClient botClient, Message message)
        {
            string captionText = "🔴 ابتدا لینک پست مورد نظر را با طی کردن مراحل مشخص شده در عکس کپی کنید. ( توجه کنید پست انتخابی نباید از یک پیج خصوصی باشد ) \n\n" +
                                 "🔵 سپس با ارسال دستور /download و یا انتخاب آن از منوی دستورات منتظر پاسخ ربات باشید. \n\n" +
                                 "🟢 حالا با ارسال لینک کپی شده در مراحل قبل برای ربات و کمی انتظار محتوای پست مورد نظر را دریافت و در گالری ذخیره کنید. \n\n" +
                                 "⭕️❗️اینستاگرام دانلودر فارسی❗️⭕️ \n\n" +
                                 "🔵 @InstaSaveFarsi_bot 🔵";

            InlineKeyboardMarkup inlineKeyboard = new(
                new[]
                {
                    InlineKeyboardButton.WithCallbackData("دریافت پست" , "/download")
                }
                );

            return await botClient.SendPhotoAsync(
                chatId: message.Chat.Id,
                photo: "https://www.dropbox.com/s/zdj91kp22ftbiwz/MainHelp.jpg?raw=1",
                caption: captionText,
                replyMarkup: inlineKeyboard
                );
        }

        static async Task<Message> getPost(ITelegramBotClient botClient, Message message)
        {
            string captionText = "⭕️❗️اینستاگرام دانلودر فارسی❗️⭕ \n\n 🔵 @InstaSaveFarsi_bot 🔵";

            await botClient.SendTextMessageAsync(
                chatId: message.Chat.Id,
                text: "لطفا صبر کنید... ⚠️"
                );

            var igd = new IGDownloader();

            try
            {
                string link = await igd.GetLink(url: message.Text);

                JObject linkOBJ = JObject.Parse(link);
                string linkType = (string)linkOBJ["type"];


                if (linkType == "video")
                {
                    return await botClient.SendVideoAsync(
                        chatId: message.Chat.Id,
                        video: (string)linkOBJ["video_url"],
                        supportsStreaming: true,
                        caption: captionText
                        );
                }
                else if (linkType == "photo")
                {
                    return await botClient.SendPhotoAsync(
                        chatId: message.Chat.Id,
                        photo: (string)linkOBJ["photo_url"],
                        caption: captionText
                        );
                }


            }
            catch (Exception exception)
            {
                Console.WriteLine(exception);
                return await botClient.SendTextMessageAsync(
                    chatId: message.Chat.Id,
                    text: "مشکلی رخ داد... مجددا تلاش کنید. 🚫");
            }
            finally
            {
                state.isDownload = false;
            }

            Message emptyMessage = new Message { Text = "Completed" };
            return emptyMessage;
        }

        static async Task<Message> Usage(ITelegramBotClient botClient, Message message)
        {

            const string usage = "🚫دستور یافت نشد  \n\n" +
                                 "⚠️ لیست دستورات : \n\n" +
                                 "/download  - ذخیره محتوای مورد نظر \n" +
                                 "/help   - راهنمای استفاده از ربات";

            return await botClient.SendTextMessageAsync(chatId: message.Chat.Id,
                                                        text: usage,
                                                        replyMarkup: new ReplyKeyboardRemove());
        }
    }

    // Process Inline Keyboard callback data
    private static async Task BotOnCallbackQueryReceived(ITelegramBotClient botClient, CallbackQuery callbackQuery)
    {
        string DLMessage = "لطفا لینک پست مورد نظر را ارسال کنید. 🌐";

        await botClient.AnswerCallbackQueryAsync(
            callbackQueryId: callbackQuery.Id
            );

        if (callbackQuery.Data == "/download")
        {
            await botClient.SendTextMessageAsync(
            chatId: callbackQuery.Message!.Chat.Id,
            text: DLMessage
            );
        }
        else if (callbackQuery.Data == "/help")
        {
            string captionText = "🔴 ابتدا لینک پست مورد نظر را با طی کردن مراحل مشخص شده در عکس کپی کنید. ( توجه کنید پست انتخابی نباید از یک پیج خصوصی باشد ) \n\n" +
                     "🔵 سپس با ارسال دستور /download و یا انتخاب آن از منوی دستورات منتظر پاسخ ربات باشید. \n\n" +
                     "🟢 حالا با ارسال لینک کپی شده در مراحل قبل برای ربات و کمی انتظار محتوای پست مورد نظر را دریافت و در گالری ذخیره کنید. \n\n" +
                     "⭕️❗️اینستاگرام دانلودر فارسی❗️⭕️ \n\n" +
                     "🔵 @InstaSaveFarsi_bot 🔵";

            InlineKeyboardMarkup inlineKeyboard = new(
                new[]
                {
                    InlineKeyboardButton.WithCallbackData("دریافت پست" , "/download")
                }
                );

            await botClient.SendPhotoAsync(
                chatId: callbackQuery.Message!.Chat.Id,
                photo: "https://www.dropbox.com/s/zdj91kp22ftbiwz/MainHelp.jpg?raw=1",
                caption: captionText,
                replyMarkup: inlineKeyboard
                );
        }

        Message dataTransfer = new Message { Text = callbackQuery.Data };
        await BotOnMessageReceived(botClient, dataTransfer);

    }

    private static Task UnknownUpdateHandlerAsync(ITelegramBotClient botClient, Update update)
    {
        Console.WriteLine($"Unknown update type: {update.Type}");
        return Task.CompletedTask;
    }
}