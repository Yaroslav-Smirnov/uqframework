using System;
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
        private const string FilesFolder = @"C:\UQFramework.Demo\Files";
        private const int NumberOfFilesToGenerate = 100000;

        private ObservableCollection<EntityViewModel> _selectedEntities;

        public MainWindow()
        {
            Generator.GenerateFiles(FilesFolder, NumberOfFilesToGenerate, false);

            InitializeComponent();

            var context = new DataStoreContext();

            var list = context.Entities.Select(x => new Item { Id = x.Identifier, Name = x.Name }).ToList();

            dgMain.ItemsSource = list;
        }

        private void TextBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            UpdateGridSource();
        }

        private void dgMain_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            var items = dgMain.SelectedItems.Cast<Item>();

            var selectedIndexes = items.Select(item => item.Id);

            var context = new DataStoreContext();

            _selectedEntities = new ObservableCollection<EntityViewModel>(context.Entities.Where(x => selectedIndexes.Contains(x.Identifier)).Select(x => new EntityViewModel(x, false)).ToList());

            tb1.ItemsSource = _selectedEntities;

            btnSave.Content = $"Save ({_selectedEntities.Count})";

            if (_selectedEntities.Any())
            {
                tb1.SelectedIndex = 0;
            }
        }

        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
            var context = new DataStoreContext();

            foreach (var x in _selectedEntities.Where(x => !(x.IsNew && x.IsDeleted)))
            {
                if (x.IsNew)
                    context.Entities.Add(x.Entity);
                else if (x.IsDeleted)
                    context.Entities.Remove(x.Entity);
                else
                    context.Entities.Update(x.Entity);
            }

            context.SaveChanges();

            UpdateGridSource();
        }

        private void UpdateGridSource()
        {
            var context = new DataStoreContext();

            var list = context.Entities
                             .Where(x => (x.Name ?? string.Empty).IndexOf(tbFilter.Text, StringComparison.InvariantCultureIgnoreCase) >= 0)
                             .Select(x => new Item { Id = x.Identifier, Name = x.Name })
                             .ToList();

            dgMain.ItemsSource = list;
        }

        private void btnAdd_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedEntities == null)
                _selectedEntities = new ObservableCollection<EntityViewModel>();

            var entity = new EntityViewModel(new Entity(), true);

            _selectedEntities.Add(entity);

            tb1.ItemsSource = _selectedEntities;
        }

        private class Item
        {
            public string Id { get; set; }

            public string Name { get; set; }
        }
    }
}
