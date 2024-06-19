using System.Net.Sockets;
using System.Text;
using Chatting_Client.CommunicationHelpers.Enums;

namespace Chatting_Client.CommunicationHelpers
{
  public class TransmitHelper
  {
    private const string heartbeatMessage = "HEARTBEAT";

    public async Task SendKeepAlive(TcpClient client)
    {
      NetworkStream stream = client.GetStream();
      // Send the server notification
      await Send(OpCodes.HeartBeat, stream, heartbeatMessage);
    }

    public async Task SendServerNotification(TcpClient client, string message)
    {
      NetworkStream stream = client.GetStream();
      // Send the server notification
      await Send(OpCodes.ServerNotification, stream, message);
    }

    public async Task SendMessage(TcpClient client, MessageData messageData)
    {
      NetworkStream stream = client.GetStream();

      // Send the date first
      await Send(OpCodes.DateTime, stream, messageData.DateTime.ToString());

      // Send the name second
      await Send(OpCodes.Name, stream, messageData.Name);

      // Send the message last
      await Send(OpCodes.Message, stream, messageData.Message);
    }

    private async Task Send(OpCodes opCode, NetworkStream stream, string payload)
    {
      byte[] messageBytes = Encoding.UTF8.GetBytes(payload);
      int size = messageBytes.Length;

      // 1 byte for opCode, 4 bytes for size, rest for message
      byte[] buffer = new byte[1 + 4 + size];

      // opCode
      buffer[0] = (byte)opCode;
      // size
      BitConverter.GetBytes(size).CopyTo(buffer, 1);
      // payload
      messageBytes.CopyTo(buffer, 5);
      try
      {
        // Write
        await stream.WriteAsync(buffer, 0, buffer.Length);
      }
      catch (Exception _)
      {
        throw;
      }
    }
  }

}
