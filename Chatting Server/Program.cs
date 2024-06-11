using System;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Text;

public class Program
{
  static ConcurrentDictionary<string, TcpClient> clients = new ConcurrentDictionary<string, TcpClient>();
  private static BlockingCollection<string> messages = new BlockingCollection<string>();
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
        foreach (var message in messages)
        {
          NetworkStream stream = client.GetStream();
          byte[] buffer = Encoding.UTF8.GetBytes(message + "\n");
          await stream.WriteAsync(buffer, 0, buffer.Length);
        }
        _ = Task.Run(() => HandleClient(clientId, client));
      }
      catch (Exception ex)
      {
        Console.WriteLine($"Error accepting client: {ex.Message}");
      }
    }
  }

  static async Task HandleClient(string clientId, TcpClient client)
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
          messages.Add(message);
          Console.WriteLine($"Received: {message}");

          // Broadcast the message to all clients
          await BroadcastMessage(message, client);
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
      client.Close();
      Console.WriteLine("Client disconnected...");
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
}
