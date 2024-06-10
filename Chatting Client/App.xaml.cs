using Chatting_Client.Views;

namespace Chatting_Client
{
  public partial class App : Application
  {
    public App(MainViewModel mainViewModel)
    {
      InitializeComponent();

      MainPage = new MainView(mainViewModel);
    }
  }
}
