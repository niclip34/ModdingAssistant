using HandyControl.Tools.Extension;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Documents;

namespace ModdingAssistant
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void VtableRun_Click(object sender, RoutedEventArgs e)
        {
            var functions = new List<Function>();

            var range = new TextRange(VtableInput.Document.ContentStart, VtableInput.Document.ContentEnd);
            foreach (var line in range.Text.Split('\n'))
            {
                var offset = line.Split(new string[] { "offset" /*win*/ , "DCQ" /*android*/ }, StringSplitOptions.None);
                if (offset.Length <= 1)
                    continue;

                var func = new Function();

                // Return Type
                var r = offset[1].Split(new string[] { "->" }, StringSplitOptions.None);
                if (r.Length > 1)
                {
                    func.ReturnType = r[1].Trim();
                    offset[1] = offset[1].Replace("->", "").Replace(r[1], ""); // :skull:
                }

                // Split for comment
                var comment = offset[1].Split(';');

                func.Name = comment[0].Trim(); // ^-^
                if (comment.Length > 1)
                {
                    var commentContent = comment[1];
                    // Read function parameters from comment
                    var match = new Regex("[\\w ;]+(\\(.+\\))").Match(commentContent);
                    if (match.Success)
                    {
                        var param = match.Groups[1].Value;
                        func.Paramerters = param.Substring(1, param.Length - 2);
                        // Replace paramerters to empty
                        commentContent = commentContent.Replace(param, string.Empty);
                    }

                    commentContent = commentContent.Trim();
                    if (commentContent.Length > 1)
                        func.Name = commentContent;
                }

                if (func.Name.Length < 1)
                    func.Name = "function";

                // Clean function name
                var newName = func.Name.Trim();
                Console.WriteLine(newName);
                var nameSpace = newName.Split(new string[] { "__", "::" }, StringSplitOptions.None);
                if (nameSpace.Length > 1)
                {
                    newName = nameSpace[1]; // ClientInstance::getLocalPlayer => getLocalPlayer
                    if (newName == nameSpace[0])
                        newName = "constructor()";
                }

                foreach (var c in "?@()* ")
                    newName = newName.Replace(c.ToString(), "");

                if (newName.Contains("~"))
                    newName = "destructor()";

                var suffix = 0;
                var old = newName;
                while (true)
                {
                    foreach (var f in functions)
                    {
                        if (f.Name == newName)
                        {
                            newName = $"{old}_{suffix}";
                            suffix++;
                            continue;
                        }
                    }

                    break;
                }
                func.Name = newName;

                functions.Add(func);
            }

            var builder = new StringBuilder();
            for (int i = 0; i < functions.Count; i++)
            {
                var function = functions[i];
                var built = string.Format("virtual {0} {1}({2});", function.ReturnType == null ? "void" : function.ReturnType,
                    function.Name, function.Paramerters);
                if (VtablePrint.IsChecked.Value)
                    built += string.Format(" //{0}", i);
                builder.AppendLine(built);
            }

            range = new TextRange(VtableOutput.Document.ContentStart, VtableOutput.Document.ContentEnd);
            range.Text = builder.ToString();
        }
    }
}
