using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Windows.Data;

namespace WindowsADExplorer.Models
{
    public class ThreadSafeObservableCollection<TElement> : ObservableCollection<TElement>
    {
        private readonly object _sync;

        public ThreadSafeObservableCollection()
        {
            this._sync = new object();
        }

        public void EnableSync()
        {
            BindingOperations.EnableCollectionSynchronization(this, this._sync);
        }

        protected override void ClearItems()
        {
            lock (_sync)
            {
                base.ClearItems();
            }
        }

        protected override void OnCollectionChanged(NotifyCollectionChangedEventArgs e)
        {
            lock (_sync)
            {
                base.OnCollectionChanged(e);
            }
        }

        protected override void OnPropertyChanged(PropertyChangedEventArgs e)
        {
            lock (_sync)
            {
                base.OnPropertyChanged(e);
            }
        }

        public override bool Equals(object obj)
        {
            lock (_sync)
            {
                return base.Equals(obj);
            }
        }

        protected override void InsertItem(int index, TElement item)
        {
            lock (_sync)
            {
                base.InsertItem(index, item);
            }
        }

        public override int GetHashCode()
        {
            lock (_sync)
            {
                return base.GetHashCode();
            }
        }

        protected override void MoveItem(int oldIndex, int newIndex)
        {
            lock (_sync)
            {
                base.MoveItem(oldIndex, newIndex);
            }
        }

        protected override void RemoveItem(int index)
        {
            lock (_sync)
            {
                base.RemoveItem(index);
            }
        }

        protected override void SetItem(int index, TElement item)
        {
            lock (_sync)
            {
                base.SetItem(index, item);
            }
        }

        public override string ToString()
        {
            lock (_sync)
            {
                return base.ToString();
            }
        }
    }
}
