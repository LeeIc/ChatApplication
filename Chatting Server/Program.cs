using Chatting_Server;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Net.NetworkInformation;
using System.Timers;

public class Program
{
  static ConcurrentDictionary<string, ClientData> clientsData = new ConcurrentDictionary<string, ClientData>();
  private static BlockingCollection<MessageData> messages = new BlockingCollection<MessageData>();
  private static TransmitHelper transmitHelper = new TransmitHelper();
  private static ReceiveHelper receiveHelper = new ReceiveHelper();

  private const int heartbeatInterval = 1860000; // 1 hr 3600000 3660000

  public static async Task Main(string[] args)
  {
    try
    {
      await StartServer();
    }
    catch (Exception ex)
    {
      Console.WriteLine($"Unhandled exception: {ex.Message}");
    }
    finally
    {
      Console.WriteLine("Server shutting down...");
      Console.ReadLine(); // Wait for user input before closing
    }
  }

  ~Program()
  {
    foreach (var clientData in clientsData.Values)
    {
      clientData.Client.Close();
      clientData.Timer.Elapsed -= OnHeartbeatTimerElapsed;
    }
  }

  private static async Task StartServer()
  {
    List<string> lanIpAddresses = GetLocalIPAddress();
    if (lanIpAddresses.Any())
    {
      Console.WriteLine("LAN IP Addresses:");
      foreach (string ipAddress in lanIpAddresses)
      {
        Console.WriteLine(ipAddress);
      }
    }
    else
    {
      Console.WriteLine("No LAN IP Addresses found.");
    }

    var port = "";
    while (port == "")
    {
      Console.Write("Enter Port: ");
      port = Console.ReadLine();
    }

    TcpListener listener = new TcpListener(IPAddress.Any, int.Parse(port));
    listener.Start();
    Console.WriteLine("Server started...");

    while (true)
    {
      try
      {
        TcpClient client = await listener.AcceptTcpClientAsync();
        string clientId = Guid.NewGuid().ToString();
        var newTimer = new CustomTimer(heartbeatInterval) { ClientId = clientId };
        newTimer.Elapsed += OnHeartbeatTimerElapsed;
        newTimer.Start();
        var clientData = new ClientData(client, clientId, newTimer);
        clientsData.TryAdd(clientId, clientData);

        Console.WriteLine($"Client connected with ID: {clientId}");

        foreach (var message in messages)
        {
          await transmitHelper.SendMessage(client, message);
        }
        _ = Task.Run(() => HandleClient(clientData));
      }
      catch (Exception ex)
      {
        Console.WriteLine($"Error accepting client: {ex.Message}");
      }
    }
  }

  private static async Task HandleClient(ClientData clientData)
  {
    // Needs this so id can be written out at the end even after the using
    var id = clientData.Id;
    try
    {
      using (clientData)
      {

        while (true)
        {
          var opCodeAndPayload = await receiveHelper.Read(clientData.Client);

          // Reset timer if any messages are received
          clientData.Timer.Stop();
          clientData.Timer.Start();

          await ProcessOpCodeAndPayload(clientData.Client, opCodeAndPayload);
        }
      }
    }
    catch (Exception ex)
    {
      Console.WriteLine($"Error handling client: {ex.Message}");
    }
    finally
    {
      if (clientsData.ContainsKey(id))
      {
        clientsData[id].Timer.Elapsed -= OnHeartbeatTimerElapsed;
        clientsData[id].Dispose();
        clientsData.TryRemove(id, out _);
      }

      Console.WriteLine($"{id} disconnected...");
    }
  }

  private static void OnHeartbeatTimerElapsed(object? sender, ElapsedEventArgs e)
  {
    string? clientId = ((CustomTimer?)sender)?.ClientId;
    if (clientId == null)
      return;
    clientsData[clientId].Timer.Elapsed -= OnHeartbeatTimerElapsed;
    clientsData[clientId].Dispose();
    clientsData.TryRemove(clientId, out _);
    Console.WriteLine($"{clientId} stopped responding...");
  }
  
  private static async Task ProcessOpCodeAndPayload(TcpClient client, (OpCodes, string) opCodeAndPayload)
  {
    OpCodes opCode = opCodeAndPayload.Item1;
    string payload = opCodeAndPayload.Item2;
    var messageData = new MessageData();
    try
    {
      switch (opCode)
      {
        case OpCodes.HeartBeat:
          await transmitHelper.SendKeepAlive(client);
          break;
        case OpCodes.ServerNotification:
          throw new Exception("Server cannot receive server notifications");
        case OpCodes.DateTime:
          // Using the server time instead of the client sent time for now
          messageData.DateTime = DateTime.Now;
          // Assuming that if datetime is read, the next two is going to be name and message.
          // Could use some error checking here in the future.
          messageData.Name = (await receiveHelper.Read(client)).Item2;
          messageData.Message = (await receiveHelper.Read(client)).Item2;
          messages.Add(messageData);
          await transmitHelper.BroadcastMessage(clientsData.Values, client, messageData);
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
      Console.WriteLine($"Error Processing: {ex}");
    }
  }
  static List<string> GetLocalIPAddress()
  {
    List<string> ipAddresses = new List<string>();

    NetworkInterface[] networkInterfaces = NetworkInterface.GetAllNetworkInterfaces();

    foreach (NetworkInterface ni in networkInterfaces)
    {
      if (ni.NetworkInterfaceType != NetworkInterfaceType.Loopback &&
          ni.NetworkInterfaceType != NetworkInterfaceType.Tunnel &&
          ni.GetIPProperties().GatewayAddresses.Any() &&
          ni.GetIPProperties().UnicastAddresses.Any(a => a.Address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork))
      {
        foreach (UnicastIPAddressInformation ipAddressInfo in ni.GetIPProperties().UnicastAddresses)
        {
          if (ipAddressInfo.Address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
          {
            ipAddresses.Add(ipAddressInfo.Address.ToString());
          }
        }
      }
    }

    return ipAddresses;
  }

}
