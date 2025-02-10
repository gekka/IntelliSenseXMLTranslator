namespace Gekka.Language.IntelliSenseXMLTranslator.Doc
{
    using System;
    using System.Linq;
    using System.Collections.Generic;
    using System.IO;
    using System.Text.RegularExpressions;

    /// <summary>
    /// XMLファイルの正常性確認
    /// </summary>
    class XmlChecker
    {
        [System.Diagnostics.DebuggerStepThrough()]
        public XmlChecker()
        {
        }
        public XmlChecker(System.IO.TextWriter log)
        {
            this.Log = log;
        }

        public System.IO.TextWriter? Log { get; set; }

        /// <summary>XMLが不正になっているElementを含んでいる場合に、可能なら修復する</summary>
        /// <param name="originalFile">元のXMLのパス</param>
        /// <param name="repairedXMLFile">修復したXMLのパス</param>
        /// <returns>不正がないか修復出来たらtrue,修復してみても不正のままならfalse</returns>
        public RepairResult RepairXML(System.IO.FileInfo originalFile, System.IO.FileInfo repairedXMLFile)
        {
            var xml = ReadOriginalXMLString(originalFile.FullName);
            var result = RepairXML(xml, out var repaired);
            if (result == RepairResult.Broken)
            {
                return RepairResult.Broken;
            }

            repairedXMLFile.Delete();
            if (result == RepairResult.Pass)
            {
                originalFile.CopyTo(repairedXMLFile.FullName, true);
            }
            else
            {
                using (System.IO.StreamWriter sw = new System.IO.StreamWriter(repairedXMLFile.FullName, false, new System.Text.UTF8Encoding(true)))
                {
                    sw.Write(repaired);
                }
            }


            return result;
        }

        /// <inheritdoc cref="RepairXML(System.IO.FileInfo, System.IO.FileInfo)"/>
        internal RepairResult RepairXML(string xml, out string repaired)
        {
            RepairResult rr1 = RepairResult.Broken;

            repaired = xml;

            //xml = xml.Replace("/>", " />");
            xml = Regex.Replace(xml, @"</\s+", "</", RegexOptions.Singleline);
            xml = Regex.Replace(xml, @"<\s+", "<", RegexOptions.Singleline);

            for (int i = 0; i < 1 && rr1 != RepairResult.Pass; i++)
            {
                System.Text.StringBuilder sb = new System.Text.StringBuilder();
                System.IO.TextWriter tw = new System.IO.StringWriter(sb);
                rr1 = RepaireMemberNode(tw, xml);
                repaired = sb.ToString();
            }

            if (rr1 == RepairResult.Broken)
            {
                return RepairResult.Broken;
            }

            var rr2 = RepaireRoot(repaired, out repaired);
            if (rr2 == RepairResult.Broken)
            {
                return RepairResult.Broken;
            }

            var rr3 = (RepairResult)CheckXMLString(repaired, false);
            if (rr3 == RepairResult.Broken)
            {
                return RepairResult.Broken;
            }
            return (rr1 | rr2 | rr3);
        }

        internal const string GROUP_NAME = "TAGNAME";
        internal static readonly string[] TAGNAME_CHILDREN = new string[] { "summary", "remarks", "returns", "param", "paramref", "typeparam", "typeparamref", "value", "exception" };

        internal static readonly Regex REG_Member = CreateReg("member");
        internal static readonly Regex REG_MemberChildren = CreateReg(string.Join("|", TAGNAME_CHILDREN));
        internal static readonly Regex REG_MemberChildrenStart = CreateReg($@"<(?<TAGNAME>{string.Join("|", TAGNAME_CHILDREN)})(?=[\s/>])");

        private static Regex CreateReg(string tagnames)
        {
            var pattern = $@"(<(?<TAGNAME>{tagnames})((\s*?/>)|(\s[^<>]*?/>)|([\s>]((?!<\k<TAGNAME>[\s/>]).)*?</\k<TAGNAME>\s*?>)))";
            return new Regex(pattern, RegexOptions.Singleline | RegexOptions.Compiled);
        }


        private static void WriteBroken(System.IO.TextWriter tw, string brokenXML)
        {
            tw.Write($"<?broken {brokenXML} ?>");// 壊れたタグを読み込みから除外されるように細工
        }

        internal RepairResult RepaireMemberNode(System.IO.TextWriter tw, string xml)
        {
            xml = xml.Replace("/>", " />");
            xml = Regex.Replace(xml, @"</\s+", "</", RegexOptions.Singleline);

            return RepaireNodeInternal(tw, xml, REG_Member, (tw, xml) => RepaireMemberChildren(tw, xml));
        }

        internal RepairResult RepaireMemberChildren(System.IO.TextWriter tw, string xml)
        {
            return RepaireNodeInternal(tw, xml, REG_MemberChildren, (text, tw) => { WriteBroken(text, tw); });
        }


        private delegate void ModifyDelegate(System.IO.TextWriter tw, string xml);

        private RepairResult RepaireNodeInternal(TextWriter tw, string xml, Regex reg, ModifyDelegate repaireAction)
        {
            RepairResult rr = RepairResult.Pass;


            int lastIndex = 0;
            foreach (var match in reg.EnumerateMatches(xml))
            {
                var text = xml.Substring(match.Index, match.Length);
                if (BrokenCheckResult.Pass == CheckXMLString(text, true))
                {
                    continue;
                }
                else
                {
                    rr |= RepairResult.Broken;

                    tw.Write(xml.AsSpan(lastIndex, match.Index - lastIndex)); //壊れたタグの前までを書き込み
                                                                              //tw.Write($"<?broken {text} ?>");  // 壊れたタグを読み込みから除外されるように細工
                    repaireAction(tw, text);

                    lastIndex = match.Index + match.Length;

                    rr |= RepairResult.Repaired;
                }
            }


            tw.Write(xml.AsSpan(lastIndex, xml.Length - lastIndex));

            tw.Flush();
            return rr;
        }

        private BrokenCheckResult CheckXMLString(string text, bool isWriteLog)
        {
            try
            {
                using (var sr = new System.IO.StringReader(text))
                {
                    System.Xml.XmlReader xr = System.Xml.XmlReader.Create(sr);
                    while (xr.Read())
                    {
                    }
                }
                return BrokenCheckResult.Pass;
            }
            catch (System.Xml.XmlException ex)
            {
                if (isWriteLog)
                {
                    Log?.WriteLine($"{ex.Message}\r\n不正なXMLを検出\r\n{text}");
                }

                return BrokenCheckResult.Broken;
            }
        }

        #region RepaireRoot

        /// <summary>XMLのルートがおかしい状態を修正する</summary>
        /// <param name="source">修復対象のXML</param>
        /// <param name="repaired">修復対象のXML</param>
        /// <returns>
        /// RepairResult.NoBroken : 修復不要
        /// RepairResult.Repaired : 修復されたか
        /// RepairResult.Broken   : 修復できなかった
        /// </returns>
        internal RepairResult RepaireRoot(string source, out string repaired)
        {
            repaired = source;

            var xdoc = new System.Xml.XmlDocument();
            try
            {
                xdoc.LoadXml(source);
            }
            catch
            {
                return RepairResult.Broken;
            }

            var result = RepaireRoot(xdoc);
            if (result == RepairResult.Repaired)
            {
                repaired = xdoc.OuterXml;
            }
            return result;
        }

        private RepairResult RepaireRoot(System.Xml.XmlDocument xdoc)
        {

            try
            {
                if (DocXml.GetDocMembersNode(xdoc, out var members))
                {
                    return RepairResult.Pass;
                }

                Log?.WriteLine("XMLにdocタグが見つかりません");

                // なぜか<?xml><span><doc></doc><span> になっているXMLがある(netstandard.xml ～2.0だよ)
                // <?xml><doc></doc>に修正
                foreach (System.Xml.XmlNode cn0 in xdoc.ChildNodes.XmlNodes())
                {
                    if (cn0 is System.Xml.XmlDeclaration)
                    {
                        continue;
                    }

                    if (DocXml.GetDocMembersNode(cn0, out members))
                    {
                        if (members.Count == 1)
                        {
                            if (members[0].ChildNodes.XmlNodes().Any(_ => _.Name == "member"))
                            {
                                var doc = cn0.SelectSingleNode("doc")!;
                                cn0.RemoveChild(doc);
                                xdoc.RemoveChild(cn0);
                                xdoc.AppendChild(doc);

                                Log?.WriteLine("XMLのdocタグの階層を変更しました。");
                                return RepairResult.Broken | RepairResult.Repaired;
                            }
                        }
                    }
                }

                return RepairResult.Broken;
            }
            catch
            {
                return RepairResult.Broken;
            }
        }

        #endregion

        private string ReadOriginalXMLString(string originalFile)
        {
            using (System.IO.StreamReader sr = new System.IO.StreamReader(originalFile, true))
            {
                return sr.ReadToEnd();
            }
        }
    }

    [Flags]
    enum BrokenCheckResult
    {
        Pass = 0,
        Broken = 1,
    }

    [Flags]
    enum RepairResult
    {
        Pass = BrokenCheckResult.Pass,
        Broken = BrokenCheckResult.Broken,

        Repaired = 3
    }
}
