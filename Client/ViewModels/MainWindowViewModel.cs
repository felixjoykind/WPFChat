using System;
using System.Collections.ObjectModel;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace WPFChat
{
    class MainWindowViewModel : BaseViewModel
    {
        public ObservableCollection<string> Messages { get; private set; }

        private string _port = "8080";
        public string Port
        {
            get => _port;
            set => Set(ref _port, value);
        }

        private string _address = "127.0.0.1";
        public string Address
        {
            get => _address;
            set => Set(ref _address, value);
        }

        private string _username = "Power User";
        public string Username
        {
            get => _username;
            set => Set(ref _username, value);
        }

        private string _message = string.Empty;
        public string Message
        {
            get => _message;
            set => Set(ref _message, value);
        }

        private bool _isTextboxEnabled = true;
        public bool isTextboxEnabled
        {
            get => _isTextboxEnabled;
            set => Set(ref _isTextboxEnabled, value);
        }

        private bool isConnected = false;
        private Client client;

        public ICommand ConnectCommand { get; }
        public ICommand DisconnectCommand { get; }
        public ICommand SendMessageCommand { get; }

        private void OnConnectCommandExecuted(object p)
        {
            try
            {
                if (ConnectClient())
                {
                    isConnected = true;
                    isTextboxEnabled = false;
                }
            }
            catch (Exception)
            {
                DisplayError("Something went wrong!");
            }
        }

        private void OnDisconnectCommandExecuted(object p)
        {
            DisconnectClient();
        }

        private void OnSendMessageCommandExecuted(object p)
        {
            Message = Message.Trim();
            if (Message.Length == 0) return;
            client.SendMessage(Message);
            WriteMessage($"{Username}: {Message}");
            Message = string.Empty;
        }

        private bool CanConnectCommandExecute(object p) => isConnected == false;
        private bool CanDisconnectCommandExecute(object p) => isConnected == true;
        private bool CanSendMessageCommandExecute(object p) => isConnected == true;

        public MainWindowViewModel()
        {
            Messages = new ObservableCollection<string>();

            ConnectCommand = new Command(OnConnectCommandExecuted, CanConnectCommandExecute);
            DisconnectCommand = new Command(OnDisconnectCommandExecuted, CanDisconnectCommandExecute);
            SendMessageCommand = new Command(OnSendMessageCommandExecuted, CanSendMessageCommandExecute);
        }

        private bool ConnectClient()
        {
            // перевіряємо чи порт, адрес та нікнейм введено коректно
            Port = Port.Trim();
            Address = Address.Trim();
            Username = Username.Trim();
            bool isPortValid = int.TryParse(Port, out int socketPort) && Port.Length < 5;
            bool isAddressValid = IPAddress.TryParse(Address, out IPAddress socketAddress);
            bool isUsernameValid = Username.Length > 0;

            // якщо щось введенор не правильно
            if (!isPortValid || !isAddressValid || !isUsernameValid)
            {
                DisplayError("Port, address or username value is !");
                return false;
            }

            client = new Client(new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp), Username);
            client.Connect(socketPort, socketAddress);
            isConnected = true;
            // новий потік для отримання даних
            Task.Run(() =>
            {
                while (isConnected)
                {
                    string message = RecieveMessage();
                    if (message != string.Empty)
                        WriteMessage(message);
                    else
                    {
                        DisconnectClient();
                    }
                }
            });
            Messages.Clear();
            WriteMessage($"Welcome, {Username}!");
            return true;
        }

        public string RecieveMessage()
        {
            string messsage = string.Empty;
            try
            {
                var buffer = new byte[1024]; // буфер для отриманих даних
                int bytes = client.Socket.Receive(buffer);
                messsage = Encoding.UTF8.GetString(buffer, 0, bytes);
            }
            catch (Exception)
            {
                DisconnectClient();
            }
            return messsage;
        }

        private void DisconnectClient()
        {
            if (isConnected == false) return;
            client.Disconnect();
            isConnected = false;
            isTextboxEnabled = true;
            WriteMessage("You were disconncted from the server!");
        }

        private void WriteMessage(string message)
        {
            // ми додаємо в Messages елементи, які не були створені в тому ж самому потоці, що і Messages.
            // тому ми додаємо повідомлення з того потоку, в якому він був створений
            // https://overcoder.net/q/27413/%D1%8D%D1%82%D0%BE%D1%82-%D1%82%D0%B8%D0%BF-collectionview-%D0%BD%D0%B5-%D0%BF%D0%BE%D0%B4%D0%B4%D0%B5%D1%80%D0%B6%D0%B8%D0%B2%D0%B0%D0%B5%D1%82-%D0%B8%D0%B7%D0%BC%D0%B5%D0%BD%D0%B5%D0%BD%D0%B8%D1%8F-%D0%B2-%D0%B5%D0%B3%D0%BE-sourcecollection-%D0%B8%D0%B7
            App.Current.Dispatcher.Invoke((Action)delegate
            {
                Messages.Add($"{DateTime.Now.ToShortTimeString()}|{message}");
            });
        }

        private void DisplayError(string message) =>
            MessageBox.Show(message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
    }
}
