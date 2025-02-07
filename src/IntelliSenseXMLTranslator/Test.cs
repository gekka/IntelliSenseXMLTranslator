namespace Gekka.Language.IntelliSenseXMLTranslator
{
    using System;
    using System.Linq;
    using System.Collections;
    using System.Collections.Generic;
    using System.Xml;
    using System.Xml.Linq;
    class Test
    {
        private static string[] TargetTags = new[] { "summary", "returns", "remarks", "params", "typeparams", "exceptions", "examples" };
        private static string[][] TargetPaths = TargetTags.Select(x => ("doc/members/member/" + x).Split("/").ToArray()).ToArray();

        public static Dictionary<string, Temp> dic = new Dictionary<string, Temp>();

        public class Temp
        {
            public string Tag;
            public bool HasInner;
            public string? XML;
        }

        public static void TestX(System.IO.FileInfo file)
        {
            System.Xml.XmlDocument doc = new System.Xml.XmlDocument();
            using (var filestream = file.OpenRead())
            {
                doc.Load(filestream);
            }
            System.Collections.Generic.Stack<System.Xml.XmlNode> stack = new Stack<System.Xml.XmlNode>();

            System.Collections.Generic.List<System.Xml.XmlNode> list = new List<System.Xml.XmlNode>();
            TestX3(doc, list);

            var list2 = list.Where(_ => _.XmlNodes().Any(_ => !(_ is System.Xml.XmlText))).ToList();
            if (list2.Count > 0)
            {
                var x = list2.SelectMany(_ => _.XmlNodesNotText())
                    .Select(_ => _.Name).Distinct().ToArray();

                var diff = x.Except(dic.Keys).ToList();
                if (diff.Count > 0)
                {
                    foreach (var d in diff)
                    {
                        var f = list2.First(_ => _.XmlNodes().Any(_ => _.Name == d));
                        //System.Diagnostics.Debug.WriteLine("@" + d + "\t:" + f.InnerXml + "\r\n");

                        Temp t = new Temp() { Tag = d };
                        t.XML = f.InnerXml;
                        dic.Add(d, t);
                    }
                }

                foreach (var n in list2.SelectMany(_ => _.XmlNodesNotText()))
                {
                    if (n.HasChildNodes)
                    {
                        var temp = dic[n.Name];
                        if (!temp.HasInner)
                        {
                            temp.HasInner = true;
                            temp.XML = n.ParentNode!.InnerXml;
                        }
                    }
                }


            }
        }
        private static void TestX3(System.Xml.XmlNode node, List<System.Xml.XmlNode> list)
        {
            var nodes = node.ChildNodes.OfType<System.Xml.XmlNode>().ToArray();
            if (node is System.Xml.XmlElement elem)
            {
                if (elem.Name == "th")
                {
                }
                if (nodes.Any(_ => _ is System.Xml.XmlText))
                {
                    list.Add(elem);
                }
            }

            foreach (var n in nodes)
            {
                TestX3(n, list);
            }

        }

        public static void TestX2(System.IO.FileInfo xmlfile)
        {
            System.IO.MemoryStream ms = new System.IO.MemoryStream();

            var ws = new System.Xml.XmlWriterSettings();
            ws.NewLineHandling = System.Xml.NewLineHandling.Entitize;
            ws.Encoding = new System.Text.UTF8Encoding(true);
            ws.OmitXmlDeclaration = false;
            System.Xml.XmlWriter xw = System.Xml.XmlWriter.Create(ms, ws);

            using var stream = xmlfile.OpenRead();
            System.Xml.XmlReader xr = System.Xml.XmlReader.Create(stream);

            TestY(xr, xw);

            xw.Flush();


            var s = System.Text.Encoding.UTF8.GetString(ms.ToArray());


        }


        private static void TestY(System.Xml.XmlReader xr, System.Xml.XmlWriter xw)
        {
            System.Collections.Generic.Stack<string> tags = new System.Collections.Generic.Stack<string>();

            while (xr.Read())
            {
                switch (xr.NodeType)
                {
                case System.Xml.XmlNodeType.None:
                    break;
                case System.Xml.XmlNodeType.Element:

                    bool isEmptyElement = xr.IsEmptyElement;
                    string name = xr.Name;
                    tags.Push(xr.Name);
                    xw.WriteStartElement(xr.Name);

                    if (xr.HasAttributes)
                    {
                        for (int i = 0; i < xr.AttributeCount; i++)
                        {
                            xr.MoveToAttribute(i);
                            var aname = xr.Name;
                            var av = xr.Value;

                            xw.WriteAttributeString(aname, av);
                        }
                    }

                    if (isEmptyElement)
                    {
                        xw.WriteEndElement();
                        tags.Pop();
                    }
                    else if (TargetTags.Contains(name))
                    {
                        var ar = tags.Reverse().ToArray();
                        if (TargetPaths.Any(_ => ar.SequenceEqual(_)))
                        {
                            TestZ(xr, xw);

                            if (xr.NodeType != System.Xml.XmlNodeType.EndElement)
                            {
                                throw new ApplicationException();
                            }
                            xw.WriteEndElement();
                            tags.Pop();
                        }
                    }


                    break;
                case System.Xml.XmlNodeType.Attribute:
                    break;
                case System.Xml.XmlNodeType.Text:
                    xw.WriteValue(xr.Value);
                    break;
                case System.Xml.XmlNodeType.CDATA:
                    break;
                case System.Xml.XmlNodeType.EntityReference:
                    break;
                case System.Xml.XmlNodeType.Entity:
                    break;
                case System.Xml.XmlNodeType.ProcessingInstruction:
                    break;
                case System.Xml.XmlNodeType.Comment:
                    break;
                case System.Xml.XmlNodeType.Document:
                    break;
                case System.Xml.XmlNodeType.DocumentType:
                    break;
                case System.Xml.XmlNodeType.DocumentFragment:
                    break;
                case System.Xml.XmlNodeType.Notation:
                    break;
                case System.Xml.XmlNodeType.Whitespace:
                    xw.WriteWhitespace(xr.Value);
                    break;
                case System.Xml.XmlNodeType.SignificantWhitespace:
                    break;
                case System.Xml.XmlNodeType.EndElement:
                    if (tags.Peek() != xr.Name)
                    {
                        throw new ApplicationException();
                    }

                    xw.WriteEndElement();

                    tags.Pop();
                    break;
                case System.Xml.XmlNodeType.EndEntity:
                    break;
                case System.Xml.XmlNodeType.XmlDeclaration:

                    break;
                default:
                    break;
                }
            }
        }

        private static void TestZ(System.Xml.XmlReader xr, System.Xml.XmlWriter xw)
        {
            System.Collections.Generic.Stack<string> tags = new System.Collections.Generic.Stack<string>();

            while (xr.Read())
            {
                switch (xr.NodeType)
                {
                case System.Xml.XmlNodeType.None:
                    break;
                case System.Xml.XmlNodeType.Element:

                    bool isEmptyElement = xr.IsEmptyElement;

                    tags.Push(xr.Name);
                    xw.WriteStartElement(xr.Name);

                    if (xr.HasAttributes)
                    {
                        for (int i = 0; i < xr.AttributeCount; i++)
                        {
                            xr.MoveToAttribute(i);
                            var aname = xr.Name;
                            var av = xr.Value;

                            xw.WriteAttributeString(aname, av);
                        }
                    }

                    if (isEmptyElement)
                    {
                        xw.WriteEndElement();
                        tags.Pop();
                    }

                    break;
                case System.Xml.XmlNodeType.Attribute:
                    break;
                case System.Xml.XmlNodeType.Text:
                    xw.WriteValue(xr.Value);
                    break;
                case System.Xml.XmlNodeType.CDATA:
                    break;
                case System.Xml.XmlNodeType.EntityReference:
                    break;
                case System.Xml.XmlNodeType.Entity:
                    break;
                case System.Xml.XmlNodeType.ProcessingInstruction:
                    break;
                case System.Xml.XmlNodeType.Comment:
                    break;
                case System.Xml.XmlNodeType.Document:
                    break;
                case System.Xml.XmlNodeType.DocumentType:
                    break;
                case System.Xml.XmlNodeType.DocumentFragment:
                    break;
                case System.Xml.XmlNodeType.Notation:
                    break;
                case System.Xml.XmlNodeType.Whitespace:
                    xw.WriteWhitespace(xr.Value);
                    break;
                case System.Xml.XmlNodeType.SignificantWhitespace:
                    break;
                case System.Xml.XmlNodeType.EndElement:

                    if (tags.Count() == 0)
                    {
                        return;
                    }
                    xw.WriteEndElement();
                    tags.Pop();
                    break;
                case System.Xml.XmlNodeType.EndEntity:
                    break;
                case System.Xml.XmlNodeType.XmlDeclaration:

                    break;
                default:
                    break;
                }
            }
        }


        public static void TextSelfClose()
        {
            string xml= """
                <?xml version="1.0" encoding="utf-8"?>
                <doc>
                    <assembly>
                        <name>PresentationCore</name>
                    </assembly>
                    <members>
                        <member name="P:System.Windows.FreezableCollection`1.System#Collections#IList#Item(System.Int32)">
                          <summary>For a description of this member, see <see cref="P:System.Collections.IList.Item(System.Int32)" />.</summary>
                          <param name="index" />
                          <returns>The element at the specified index.</returns>
                        </member>
                        <member name="T:System.Windows.FreezableCollection`1.Enumerator">
                          <summary>Enumerates the members of a <see cref="T:System.Windows.FreezableCollection`1" />.</summary>
                          <typeparam name="T" />
                        </member>
                    </members>
                </doc>
                """;
        }
    }

    static class XMLExention
    {
        public static IEnumerable<XmlNode> XmlNodes(this System.Xml.XmlNode node) => node.ChildNodes.OfType<XmlNode>();
        public static IEnumerable<XmlNode> XmlNodesNotText(this System.Xml.XmlNode node) => node.XmlNodes().Where(_ => !(_ is XmlText));
    }
}
