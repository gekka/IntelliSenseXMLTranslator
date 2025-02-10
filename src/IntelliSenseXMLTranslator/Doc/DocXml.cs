namespace Gekka.Language.IntelliSenseXMLTranslator.Doc
{
    using System.Linq;
    using System.Collections.Generic;
    using System.Xml;

    class DocXml
    {
        #region static

        /// <summary>ファイルからXMLを読み込む</summary>
        /// <param name="xmlFile"></param>
        /// <param name="log"></param>
        /// <returns></returns>
        public static DocXml Load(System.IO.FileInfo xmlFile, System.IO.TextWriter log)
        {
            string repairedXMLFile = System.IO.Path.GetTempFileName();
            try
            {
                var checker = new XmlChecker(log);

                var repairResult = checker.RepairXML(xmlFile, new System.IO.FileInfo(repairedXMLFile));

                XmlDocument doc = new XmlDocument();
                using (var filestream = System.IO.File.OpenRead(repairedXMLFile))
                {
                    System.Xml.XmlReaderSettings xrs = new XmlReaderSettings()
                    {
                        CloseInput = false,

                    };

                    System.Xml.XmlReader xr = System.Xml.XmlReader.Create(filestream, xrs);

                    try
                    {
                        doc.Load(xr);
                    }
                    catch
                    {
                        throw;
                    }
                    //doc.Load(filestream);
                }

                DocXml dx = new DocXml(doc);

                //var members = doc.SelectNodes("doc/members")?.OfType<XmlNode>().ToList();

                if (GetDocMembersNode(doc, out var members))
                {
                    List<XmlNode> list = new List<XmlNode>();
                    foreach (var member in members)
                    {
                        GetTextNodes(member, list);
                    }

                    dx.Items.AddRange(list.Select(node => new DocXmlItem(node)));
                }
                return dx;
            }
            finally
            {
                System.IO.File.Delete(repairedXMLFile);
            }

        }

        /// <summary>値に文字列を持っているノード一覧を取得する</summary>
        /// <param name="node"></param>
        /// <param name="list"></param>
        private static void GetTextNodes(XmlNode node, List<XmlNode> list)
        {
            var nodes = node.ChildNodes.OfType<XmlNode>().ToArray();
            if (node is XmlElement elem)
            {
                if (nodes.Any(_ => _ is XmlText))
                {
                    list.Add(elem);
                }
            }

            foreach (var n in nodes)
            {
                GetTextNodes(n, list);
            }
        }

        /// <summary>
        /// &lt;?xml&gt;&lt;doc&gt;&lt;/doc&gt;を探す
        /// </summary>
        /// <param name="docOrNode"></param>
        /// <param name="members">見つかったmembers</param>
        /// <returns></returns>
        public static bool GetDocMembersNode(System.Xml.XmlNode docOrNode, out IList<System.Xml.XmlNode> members)
        {
            members = docOrNode.SelectNodes("doc/members")?.XmlNodes().ToList()
                ?? (IList<System.Xml.XmlNode>)System.Array.Empty<System.Xml.XmlNode>();
            return members.Count != 0;
        }

        #endregion

        private DocXml(XmlDocument doc)
        {
            XmlDocument = doc;
        }

        /// <summary></summary>
        public XmlDocument XmlDocument { get; }

        /// <summary>置換対象となりうるノードの一覧</summary>
        public List<DocXmlItem> Items { get; } = new List<DocXmlItem>();

        public void Save(string path)
        {
            if (System.IO.File.Exists(path))
            {
                System.IO.File.Delete(path);
            }
            using (var stream = System.IO.File.OpenWrite(path))
            {
                Save(stream);
            }
        }

        public void Save(System.IO.Stream stream)
        {
            var ws = new XmlWriterSettings()
            {
                Encoding = new System.Text.UTF8Encoding(true),
            };

            using (XmlWriter xw = XmlWriter.Create(stream, ws))
            {
                XmlDocument.Save(stream);
                xw.Flush();
            }

        }

        public void InsertComment(string comment)
        {
            var docNode = this.XmlDocument.SelectSingleNode("doc");
            if (docNode?.OwnerDocument != null)
            {
                var commentNode = docNode.OwnerDocument.CreateComment(comment);
                docNode.InsertBefore(commentNode, docNode.FirstChild);
            }
        }

    }

}
