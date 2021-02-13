using PerchikSharp.Events;

namespace PerchikSharp.Commands
{
    interface INativeCommand
    {
        string Command { get; }
        void OnExecution(object sender, CommandEventArgs command);
    }
}
