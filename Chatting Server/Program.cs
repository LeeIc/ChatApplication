using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Timers;

public class Program
{
  static ConcurrentDictionary<string, TcpClient> clients = new ConcurrentDictionary<string, TcpClient>();
  static ConcurrentDictionary<string, System.Timers.Timer> clientTimers = new ConcurrentDictionary<string, System.Timers.Timer>();
  private static BlockingCollection<string> messages = new BlockingCollection<string>();
  private const string heartbeatMessage = "HEARTBEAT";
  private const int heartbeatInterval = 3660000; // little more than 1 hour

  static async Task Main(string[] args)
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
    }
  }

  ~Program()
  {
    foreach (var client in clients.Values)
    {
      client.Close();
    }
    foreach (var clientTimer in clientTimers.Values)
    {
      clientTimer.Elapsed -= OnHeartbeatTimerElapsed;
    }
  }

  static async Task StartServer()
  {
    TcpListener listener = new TcpListener(IPAddress.Any, 1857);
    listener.Start();
    Console.WriteLine("Server started...");

    while (true)
    {
      try
      {
        TcpClient client = await listener.AcceptTcpClientAsync();
        string clientId = Guid.NewGuid().ToString();
        Console.WriteLine($"Client connected with ID: {clientId}");
        clients.TryAdd(clientId, client);
        var newTimer = new CustomTimer(heartbeatInterval) { ClientId = clientId };
        newTimer.Elapsed += OnHeartbeatTimerElapsed;
        clientTimers.TryAdd(clientId, newTimer);
        newTimer?.Start();
        foreach (var message in messages)
        {
          NetworkStream stream = client.GetStream();
          byte[] buffer = Encoding.UTF8.GetBytes(message + "\n");
          await stream.WriteAsync(buffer, 0, buffer.Length);
        }
        _ = Task.Run(() => HandleClient(clientId, client, newTimer));
      }
      catch (Exception ex)
      {
        Console.WriteLine($"Error accepting client: {ex.Message}");
      }
    }
  }

  static async Task HandleClient(string clientId, TcpClient client, CustomTimer? timer)
  {
    try
    {
      using (client)
      {
        NetworkStream stream = client.GetStream();
        byte[] buffer = new byte[1024];
        int byteCount;

        while ((byteCount = await stream.ReadAsync(buffer, 0, buffer.Length)) > 0)
        {
          string message = Encoding.UTF8.GetString(buffer, 0, byteCount);

          Console.WriteLine($"Received: {message}");
          // Reset timer if any messages are received
          timer?.Stop();
          timer?.Start();
          if (message == heartbeatMessage)
          {
            try
            {
              buffer = Encoding.UTF8.GetBytes(message);
              await stream.WriteAsync(buffer, 0, buffer.Length);
            }
            catch (Exception ex)
            {
              Console.WriteLine($"Error sending to client: {ex.Message}");
            }
          }
          else
          {
            message = $"[{DateTime.Now.ToString()}]\n" + message;
            messages.Add(message);
            // Broadcast the message to all clients
            await BroadcastMessage(message, client);
          }
        }
      }
    }
    catch (Exception ex)
    {
      Console.WriteLine($"Error handling client: {ex.Message}");
    }
    finally
    {
      clients.TryRemove(clientId, out _);
      client?.Close();
      clientTimers.TryRemove(clientId, out _);
      if (timer != null)
        timer.Elapsed -= OnHeartbeatTimerElapsed;
      Console.WriteLine($"{clientId} disconnected...");
    }
  }

  static async Task BroadcastMessage(string message, TcpClient sender)
  {
    byte[] buffer = Encoding.UTF8.GetBytes(message);

    foreach (var client in clients.Values)
    {
      if (client != sender && client.Connected)
      {
        try
        {
          NetworkStream stream = client.GetStream();
          await stream.WriteAsync(buffer, 0, buffer.Length);
        }
        catch (Exception ex)
        {
          Console.WriteLine($"Error broadcasting to client: {ex.Message}");
        }
      }
    }
  }

  private static void OnHeartbeatTimerElapsed(object? sender, ElapsedEventArgs e)
  {
    string? clientId = ((CustomTimer?)sender)?.ClientId;
    if (clientId == null)
      return;
    clients[clientId].Close();
    clients.TryRemove(clientId, out _);
    clientTimers[clientId].Elapsed -= OnHeartbeatTimerElapsed;
    clientTimers.TryRemove(clientId, out _);
    Console.WriteLine($"{clientId} stopped responding...");
  }

}
