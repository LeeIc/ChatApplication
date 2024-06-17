using System.IO;
using System.Net.Sockets;
using System.Text;

namespace Chatting_Server
{
  public class ReceiveHelper
  {
    public async Task<(OpCodes, string)> Read(TcpClient client)
    {
      NetworkStream stream = client.GetStream();
      try
      {
        byte[] headerBuffer = new byte[5];
        int bytesRead = await stream.ReadAtLeastAsync(headerBuffer, 5);
        if (bytesRead < 5)
        {
          throw new Exception("Failed to read header");
        }
        // Op code
        byte opCode = headerBuffer[0];
        // Payload size
        int size = BitConverter.ToInt32(headerBuffer, 1);

        // Payload
        byte[] payloadBuffer = new byte[size];
        bytesRead = await stream.ReadAtLeastAsync(payloadBuffer, size);
        if (bytesRead < size)
        {
          throw new Exception("Failed to read payload");
        }

        string payload = Encoding.UTF8.GetString(payloadBuffer);
        Console.WriteLine($"Received: {payload}");
        return ((OpCodes) opCode, payload);
      }
      catch (Exception ex)
      {
        Console.WriteLine($"Error reading from client: {ex.Message}");
        throw;
      }

    }
  }
}