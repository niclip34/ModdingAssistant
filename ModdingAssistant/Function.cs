using HandyControl.Tools.Extension;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ModdingAssistant
{
    internal class Function
    {
        public string Name { get; set; }
        public string ReturnType { get; set; }
        public string Paramerters { get; set; }

        public Function()
        {
            Name = string.Empty;
            ReturnType = null;
            Paramerters = string.Empty;
        }
    }
}
