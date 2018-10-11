using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;

namespace AnimatedListTest
{
    public class ObservableSortableFilterableCollection<T> : ObservableCollection<T>
    {
        private IList<T> originalList;
        public int FullCount { get { return originalList.Count; } }
        public SortDescriptions SortDescriptions;

        #region Filter property
        private Predicate<T> _filter;
        public Predicate<T> Filter
        {
            get { return _filter; }
            set
            {
                _filter = value;
                Refresh();
                OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
            }
        }
        #endregion

        #region Constructors
        public ObservableSortableFilterableCollection()
        {
            Setup();
        }

        public ObservableSortableFilterableCollection(List<T> list)
        {
            if (list == null)
                throw new ArgumentNullException("list");

            Setup();
            CopyOriginal(list);
        }

        public ObservableSortableFilterableCollection(IEnumerable<T> collection)
        {
            if (collection == null)
                throw new ArgumentNullException("collection");

            Setup();
            CopyOriginal(collection);
        }

        private void Setup()
        {
            originalList = new List<T>();

            SortDescriptions = new SortDescriptions();
            SortDescriptions.AddListener(OnChanged);
        }

        private void CopyOriginal(IEnumerable<T> collection)
        {
            if (collection != null && originalList != null)
            {
                using (IEnumerator<T> enumerator = collection.GetEnumerator())
                {
                    while (enumerator.MoveNext())
                    {
                        originalList.Add(enumerator.Current);
                    }
                }
            }
        }
        #endregion

        public void OnChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            Refresh();
        }

        protected override void InsertItem(int index, T item)
        {
            originalList.Insert(index, item);
            base.InsertItem(index, item);

            //Need to insert it into correct place and refresh
        }

        protected override void RemoveItem(int index)
        {
            originalList.RemoveAt(index);
            base.RemoveItem(index);
        }

        protected override void MoveItem(int oldIndex, int newIndex)
        {
            T originalItem = originalList[oldIndex];
            Items.RemoveAt(oldIndex);
            Items.Insert(newIndex, originalItem);

            OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Move, originalItem, newIndex, oldIndex));
        }

        protected override void SetItem(int index, T item)
        {
            T originalItem = originalList[index];
            originalList[index] = item;

            OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Replace, originalItem, item, index));
        }

        protected override void ClearItems()
        {
            originalList.Clear();
            base.ClearItems();
        }

        public void Refresh()
        {
            Items.Clear();

            // TODO Sort original then apply filter to add to items

            foreach(T item in originalList)
            {
                if (Filter == null)
                {
                    Items.Add(item);
                }
                else
                {
                    if (Filter(item))
                    {
                        Items.Add(item);
                    }
                }
            }
        }
    }
}
