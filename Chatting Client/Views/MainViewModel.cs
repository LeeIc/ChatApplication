using System.ComponentModel;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using Plugin.LocalNotification;
using System.Collections.ObjectModel;
using System.Text;
using CommunityToolkit.Mvvm.Input;
using System.Timers;

namespace Chatting_Client.Views
{
  public class MainViewModel : INotifyPropertyChanged, IDisposable
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
    private readonly System.Timers.Timer heartbeatTimer;
    private const int heartbeatInterval = 3600000; // 1 hr 3600000
    private const string heartbeatMessage = "HEARTBEAT";
    private CancellationTokenSource heartbeatCancellationTokenSource;
    private bool isServerResponsive = true;

    #endregion

    #region Constructor
    public MainViewModel()
    {
      messages = new ObservableCollection<string>();
      SetupCommands();
      heartbeatTimer = new System.Timers.Timer(heartbeatInterval);
      heartbeatTimer.Elapsed += OnHeartbeatTimerElapsed;
      heartbeatCancellationTokenSource = new CancellationTokenSource();
    }
    #endregion
    #region Public Methods
    public void Dispose()
    {
      heartbeatTimer.Elapsed -= OnHeartbeatTimerElapsed;
      GC.SuppressFinalize(this);
    }
    public void OnPropertyChanged([CallerMemberName] string name = "") =>
      PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    #endregion

    #region Private methods
    private void SetupCommands()
    {
      ConnectUserCommand = new AsyncRelayCommand(ConnectUser);
      SendMessageCommand = new AsyncRelayCommand(SendMessage);
    }

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
        if (!await TryConnectAsync(ip, int.Parse(port), 1000))
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
        if (client?.Connected == true && stream != null && stream.CanWrite)
        {
          string message = $"{Username}: {Message}";
          byte[] buffer = Encoding.UTF8.GetBytes(message);
          //await SendHeartbeat();
          await stream.WriteAsync(buffer, 0, buffer.Length);
          DisplayMessage($"[{DateTime.Now.ToString()}]\n" + message);
          Message = string.Empty;
        }
        else
        {
          client?.Close();
          ClearMessages();
          DisplayMessage($"Error: Disconnect or stream cannot be written to");
          ShowConnectScreen(true);
        }
      }
      catch (Exception ex)
      {
        ClearMessages();
        DisplayMessage($"Error: {ex.Message}");
        ShowConnectScreen(true);
      }
    }

    private async void OnHeartbeatTimerElapsed(object? _, ElapsedEventArgs e)
    {
      // Send heartbeat message to the server
      _ = SendHeartbeat();
      isServerResponsive = false;
      try
      {
        await Task.Delay(heartbeatInterval, heartbeatCancellationTokenSource.Token);
        if (!isServerResponsive)
        {
          // Server did not respond to the previous heartbeat
          OnServerDisconnected();
        }
      }
      catch (TaskCanceledException)
      {
        // Response received
      }
      finally { heartbeatCancellationTokenSource = new CancellationTokenSource(); }
    }

    private async Task SendHeartbeat()
    {
      try
      {
        if (stream != null && stream.CanWrite)
        {
          byte[] buffer = Encoding.UTF8.GetBytes(heartbeatMessage);
          await stream.WriteAsync(buffer, 0, buffer.Length);
        }
      }
      catch (Exception)
      {
        OnServerDisconnected();
      }
    }

    private void OnServerDisconnected()
    {
      isServerResponsive = false;
      MainThread.BeginInvokeOnMainThread(async () =>
      {
        if (heartbeatTimer.Enabled == false)
        {
          return;
        }
        heartbeatTimer.Stop();
        client?.Close();
        ClearMessages();
        Message = "";
        var message = "Disconnected from server";
        var request = new NotificationRequest
        {
          NotificationId = 1,
          Title = "Error",
          Description = message
        };
        await LocalNotificationCenter.Current.Show(request);
        DisplayMessage(message);
        ShowConnectScreen(true);
      });
    }
    private async Task<bool> TryConnectAsync(string ip, int port, int timeout)
    {
      using (var cts = new CancellationTokenSource(timeout))
      {
        try
        {
          if (client != null)
            await client.ConnectAsync(ip, port, cts.Token);
          else
            return false;
          return true;
        }
        catch (OperationCanceledException)
        {
          client?.Close();  // Ensure the client is closed if the connection is canceled
          return false;
        }
        catch (Exception ex)
        {
          Console.WriteLine($"Connection attempt failed: {ex.Message}");
          client?.Close();  // Ensure the client is closed on any exception
          return false;
        }
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
      heartbeatTimer.Start();
      while (stream != null)
      {
        byteCount = await stream.ReadAsync(buffer, 0, buffer.Length);
        string message = Encoding.UTF8.GetString(buffer, 0, byteCount);

        if (message.Contains(heartbeatMessage))
        {
          isServerResponsive = true;
          heartbeatCancellationTokenSource.Cancel();
          continue; // Ignore heartbeat messages
        }
        else if (message == "" || new string(message.Where(c => !char.IsControl(c)).ToString()) == "" || message == " " || message == "  ")
        {
          continue; // Ignore empty messages
        }
        var request = new NotificationRequest
        {
          NotificationId = 1,
          Title = $"New Message",
          Description = message
        };
        await LocalNotificationCenter.Current.Show(request);
        DisplayMessage(message);
      }
    }

    private void ShowConnectScreen(bool isShown)
    {
      MainThread.BeginInvokeOnMainThread(() =>
      {
        if (isShown)
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
