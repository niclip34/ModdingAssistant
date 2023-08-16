using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ModdingAssistant.Processers
{
    internal interface IProcessor
    {
        string Process(string input);
    }
}
