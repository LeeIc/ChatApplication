using System.Net.Sockets;
using System.Text;
using Chatting_Client.CommunicationHelpers.Enums;

namespace Chatting_Client.CommunicationHelpers
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
        return ((OpCodes)opCode, payload);
      }
      catch (Exception _)
      {
        throw;
      }

    }
  }
}
