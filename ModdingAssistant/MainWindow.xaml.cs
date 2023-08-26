using HandyControl.Tools.Extension;
using Microsoft.Win32;
using ModdingAssistant.Processer;
using ModdingAssistant.Processers;
using ModdingAssistant.Structure;
using Newtonsoft.Json.Linq;
using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading;
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


        private void InjectButton_Click(object sender, RoutedEventArgs e)
        {
            var path = InjectorTextBox.Text;
            if (!File.Exists(path))
            {
                MessageBox.Show("Please input correct path to dll!", "Error", MessageBoxButton.OK, MessageBoxImage.Hand);
                return;
            }

            if (!MemoryHelper.CheckMinecraft())
            {
                MessageBox.Show("Please launch Minecraft to inject your dll!", "Error", MessageBoxButton.OK, MessageBoxImage.Hand);
                return;
            }

            string dllpath = GetDllPath(path);
            if (MemoryHelper.AlreadyInjected(dllpath))
            {
                MemoryHelper.Unload(dllpath);
                Thread.Sleep(300);
            }
            File.Copy(path, dllpath, true);

            MemoryHelper.AddPermission(path);
            MemoryHelper.Inject(path);
            MessageBox.Show("Injected!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void InjectorRefButton_Click(object sender, RoutedEventArgs e)
        {
            var ofd = new OpenFileDialog();
            ofd.Filter = ".DLL File (*.dll)|*.dll|All File (*.*)|*.*";
            if (ofd.ShowDialog().Value)
            {
                InjectorTextBox.Text = ofd.FileName;
            }
        }

        private void InjectorRefresh_Click(object sender, RoutedEventArgs e)
        {
            if (!MemoryHelper.CheckMinecraft())
                return;

            InjectorModules.Items.Clear();
            foreach (ProcessModule module in MemoryHelper.GetMinecraftProcess().Modules)
            {
                if (module.ModuleName == "Minecraft.Windows.exe" || module.FileName.ToLower().Contains("system32"))
                    continue;
                InjectorModules.Items.Add(module.ModuleName);
            }
            InjectorModules.SelectedIndex = 0;
        }

        private void UnloadButton_Click(object sender, RoutedEventArgs e)
        {
            if (InjectorModules.SelectedIndex == -1)
                return;

            foreach (ProcessModule module in MemoryHelper.GetMinecraftProcess().Modules)
            {
                if (module.ModuleName == "Minecraft.Windows.exe" || module.FileName.ToLower().Contains("system32"))
                    continue;

                if (module.ModuleName == (string)InjectorModules.Items[InjectorModules.SelectedIndex])
                {
                    MemoryHelper.Unload(module.FileName);
                    MessageBox.Show("Unloaded", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }
            }
        }

        private void DumpButton_Click(object sender, RoutedEventArgs e)
        {
            if (InjectorModules.SelectedIndex == -1)
                return;

            var sfd = new SaveFileDialog();
            sfd.Filter = ".Dll File (*.dll)|*.dll|All File (*.*)|*.*";
            if (!sfd.ShowDialog().Value)
                return;

            foreach (ProcessModule module in MemoryHelper.GetMinecraftProcess().Modules)
            {
                if (module.ModuleName == "Minecraft.Windows.exe" || module.FileName.ToLower().Contains("system32"))
                    continue;

                if (module.ModuleName == (string)InjectorModules.Items[InjectorModules.SelectedIndex])
                {
                    MemoryHelper.DumpToFile(module.FileName, sfd.FileName);
                    MessageBox.Show("Dumped", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }
            }
        }

        private void UpdateCurrent()
        {
            var index = StructuresList.SelectedIndex;
            if (index == -1)
                return;

            var grid = (StructureGrid)StructuresList.Items[index];
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

        private string GetDllPath(string dllpath)
        {
            string filename = Path.GetFileName(dllpath);
            return Path.Combine(Path.GetTempPath(), filename);
        }
    }
}
