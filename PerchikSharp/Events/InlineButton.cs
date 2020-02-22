using System;
using System.Collections.Generic;
using System.Text;

namespace PerchikSharp
{
    class InlineButton
    {
        public InlineButton(string data, int userid = 0, object obj = null) =>
            (Data, UserId, Arg) = (data, userid, obj);
        public string Data { get; }
        public int UserId { get; }
        public object Arg { get; }
    }
}
