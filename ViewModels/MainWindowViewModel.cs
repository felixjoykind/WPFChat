using System;
using System.Collections.ObjectModel;
using System.Net;
using System.Net.Sockets;
using System.Windows;
using System.Windows.Input;

namespace WPFChat
{
    class MainWindowViewModel : BaseViewModel
    {
        private Server server;
        public ObservableCollection<string> Messages { get; private set; }
        public ObservableCollection<string> Users { get; private set; }

        public bool isRunning { get; private set; } = false;

        private string _Port = "8080";
        public string Port
        {
            get => _Port;
            set => Set(ref _Port, value);
        }

        private string _Address = string.Empty;
        public string Address
        {
            get => _Address;
            set => Set(ref _Address, value);
        }

        private bool _isPortTextboxEnabled = true;
        public bool isPortTextboxEnabled
        {
            get => _isPortTextboxEnabled;
            set => Set(ref _isPortTextboxEnabled, value);
        }

        public ICommand StartServerCommand { get; }
        public ICommand StopServerCommand { get; }

        private bool CanStartServerCommandExecute(object p) => isRunning == false;

        private void OnStartServerCommandExecuted(object p)
        {
            try
            {
                // налаштовуємо та стартуємо сервер
                if (SetupServer())
                    StartServer();
            }
            catch (Exception e)
            {
                StopServer();
                DisplayError(e.ToString());
            }
        }

        private bool CanStopServerCommandExecute(object p) => isRunning == true;

        private void OnStopServerCommandExecuted(object p) => StopServer();

        public MainWindowViewModel()
        {
            Messages = new ObservableCollection<string>();
            Users = new ObservableCollection<string>();

            StartServerCommand = new Command(OnStartServerCommandExecuted, CanStartServerCommandExecute);
            StopServerCommand = new Command(OnStopServerCommandExecuted, CanStopServerCommandExecute);
        }

        private bool SetupServer()
        {
            // перевіряємо чи порт введено правильно
            Port = Port.Trim();
            bool isPortValid = int.TryParse(Port, out int socketPort) && Port.Length < 5;

            if (!isPortValid)
            {
                DisplayError("Port value is invalid!");
                return false;
            }

            server = new Server(socketPort);
            Address = GetLocalIP().ToString();
            // підписуємося на події сервера
            server.OnClientConnected += Server_OnClientConnected;
            server.OnClientDisconnected += Server_OnClientDisconnected;
            server.OnServerStarted += Server_OnServerStarted;
            server.OnServerClosed += Server_OnServerClosed;
            server.OnMessageRecieved += Server_OnMessageRecieved;
            return true;
        }

        private void StartServer()
        {
            server.Start();
            isRunning = true;
            isPortTextboxEnabled = false;
        }

        private void StopServer()
        {
            server.Stop();
            isRunning = false;
            isPortTextboxEnabled = true;
            Address = string.Empty;
        }

        private void Server_OnClientConnected(object sender, ServerArgs sa)
        {
            Client client = sa.Client;
            App.Current.Dispatcher.Invoke((Action)delegate
            {
                Users.Add(client.Username);
            });
            WriteMessage($"{client.Username} connected from {client.Socket.RemoteEndPoint}!", "Server");
        }

        private void Server_OnClientDisconnected(object sender, ServerArgs sa)
        {
            string clientUsername = sa.Client.Username;
            App.Current.Dispatcher.Invoke((Action)delegate
            {
                Users.Remove(clientUsername);
            });
            WriteMessage($"{clientUsername} disconnected!", "Server");
        }

        private void Server_OnServerStarted(object sender, ServerArgs sa)
        {
            Messages.Clear();
            WriteMessage($"Server has started with port {Port}!", "Server");
        }

        private void Server_OnServerClosed(object sender, ServerArgs sa)
        {
            WriteMessage("Server stopped!", "Server");
        }

        private void Server_OnMessageRecieved(object sender, ChatArgs ca)
        {
            WriteMessage(ca?.Message, ca?.Sender?.Username);
        }

        private void WriteMessage(string message, string author)
        {
            // ми додаємо в Messages елементи, які не були створені в тому ж самому потоці, що і Messages.
            // тому ми додаємо повідомлення з того потоку, в якому він був створений
            // https://overcoder.net/q/27413/%D1%8D%D1%82%D0%BE%D1%82-%D1%82%D0%B8%D0%BF-collectionview-%D0%BD%D0%B5-%D0%BF%D0%BE%D0%B4%D0%B4%D0%B5%D1%80%D0%B6%D0%B8%D0%B2%D0%B0%D0%B5%D1%82-%D0%B8%D0%B7%D0%BC%D0%B5%D0%BD%D0%B5%D0%BD%D0%B8%D1%8F-%D0%B2-%D0%B5%D0%B3%D0%BE-sourcecollection-%D0%B8%D0%B7
            App.Current.Dispatcher.Invoke((Action)delegate
            {
                Messages.Add($"{DateTime.Now.ToShortTimeString()}|{author} : {message}");
            });
        }

        private IPAddress GetLocalIP()
        {
            var host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (var ip in host.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                    return ip;
            }
            return IPAddress.Parse("127.0.0.1");
        }

        private void DisplayError(string message) =>
            MessageBox.Show(message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
    }
}
