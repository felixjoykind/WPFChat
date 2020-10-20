using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Windows;

namespace WPFChat
{
    public class Client
    {
        public Socket Socket { get; private set; }
        public string Id { get; private set; }
        public string Username;
        public bool isConnected = false;

        public Client(Socket socket)
        {
            Id = Guid.NewGuid().ToString();
            Socket = socket;
        }
        public Client(Socket socket, string username)
        {
            Id = Guid.NewGuid().ToString();
            Socket = socket;
            Username = username;
        }

        public bool Connect(int port, IPAddress address)
        {
            if (isConnected) return false;

            Socket.Connect(address, port);
            isConnected = true;
            SendMessage(Username);
            return true;
        }

        public void Disconnect()
        {
            if (!isConnected) return;

            Socket.Shutdown(SocketShutdown.Both);
            Socket.Close();
            isConnected = false;
        }

        public void SendMessage(string message)
        {
            if (!isConnected) return;

            try
            {
                var data = Encoding.UTF8.GetBytes(message);
                Socket.Send(data);
            }
            catch (Exception)
            {
                MessageBox.Show("Connection aborted!", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                Disconnect();
            }
        }
    }
}
