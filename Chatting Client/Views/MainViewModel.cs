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
    public ICommand ConnectUserCommand { get; private set; } = null!;
    public IAsyncRelayCommand SendMessageCommand { get; private set; } = null!;

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
      this.PropertyChanged += PropertyChangedHandler;
    }
    #endregion
    #region Public Methods

    public void OnPropertyChanged([CallerMemberName] string name = "") =>
      PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    #endregion

    #region Private methods
    private void SetupCommands()
    {
      ConnectUserCommand = new Command(ConnectUser);
      SendMessageCommand = new AsyncRelayCommand(SendMessage);
    }
    private void PropertyChangedHandler(object? sender, PropertyChangedEventArgs e)
    {
      switch (e.PropertyName)
      {
        case "Username":
         
          break;
      }
    }
    private void ConnectUser()
    {
      if (Username == string.Empty)
      {
        IsUserSelectorVisible = true;
        IsMessageEntryVisible = false;
        return;
      }
      try
      {
        client = new TcpClient(IPAddress.Loopback.ToString(), 4356);
        stream = client.GetStream();
        /*if (stream != null && stream.CanWrite)
        {
          string message = $"System: {Username} connected to the server";
          byte[] buffer = Encoding.UTF8.GetBytes(message);
          await stream.WriteAsync(buffer, 0, buffer.Length);
          DisplayMessage(message);
          Message = string.Empty;
        }*/
        _ = Task.Run(() => ReceiveMessages());
        IsUserSelectorVisible = false;
        IsMessageEntryVisible = true;
      }
      catch (Exception ex)
      {
        DisplayMessage($"Error: {ex.Message}");
      }
    }

    private async Task SendMessage()
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
    #endregion
  }
}
