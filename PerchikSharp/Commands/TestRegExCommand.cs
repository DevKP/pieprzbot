using System;
using System.Collections.Generic;
using System.Text;
using PerchikSharp.Events;
using Telegram.Bot;

namespace PerchikSharp.Commands
{
    class TestRegExCommand : IRegExCommand
    {
        public string RegEx => @"Pa$$word";

        public void OnExecution(object sender, RegExArgs command)
        {
        }
    }
}
