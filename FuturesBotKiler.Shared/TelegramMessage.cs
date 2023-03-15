using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Telegram.Bot;

namespace FuturesBotKiler.Shared
{
    public static class TelegramMessage
    {
        private static ITelegramBotClient botClient;

        public static void Message(string message)
        {
            botClient = new TelegramBotClient(Parametros.TelegramKey);

            botClient.SendTextMessageAsync(
                chatId: 326629231, //YO
                text: $"1 {message}"
            );

            botClient.SendTextMessageAsync(
                chatId: 692786235, //KILER
                text: $"{message}"
            );
        }
    }
}
