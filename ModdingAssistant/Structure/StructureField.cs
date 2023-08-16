using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ModdingAssistant.Structure
{
    public class StructureField
    {
        public string Name { get; set; }
        public string FieldType { get; set; }
        public int Offset { get; set; }
        public int Size { get; set; }

        public StructureField(string name, string fieldType, int offset, int size) 
        {
            this.Name = name;
            this.FieldType = fieldType;
            this.Offset = offset;
            this.Size = size;
        }

        public override string ToString()
        {
            var result = "";
            if (Offset != -1)
                result = string.Format("{0} {1} : {2}", FieldType, Name, Offset.ToString("x"));
            if (Size != 8)
                result += string.Format(", size = {0}", Size.ToString("x"));
            return result;
        }
    }
}
