namespace Chatting_Client.CommunicationHelpers
{
  public class MessageData
  {
    public string Name { get; set; } = "";
    public string Message { get; set; } = "";
    public DateTime DateTime { get; set; } = DateTime.Now;

    public MessageData(string name, string message, DateTime dateTime)
    {
      Name = name;
      Message = message;
      DateTime = dateTime;
    }
    public MessageData()
    {

    }
  }
}
