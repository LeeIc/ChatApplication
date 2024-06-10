using System.Collections;
using System.Collections.Specialized;
using System.Runtime.CompilerServices;
using System.Linq;
namespace Chatting_Client.Views.Custom
{
  public class AutoScrollListView : ListView
  {
    private INotifyCollectionChanged? _previousObservableCollection;

    public AutoScrollListView(ListViewCachingStrategy cachingStrategy)
        : base(cachingStrategy)
    {
    }

    public AutoScrollListView()
        : base()
    {
    }

    protected override void OnPropertyChanged([CallerMemberName] string propertyName = "")
    {
      base.OnPropertyChanged(propertyName);

      if (propertyName == nameof(ItemsSource))
      {
        if (_previousObservableCollection != null)
        {
          _previousObservableCollection.CollectionChanged -= OnItemsSourceCollectionChanged;
          _previousObservableCollection = null;
        }

        if (ItemsSource is INotifyCollectionChanged newObservableCollection)
        {
          _previousObservableCollection = newObservableCollection;
          newObservableCollection.CollectionChanged += OnItemsSourceCollectionChanged;
        }
      }
    }

    private void OnItemsSourceCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
      if ((e.Action == NotifyCollectionChangedAction.Add) && e.NewItems != null)
      {
        var collection = e.NewItems;
        if (collection != null && collection.Count > 0)
        {
          // Scroll to the first item that can be found in the list.
          MainThread.BeginInvokeOnMainThread(() =>
          {
            ScrollTo(collection[0], ScrollToPosition.MakeVisible, true);
          });
        }
      }
    }
  }
}