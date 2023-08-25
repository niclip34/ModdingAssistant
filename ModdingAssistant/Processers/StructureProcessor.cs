using ModdingAssistant.Structure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ModdingAssistant.Processers
{
    internal class StructureProcessor : IProcessor
    {
        private bool PrintOffset { get; set; }

        public StructureProcessor(bool printOffset)
        {
            this.PrintOffset = printOffset;
        }

        public string Process(string input)
        {
            var result = new StringBuilder();
            var vtable = false;
            var fields = ReadFields(input, ref vtable, result);
            fields.Sort(delegate (StructureField a, StructureField b)
            {
                return a.Offset - b.Offset;
            });

            var currentPos = 0;
            var pad = 0;
            if (vtable)
                currentPos += 8;

            for (int i = 0; i < fields.Count; i++)
            {
                // append offset
                if (fields[i].Offset > 0)
                {
                    if (!vtable && i == 0)
                        currentPos--;
                    var diff = fields[i].Offset - currentPos - 1;
                    if (diff < -1)
                    {
                        result.AppendLine("// Invalid field " + fields[i].Name);
                        continue;
                    }
                    else if (diff > 0)
                    {
                        result.AppendLine("private:");
                        result.AppendLine($"char padding_{pad}[0x{diff.ToString("x")}];");
                        currentPos += diff;
                        pad++;
                    }
                } 

                result.AppendLine("public:");
                result.Append(string.Format("{0} {1};", fields[i].FieldType, fields[i].Name));
                if (PrintOffset)
                    result.Append($" //0x{fields[i].Offset.ToString("x")}");
                result.AppendLine();

                currentPos += fields[i].Size;
            }

            Console.WriteLine(result.ToString());

            return result.ToString();
        }

        private List<StructureField> ReadFields(string input, ref bool vtable, StringBuilder error)
        {
            var fields = new List<StructureField>();

            foreach (var line in input.Split('\n'))
            {
                if (line.Trim().ToLower() == "#offset_vtable")
                {
                    vtable = true;
                    continue;
                }

                string fieldType = null;
                string name = null;
                int offset = -1;
                int size = 8;

                var removedComment = line.Split(new string[] { "//" }, StringSplitOptions.None)[0];

                var typeinfo = removedComment.Split(':');
                if (typeinfo.Length > 1)
                {
                    if (!TryReadTypeInfo(typeinfo[0], ref fieldType, ref name))
                    {
                        error.AppendLine(string.Format("// Failed to read typeinfo : {0}", line));
                        continue;
                    }

                    if (!TryReadOffset(typeinfo[1], ref offset))
                    {
                        error.AppendLine(string.Format("// Failed to read offset : {0}", line));
                        continue;
                    }
                }

                var sizeInfo = removedComment.Split(',');
                if (sizeInfo.Length > 1 && !TryReadOffset(sizeInfo[1], ref size))
                {
                    error.AppendLine(string.Format("// Failed to read size : {0}", line));
                    continue;
                }

                if (IsNullOrEmpty(fieldType) || IsNullOrEmpty(name) || offset == -1)
                {
                    if (removedComment.Trim().Length > 0)
                        error.AppendLine(string.Format("// Skipped line: {0}", line));
                    continue;
                }

                fields.Add(new StructureField(name, fieldType, offset, size));
            }

            return fields;
        }

        private bool TryReadTypeInfo(string typeinfo, ref string fieldType, ref string name)
        {
            var definition = typeinfo.Split(' ');
            var readingIndex = 0;
            foreach (var d in definition)
            {
                if (!(d.Trim().Length > 1))
                    continue;

                if (readingIndex == 0)
                    fieldType = d.Trim();
                else if (readingIndex == 1)
                    name = definition[1].Trim();
                readingIndex++;
            }

            if (readingIndex != 2)
            {
                return false;
            }

            return true;
        }

        private bool TryReadOffset(string line, ref int offset)
        {
            var allowedChars = new char[]
            {
                    '0', '1', '2', '3', '4', '5', '6', '7', '8', '9',
                    'a', 'b', 'c', 'd', 'e', 'f', 'A', 'B', 'C', 'D', 'E', 'F',
                    'x' // ^^;
            };

            var readingNumber = false;
            var result = "";
            for (int i = 0; i < line.Length; i++)
            {
                var c = line[i];
                if (c.ToString().Trim().Length < 1) // ignore space
                {
                    if (readingNumber)
                        break;
                    continue;
                }

                readingNumber = true;
                if (!allowedChars.Contains(c))
                    break;

                result += c;
            }

            result = result.Replace("0x", "");
            if (result.Length < 1)
                return false;

            try
            {
                offset = Convert.ToInt32(result, 16);
                return true;
            }
            catch 
            { 
                return false;
            }
        }

        private static bool IsNullOrEmpty(string content)
        {
            return content == null || content.Trim().Length < 1;
        }
    }
}
