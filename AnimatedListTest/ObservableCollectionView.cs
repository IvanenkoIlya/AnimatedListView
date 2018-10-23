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
        where T : INotifyPropertyChanged
    {
        IList<T> items;
        Dictionary<int, VisibleItem> filteredItems;
        
        public T this[int index]
        {
            get { return items[index]; }
            set
            {
                if (index < 0 || index >= items.Count)
                    throw new ArgumentOutOfRangeException("index");

                SetItem(index, value);
            }
        }
        
        public int Count { get { return items.Count; } }
        public int VisibleCount { get { return filteredItems.Values.Where(x => x.Visible).Count(); } }

        // TODO: implement later
        public bool IsReadOnly { get { return false; } }

        /// <summary>
        /// List of SortDescription objects used to keep the ObsevableCollectionView in order. Sorts the collection in order of SortDescription. 
        /// </summary>
        /// <exception cref="System.ArgumentException">
        /// Thrown when SortDescription "propertyName" is not found in object T
        /// </exception>
        /// <exception cref="System.ArgumentException">
        /// Thrown when object T's property named "propertyName" is not of type IComparable
        /// </exception>
        public ObservableSortDescriptions SortDescriptions;

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
            filteredItems = new Dictionary<int, VisibleItem>();

            SortDescriptions = new ObservableSortDescriptions();
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
            if (collection != null && items != null)
            {
                using (IEnumerator<T> enumerator = collection.GetEnumerator())
                {
                    while (enumerator.MoveNext())
                    {
                        items.Add(enumerator.Current);
                    }
                }
            }
        }
        #endregion

        public int IndexOf(T item)
        {
            return items.IndexOf(item);
            
        }

        public int VisibleIndexOf(T item)
        {
            return filteredItems[items.IndexOf(item)].VisibleIndex;
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

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable)items).GetEnumerator();
        }

        #region Insert/Add
        public void Add(T item)
        {
            int index = items.Count;
            
            if (SortDescriptions.Any())
            {
                index = GetSortedIndex(item);
            }

            InsertItem(index, item);
        }        

        public void Insert(int index, T item)
        {
            if (SortDescriptions.Any())
                throw new ArgumentException("Cannot insert item at specific index if ObservableCollectionView has any SortDescriptions, use Add instead", "index");

            if (index < 0 || index >= items.Count)
                throw new ArgumentOutOfRangeException("index");

            InsertItem(index, item);
        }

        protected virtual void InsertItem(int index, T item)
        {
            InsertItem(index, item, false);
        }

        private void InsertItem(int index, T item, bool supressCollectionChanged)
        {
            bool visible = (Filter != null) ? Filter(item) : true;
            int visibleIndex = -1;

            // Move up all items after that are after index
            for (int i = items.Count; i > index; i--)
            {
                filteredItems[i] = filteredItems[i - 1];

                // Update visible index if new item is also visable
                if (filteredItems[i].Visible && visible)
                {
                    visibleIndex = filteredItems[i].VisibleIndex;
                    filteredItems[i].VisibleIndex++;
                }
            }

            // If inserting at end of list and item is visible, set visible index as largest VisibleIndex
            if (index == items.Count && visible)
            {
                VisibleItem last = (filteredItems.Values.Where(x => x.Visible)).LastOrDefault();

                if (last != null)
                    visibleIndex = last.VisibleIndex + 1;
                else
                    visibleIndex = 0;
            }

            items.Insert(index, item);

            filteredItems[index] = new VisibleItem(visibleIndex, item, visible);

            item.PropertyChanged += Item_PropertyChanged;

            if (!supressCollectionChanged && visible)
                OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, item, visibleIndex));
        }
        #endregion

        #region Remove
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

        protected virtual void RemoveItem(int index)
        {
            RemoveItem(index, false);
        }

        private void RemoveItem(int index, bool supressCollectionChanged)
        {
            int oldCount = Count;
            VisibleItem removedItem = filteredItems[index];
            items.RemoveAt(index);

            for (int i = index + 1; i < oldCount; i++)
            {
                if (removedItem.Visible && filteredItems[i].Visible)
                    filteredItems[i].VisibleIndex--;

                filteredItems[i - 1] = filteredItems[i];
            }

            filteredItems.Remove(filteredItems.Count - 1);

            removedItem.Item.PropertyChanged -= Item_PropertyChanged;

            if (!supressCollectionChanged && removedItem.Visible)
                OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, removedItem.Item, index));
        }
        #endregion

        protected virtual void MoveItem(int oldIndex, int newIndex)
        {
            T removedItem = items[oldIndex];

            int oldVisibleIndex = filteredItems[oldIndex].VisibleIndex;
            bool visible = filteredItems[oldIndex].Visible;

            RemoveItem(oldIndex, true);
            InsertItem(newIndex, removedItem, true);

            int newVisibleIndex = filteredItems[newIndex].VisibleIndex;
            
            if(visible)
                OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Move, removedItem, newVisibleIndex, oldVisibleIndex));
        }
        
        protected virtual void SetItem(int index, T item)
        {
            T originalItem = items[index];
            items[index] = item;
            filteredItems[index].Item = item;

            if(filteredItems[index].Visible)
                OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Replace, item, originalItem, index));

            if (SortDescriptions.Any())
                ResortItem(item);
        }

        #region Clear
        public void Clear()
        {
            ClearItems();
        }

        protected virtual void ClearItems()
        {
            filteredItems.Clear();
            items.Clear();

            OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
        }
        #endregion

        #region Helper methods
        private int GetSortedIndex(T item)
        {
            int i = 0;

            foreach (SortDescription sd in SortDescriptions)
            {
                IComparable value = item.GetType().GetProperty(sd.PropertyName).GetValue(item) as IComparable;
                int diff = value.CompareTo((IComparable)items[i].GetType().GetProperty(sd.PropertyName).GetValue(items[i]));

                while (sd.Direction == ListSortDirection.Ascending ? diff > 0 : diff < 0)
                {
                    i++;
                    if (i == Count)
                        break;
                    diff = value.CompareTo((IComparable)items[i].GetType().GetProperty(sd.PropertyName).GetValue(items[i]));
                }

                if (i == Count)
                    break;
            }

            return i;
        }

        private void ResortItem(T item)
        {
            int oldIndex = IndexOf(item);
            int oldVisibleIndex = filteredItems[oldIndex].VisibleIndex;
            bool oldVisible = filteredItems[oldIndex].Visible;
            bool newVisible = (Filter != null) ? Filter(item) : true;

            // if item was visible but is now filtered out, don't suppress the CollectionChanged event 
            RemoveItem(oldIndex, (oldVisible && !newVisible) ? false : true);

            int newIndex = GetSortedIndex(item);

            // if item was filtered out but is now visible, don't suppress the CollectionChanged event
            InsertItem(newIndex, item, (!oldVisible && newVisible) ? false : true);
            
            int newVisibleIndex = filteredItems[newIndex].VisibleIndex;
            
            // if item remained visible, notify Move event
            if (oldVisible && newVisible)
                OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Move, item, newVisibleIndex, oldVisibleIndex));
        }

        #region Refresh
        public void Refresh()
        {
            int index = 0;
            for (int i = 0; i < items.Count; i++)
            {
                bool visible = (Filter != null) ? Filter(items[i]) : true;

                if (visible)
                {
                    filteredItems[i].VisibleIndex = index;

                    if (!filteredItems[i].Visible)
                    {
                        OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add,
                            filteredItems[i].Item, index));
                    }

                    index++;
                }
                else
                {
                    filteredItems[i].VisibleIndex = -1;

                    if (filteredItems[i].Visible)
                    {
                        OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove,
                        filteredItems[i].Item, index));
                    }
                }

                filteredItems[i].Visible = visible;
            }
        }
        #endregion

        #region MergeSort
        public void MergeSort()
        {
            int[] index = new int[Count];
            for (int i = 0; i < index.Count(); i++) index[i] = i;

            Sort(index, 0, index.Length - 1);

            for(int i = 0; i < index.Length; i++)
            {
                if( i != index[i])
                {
                    MoveItem(index[i], i);
                }

                for (int j = i; j < index.Length; j++)
                {
                    if (index[j] < index[i]) index[j]++;
                }
            }
        }

        private void Sort(int[] arr, int left, int right)
        {
            if (left < right)
            {
                int mid = (left + right) / 2;

                Sort(arr, left, mid);
                Sort(arr, mid + 1, right);

                Merge(arr, left, mid, right);
            }
        }

        private void Merge(int[] arr, int left, int mid, int right)
        {
            int size1 = mid - left + 1;
            int size2 = right - mid;

            int[] leftArr = new int[size1];
            int[] rightArr = new int[size2];

            int i, j;

            for (i = 0; i < size1; ++i)
                leftArr[i] = arr[left + i];

            for (j = 0; j < size2; ++j)
                rightArr[j] = arr[mid + 1 + j];

            i = 0; j = 0;
            int k = left;

            using (IEnumerator<SortDescription> e = SortDescriptions.GetEnumerator())
            {
                while (i < size1 && j < size2)
                {
                    if (!e.MoveNext())
                    {
                        e.Reset();
                        arr[k] = leftArr[i];
                        i++;
                        k++;
                    }
                    else
                    {
                        string propertyName = e.Current.PropertyName;
                        int compare = ((IComparable)typeof(T).GetProperty(propertyName).GetValue(items[leftArr[i]])).CompareTo(
                            ((IComparable)typeof(T).GetProperty(propertyName).GetValue(items[rightArr[j]])));

                        if (compare != 0)
                        {
                            if (compare < 0)
                            {
                                if (e.Current.Direction == ListSortDirection.Ascending)
                                {
                                    arr[k] = leftArr[i];
                                    i++;
                                }
                                else
                                {
                                    arr[k] = rightArr[j];
                                    j++;
                                }
                                e.Reset();
                            }
                            else
                            {
                                if (e.Current.Direction == ListSortDirection.Ascending)
                                {
                                    arr[k] = rightArr[j];
                                    j++;
                                }
                                else
                                { 
                                    arr[k] = leftArr[i];
                                    i++;
                                }
                                e.Reset();
                            }

                            k++;
                        }
                    }
                }
            }

            while (i < size1)
            {
                arr[k] = leftArr[i];
                i++;
                k++;
            }

            while (j < size2)
            {
                arr[k] = rightArr[j];
                j++;
                k++;
            }
        }
        #endregion
        #endregion

        #region Event Handlers
        private void Item_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if(SortDescriptions.Any(x => x.PropertyName == e.PropertyName))
            {
                ResortItem((T)sender);
                Refresh();
            }
        }

        private void SortDescriptionsChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if(e.NewItems != null && e.NewItems.Count > 0)
            {
                var propertyName = ((SortDescription) e.NewItems[0]).PropertyName;
                if (typeof(T).GetProperty(propertyName) == null)
                    throw new ArgumentException($"Property \"{propertyName}\" not found in object of type {typeof(T)}");

                if (typeof(T).GetProperty(propertyName).PropertyType.GetInterface("IComparable") == null)
                    throw new ArgumentException($"Property \"{propertyName}\" of type \"{typeof(T).GetProperty(propertyName).PropertyType.FullName}\" does not implement IComparable");
            }

            // Don't need to sort if the last sort condidition was removed as list is already sorted by remaining criteria
            if (!(e.Action == NotifyCollectionChangedAction.Remove && e.OldStartingIndex == SortDescriptions.Count - 1))
                MergeSort();
        }
        #endregion

        #region INotifyPropertyChanged implementation
        public event PropertyChangedEventHandler PropertyChanged;
        protected internal void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        #endregion

        #region INotifyCollectionChanged implementation
        public event NotifyCollectionChangedEventHandler CollectionChanged;

        internal void OnCollectionChanged(NotifyCollectionChangedEventArgs e)
        {
            CollectionChanged?.Invoke(this, e);
        }
        #endregion

        #region Nested class to house dictionary entries for filtered list
        [DebuggerDisplay("Index: {VisibleIndex} ; Item: {Item} ; Visible: {Visible}")]
        private class VisibleItem
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
        #endregion
    }

    public class ObservableSortDescriptions : SortDescriptionCollection
    {
        public void AddListener(NotifyCollectionChangedEventHandler a)
        {
            CollectionChanged += a;
        }
    }
}
