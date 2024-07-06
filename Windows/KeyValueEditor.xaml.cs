using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.ComponentModel;
using System.Collections.ObjectModel;

namespace FanSync.Windows
{
    public class TableEntry
    {
        public string Key { get; set; }
        public string Value { get; set; }
        public bool Protected { get; set; }

        public TableEntry()
        {
            Key = "";
            Value = "";
            Protected = true;
        }

        public TableEntry(string key, string value, bool isProtected = false)
        {
            Key = key;
            Value = value;
            Protected = isProtected;
        }

        public bool IsValid()
        {
            return !string.IsNullOrEmpty(Key?.Trim());
        }

        public override bool Equals(object other)
        {
            if (other is TableEntry o)
                return Key?.Equals(o.Key) == true;
            else
                return false;
        }

        public override int GetHashCode()
        {
            return Key.GetHashCode();
        }
    }

    public partial class KeyValueEditor : Window
    {
        private Dictionary<string, string> Initial { get; }
        public ObservableCollection<TableEntry> Current { get; set; }
        public IReadOnlyList<string> Required { get; set; }

        private TaskCompletionSource<bool> Tcs { get; set; }

        public KeyValueEditor(Window owner, string title, Dictionary<string, string> initial, IReadOnlyList<string> required)
        {
            Owner = owner;
            Initial = initial;
            Current = new ObservableCollection<TableEntry>(initial.Select(x => new TableEntry(x.Key, x.Value, required.Contains(x.Key))));
            Required = required;

            InitializeComponent();

            Title = title;
        }

        private void MainGrid_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.Delete:
                    e.Handled = true;

                    var entry = MainGrid.SelectedItem as TableEntry;

                    if (entry != null)
                    {
                        if (!Required.Contains(entry.Key))
                            Current.Remove(entry);
                    }
                    break;
            }
        }

        private void Ok_Click(object sender, RoutedEventArgs e)
        {
            Tcs?.TrySetResult(true);
        }
        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            Tcs?.TrySetResult(false);
        }
        private void Window_Closing(object sender, CancelEventArgs e)
        {
            Tcs?.TrySetResult(false);
        }

        public async Task<Dictionary<string, string>> ShowAndWait()
        {
            Tcs = new TaskCompletionSource<bool>();
            Show();

            bool result = await Tcs.Task;
            Tcs = null;
            Close();

            if (result)
            {
                return Current.Where(x => x.IsValid()).Distinct().ToDictionary(x => x.Key, x => x.Value);
            }
            else
            {
                return Initial;
            }
        }
    }
}
