using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace WPFChat
{
    public class Server
    {
        public delegate void ServerEventHandler(object sender, ServerArgs sa);
        public delegate void ChatEventHandler(object sender, ChatArgs sa);

        public Socket Socket { get; private set; }
        public List<Client> Clients { get; private set; }
        public int Port { get; private set; }
        private readonly byte[] buffer = new byte[1024];

        public bool isRunning { get; private set; } = false;

        public event ServerEventHandler OnClientConnected;
        public event ServerEventHandler OnClientDisconnected;
        public event ServerEventHandler OnServerStarted;
        public event ServerEventHandler OnServerClosed;
        public event ChatEventHandler OnMessageRecieved;

        public Server(int port)
        {
            Port = port;
            Socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            Clients = new List<Client>();
        }

        public void Start()
        {
            if (isRunning) return;

            Socket.Bind(new IPEndPoint(IPAddress.Any, Port));
            Socket.Listen(5);

            isRunning = true;
            Socket.BeginAccept(AcceptCallback, null); // починаємо прослуховування (як тільки хтось спробує підключитися викличеться метод AcceptCallback)
            OnServerStarted?.Invoke(this, null);
        }

        public void Stop()
        {
            if (!isRunning) return;

            RemoveAllConnections(); // відключаємо всіх клієнтів

            Socket.Close(); // закриваємо сокет сервера
            isRunning = false;
            OnServerClosed?.Invoke(this, null);
        }

        private void AcceptCallback(IAsyncResult ar)
        {
            // цей блок поміщено в try...catch тому що при закритті сервера прослуховування залишається відкритим
            // і дані передаються некоректно, що викликає помилку. Це нормално, тому ми ніяк її не обробляємо
            try
            {
                Client newClient = new Client(Socket.EndAccept(ar)); // створюємо нового клієнта з отриманим сокетом
                string clientUsername = GetClientMessage(newClient); // отримуємо нікнейм від клієнта, який щойно підключився
                newClient.Username = clientUsername;
                newClient.isConnected = true;
                Clients.Add(newClient);

                Thread clientThread = new Thread(HandleClient); // створюємо новий потік для обробки даних, які надсилає клієнт (метоl HandleClient)
                clientThread.Start(newClient); // запускаємо потік клієнта з вхідним параметром newClient
                BroadcastMessage($"{clientUsername} has joined the chat!", newClient.Id);
                OnClientConnected?.Invoke(this, new ServerArgs(this, newClient));
                Socket.BeginAccept(AcceptCallback, null); // починаємо прослуховування знову
            }
            catch (Exception) { }
        }
        private void HandleClient(object client)
        {
            Client clientObject = (Client)client; // приводимо параметр client до користувацького типу Client
            try
            {
                while (isRunning && clientObject.isConnected) // поки сервер працює (isRunning) та клієнта підключено(clientObject.isConnected)
                {
                    string message = GetClientMessage(clientObject);
                    // відключаємо клієнта, якщо отримали пусте повідомлення (клієнт не можу відкправити пустоту)
                    if (message.Trim().Length == 0)
                    {
                        RemoveConnection(clientObject.Id);
                        break;
                    }

                    OnMessageRecieved?.Invoke(this, new ChatArgs(clientObject, message));
                    BroadcastMessage($"{clientObject.Username}: {message}", clientObject.Id);
                }
                RemoveConnection(clientObject.Id);
            }
            catch (Exception)
            {
                RemoveConnection(clientObject.Id);
            }
        }

        private string GetClientMessage(Client client)
        {
            var buffer = new byte[1024]; // буфер для отриманих даних
            int bytes = client.Socket.Receive(buffer);
            string message = Encoding.UTF8.GetString(buffer, 0, bytes);
            return message;
        }

        private void RemoveAllConnections()
        {
            List<string> ids = new List<string>(); // список всіх id
            foreach (var c in Clients)
                ids.Add(c.Id);
            foreach (var id in ids)
                RemoveConnection(id);
        }

        private void RemoveConnection(string id)
        {
            Client clientToRemove = Clients.FirstOrDefault(c => c.Id == id);
            if (clientToRemove != null) // якщо клієнт з таким id існує
            {
                clientToRemove.Disconnect();
                OnClientDisconnected?.Invoke(this, new ServerArgs(this, clientToRemove));
                BroadcastMessage($"{clientToRemove.Username} has left the chat!", clientToRemove.Id);
                Clients.Remove(clientToRemove);
            }
        }

        private void BroadcastMessage(string message, string id)
        {
            var sender = Clients.FirstOrDefault(c => c.Id == id);
            if (sender == null) return;

            foreach (var client in Clients)
            {
                if (client == sender)
                    continue;
                else
                {
                    var data = Encoding.UTF8.GetBytes($"{message}");
                    client.Socket.Send(data);
                }
            }
        }
    }
}
