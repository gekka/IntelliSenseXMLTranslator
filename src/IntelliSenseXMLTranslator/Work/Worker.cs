namespace Gekka.Language.IntelliSenseXMLTranslator.Work
{
    using IntelliSenseXMLTranslator.DB;
    using IntelliSenseXMLTranslator.Doc;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;

    using Gekka.Language.Translator.Interfaces;

    class Worker
    {
        /// <summary></summary>
        /// <param name="dictionary"></param>
        /// <param name="translator"></param>
        /// <param name="token"></param>
        public Worker(IStringDictionary dictionary, ITranslator translator, CancellationToken token)
        {
            this.token = token;
            this.dictionary = dictionary;
            this.translator = translator;
        }

        /// <summary>数値判定用正規表現</summary>
        private static System.Text.RegularExpressions.Regex redNum = new System.Text.RegularExpressions.Regex(@"^[+-]?\d+(?:\.\d+)?$");

        private CancellationToken token;
        private IStringDictionary dictionary;
        private ITranslator translator;

        /// <summary>変換辞書を保存するか</summary>
        public bool EnableSaveDictionary { get; set; } = true;

        /// <summary>翻訳されたファイルを保存するか</summary>
        public bool EnableSaveTranslated { get; set; } = true;

        public bool IsCheckXMLOnly { get; set; } = false;

        /// <summary>翻訳済みファイルを上書きするか</summary>
        public bool IsForeOverwrite { get; set; } = false;

        //変換結果をどのように挿入もしくは置換するか
        public InsertPoint InsertPoint { get; set; } = InsertPoint.RemoveOriginal;

        //public Task RunAsync(string sourceDir, string outputDir)
        //{
        //    return RunAsync(System.IO.Directory.GetFiles( sourceDir,"*.xml", System.IO.SearchOption.TopDirectoryOnly), outputDir);
        //}
        public async Task RunAsync(IEnumerable<InOutFile> in_outs, string outputDir, string toLang = "ja")//IEnumerable<string> sourceFiles, string outputDir)
        {
            outputDir = string.IsNullOrWhiteSpace(outputDir) ? Directory.GetCurrentDirectory() : outputDir;

            foreach (InOutFile in_out in in_outs)
            {

                string inputXML = in_out.Input;
                string outputXML = in_out.GetLangXMLPath(outputDir, toLang);//.Output;

                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine(DateTimeOffset.Now.ToString("yyyy-MM-dd HH:mm:ss") + " \t" + inputXML);

                {
                    string? dir = System.IO.Path.GetDirectoryName(outputXML);
                    if (dir != null)
                    {
                        Directory.CreateDirectory(dir);
                    }
                }
                //System.IO.Directory.CreateDirectory("result");

                //var outputXML = System.IO.Path.Combine(outputDir, xml);
                if (File.Exists(outputXML) && !IsCheckXMLOnly)
                {
                    if (IsForeOverwrite)
                    {
                        try
                        {
                            System.IO.File.Delete(outputXML);
                        }
                        catch (Exception ex)
                        {
                            throw new ApplicationException("翻訳済みファイルを上書きできませんでした\r\n" + outputXML, ex);
                        }
                    }
                    else
                    {
                        //既に変換済みのファイル
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        Console.WriteLine("翻訳済みなのでスキップします");
                        continue;
                    }
                }
                Console.ResetColor();

                DocXml dx;
                try
                {
                    //XMLコメントファイルを読み込む
                    dx = DocXml.Load(new System.IO.FileInfo(inputXML), Console.Out);
                }
                catch (Exception ex)
                {
                    if (IsCheckXMLOnly)
                    {
                        throw new ApplicationException("XMLが正常に読み込めません\r\n" + ex.Message, ex);
                    }
                    throw;
                }

                var xx = dx.Items.Where(_ => _.Node.Name == "appledoc")
                    .Select(_ =>
                    {
                        return _.Texts;
                    }).ToArray(); ;



                TextRange[] textRanges = FilterItems(dx.Items);//  dx.Items.Where(_ => !_.IsCodeNode).SelectMany(_ => _.Texts).ToArray();

                if (textRanges.Count() == 0)
                {
                    //変換対象となる文字列が無い
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine("翻訳対象となる文字列を見つけられませんでした");
                    continue;
                }

                if (IsCheckXMLOnly)
                {
                    Console.WriteLine("XMLは正常に読み込めました。");
                    continue;
                }

                WorkingParameter parameter = new WorkingParameter(dictionary);

                // 辞書に登録されていない文の一覧を取得
                await WorkGetSentensesAsync(parameter, textRanges);

                if (parameter.UnTranslatedPairs.Count > 0)
                {//翻訳対象の文が存在するなら、その文を翻訳させて辞書に追加
                    await WorkGetSentenseTranslatedTextAsync(parameter);
                }

                // 辞書を使って翻訳文を追記、または置換
                await WorkReplace(parameter, textRanges);

                AddComment(dx);


                if (parameter.IsChanged && EnableSaveTranslated)
                {
                    dx.Save(outputXML);
                }

                if (EnableSaveDictionary)
                {
                    dictionary.SaveChanges();
                }
            }
        }

        /// <summary>翻訳対象のみになるようにフィルターする</summary>
        private TextRange[] FilterItems(IEnumerable<DocXmlItem> items)
        {
            System.Text.RegularExpressions.Regex regUrl = new System.Text.RegularExpressions.Regex(@"^\s*https?://[^\s]+\s*$");
            return items.Where(_ => !_.IsCodeNode)
                .SelectMany(_ => _.Texts)
                .Where(_ =>
                {
                    if (regUrl.IsMatch(_.SourceText.Trim()))
                    {
                        return false;
                    }
                    return true;
                })
                .ToArray();
        }


        private void AddComment(DocXml dx)
        {
            var docNode = dx.XmlDocument.SelectSingleNode("doc");

            var comment = docNode!.OwnerDocument!.CreateComment(translator.Comment);
            docNode.InsertBefore(comment, docNode.FirstChild);
        }


        /// <summary>翻訳の元となる範囲の一覧から、翻訳する文の一覧を取得する</summary>
        /// <param name="parameter"></param>
        /// <param name="textRanges">翻訳元になる文字範囲の一覧</param>
        /// <returns></returns>
        private async Task WorkGetSentensesAsync(WorkingParameter parameter, TextRange[] textRanges)
        {
            foreach (var range in textRanges)
            {
                var translated = await WorkSentenseAsync(parameter, range.SourceText, (p, sentence, sb) =>
                {

                    parameter.UnTranslatedPairs[sentence] = null;
                    return Task.FromResult(false);
                });
            }
        }

        /// <summary>翻訳元の文を翻訳する</summary>
        /// <param name="parameter"></param>
        /// <returns></returns>
        private async Task WorkGetSentenseTranslatedTextAsync(WorkingParameter parameter)
        {
            if (parameter.UnTranslatedPairs.Count == 0)
            {
                return;
            }

            if (translator.IsMultipleTranslator && translator is IMultipleTranslator multi)
            {//複数の文を一度に翻訳できる場合
                EventHandler<MultipleTranslatorEventArgs> handler = (s, e) =>
                {
                    Console.ForegroundColor = ConsoleColor.Blue;
                    Console.CursorLeft = 0;
                    Console.Write($"{e.TranslatedCount}/{e.SourceCount}");
                    Console.ResetColor();
                };

                multi.Progress += handler;
                try
                {
                    await multi.GetLocalizeTextAsync(parameter.UnTranslatedPairs, token);
                    foreach (var kv in parameter.UnTranslatedPairs)
                    {
                        if (kv.Value != null)
                        {
                            parameter.TranslatedDictionary.Add(kv.Key, kv.Value);
                            parameter.IsChanged = true;
                        }
                    }
                }
                finally
                {
                    multi.Progress -= handler;
                }


                Console.CursorLeft = 0;
                Console.WriteLine();
                Console.ResetColor();
            }
            else
            {
                foreach (var original in parameter.UnTranslatedPairs.Keys)
                {
                    var result = await WorkSentenseAsync(parameter, original, async (p, sentence, sb) =>
                    {
                        string? text = await translator.GetLocalizeTextAsync(sentence, token);
                        if (text == null)
                        {
                            return false;
                        }

                        sb.Append(text);
                        parameter.IsChanged = true;
                        return true;
                    });
                    parameter.UnTranslatedPairs[original] = result;
                    parameter.TranslatedDictionary.Add(original, result);
                }
            }
        }

        /// <summary>辞書を参照して文字列置換を実行する</summary>
        /// <param name="parameter"></param>
        /// <param name="textRanges"></param>
        /// <returns></returns>
        private async Task<WorkingParameter> WorkReplace(WorkingParameter parameter, TextRange[] textRanges)
        {

            parameter.ResetCount();

            for (int textRangeIndex = 0; textRangeIndex < textRanges.Length; textRangeIndex++)
            {
                var range = textRanges[textRangeIndex];

                //翻訳対象の文字列
                var original = range.SourceText;

                var translated = await WorkSentenseAsync(parameter, original, (sentense, db, parameter) =>
                {
                    return Task.FromResult(true);
                });

                range.SetTranslatedText(translated, InsertPoint);
                parameter.IsChanged = true;
                parameter.TranslatedRangeCount++;

                Console.CursorLeft = 0;
                Console.Write($"{textRangeIndex + 1}/{textRanges.Length} : {parameter.TranslatedRangeCount}");
            }
            Console.WriteLine();
            return parameter;
        }

        /// <summary>文字列を文に分解して、文ごとに処理を行う</summary>
        /// <param name="parameter"></param>
        /// <param name="original">変換もとの文字列</param>
        /// <param name="translateItemAsync">見つかった文に対して実行する処理</param>
        /// <returns>処理された結果の文字列</returns>
        private async Task<string> WorkSentenseAsync(WorkingParameter parameter, string original, TranslateItemDelegate translateItemAsync)//, ref bool docxmlsChanged, ref int translatedRangeCount)
        {
            StringBuilder sb = new StringBuilder();
            StringReader sr = new StringReader(original);
            while (sr.Peek() != -1)
            {//改行ごとに分解

                var line = sr.ReadLine();

                if (string.IsNullOrWhiteSpace(line))
                {
                    sb.Append(line);
                }
                else
                {
                    if (sb.Length > 0)
                    {
                        sb.AppendLine();
                    }

                    // 範囲を文単位に分解して処理
                    var trimmed = line.Trim();
                    var ss = trimmed.Split(". ");
                    if (ss.Length >= 2)
                    {
                        ss[0] = ss[0] + ".";
                    }

                    bool hasTranslated = false;
                    for (int i = 0; i < ss.Length; i++)
                    {
                        string sentence = ss[i];

                        sentence = sentence.Trim();

                        if (redNum.IsMatch(sentence))
                        {//数値
                            sb.Append(sentence);
                        }
                        else if (parameter.TranslatedDictionary.TryGetValue(sentence, out var log))
                        {//翻訳済みの文がある
                            sb.Append(log);
                        }
                        else
                        {//まだ翻訳されてない
                            hasTranslated |= await translateItemAsync(parameter, sentence, sb);// //await TranslateSingle(sentence, sb, parameter);
                        }
                    }

                    if (hasTranslated)
                    {
                        parameter.TranslatedRangeCount++;
                    }
                }
            }

            return sb.ToString();
        }

        private delegate Task<bool> TranslateItemDelegate(WorkingParameter parameter, string sentence, StringBuilder sb);

        class WorkingParameter
        {
            public WorkingParameter(IStringDictionary dic)
            {
                TranslatedDictionary = dic;
                ResetCount();
            }

            public bool IsChanged { get; set; }

            /// <summary>翻訳したTextRangeのカウント</summary>
            public int TranslatedRangeCount { get; set; }

            public IStringDictionary TranslatedDictionary { get; }
            public Dictionary<string, string?> UnTranslatedPairs { get; } = new Dictionary<string, string?>();

            public void ResetCount()
            {
                IsChanged = false;
                TranslatedRangeCount = 0;
            }
        }
    }


}
