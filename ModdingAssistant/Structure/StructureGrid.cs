using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows;

namespace ModdingAssistant.Structure
{
    public class StructureGrid : Grid
    {
        private MainWindow Window;
        private TextBlock NameBlock { get; set; }
        private TextBox RenameBox { get; set; }

        private string Fields;

        public StructureGrid(MainWindow window, string bindingName, string name, string defaultFields = null)
        {
            this.Window = window;
            this.Fields = defaultFields;

            var binding = new Binding("ActualWidth");
            binding.ElementName = bindingName;
            this.SetBinding(WidthProperty, binding);

            // Add Elements
            NameBlock = new TextBlock();
            NameBlock.Text = name;
            NameBlock.Foreground = new SolidColorBrush(Colors.White);
            NameBlock.Margin = new Thickness(0, 6, 0, 0); // IDK
            this.Children.Add(NameBlock);

            RenameBox = new TextBox();
            RenameBox.Text = name;
            RenameBox.Foreground = new SolidColorBrush(Colors.White);
            RenameBox.BorderBrush = null;
            RenameBox.CaretBrush = null;
            RenameBox.Visibility = Visibility.Hidden;
            RenameBox.KeyDown += RenameBox_KeyDown;
            this.Children.Add(RenameBox);
        }

        public void StartRenaming()
        {
            RenameBox.Text = NameBlock.Text;
            NameBlock.Visibility = Visibility.Hidden;
            RenameBox.Visibility = Visibility.Visible;
        }

        public void StopRenaming(bool cancelled = false)
        {
            if (!cancelled)
                NameBlock.Text = Window.RefactorStructureName(RenameBox.Text, this);
            NameBlock.Visibility = Visibility.Visible;
            RenameBox.Visibility = Visibility.Hidden;
        }

        public string GetName()
        {
            return NameBlock.Text;
        }

        public void SetFields(string content)
        {
            this.Fields = content;
        }

        public string GetFields()
        {
            return Fields;
        }

        private void RenameBox_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
                StopRenaming();
            else if (e.Key == Key.Escape)
                StopRenaming(true);
        }
    }
}
