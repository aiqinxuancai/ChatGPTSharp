using System;
using System.Collections.Generic;
using System.Text;

namespace ChatGPTSharp.Model
{

    public class ChatGPTException : Exception
    {
        public ChatGPTException()
        {
        }

        public ChatGPTException(string message)
            : base(message)
        {
        }

    }
}
