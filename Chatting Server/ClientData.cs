
using System.Net.Sockets;

namespace Chatting_Server
{
  public class ClientData : IDisposable
  {
    public TcpClient Client { get; set; }
    public string Name { get; set; } = "";
    public string Id { get; set; }
    public CustomTimer Timer { get; set; }

    public ClientData(TcpClient client, string id, CustomTimer timer)
    {
      Client = client;
      Id = id;
      Timer = timer;
    }
    public void Dispose()
    {
      Client.Close();
      Client.Dispose();
      Timer.Stop();
      Timer.Dispose();
    }
  }
}
