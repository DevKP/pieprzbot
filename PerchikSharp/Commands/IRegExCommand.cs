using PerchikSharp.Events;

namespace PerchikSharp.Commands
{
    interface IRegExCommand
    {
        public string RegEx { get; }
        public void OnExecution(object sender, RegExArgs command);
    }
}
