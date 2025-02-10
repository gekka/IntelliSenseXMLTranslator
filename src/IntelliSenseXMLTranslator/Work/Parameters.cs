
namespace Gekka.Language.IntelliSenseXMLTranslator.Work
{
    using System.Linq;
    using System.Collections.Generic;
    using Gekka.Language.IntelliSenseXMLTranslator;
    using Gekka.Language.IntelliSenseXMLTranslator.Util;
    using IntelliSenseXMLTranslator.DB;

    class Parameters
    {
        internal static Parameters Parse(string[] args)
        {
            Parameters ret = new Parameters();
            CommandLineParser.Parse(ret, args);
            ret.Help |= args.Length == 0 || ret.Paths.Count == 0;

            ret.OutputDir = System.IO.Path.GetFullPath(System.Environment.ExpandEnvironmentVariables(ret.OutputDir));
            return ret;
        }


        [CommandLineArgs(nameof(Dictionary), "D", "辞書ファイル",Description ="配布されているDictionary.ja.datあるいは自作の辞書ファイルを指定します")]
        public string Dictionary { get; set; } = "data.db";

        [CommandLineArgs("DicType", "DT", "辞書ファイル種類",Description ="標準ではテキスト辞書を使用します。\r\n必要ならSQliteの辞書を作成もできます")]
        public DBType DictionaryType { get; set; } = DBType.Text;

        [CommandLineArgs(nameof(Translator), "T", "翻訳機の名前", IsRequired = true,Description ="後述の翻訳機の名前のいずれかを指定します")]
        public string? Translator { get; set; }

        [CommandLineArgs(nameof(Browser), "B", "使用するブラウザの名前", IsRequired = true,Description ="ブラウザ翻訳させる場合に使用するブラウザ名を指定します")]
        public BrowserNames Browser { get; set; } = BrowserNames.Chrome;

        [CommandLineArgs("Point", "P", "翻訳結果をどのように表示させるかを指定します")]
        public Doc.InsertPoint InsertPoint { get; set; } = Doc.InsertPoint.BeforeWithBR;

        [CommandLineArgs(nameof(OutputDir), "O", "出力先フォルダを指定します\r\n\r\n出力は元ファイルのフルパスの構造を模倣します。\r\nドライブレター以下をそのドライブにコピーすると翻訳ファイルを適用できます。")]
        public string OutputDir { get; set; } = "result";

        [CommandLineArgs(nameof(Force), "F", "翻訳済みでも上書きするかのフラグ",hasParameter:false)]
        public bool Force { get; set; } = false;

        [CommandLineArgs(nameof(VersionType), "Ver", "最後のバージョンだけにするか")]
        public VersionType VersionType { get; set; } = VersionType.Latest;

        [CommandLineArgs(nameof(Test), "", "対象となるファイルの列挙のみ", hasParameter: false)]
        public bool Test { get; set; } = false;

        [CommandLineArgs(nameof(TestXML), "", "対象となるファイルのXMLが正常であるか確認する", hasParameter: false)]
        public bool TestXML { get; set; } = false;

        [CommandLineArgs(nameof(SkipFiles), "S", "標準では処理不可能としてスキップするリストファイル\r\n\r一部のファイルで不正な構造のXMLのために処理不可能な場合がある。\r\nそのようなファイルを無視リストアフィルで指定する")]
        public string SkipFiles { get; set; } = "";

        [CommandLineArgs("Files", "", "入力元のフォルダかファイル(.xml or .list)のパス\r\n\r\n直接XMLファイルを指定するか、リストファイルで指定します", IsRequired = true, IsMissingList = true)]
        public List<string> Paths { get; } = new List<string>();

        [CommandLineArgs("Help", "?", "ヘルプ", false)]
        public bool Help { get; set; }

        internal IEnumerable<InOutFile> GetFiles()
        {
            var outDir = System.IO.Path.Combine(System.Environment.CurrentDirectory, this.OutputDir);
            var selector = new FilesSelector(this.Paths, this.VersionType);
            if (!string.IsNullOrWhiteSpace(SkipFiles) && System.IO.File.Exists(SkipFiles))
            {
                var skip= Util.ListFileReader.ReadListFile(SkipFiles).Select(_=>_.ToLower()).ToArray();
                selector.SkipFiles.AddRange(skip);
            }
            return selector.GetFiles();
        }

    }




}
