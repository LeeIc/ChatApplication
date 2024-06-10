using System.Collections.ObjectModel;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace Chatting_Client.Views
{
  public partial class MainView : ContentPage
  {
    public MainView(MainViewModel mainViewModel)
    {
      InitializeComponent();
      BindingContext = mainViewModel;
    }

  }
}
