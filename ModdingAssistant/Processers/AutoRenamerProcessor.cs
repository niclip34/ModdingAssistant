using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ModdingAssistant.Processers
{
	internal class AutoRenamerProcessor : IProcessor
	{
		private int Address {  get; set; }

		private string ClassName { get; set; }

		public AutoRenamerProcessor(int address)
		{
			this.Address = address;
			this.ClassName = string.Empty;
		}

		public string Process(string input)
		{
			var functions = ReadFunctions(input);
			var builder = new StringBuilder();
			int currentAddress = Address;
			for (int i = 0; i < functions.Count; i++)
			{
				var func = functions[i];
				//string.Format("\"{0}\": \"{1}\",\n", Address);

				if (func.Name.StartsWith("sub_"))
				{
					currentAddress += 0x8;
					continue;
				}
				if (ClassName != string.Empty)
					func.Name = $"#{ClassName}::{func.Name}";
				var build = $"\"{func.Name}\": \"{currentAddress:x}\",";
				if ((func.Parameters != string.Empty && func.Parameters != "()") || (func.ReturnType != string.Empty && func.ReturnType != "void"))
				{
					build += $"\n\"@0x{currentAddress:x}\": \"";
					if (func.Parameters != string.Empty)
					{
						if (func.Parameters != "()")
						{
							build += $"({func.Parameters}) ";
						}
					}
					if (func.ReturnType != string.Empty)
					{
						if (func.ReturnType != "void")
						{
							build += $"-> {func.ReturnType}";
						}
					}
					build += "\",";
				}
				builder.AppendLine(build.TrimStart().TrimEnd());
				currentAddress += 0x8;
			}
			return builder.ToString();
		}

		private List<Function> ReadFunctions(string input)
		{
			var functions = new List<Function>();

			foreach(var line in input.Split('\n'))
			{
				var func = new Function();
				string trimed = line.TrimStart().TrimEnd();

				if (trimed.Contains("class"))
				{
					ClassName = trimed.Substring(6).Split('{')[0].Trim();
				}

				if (!trimed.Contains("virtual"))
					continue;


				string[] splitted = trimed.Split(' ');

				if (splitted.Length <= 1)
					continue;

				func.ReturnType = splitted[1];
				func.Name = splitted[2].Split('(')[0];

				if (func.Name.StartsWith("*"))
				{
					func.Name = func.Name.Substring(1);
					func.ReturnType += "*";
				}

				//string a = "(" + string.Join("", splitted[2].Split('(').Skip(1));
				string[] a = splitted.Skip(2).ToArray();
				string b = string.Join(" ", a.Take(a.Length - 1));
				b = string.Join("(", b.Remove(b.Length - 2).Split('(').Skip(1).ToArray());

				func.Parameters = b;

				functions.Add(func);
			}
			return functions;
		}

		private class Function
		{
			public string Name { get; set; }
			public string ReturnType { get; set; }
			public string Parameters { get; set; }

			public Function()
			{
				Name = string.Empty;
				ReturnType = string.Empty;
				Parameters = string.Empty;
			}

		}
	}
}
