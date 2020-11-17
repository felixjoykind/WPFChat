using System;

namespace WPFChat
{
    public class ServerArgs
    {
        public Server Sender { get; private set; }
        public Client Client { get; private set; }

        public ServerArgs(Server sender, Client client)
        {
            Sender = sender;
            Client = client;
        }
    }
}
