namespace Gekka.Language.IntelliSenseXMLTranslator.Doc
{
    using System;
    using System.Linq;
    using System.Collections.Generic;

    /// <summary>
    /// XMLファイルの正常性確認
    /// </summary>
    class XmlChecker
    {
        public enum RepairResult
        {
            NoBroken = 0,
            Repaired,

            Broken,

        }

        /// <summary>XMLが不正になっているElementを含んでいる場合に、可能なら修復する</summary>
        /// <param name="originalFile">元のXMLのパス</param>
        /// <param name="repairedXMLFile">修復したXMLのパス</param>
        /// <returns>不正がないか修復出来たらtrue,修復してみても不正のままならfalse</returns>
        public static RepairResult RepairXML(System.IO.FileInfo originalFile, System.IO.FileInfo repairedXMLFile)
        {
            var xml = ReadOriginalXMLString(originalFile.FullName);
            var result = RepairXML(xml, out var repaired);
            if (result == RepairResult.Broken)
            {
                return RepairResult.Broken;
            }

            repairedXMLFile.Delete();

            using (System.IO.StreamWriter sw = new System.IO.StreamWriter(repairedXMLFile.FullName, false, new System.Text.UTF8Encoding(true)))
            {
                sw.Write(repaired);
            }

            return result;
        }

        /// <inheritdoc cref="RepairXML(System.IO.FileInfo, System.IO.FileInfo)"/>
        internal static RepairResult RepairXML(string xml, out string repaired)
        {
            repaired = xml;
            bool hasBroken = true;
            for (int i = 0; i < 1 && hasBroken; i++)
            {
                System.Text.StringBuilder sb = new System.Text.StringBuilder();
                System.IO.TextWriter tw = new System.IO.StringWriter(sb);
                hasBroken = RepaireNode(xml, tw);
                repaired = sb.ToString();
            }

            var repaireResult = RepaireRoot(repaired, out repaired);
            if (repaireResult == RepairResult.NoBroken && hasBroken)
            {
                return RepairResult.Repaired;
            }
            else
            {
                return repaireResult;
            }
        }


        internal static bool RepaireNode(string xml, System.IO.TextWriter tw)
        {
            bool hasBroken = false;

            string[] tags = new string[] { "summary", "remarks", "returns", "param", "paramref", "typeparam", "typeparamref", "value", "exception" };
            var pattern = string.Join("|", tags.Select(tagName => $"(<{tagName}.+?</{tagName}>)"));
            pattern = "(?<TAGNAME>" + pattern + ")";
            System.Text.RegularExpressions.Regex reg = new System.Text.RegularExpressions.Regex(pattern, System.Text.RegularExpressions.RegexOptions.Singleline);

            int lastIndex = 0;

            Dictionary<string, int> dicBrokenTagCount = new Dictionary<string, int>();

            foreach (var match in reg.EnumerateMatches(xml))
            {
                var text = xml.Substring(match.Index, match.Length);
                try
                {
                    using (var sr = new System.IO.StringReader(text))
                    {
                        System.Xml.XmlReader xr = System.Xml.XmlReader.Create(sr);
                        while (xr.Read()) { }
                    }

                    continue;
                }
                catch (System.Xml.XmlException)
                {
                }

                hasBroken = true;

                var index = text.IndexOfAny(new char[] { ' ', '>', '/' });
                var tagName = text.Substring(1, index - 1).Trim();

                if (tagName != null)
                {
                    if (dicBrokenTagCount.TryGetValue(tagName, out var count))
                    {
                        dicBrokenTagCount[tagName] = count + 1;
                    }
                    else
                    {
                        dicBrokenTagCount[tagName] = 1;
                    }
                }
                tw.Write(xml.AsSpan(lastIndex, match.Index - lastIndex));
                //sw.Write("<returns>!!! BROKEN !!!</returns>");
                tw.Write($"<?broken {text} ?>");
                lastIndex = match.Index + match.Length;

            }

            tw.Write(xml.AsSpan(lastIndex, xml.Length - lastIndex));
            tw.Flush();

            return hasBroken;
        }

        /// <inheritdoc cref="RepaireRoot(string,out string)"/>
        internal static RepairResult RepaireRoot(System.IO.FileInfo source, System.IO.FileInfo repaired)
        {
            try
            {
                var xdoc = new System.Xml.XmlDocument();
                xdoc.Load(source.FullName);
                var result = RepaireRoot(xdoc);
                if (result == RepairResult.Repaired)
                {
                    xdoc.Save(repaired.FullName);
                }
                return result;
            }
            catch
            {
                return RepairResult.Broken;
            }
        }

        /// <summary>XMLのルートがおかしい状態を修正する</summary>
        /// <param name="source">修復対象のXML</param>
        /// <param name="repaired">修復対象のXML</param>
        /// <returns>
        /// RepairResult.NoBroken : 修復不要
        /// RepairResult.Repaired : 修復されたか
        /// RepairResult.Broken   : 修復できなかった
        /// </returns>
        internal static RepairResult RepaireRoot(string source, out string repaired)
        {
            repaired = source;

            var xdoc = new System.Xml.XmlDocument();
            xdoc.LoadXml(source);
            var result = RepaireRoot(xdoc);
            if (result == RepairResult.Repaired)
            {
                repaired = xdoc.OuterXml;
            }
            return result;
        }

        private static RepairResult RepaireRoot(System.Xml.XmlDocument xdoc)
        {
            try
            {
                if (DocXml.GetDocMembersNode(xdoc, out var members))
                {
                    return RepairResult.NoBroken;
                }

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

                                return RepairResult.Repaired;
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

        private static string ReadOriginalXMLString(string originalFile)
        {
            using (System.IO.StreamReader sr = new System.IO.StreamReader(originalFile, true))
            {
                return sr.ReadToEnd();
            }
        }
    }


}
