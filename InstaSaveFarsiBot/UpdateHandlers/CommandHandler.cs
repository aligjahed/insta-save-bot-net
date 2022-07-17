using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using InstaSaveFarsiBot.States;
using InstaSaveFarsiBot.Utility;

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

        static async Task<Message> getPost(ITelegramBotClient botClient, Message message)
        {
            var igd = new IGDownloader();

            try
            {
                string link = await igd.GetLink(message.Text);
                return await botClient.SendTextMessageAsync(chatId: message.Chat.Id, text: link);
            }
            catch (Exception exception)
            {
                Console.WriteLine(exception);
                return await botClient.SendTextMessageAsync(chatId: message.Chat.Id, text: "there was an error");
            }
            finally
            {
                state.isDownload = false;
            }


        }

        static async Task<Message> Usage(ITelegramBotClient botClient, Message message)
        {

            const string usage = "Usage:\n" +
                                 "/start   - send inline keyboard\n" +
                                 "/download  - request location or contact";

            return await botClient.SendTextMessageAsync(chatId: message.Chat.Id,
                                                        text: usage,
                                                        replyMarkup: new ReplyKeyboardRemove());
        }
    }

    // Process Inline Keyboard callback data
    private static async Task BotOnCallbackQueryReceived(ITelegramBotClient botClient, CallbackQuery callbackQuery)
    {
        Message dataTransfer = new Message { Text = callbackQuery.Data };

        await BotOnMessageReceived(botClient, dataTransfer);

    }

    private static Task UnknownUpdateHandlerAsync(ITelegramBotClient botClient, Update update)
    {
        Console.WriteLine($"Unknown update type: {update.Type}");
        return Task.CompletedTask;
    }
}