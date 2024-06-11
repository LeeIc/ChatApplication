using System.ComponentModel;
using System.Net.Sockets;
using System.Net;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using System.Collections.ObjectModel;
using System.Text;
using CommunityToolkit.Mvvm.Input;

namespace Chatting_Client.Views
{
  public class MainViewModel : INotifyPropertyChanged
  {
    public event PropertyChangedEventHandler? PropertyChanged;

    #region Bindable properties
    public IAsyncRelayCommand ConnectUserCommand { get; private set; } = null!;
    public IAsyncRelayCommand SendMessageCommand { get; private set; } = null!;
    public string IpAddressAndPort
    {
      get { return ipAddressAndPort; }
      set
      {
        ipAddressAndPort = value;
        OnPropertyChanged();
      }
    }
    private string ipAddressAndPort = "192.168.1.32:1857";

    public string Username
    {
      get { return username; }
      set
      {
        username = value;
        OnPropertyChanged();
      }
    }
    private string username = string.Empty;

    public string Message
    {
      get { return message; }
      set
      {
        message = value;
        OnPropertyChanged();
      }
    }
    private string message = string.Empty;

    public ObservableCollection<string> Messages
    {
      get => messages;
      set
      {
        messages = value;
        OnPropertyChanged();
      }
    }
    private ObservableCollection<string> messages;

    public bool IsUserSelectorVisible
    {
      get { return isUserSelectorVisible; }
      set
      {
        isUserSelectorVisible = value;
        OnPropertyChanged();
      }
    }
    private bool isUserSelectorVisible = true;

    public bool IsMessageEntryVisible
    {
      get { return isMessageEntryVisible; }
      set
      {
        isMessageEntryVisible = value;
        OnPropertyChanged();
      }
    }
    private bool isMessageEntryVisible = false;

    #endregion

    #region Private Fields
    private TcpClient? client;
    private NetworkStream? stream;
    #endregion

    #region Constructor
    public MainViewModel()
    {
      messages = new ObservableCollection<string>();
      SetupCommands();
      //this.PropertyChanged += PropertyChangedHandler;
    }
    #endregion
    #region Public Methods

    public void OnPropertyChanged([CallerMemberName] string name = "") =>
      PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    #endregion

    #region Private methods
    private void SetupCommands()
    {
      ConnectUserCommand = new AsyncRelayCommand(ConnectUser);
      SendMessageCommand = new AsyncRelayCommand(SendMessage);
    }
    /*private void PropertyChangedHandler(object? sender, PropertyChangedEventArgs e)
    {
      switch (e.PropertyName)
      {
        case "Username":

          break;
      }
    }*/
    private async Task ConnectUser()
    {
      if (Username == string.Empty)
      {
        ShowConnectScreen(true);
        return;
      }
      await Task.Run(async () =>
      {
          ProcessIpAddressAndPort(out string ip, out string port);
          client = new TcpClient();
          if (!client.ConnectAsync(ip, int.Parse(port)).Wait(1000))
          {
            DisplayMessage($"Error: Could not connect to Server");
            return;
          }
          stream = client.GetStream();
          ClearMessages();
          /*if (stream != null && stream.CanWrite)
          {
            string message = $"System: {Username} connected to the server";
            byte[] buffer = Encoding.UTF8.GetBytes(message);
            await stream.WriteAsync(buffer, 0, buffer.Length);
            DisplayMessage(message);
            Message = string.Empty;
          }*/
          _ = Task.Run(async () => await ReceiveMessages());
          ShowConnectScreen(false);
      });

    }

    private async Task SendMessage()
    {
      try
      {
        if (stream != null && stream.CanWrite)
        {
          string message = $"{Username}: {Message}";
          byte[] buffer = Encoding.UTF8.GetBytes(message);
          await stream.WriteAsync(buffer, 0, buffer.Length);
          DisplayMessage(message);
          Message = string.Empty;
        }
      }
      catch (Exception ex)
      {
        ClearMessages();
        DisplayMessage($"Error: {ex.Message}");
        ShowConnectScreen(true);
      }
    }

    private void DisplayMessage(string message)
    {
      MainThread.BeginInvokeOnMainThread(() =>
      {
        Messages.Add(message);
      });
    }

    private async Task ReceiveMessages()
    {
      byte[] buffer = new byte[1024];
      int byteCount;
      while (stream != null)
      {
        byteCount = await stream.ReadAsync(buffer, 0, buffer.Length);
        string message = Encoding.UTF8.GetString(buffer, 0, byteCount);
        DisplayMessage(message);
      }
    }

    private void ShowConnectScreen(bool isShown)
    {
      MainThread.BeginInvokeOnMainThread(() =>
      {
        if(isShown)
        {
          IsUserSelectorVisible = true;
          IsMessageEntryVisible = false;
        }
        else
        {
          IsUserSelectorVisible = false;
          IsMessageEntryVisible = true;
        }
      });
    }

    private void ClearMessages()
    {
      MainThread.BeginInvokeOnMainThread(() =>
      {
        Messages.Clear();
      });
    }

    private void ProcessIpAddressAndPort(out string ip, out string port)
    {
      var splitIpAddressAndPort = IpAddressAndPort.Split(":");
      ip = splitIpAddressAndPort[0];
      port = splitIpAddressAndPort[1];
    }

    #endregion
  }
}
