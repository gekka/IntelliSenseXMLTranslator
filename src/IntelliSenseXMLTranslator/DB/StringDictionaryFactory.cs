using Gekka.Language.IntelliSenseXMLTranslator.Work;

namespace Gekka.Language.IntelliSenseXMLTranslator.DB
{
    using System.Linq;

    static class StringDictionaryFactory
    {
        public static IStringDictionary Open(Parameters parameters)
        {
            IStringDictionary dictionary;
            if (parameters.DictionaryType == DB.DBType.Text)
            {
                var dicPath = parameters.Dictionary;
                var ext = System.IO.Path.GetExtension(dicPath);
                var wildcardname = System.IO.Path.GetFileName(dicPath);
                var wildcardnameonly = System.IO.Path.GetFileNameWithoutExtension(dicPath);
                if (wildcardnameonly.Count(c => c == '*') == 1 && wildcardnameonly.Last() == '*')
                {
                    var d = System.IO.Path.GetDirectoryName(dicPath) ?? System.IO.Directory.GetCurrentDirectory();
                    var files = System.IO.Directory.GetFiles(d, System.IO.Path.GetFileName(dicPath)).ToList();
                    if (files.Count() == 0)
                    {
                        throw new System.ApplicationException("Missing wild card files");
                    }

                    var extCount = files.GroupBy(_ => System.IO.Path.GetExtension(_).ToLower()).Count();
                    if (extCount != 1)
                    {
                        throw new System.ApplicationException("Found multiple file types");
                    }

                    var org = files.FirstOrDefault(f => wildcardname.Replace("*", "") == System.IO.Path.GetFileName(f));
                    if (org != null)
                    {
                        files.Remove(org);
                    }

                    var sortedFiles = files
                        .OrderBy(f =>
                        {
                            var name = System.IO.Path.GetFileName(f);
                            var x = System.Text.RegularExpressions.Regex.Replace(name, @"\d+", m => m.Value.PadLeft(20, '0').Substring(0, 20));
                            return x;
                        })
                        .ToList();

                    if (org != null)
                    {
                        sortedFiles.Insert(0, org);
                    }

                    dictionary = new DB.StringDictionary(sortedFiles[0]);
                    for (int i = 1; i < sortedFiles.Count(); i++)
                    {
                        dictionary = new DB.DiffStringDictionary(dictionary, sortedFiles[i]);
                    }
                }
                else
                {
                    dictionary = new DB.StringDictionary(parameters.Dictionary);
                }

                bool isDirectDic = string.IsNullOrWhiteSpace(parameters.DiffDictionary) || parameters.Dictionary == parameters.DiffDictionary;
                if (!isDirectDic)
                {
                    dictionary = new DB.DiffStringDictionary(dictionary, parameters.DiffDictionary);
                }
            }
            else
            {
                dictionary = new DB.SQLStringDictionary(parameters.Dictionary, "Text");
            }
            return dictionary;
        }


    }
}
