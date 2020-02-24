using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace PerchikSharp.Commands
{
    class DeleteCommand : INativeCommand
    {
        const int via_tcp_Id = 204678400;
        public string Command { get { return "delete"; } }
        public async void OnExecution(object sender, CommandEventArgs command)
        {
            var bot = sender as Pieprz;
            var msg = command.Message;

            if (msg.ReplyToMessage == null)
                return;

            if(bot.isUserAdmin(command.Message.Chat.Id, command.Message.From.Id) ||
                command.Message.From.Id == via_tcp_Id)
            {
                await bot.DeleteMessageAsync(msg.Chat.Id, msg.MessageId);
                await bot.DeleteMessageAsync(msg.Chat.Id, msg.ReplyToMessage.MessageId);
            }
        }
    }
}
