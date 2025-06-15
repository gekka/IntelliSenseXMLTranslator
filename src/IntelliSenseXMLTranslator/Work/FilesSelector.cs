namespace Gekka.Language.IntelliSenseXMLTranslator.Work
{
    using System;
    using System.Linq;
    using System.Collections.Generic;
    using System.Text.RegularExpressions;

    /// <summary>xmlファイルを列挙するクラス</summary>
    class FilesSelector
    {
        /// <summary></summary>
        /// <param name="inputFiles">引数で渡されたファイルかディレクトリのパス</param>
        /// <param name="outputBaseDir">出力先の基準フォルダ</param>
        /// <param name="versionType">バージョン別のフォルダに分かれている場合に列挙をどうするか</param>
        public FilesSelector(IEnumerable<string> inputFiles, VersionType versionType)
        {
            this.inputFiles = inputFiles;
            this.versionType = versionType;
        }

        private readonly IEnumerable<string> inputFiles;
        private readonly VersionType versionType;

        private const string EXT_XML = ".xml";
        private const string EXT_LIST = ".list";

        public List<string> SkipFiles { get; } = new List<string>();


        public IEnumerable<InOutFile> GetFiles()
        {
            var ret = this.inputFiles
                .Select(_ => System.Environment.ExpandEnvironmentVariables(_))
                .SelectMany(path => this.GetFiles(path))
                .DistinctBy(_ => _.Input);

            if (SkipFiles.Count > 0)
            {
                var skip = this.SkipFiles.Select(_ => _.ToLower()).ToArray();
                ret = ret.Where(_ => !skip.Contains(System.IO.Path.GetFileName(_.Input.ToLower())));
            }
            return ret;

        }

        private IEnumerable<InOutFile> GetFiles(string pathFileOrDirectory)
        {
            pathFileOrDirectory = pathFileOrDirectory.Trim();
            if (pathFileOrDirectory.StartsWith("\"") && pathFileOrDirectory.EndsWith("\""))
            {
                pathFileOrDirectory = pathFileOrDirectory.Substring(1, pathFileOrDirectory.Length - 2);
            }
            if (string.IsNullOrWhiteSpace(pathFileOrDirectory))
            {
                return Array.Empty<InOutFile>();
            }

            string ext = System.IO.Path.GetExtension(pathFileOrDirectory).ToLower();
            if (System.IO.File.Exists(pathFileOrDirectory) && ext == EXT_LIST)
            {
                return Util.ListFileReader.ReadListFile(pathFileOrDirectory).SelectMany(_ => GetFiles(_).Select(inoutfile => inoutfile.TrySetListSource(_)));
            }
            else if (System.IO.File.Exists(pathFileOrDirectory) && ext == EXT_XML)
            {
                if (hasDLL(pathFileOrDirectory, out var output))
                {
                    return new[] { output! };
                }
            }
            else
            {
                var dirName = System.IO.Path.GetFileName(pathFileOrDirectory);
                bool isWildcard = pathFileOrDirectory.Contains('*');
                if (isWildcard)
                {
                    //pathFileOrDirectory =System.IO.Path.GetFullPath( pathFileOrDirectory.TrimEnd('*'));
                    var parent = System.IO.Path.GetDirectoryName(pathFileOrDirectory) ?? "";

                    return System.IO.Directory.GetDirectories(parent, dirName).SelectMany(_ => GetDirecotryFiles(_).Select(inoutfile => inoutfile.TrySetListSource(_)));
                }
                else
                {
                    if (System.IO.Directory.Exists(pathFileOrDirectory))
                    {
                        return GetDirecotryFiles(pathFileOrDirectory);
                    }
                }
            }

            return Array.Empty<InOutFile>();
        }

        private IEnumerable<InOutFile> GetDirecotryFiles(string dir)
        {
            foreach (string xml in System.IO.Directory.GetFiles(dir, "*" + EXT_XML, System.IO.SearchOption.TopDirectoryOnly))
            {
                if (hasDLL(System.IO.Path.GetFullPath(xml), out var output))
                {
                    yield return output!;
                }
            }

            if (versionType == VersionType.Latest)
            {
                var list = System.IO.Directory.GetDirectories(dir).Select(d =>
                {
                    string name = System.IO.Path.GetFileName(d);
                    LongVersion? lv = LongVersion.Parse(name);
                    return new Temp(d, lv, name);
                });

                foreach (var grp in list.GroupBy(temp => temp.Ver != null))
                {
                    IEnumerable<InOutFile> ie;
                    if (grp.Key)
                    {
                        var last = grp.OrderBy(_ => _.Ver!, new LongVersion.Compare()).Last();
                        var dirVer = last.Dir;
                        ie = GetDirecotryFiles(dirVer).Select(inoutFile =>
                        {
                            inoutFile.ListSource = last.Dir;
                            inoutFile.Ver = last.Ver;
                            return inoutFile;
                        });
                    }
                    else
                    {
                        ie = grp.SelectMany(_ => GetDirecotryFiles(_.Dir));
                    }

                    foreach (var ret in ie)
                    {
                        yield return ret;
                    }
                }
            }
            else if (versionType == VersionType.All)
            {
                foreach (var xml in System.IO.Directory.GetFiles(dir, "*" + EXT_XML, System.IO.SearchOption.AllDirectories))
                {
                    if (hasDLL(System.IO.Path.GetFullPath(xml), out var output))
                    {
                        yield return output!;
                    }
                }
            }
        }
        /// <summary>
        /// XMLとDLLが対になっているかどうか
        /// </summary>
        /// <param name="inputXmlPath">翻訳元のXML</param>
        ///// <param name="outputPath">対になっていたら</param>
        /// <returns>対になっているならtrue</returns>
        private bool hasDLL(string inputXmlPath, out InOutFile? outputPath)
        {
            outputPath = null;

            if (System.IO.File.Exists(System.IO.Path.ChangeExtension(inputXmlPath, ".dll")))
            {
                outputPath = new InOutFile(inputXmlPath);
                //outputPath = new InOutFile(inputXmlPath, System.IO.Path.Combine(this.outputBaseDir, inputXmlPath.Replace(":", "：")));

                return true;
            }
            else
            {
                return false;
            }
        }

        class Temp
        {
            public string Dir;
            public LongVersion? Ver;
            public string Name;

            public Temp(string dir, LongVersion? ver, string name)
            {
                Dir = dir;
                Ver = ver;
                Name = name;
            }
        }
    }


    /// <summary>4以上の項目のあるバージョン</summary>
    class LongVersion : IComparable<LongVersion?>, IEquatable<LongVersion>
    {
        public LongVersion()
        {
            this._Values = Array.Empty<long>();
        }
        public LongVersion(IEnumerable<long> ie)
        {
            this._Values = ie.ToArray();
        }
        public LongVersion(IEnumerable<long> ie,string original)
        {
            this._Values = ie.ToArray();
            this._Original = original;
        }
        //public long[] Values => values;
        private long[] _Values;

        public string Original => _Original;
        private string _Original = "";

        static readonly Regex reg = new Regex(@"^\d+(.-?\d+)*$");

        public static LongVersion? Parse(string name)
        {
            if (!string.IsNullOrEmpty(name))
            {
                if (name.Contains("preview") || name.Contains("alpha"))
                {
                }
                var name2 = name.Replace("-preview", ".-1").Replace("-alpha", ".-1000").Replace("-beta", ".-900");
                if (reg.IsMatch(name2))
                {
                    return new LongVersion(name2.Split(".").Select(_ => long.Parse(_)), name);
                }
            }
            return null;
        }

        public class Compare : IComparer<LongVersion?>
        {
            int IComparer<LongVersion?>.Compare(LongVersion? x, LongVersion? y) => compare(x, y);
        }

        private static int compare(LongVersion? x, LongVersion? y)
        {
            if (x == null && y == null) return 0;
            if (x == null) return -1;
            if (y == null) return 1;

            int len = Math.Max(x._Values.Length, y._Values.Length);
            long[] xa = new long[len]; Array.Copy(x._Values, xa, x._Values.Length);
            long[] ya = new long[len]; Array.Copy(y._Values, ya, y._Values.Length);

            for (int i = 0; i < len; i++)
            {
                var z = xa[i].CompareTo(ya[i]);
                if (z != 0)
                {
                    return z;
                }
            }
            return 0;
        }

        int IComparable<LongVersion?>.CompareTo(LongVersion? other)
        {
            return compare(this, other);
        }

        bool IEquatable<LongVersion>.Equals(LongVersion? other)
        {
            return compare(this, other) == 0;
        }

        public override bool Equals(object? obj)
        {
            return this.Equals(obj as LongVersion);
        }

        public override int GetHashCode()
        {
            return ((System.Collections.IStructuralEquatable)this._Values).GetHashCode(EqualityComparer<long>.Default);
        }

        public override string ToString()
        {
            return string.Join(".", _Values.Select(_ => _.ToString()));
        }
    }
}
