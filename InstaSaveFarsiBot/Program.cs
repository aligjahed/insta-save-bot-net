﻿using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types.Enums;
using DotNetEnv;
using InstaSaveFarsiBot.Config;
using InstaSaveFarsiBot.UpdateHandlers;
using InstaSaveFarsiBot.States;

var state = new ConversationState();

// Load Enviremont variables from .env file
if (state.isDevelopment == true) Env.TraversePath().Load();



var bot = new TelegramBotClient(BotConfiguration.BotToken);

var me = await bot.GetMeAsync();
Console.Title = me.Username ?? "Insta Save Farsi";

using var cts = new CancellationTokenSource();

// StartReceiving does not block the caller thread. Receiving is done on the ThreadPool.
var receiverOptions = new ReceiverOptions()
{
    AllowedUpdates = Array.Empty<UpdateType>(),
    ThrowPendingUpdates = true,
};

bot.StartReceiving(updateHandler: CommandHandler.HandleUpdateAsync,
               pollingErrorHandler: CommandHandler.PollingErrorHandler,
               receiverOptions: receiverOptions,
               cancellationToken: cts.Token);



Console.WriteLine($"Start listening for @{me.Username}");
Task.Delay(int.MaxValue).Wait();
Console.ReadLine();

// Send cancellation request to stop bot
cts.Cancel();