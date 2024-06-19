using System.ComponentModel;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using Plugin.LocalNotification;
using System.Collections.ObjectModel;
using Microsoft.Maui.Storage;
using CommunityToolkit.Mvvm.Input;
using System.Timers;
using Chatting_Client.CommunicationHelpers;
using Chatting_Client.CommunicationHelpers.Enums;

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

    public ObservableCollection<MessageData> Messages
    {
      get => messages;
      set
      {
        messages = value;
        OnPropertyChanged();
      }
    }
    private ObservableCollection<MessageData> messages;

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
    private const int heartbeatInterval = 1800000; // 1 hr 3600000, 30 minutes 1800000
    private CancellationTokenSource heartbeatCancellationTokenSource;
    private bool isServerResponsive = true;
    private TransmitHelper transmitHelper = new TransmitHelper();
    private ReceiveHelper receiveHelper = new ReceiveHelper();
    #endregion

    #region Constructor
    public MainViewModel()
    {
      messages = new ObservableCollection<MessageData>();
      SetupCommands();
      heartbeatTimer = new System.Timers.Timer(heartbeatInterval);
      heartbeatTimer.Elapsed += OnHeartbeatTimerElapsed;
      heartbeatCancellationTokenSource = new CancellationTokenSource();

      Username = Preferences.Get("Username", "Guest");
      IpAddressAndPort = Preferences.Get("IpAddressAndPort", IpAddressAndPort);
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
          DisplayMessage($"Could not connect to Server");
          return;
        }
        Preferences.Set("IpAddressAndPort", IpAddressAndPort);
        Preferences.Set("Username", Username);

        stream = client.GetStream();
        ClearMessages();
        _ = Task.Run(async () => await ReceiveMessages());
        ShowConnectScreen(false);
      });

    }

    private async Task SendMessage()
    {
      try
      {
        if (client == null)
          return;
        if (client.Connected == true && stream != null && stream.CanWrite)
        {
          var messageData = new MessageData(Username, Message, DateTime.Now);
          await transmitHelper.SendMessage(client, messageData);

          DisplayMessage(messageData);
          Message = string.Empty;
        }
        else
        {
          client?.Close();
          ClearMessages();
          DisplayMessage($"Disconnect or stream cannot be written to");
          ShowConnectScreen(true);
        }
      }
      catch (Exception ex)
      {
        ClearMessages();
        DisplayMessage($"{ex.Message}");
        ShowConnectScreen(true);
      }
    }

    private async void OnHeartbeatTimerElapsed(object? _, ElapsedEventArgs e)
    {
      if (client == null)
        return;
      try
      {
        // Send heartbeat message to the server
        await transmitHelper.SendKeepAlive(client);
        isServerResponsive = false;
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
      catch(Exception ex)
      {
        ClearMessages();
        DisplayMessage($"{ex.Message}");
        ShowConnectScreen(true);
      }
      finally { heartbeatCancellationTokenSource = new CancellationTokenSource(); }
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
          DisplayMessage($"Connection attempt failed: {ex.Message}");
          client?.Close();  // Ensure the client is closed on any exception
          return false;
        }
      }
    }

    private void DisplayMessage(MessageData message)
    {
      MainThread.BeginInvokeOnMainThread(() =>
      {
        Messages.Add(message);
      });
    }

    private void DisplayMessage(string message)
    {
      var notificationData = new MessageData();
      notificationData.Name = "Notification";
      notificationData.Message = message;
      notificationData.DateTime = DateTime.Now;

      MainThread.BeginInvokeOnMainThread(() =>
      {
        Messages.Add(notificationData);
      });
    }
    private async Task ReceiveMessages()
    {
      heartbeatTimer.Start();
      while (stream != null)
      {
        if (client == null)
          throw new Exception("Client is null");
        var opCodeAndPayload = await receiveHelper.Read(client);

        await ProcessOpCodeAndPayload(client, opCodeAndPayload);
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

    private async Task ProcessOpCodeAndPayload(TcpClient client, (OpCodes, string) opCodeAndPayload)
    {
      OpCodes opCode = opCodeAndPayload.Item1;
      string payload = opCodeAndPayload.Item2;
      try
      {
        switch (opCode)
        {
          case OpCodes.HeartBeat:
            isServerResponsive = true;
            heartbeatCancellationTokenSource.Cancel();
            break;
          case OpCodes.ServerNotification:
            DisplayMessage(payload);
            break;
          case OpCodes.DateTime:
            var messageData = new MessageData();
            // Using the server time instead of the client date time for now
            messageData.DateTime = DateTime.Parse(payload);
            // Assuming that if datetime is read, the next two is going to be name and message.
            // Could use some error checking here in the future.
            messageData.Name = (await receiveHelper.Read(client)).Item2;
            messageData.Message = (await receiveHelper.Read(client)).Item2;
            var request = new NotificationRequest
            {
              NotificationId = 1,
              Title = $"New Message",
              Description = messageData.Message
            };
            await LocalNotificationCenter.Current.Show(request);
            DisplayMessage(messageData);
            break;
          case OpCodes.Name:
            throw new Exception("Cannot receive name on it's own");
          case OpCodes.Message:
            throw new Exception("Cannot receive message on it's own");
          default:
            throw new Exception("Invalid Op code received");
        }
      }
      catch (Exception ex)
      {
        // DisplayMessage($"Error Processing: {ex}"); Ignore these for now since it might spam empty messages if the server disonnnects
      }
    }

    #endregion
  }
}
