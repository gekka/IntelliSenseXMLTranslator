using Gekka.Language.IntelliSenseXMLTranslator.Doc;

namespace TestProject
{
    class Util
    {
        public static bool CompareXML(string? xml1, string? xml2)
        {
            xml1 = RemoveBrokenTag(FlatXML(xml1));
            xml2 = RemoveBrokenTag(FlatXML(xml2));
            if (xml1 == xml2)
            {
                return true;
            }
            else
            {
                for (int i = 0; i < System.Math.Min(xml1.Length, xml2.Length); i++)
                {
                    if (xml1[i] != xml2[i])
                    {
                    }
                }


                return false;
            }
        }
        public static string FlatXML(string? xml)
        {
            if (xml == null) return "";
            var temp = System.Text.RegularExpressions.Regex.Replace(xml, @"[\r\n\s]", "");
            return temp;
        }
        public static string RemoveBrokenTag(string? xml)
        {
            if (xml == null) return "";
            var temp = System.Text.RegularExpressions.Regex.Replace(xml, @"<\?broken.*?\?>", "", System.Text.RegularExpressions.RegexOptions.Singleline);
            return temp;
        }

        public static void CheckTrue(System.Text.RegularExpressions.Regex reg, string input, string value, string tag)
        {
            var match = reg.Match(input);
            Assert.IsTrue(match.Success, "正規表現で一致するはずが一致してません");
            Assert.IsTrue(match.Value == value, "Matchした文字列が想定した文字列になっていません");
            Assert.IsTrue(match.Groups[XmlChecker.GROUP_NAME].Value == tag, "Matchしたグループが想定した文字列になっていません");
        }

        public static void CheckFalse(System.Text.RegularExpressions.Regex reg, string input)
        {
            var match = XmlChecker.REG_Member.Match(input);
            Assert.IsFalse(match.Success, "正規表現で一致しないはずが一致してます");
        }
    }
}