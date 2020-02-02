using System;
using System.Collections.Generic;
using System.Text;

namespace PerchikSharp
{
    class InlineButton
    {
        public InlineButton(string data, int userid = 0)
        { Data = data; UserId = userid; }
        public string Data { get; }
        public int UserId { get; }
    }
}
