using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AnimatedListTest
{
    public class ObservableCollectionView<T> : IList<T>, INotifyPropertyChanged, INotifyCollectionChanged
    {
        IList<T> items;
        Dictionary<int, VisibleItem<T>> filteredItems;

        public SortDescriptions SortDescriptions;

        private Predicate<T> _filter;
        public Predicate<T> Filter
        {
            get { return _filter; }
            set
            {
                _filter = value;
                Refresh();
            }
        }

        #region Constructors
        public ObservableCollectionView()
        {
            items = new List<T>();
            filteredItems = new Dictionary<int, VisibleItem<T>>();

            SortDescriptions = new SortDescriptions();
            SortDescriptions.AddListener(SortDescriptionsChanged);
        }

        public ObservableCollectionView(IList<T> list) : this()
        {
            if (list == null)
                throw new ArgumentNullException("list");

            CopyList(list);
        }

        private void CopyList(IEnumerable<T> collection)
        {
            if(collection != null && items != null)
            {
                using(IEnumerator<T> enumerator = collection.GetEnumerator())
                {
                    while (enumerator.MoveNext())
                    {
                        items.Add(enumerator.Current);
                    }
                }
            }
        }
        #endregion

        public T this[int index]
        {
            get
            {
                return items[index];
            }
            set
            {
                if (index < 0 || index >= items.Count)
                    throw new ArgumentOutOfRangeException("index");

                SetItem(index, value);
            }
        }

        public int Count { get { return items.Count; } }
        
        public bool IsReadOnly => throw new NotImplementedException();

        public void Add(T item)
        {
            int index = items.Count; // index should be dependant on sort criteria
            InsertItem(index, item);
        }

        public void Clear()
        {
            ClearItems();
        }

        public bool Contains(T item)
        {
            return items.Contains(item);
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            items.CopyTo(array, arrayIndex);
        }

        public IEnumerator<T> GetEnumerator()
        {
            return items.GetEnumerator();
        }

        public int IndexOf(T item)
        {
            return items.IndexOf(item);
        }

        public void Insert(int index, T item)
        {
            if (index < 0 || index >= items.Count)
                throw new ArgumentOutOfRangeException("index");

            if (SortDescriptions.Any())
                throw new ArgumentException("Cannot insert item at specific index if ObservableCollectionView has any SortDescriptions", "index");

            InsertItem(index, item);
        }

        public bool Remove(T item)
        {
            int index = items.IndexOf(item);
            if (index < 0)
                return false;

            RemoveItem(index);
            return true;
        }

        public void RemoveAt(int index)
        {
            if (index < 0 || index >= items.Count)
                throw new ArgumentOutOfRangeException("index");

            RemoveItem(index);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable)items).GetEnumerator();
        }

        protected virtual void ClearItems()
        {
            filteredItems.Clear();
            items.Clear();

            OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
        }

        protected virtual void InsertItem(int index, T item)
        {
            items.Insert(index, item);

            filteredItems[index] = new VisibleItem<T>(index, item, (Filter != null) ? Filter(item) : true);

            OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, item, index));
            //OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
        }

        protected virtual void RemoveItem(int index)
        {
            T removedItem = items[index];
            items.RemoveAt(index);

            //TODO remove item from filteredItems and
            
            OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, removedItem, index));
        }

        protected virtual void SetItem(int index, T item)
        {
            T originalItem = items[index];
            items[index] = item;

            OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Replace, item, originalItem, index));
        }

        private void SortDescriptionsChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            // Anything else that may go on here
            Sort();
        }

        private void Sort()
        {
            // TODO sort
            // This should get called whenever sort descriptions gets updated to keep list in sorted state
            // doesn't need to be called on insertion as insert should take care of putting item in correct place
        }

        private void Refresh()
        {
            int index = 0;
            for (int i = 0; i < items.Count; i++)
            {
                bool visible = (Filter != null) ? Filter(items[i]) : true;

                if (visible)
                {
                    filteredItems[i].VisibleIndex = index;
                }
                else
                {
                    filteredItems[i].VisibleIndex = -1;
                }

                if (visible && !filteredItems[i].Visible)
                {
                    OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add,
                        filteredItems[i].Item, index));
                }
                else if (!visible && filteredItems[i].Visible)
                {
                    OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove,
                        filteredItems[i].Item, index));
                }

                if(visible)
                    index++;

                filteredItems[i].Visible = visible;
            }
        }

        #region INotifyPropertyChanged implementation
        public event PropertyChangedEventHandler PropertyChanged;

        // TODO implement OnPropertyChanged
        #endregion

        #region INotifyCollectionChanged implementation
        public event NotifyCollectionChangedEventHandler CollectionChanged;

        internal void OnCollectionChanged(NotifyCollectionChangedEventArgs e)
        {
            CollectionChanged?.Invoke(this, e);
        }
        #endregion

        [DebuggerDisplay("{VisibleIndex},{Visible}")]
        private class VisibleItem<T>
        {
            public int VisibleIndex;
            public bool Visible;
            public T Item;

            public VisibleItem(int index, T item, bool visible = true)
            {
                VisibleIndex = index;
                Item = item;
                Visible = visible;
            }
        }
    }

    public class SortDescriptions : SortDescriptionCollection
    {
        public void AddListener(NotifyCollectionChangedEventHandler a)
        {
            CollectionChanged += a;
        }
    }
}
