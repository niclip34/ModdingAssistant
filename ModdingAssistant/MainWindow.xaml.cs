using HandyControl.Tools.Extension;
using ModdingAssistant.Processer;
using ModdingAssistant.Processers;
using ModdingAssistant.Structure;
using System;
using System.Reflection;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;

namespace ModdingAssistant
{
    public partial class MainWindow : Window
    {
        private StructureGrid currentInput = null;

        public MainWindow()
        {
            InitializeComponent();
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
                "// append offset_vtable if the sdk has virtual function",
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
        }

        private void StructureRename_Click(object sender, RoutedEventArgs e)
        {
            var index = StructuresList.SelectedIndex;
            if (index == -1)
                return;

            ((StructureGrid)StructuresList.Items[index]).StartRenaming();
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
        }

        private void StructuresList_KeyDown(object sender, KeyEventArgs e)
        {
            var index = StructuresList.SelectedIndex;
            if (index == -1)
                return;

            if (e.Key == Key.Delete)
                StructuresList.Items.RemoveAt(index);
        }

        private void StructureRun_Click(object sender, RoutedEventArgs e)
        {
            var processor = new StructureProcessor();
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
