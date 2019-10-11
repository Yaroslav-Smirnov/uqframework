using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Linq;
using System.Windows;

namespace UQFrameWork.Demo
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly string _cacheFolder = @"C:\UQFramework.Demo\_Cache";
        private readonly string _filesFolder = @"C:\UQFramework.Demo\Files";

        private readonly List<Item> _mainList;

        private ObservableCollection<Entity> _selectedEntities;
        private IList<Entity> _deletedEntities = new List<Entity>();

        public MainWindow()
        {
            Generator.GenerateFiles(_filesFolder, 100000, false);

            InitializeComponent();

            var stopwatch = new System.Diagnostics.Stopwatch();
            stopwatch.Start();

            var context = new DataStoreContext();

            var list = context.Entities.Select(x => new Item { Id = x.Identifier, Name = x.Name }).ToList();

            stopwatch.Stop();

            _mainList = list;

            dgMain.ItemsSource = list;
        }

        private void TextBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            UpdateGridSource();
        }

        private class Item
        {
            public string Id { get; set; }

            public string Name { get; set; }
        }

        private void dgMain_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            var items = dgMain.SelectedItems.Cast<Item>();

            var selectedIndexes = items.Select(item => item.Id);

            var context = new DataStoreContext();

            var stopWatch = new System.Diagnostics.Stopwatch();

            stopWatch.Start();

            _selectedEntities = new ObservableCollection<Entity>(context.Entities.Where(x => selectedIndexes.Contains(x.Identifier)).ToList());

            stopWatch.Stop();

            System.Diagnostics.Debug.WriteLine(stopWatch.ElapsedMilliseconds);

            tb1.ItemsSource = _selectedEntities; // new[] { _selectedEntity };

            btnSave.Content = $"Save ({_selectedEntities.Count})";

            if (_selectedEntities.Any())
            {
                tb1.SelectedIndex = 0;
            }
        }

        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
            var context = new DataStoreContext();

            foreach (var x in _selectedEntities)
            {
                if (x.IsAdded)
                    context.Entities.Add(x);
                else if (x.IsDeleted)
                    context.Entities.Remove(x);
                else
                    context.Entities.Update(x);
            }

            context.SaveChanges();

            UpdateGridSource();
        }

        private void UpdateGridSource()
        {
            var context = new DataStoreContext();

            var stopWatch = new System.Diagnostics.Stopwatch();

            stopWatch.Start();

            var list = context.Entities
                             .Where(x => (x.Name ?? string.Empty).IndexOf(tbFilter.Text, StringComparison.InvariantCultureIgnoreCase) >= 0)
                             .Select(x => new Item { Id = x.Identifier, Name = x.Name })
                             .ToList(); // removing ToList() causes a sort of recursive cache access so that exeception happens in MemoryCache on attempt to acquire SlimReaderLock

            //stopWatch.Stop();
            //
            //System.Diagnostics.Debug.WriteLine($"{stopWatch.ElapsedMilliseconds}");

            //stopWatch.Start();

            //var count = context.Entities
            //                 //.Select(x => new Item { Id = x.Identifier, Name = x.Name })
            //                 .Count(x => (x.Name ?? string.Empty).IndexOf(tbFilter.Text, StringComparison.InvariantCultureIgnoreCase) >= 0);

            stopWatch.Stop();

            System.Diagnostics.Debug.WriteLine($"{stopWatch.ElapsedMilliseconds}");


            //var range = Enumerable.Range(1, 20000).Select(i => i.ToString());
            //
            //var stopWatch = new System.Diagnostics.Stopwatch();
            //
            //stopWatch.Start();
            //
            //var newList = context.Entities.Where(x => range.Contains(x.Identifier)).Select(x => new Item { Id = x.Identifier, Name = x.Name }).ToList();
            //
            //stopWatch.Stop();
            //
            //System.Diagnostics.Debug.WriteLine($"{stopWatch.ElapsedMilliseconds}"); //Average ~40ms

            dgMain.ItemsSource = list;
        }

        private void btnAdd_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedEntities == null)
                _selectedEntities = new ObservableCollection<Entity>();

            _selectedEntities.Add(new Entity { IsAdded = true });

            tb1.ItemsSource = _selectedEntities;

        }
    }
}
