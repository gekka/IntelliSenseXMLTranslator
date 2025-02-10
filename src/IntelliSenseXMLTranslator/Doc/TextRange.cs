namespace Gekka.Language.IntelliSenseXMLTranslator.Doc
{
    using System;
    using System.Linq;
    using System.Collections.Generic;
    using System.Xml;

    /// <summary>文字列として置換対象となる範囲を表すクラス</summary>
    class TextRange
    {
        /// <summary>コンストラクタ</summary>
        /// <param name="originalNodes">置換する文字列を構成するXMLのノード</param>
        /// <exception cref="InvalidOperationException"></exception>
        /// <exception cref="ApplicationException"></exception>
        public TextRange(IReadOnlyList<XmlNode> originalNodes)
        {
            this.OriginalNodes = originalNodes;

            if (originalNodes.Select(_ => _.ParentNode).Distinct().Count() != 1)
            {
                throw new InvalidOperationException("共通の親ノードの子ではない");
            }
            Parent = originalNodes[0].ParentNode!;
            if (Parent.OwnerDocument == null)
            {
                throw new InvalidOperationException("XMLDocumentに含まれていない");
            }

            XmlNode parent = originalNodes[0].ParentNode!;
            for (int i = 1; i < originalNodes.Count; i++)
            {
                if (originalNodes[i - 1].NextSibling != originalNodes[i])
                {
                    throw new InvalidOperationException("不連続");
                }
            }



            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            int num = 0;
            foreach (var n in originalNodes)
            {
                if (n is XmlText xt)
                {
                    string txt = ReplaceHexQ(ref num, (xt.Value ?? ""));
                    sb.Append(txt);
                }
                else
                {
                    num++;
                    sb.Append(MakeQ(num));
                    tagNodes.Add(num, new TagNodeOrText() { XmlNode = n });
                }
            }

            SourceText = sb.ToString();
            TagCount = num;
            HasTag = num > 0;


            if (regQNumber.IsMatch(Parent.InnerText))
            {
                throw new ApplicationException("置換用文字パターンが衝突しています");
            }
        }

        private static string MakeQ(int num) => "Q" + num + "q";

        /// <summary>HEX表記の文字列を置換用文字列に置換</summary>
        /// <param name="num"></param>
        /// <param name="text"></param>
        /// <returns></returns>
        private string ReplaceHexQ(ref int num, string text)
        { 
            if (text.Contains("#") == true)
            {
                var matches = regHex.Matches(text);
                if (matches.Count > 0)
                {
                    regHex.Replace(text, "\0").Split("\0");

                    System.Text.StringBuilder sb = new System.Text.StringBuilder();

                    int p = 0;
                    for (int i = 0; i < matches.Count; i++)
                    {
                        System.Text.RegularExpressions.Match m = matches[i];
                        sb.Append(text.Substring(p, matches[0].Index));

                        num++;
                        sb.Append(MakeQ(num));
                        tagNodes.Add(num, new TagNodeOrText() { Text = m.Value });

                        p = m.Index + m.Length;
                    }

                    sb.Append(text.Substring(p));
                    text = sb.ToString();
                }
            }
            return text;
        }
        
        /// <summary>HEX表記の置換用文字列を元の文字列に戻す</summary>
        /// <param name="text"></param>
        /// <returns></returns>
        private string RestoreHexQ(string text)
        {
            foreach (var kv in this.tagNodes)
            {
                if (kv.Value.Text != null)
                {
                    text = text.Replace( MakeQ(kv.Key), kv.Value.Text);
                }
            }
            return text;
        }


        /// <summary>置換対象の親ノード</summary>
        public XmlNode Parent { get; }

        /// <summary>置換対象の中にあるXMLText以外のNode一覧</summary>
        private Dictionary<int, TagNodeOrText> tagNodes = new Dictionary<int, TagNodeOrText>();



        /// <summary>置換のもとになるノード一覧</summary>
        public IReadOnlyList<XmlNode> OriginalNodes { get; }

        /// <summary>文字列に含まれているタグの数</summary>
        public int TagCount { get; }

        /// <summary>文字列にタグが含まれているか</summary>
        public bool HasTag { get; }

        /// <summary>翻訳に渡す文字列</summary>
        public string SourceText { get; }

        /// <summary>翻訳済みか</summary>
        public bool IsReplaced { get; private set; } = false;

        /// <summary>#XXXXXX 形式の16新文字列を見つけるための正規表現</summary>
        protected static System.Text.RegularExpressions.Regex regHex
            = new System.Text.RegularExpressions.Regex(@"(?<=\s)#([0-9A-Fa-f]{6}|[0-9A-Fa-f]{8})(?=(\s|\.))");

        /// <summary>XMLタグを置換しておくための文字列を見つけるための正規表現</summary>
        protected static System.Text.RegularExpressions.Regex regQNumber
            = new System.Text.RegularExpressions.Regex(@"Q(?<NUM>\d+?)q");



        /// <summary>翻訳後の文字列をドキュメントに書き込む</summary>
        /// <param name="text"></param>
        /// <returns>結果</returns>
        /// <exception cref="InvalidOperationException"></exception>
        public ReplaceResult SetTranslatedText(string text, InsertPoint insertPoint)
        {
            if (IsReplaced)
            {
                throw new InvalidOperationException();
            }

            text = RestoreHexQ(text);

            var matches = regQNumber.Matches(text);
            if (matches.Count != TagCount)
            {
                return ReplaceResult.NumberCountMissmatch;
            }



            int p = 0;
            List<TextAndNum> split = new List<TextAndNum>();

            foreach (System.Text.RegularExpressions.Match m in matches)
            {
                if (m.Index > 0)
                {
                    split.Add(new TextAndNum(text.Substring(p, m.Index - p), -1));
                }

                var num = int.Parse(m.Groups["NUM"].Value);
                if (num > TagCount)
                {
                    return ReplaceResult.NumberValueMissmatch;
                }
                split.Add(new TextAndNum(m.Value, num));
                p = m.Index + m.Length;
            }
            if (p < text.Length)
            {
                split.Add(new TextAndNum(text.Substring(p), -1));
            }

            XmlNode? last;
            if (insertPoint == InsertPoint.Before || insertPoint == InsertPoint.BeforeWithBR)
            {
                last = OriginalNodes[0].PreviousSibling;
            }
            else if (insertPoint == InsertPoint.After)
            {
                last = OriginalNodes.Last();
                last = Parent!.InsertAfter(Parent.OwnerDocument!.CreateTextNode("\r\n"), last);
            }
            else if (insertPoint == InsertPoint.AfterWithBR)
            {
                last = OriginalNodes.Last();

                var br = Parent.OwnerDocument!.CreateElement("br");
                last = Parent!.InsertAfter(br, last);
            }
            else if (insertPoint == InsertPoint.RemoveOriginal)
            {
                last = OriginalNodes.Last();
            }
            else
            {
                throw new InvalidOperationException();
            }

            {
                if (OriginalNodes[0] is XmlText xt && xt.Value != null)
                {
                    var trimed = xt.Value.TrimStart();
                    var diff = xt.Value.Length - trimed.Length;
                    if (diff > 0)
                    {
                        var space = xt.Value.Substring(0, diff);
                        last = Parent!.InsertAfter(Parent!.OwnerDocument!.CreateTextNode(space), last);
                    }
                }
            }

            foreach (TextAndNum txnum in split)
            {
                XmlNode n;
                if (txnum.matchNum >= 0)
                {
                    TagNodeOrText tagnode = tagNodes[txnum.matchNum];
                    if (tagnode.XmlNode != null)
                    {
                        n = tagnode.XmlNode.CloneNode(true);
                    }
                    else
                    {
                        throw new InvalidOperationException("置換方法が間違っている");
                    }
                }
                else
                {
                    n = Parent!.OwnerDocument!.CreateTextNode(txnum.text);
                }
                last = Parent!.InsertAfter(n, last);
            }

            {
                if (OriginalNodes.Last() is XmlText xt && xt.Value != null)
                {
                    var trimed = xt.Value.TrimEnd();
                    var diff = xt.Value.Length - trimed.Length;
                    if (diff > 0)
                    {
                        var space = xt.Value.Substring(trimed.Length);
                        last = Parent!.InsertAfter(Parent.OwnerDocument!.CreateTextNode(space), last);
                    }
                }
            }

            if (insertPoint == InsertPoint.Before)
            {
                last = Parent!.InsertAfter(Parent.OwnerDocument!.CreateTextNode("\r\n"), last);
            }
            else if (insertPoint == InsertPoint.BeforeWithBR)
            {
                var br = Parent.OwnerDocument!.CreateElement("br");
                last = Parent!.InsertAfter(br, last);
            }
            else if (insertPoint == InsertPoint.RemoveOriginal)
            {
                foreach (var n in OriginalNodes)
                {
                    Parent.RemoveChild(n);
                }
            }

            IsReplaced = true;
            return ReplaceResult.Success;

        }

        public override string ToString() => HasTag + " : " + SourceText;

        class TagNodeOrText
        {
            public XmlNode? XmlNode;
            public string? Text;
        }
        class TextAndNum
        {
            public TextAndNum(string text, int matchNum)
            {
                this.text = text;
                this.matchNum = matchNum;
            }
            public readonly string text;
            public readonly int matchNum;
        }
    }

    enum ReplaceResult
    {
        Success,

        NumberCountMissmatch,
        NumberValueMissmatch,
    }

    enum InsertPoint
    {
        /// <summary>元の文字列の前に翻訳文字列を挿入してから改行</summary>
        [Util.EnumParameterDescription("元の文字列の前に翻訳文字列を挿入してから改行")]
        Before,

        /// <summary>元の文字列の前に翻訳文字列を挿入してからBRタグを挿入</summary>
        [Util.EnumParameterDescription("元の文字列の前に翻訳文字列を挿入してからBRタグを挿入")]
        BeforeWithBR,

        /// <summary>元の文字列の後に改行してから翻訳文字列を挿入</summary>
        [Util.EnumParameterDescription("元の文字列の後に改行してから翻訳文字列を挿入")]
        After,

        /// <summary>元の文字列の前にBRタグを挿入してから翻訳文字列を挿入</summary>
        [Util.EnumParameterDescription("元の文字列の前にBRタグを挿入してから翻訳文字列を挿入")]
        AfterWithBR,


        /// <summary>元の文字列を翻訳文字列で上書き</summary>
        [Util.EnumParameterDescription("元の文字列を翻訳文字列で上書き")]
        RemoveOriginal
    }
}
