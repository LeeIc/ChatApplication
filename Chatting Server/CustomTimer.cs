
public class CustomTimer : System.Timers.Timer
{
  public string? ClientId { get; set; }

  public CustomTimer(int interval): base(interval)
  {

  }
}