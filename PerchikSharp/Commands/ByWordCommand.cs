using PerchikSharp.Events;

namespace PerchikSharp.Commands
{
    class ByWordCommand : IRegExCommand
    {
        public string RegEx => @".*?((б)?[еeе́ė]+л[оoаaа́â]+[pр][уyу́]+[cсċ]+[uи́иеe]+[я́яию]+).*?";

        public void OnExecution(object sender, RegExArgs command)
        {
        }
    }
}
