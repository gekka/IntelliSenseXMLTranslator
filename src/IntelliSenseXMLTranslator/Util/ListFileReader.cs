using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gekka.Language.IntelliSenseXMLTranslator.Util
{
    internal class ListFileReader
    {
        public static IList<string> ReadListFile(string path)
        {
            using var sr = new System.IO.StreamReader(path);
            List<string> lines = new List<string>();
            while (sr.Peek() != -1)
            {
                var line = sr.ReadLine()?.Trim();
                if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#"))
                {
                    continue;
                }
                line = System.Environment.ExpandEnvironmentVariables(line);
                if (line.StartsWith("\"") && line.EndsWith("\"") & line.Length > 2)
                {
                    line = line.Substring(1, line.Length - 2);
                }

                if (lines.Contains(line))
                {
                    continue;
                }
                lines.Add(line);
            }
            return lines;
        }
    }
}
