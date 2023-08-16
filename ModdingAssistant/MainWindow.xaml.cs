using HandyControl.Tools.Extension;
using Microsoft.Win32;
using ModdingAssistant.Processer;
using ModdingAssistant.Processers;
using ModdingAssistant.Structure;
using Newtonsoft.Json.Linq;
using System;
using System.IO;
using System.Reflection;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace ModdingAssistant
{
    public partial class MainWindow : Window
    {
        private StructureGrid currentInput = null;
        private bool structureSaved = true;

        public MainWindow()
        {
            InitializeComponent();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (structureSaved)
                return;

            var result = MessageBox.Show("Changed content will not saved!\nDo you still want to close this tool?", "Warning", 
                MessageBoxButton.OKCancel, MessageBoxImage.Warning);
            if (result != MessageBoxResult.OK)
                e.Cancel = true;
        }

        private void VtableRun_Click(object sender, RoutedEventArgs e)
        {            
            var processor = new VtableProcessor(VtablePrint.IsChecked.Value);
            var result = processor.Process(VtableInput.Text);
            VtableOutput.Text = result;
        }

        private void StructureAdd_Click(object sender, RoutedEventArgs e)
        {
            var builder = new StringBuilder();
            var examples = new string[]
            {
                "// append offset_vtable if your sdk has virtual function",
                "#offset_vtable\n",
                "MinecraftGame* minecraftGame: 0xC8 // default size is 8",
                "int someFiled: 0x200, 4 // size will be 4"
            };
            foreach (var line in examples)
                builder.AppendLine(line);

            var grid = new StructureGrid(this, StructuresList.Name, RefactorStructureName("Class"), builder.ToString());
            StructuresList.Items.Add(grid);

            StructuresList.SelectedIndex = StructuresList.Items.Count - 1;
            UpdateCurrent();
            structureSaved = false;
        }

        private void StructureRename_Click(object sender, RoutedEventArgs e)
        {
            var index = StructuresList.SelectedIndex;
            if (index == -1)
                return;

            ((StructureGrid)StructuresList.Items[index]).StartRenaming();
            structureSaved = false;
        }

        private void StructuresList_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            var index = StructuresList.SelectedIndex;
            if (index == -1)
                return;

            ((StructureGrid)StructuresList.Items[index]).StartRenaming();
        }

        private void StructureDelete_Click(object sender, RoutedEventArgs e)
        {
            var index = StructuresList.SelectedIndex;
            if (index == -1)
                return;

            StructuresList.Items.RemoveAt(index);
            structureSaved = false;
        }

        private void StructuresList_KeyDown(object sender, KeyEventArgs e)
        {
            var index = StructuresList.SelectedIndex;
            if (index == -1)
                return;

            if (e.Key == Key.Delete)
            {
                StructuresList.Items.RemoveAt(index);
                structureSaved = false;
            }
        }

        private void StructureRun_Click(object sender, RoutedEventArgs e)
        {
            var processor = new StructureProcessor(StructurePrint.IsChecked.Value);
            var result = processor.Process(StructureInput.Text);
            StructureOutput.Text = result;
        }

        private void StructuresList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateCurrent();
        }

        private void StructureInput_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (currentInput != null)
            {
                currentInput.SetFields(StructureInput.Text);
                structureSaved = false;
            }
        }

        private void StructureImport_Click(object sender, RoutedEventArgs e)
        {
            if (!structureSaved)
            {
                var result = MessageBox.Show("There are changed content of structure that you didn't save.\nDo you want to continue?",
                    "Warning", MessageBoxButton.OKCancel, MessageBoxImage.Warning);
                if (result != MessageBoxResult.OK)
                    return;
                structureSaved = true;
            }

            var ofd = new OpenFileDialog();
            ofd.Filter = ".json File (*.json)|*.json|All Type (*.*)|*.*";
            if (ofd.ShowDialog().Value)
            {
                StructuresList.Items.Clear();

                var json = JObject.Parse(File.ReadAllText(ofd.FileName));
                foreach (var obj in json)
                {
                    var grid = new StructureGrid(this, StructuresList.Name, obj.Key, obj.Value.ToString());
                    StructuresList.Items.Add(grid);
                }

                StructuresList.SelectedIndex = StructuresList.Items.Count - 1;
                UpdateCurrent();

                structureSaved = true;
            }
        }

        private void StructureExport_Click(object sender, RoutedEventArgs e)
        {
            var sfd = new SaveFileDialog();
            sfd.FileName = "structures.json";
            sfd.Filter = ".json File (*.json)|*.json";
            if (sfd.ShowDialog().Value)
            {
                var json = new JObject();
                foreach (var item in StructuresList.Items)
                {
                    var grid = (StructureGrid)item;
                    json[grid.GetName()] = grid.GetFields();
                }

                File.WriteAllText(sfd.FileName, json.ToString());
                structureSaved = true;
            }
        }

        private void UpdateCurrent()
        {
            var index = StructuresList.SelectedIndex;
            if (index == -1)
                return;

            var grid = (StructureGrid) StructuresList.Items[index];
            currentInput = grid;
            StructureInput.Text = grid.GetFields();
        }

        public string RefactorStructureName(string original, StructureGrid from = null)
        {
            "@?\\><{}^|*()&%$#\"!\'-+*:".ForEach(c => original = original.Replace(c.ToString(), ""));
            original = original.Trim();

            var refactored = original;
            var count = 0;
            while (true)
            {
                var flagged = false;
                foreach (var grid in StructuresList.Items)
                {
                    var st = (StructureGrid)grid;
                    if (st.GetName().ToLower().Equals(refactored, StringComparison.OrdinalIgnoreCase)
                        && from != st)
                    {
                        count++;
                        refactored = string.Format("{0}_{1}", original, count);
                        flagged = true;
                        break;
                    }
                }
                if (!flagged)
                    break;
            }

            return refactored;
        }
    }
}
