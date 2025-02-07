using Gekka.Language.IntelliSenseXMLTranslator.Doc;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace TestProject
{
    using static Util;

    [TestClass]
    public class UnitTest1
    {
        [TestMethod]
        public void TestRepairRoot()
        {
            string xmlInput = """
                <?xml version="1.0" encoding="utf-8"?>
                <span>
                <doc>                   
                    <members>
                        <member/>
                    </members>
                </doc>
                </span>
                """;

            string xmlCheck = """
                <?xml version="1.0" encoding="utf-8"?>
                <doc>
                  <members>
                    <member />
                  </members>
                </doc>
                """;
           
            var result1 = XmlChecker.RepaireRoot(xmlInput,out var repaired1);
            Assert.IsTrue(result1 == XmlChecker.RepairResult.Repaired);

            var result2 = XmlChecker.RepaireRoot(repaired1, out var repaired2);

            Assert.IsTrue(result2 == XmlChecker.RepairResult.NoBroken);
            Assert.IsTrue(repaired1 == repaired2);
            Assert.IsTrue(CompareXML(xmlCheck,repaired2));
        }

        [TestMethod]
        public void TestRepaireXML()
        {
            string xmlInput = """
                <?xml version="1.0" encoding="utf-8"?>
                <doc>
                    <assembly>
                        <name>PresentationCore</name>
                    </assembly>
                    <members>
                        <member name="P:System.Windows.FreezableCollection`1.System#Collections#IList#Item(System.Int32)">
                          <summary>AAA<see cref="P:System.Collections.IList.Item(System.Int32)" />.</summary>
                          <param name="index" />
                          <returns>BBB</returns>
                        </member>
                        <member name="T:System.Windows.FreezableCollection`1.Enumerator">
                          <summary>CCC<see cref="T:System.Windows.FreezableCollection`1" />.</summary>
                          <typeparam name="T" />
                        </member>
                        <member name="T:System.Windows.FreezableCollection`1.Enumerator">
                          <param name="index">DDD</param>
                          <typeparam name="T" >EEE</typeparam>
                        </member>
                    </members>
                </doc>
                """;

            var result1=XmlChecker.RepairXML(xmlInput, out var repired1);
        }
    }

    class Util
    {
        public static bool CompareXML(string xml1, string xml2)
        {
            return FlatXML(xml1) == FlatXML(xml2);
        }
        public static string FlatXML(string xml)
        {
            var temp = System.Text.RegularExpressions.Regex.Replace(xml, @"[\r\n\s]", "");
            return temp;
        }
    }
}