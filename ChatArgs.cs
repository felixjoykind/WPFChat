using System;

namespace WPFChat
{
    public class ChatArgs
    {
        public Client Sender { get; private set; }
        public string Message { get; private set; }

        public ChatArgs(Client sender, string message)
        {
            Sender = sender;
            Message = message;
        }
    }
}
