using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Data;

namespace AnimatedListTest
{
    

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        ObservableSortableFilterableCollection<int> test;
        ObservableCollectionView<string> collection;

        public MainWindow()
        {
            DataContext = this;

            InitializeComponent();

            collection = new ObservableCollectionView<string>()
            {
                "test0",
                "test1",
                "test1",
                "test2",
                "test3",
                "test4"
            };

            //collection.Remove("test1");

            test = new ObservableSortableFilterableCollection<int>()
            {
                1,1,1,5,5,5
            };
            
            IC.ItemsSource = collection;
        }

        private void MoveItemClicked(object sender, RoutedEventArgs e)
        {
            Random rand = new Random();
            int index = rand.Next(collection.Count);
            int index2 = rand.Next(collection.Count);

            //collection.Move(index, index2);
        }
        
        private void AddItemClicked(object sender, RoutedEventArgs e)
        {
            collection.Add(TB1.Text);
            TB1.Clear();
        }

        private void FilterClicked(object sender, RoutedEventArgs e)
        {
            if (collection.Filter == null)
                collection.Filter = (x => int.Parse(x.Substring(x.Length - 1)) > 2);
            else
                collection.Filter = null;
        }

        private void DeleteItemClicked(object sender, RoutedEventArgs e)
        {
            if (collection.Count > 0)
                collection.RemoveAt(0);
        }

        private void SetItemClicked(object sender, RoutedEventArgs e)
        {
            if (collection.Count > 0)
            {
                collection[0] = TB2.Text;
                TB2.Clear();
            }
        }

        #region INotifyPropertyChanged implementation
        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string property)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(property));
        }
        #endregion
    }
}
