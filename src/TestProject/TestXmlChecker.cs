using Gekka.Language.IntelliSenseXMLTranslator.Doc;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace TestProject
{
    using static Util;

    /// <summary>XML修復の解析用の正規表現をテストするクラス</summary>
    [TestClass]
    public sealed class TestXmlChecker
    {
        private readonly XmlChecker checker = new XmlChecker();

        [TestMethod]
        public void TestRegMember()
        {
            CheckTrue("<member/>", "<member/>", "member");
            CheckTrue("<member />", "<member />", "member");

            CheckTrue("<member></member>", "<member></member>", "member");
            CheckTrue("<member></member >", "<member></member >", "member");

            CheckTrue("<member x='member'></member>", "<member x='member'></member>", "member");

            CheckTrue("<member><summary/></member>", "<member><summary/></member>", "member");
            CheckTrue("<member><memberX/></member>", "<member><memberX/></member>", "member");
            CheckTrue("<memberX><member></member>", "<member></member>", "member");

            CheckTrue("<members><member><summary/></member></members>", "<member><summary/></member>", "member");

            CheckFalse("<memberX>");
            CheckFalse("<memberX/>");
            CheckFalse("<memberX />");
            CheckFalse("<memberX /></member>");
            CheckFalse("<memberX /></member>");
            CheckFalse("<memberX /><member></memberX>");
            CheckFalse("<member ></memberX>");
            CheckFalse("<member ></memberX>");

            void CheckTrue(string input, string value, string tag)
            {
                Util.CheckTrue(XmlChecker.REG_Member, input, value, tag);
            }
            void CheckFalse(string input)
            {
                Util.CheckFalse(XmlChecker.REG_Member, input);
            }
        }


        [TestMethod]
        public void TestRegMemberChildren()
        {
            foreach (string tagname in XmlChecker.TAGNAME_CHILDREN)
            {
                CheckTrue($"<member><{tagname}/></member>", $"<{tagname}/>", tagname);
                CheckTrue($"<member><{tagname} /></member>", $"<{tagname} />", tagname);
                CheckTrue($"<member><{tagname} X='{tagname}'/></member>", $"<{tagname} X='{tagname}'/>", tagname);

                CheckTrue($"<member>AAA<{tagname}/>BBB</member>", $"<{tagname}/>", tagname);
                CheckTrue($"<member>AAA<{tagname} />BBB</member>", $"<{tagname} />", tagname);
                CheckTrue($"<member>AAA<{tagname} X='zz'/>BBB</member>", $"<{tagname} X='zz'/>", tagname);

                CheckTrue($"<member>AAA<{tagname} X='zz'>BBB</{tagname}>CCC</member>", $"<{tagname} X='zz'>BBB</{tagname}>", tagname);
            }

            Assert.IsTrue(XmlChecker.REG_MemberChildren.Matches($"<member><summary/><typeparam T='X'/></member>").Count == 2);

            void CheckTrue(string input, string value, string tag)
            {
                Util.CheckTrue(XmlChecker.REG_MemberChildren, input, value, tag);
            }
        }


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

            var checker = this.checker;
            var result1 = checker.RepaireRoot(xmlInput, out var repaired1);
            Assert.IsTrue((result1 & RepairResult.Repaired) == RepairResult.Repaired, "修復されてません");

            var result2 = checker.RepaireRoot(repaired1, out var repaired2);

            Assert.IsTrue(result2 == RepairResult.Pass, $"修復必要ないのに{result2}になっています");
            Assert.IsTrue(repaired1 == repaired2, "変更必要ない文字列が変更されてます");
            Assert.IsTrue(CompareXML(xmlCheck, repaired2), "修復後のXMLが想定された文字列になってません");
        }

        [TestMethod]
        public void TestRepaireMemberNode1()
        {
            string xmlInput = """
                <?xml version="1.0" encoding="utf-8"?>
                <doc>
                    <members>
                        <member/> 

                        <member name="AAA" />
                            
                        <member name="BBB">
                          <summary>CCC</summary>
                        </member>     
                        
                        <member name="DDD" /> 
                    </members>
                </doc>
                """;

            System.IO.TextWriter tw = new System.IO.StringWriter(new System.Text.StringBuilder());
            var result1 = this.checker.RepaireMemberNode(tw, xmlInput);
            Assert.IsTrue(result1 == RepairResult.Pass, $"修復必要ないはずなのに{result1}になっています");
            Assert.IsTrue(CompareXML(xmlInput, tw.ToString()), "修復後のXMLが想定された文字列になってません");
        }

        // XMLのタグが閉じてない場合のテストだけど、修復が難しいので無し
        //[TestMethod()]
        //public void TestRepaireMemberNode2()
        //{ 
        //    string xmlInput = """
        //        <?xml version="1.0" encoding="utf-8"?>
        //        <doc>
        //            <members>
        //                <member/> 

        //                <member name="AAA" />

        //                <member name="BBB">

        //                </member>     

        //                <member name="DDD" >
        //                    <summary />
        //                    <remarks>
        //                </member

        //                <member name="EEE" >
        //                    <summary />
        //                    <remarks>
        //                    <returns/>
        //                </member
        //            </members>
        //        </doc>
        //        """;

        //    string xmlCheck = """
        //        <?xml version="1.0" encoding="utf-8"?>
        //        <doc>
        //            <members>
        //                <member/> 

        //                <member name="AAA" />                            

        //                <member name="BBB">
        //                  <?broken <summary>CCC ?>
        //                </member> 

        //                <member name="DDD" >
        //                    <summary />
        //                </member

        //                <member name="EEE" >
        //                    <summary />
        //                    <?broken <remarks> ?>
        //                    <returns/>
        //                </member
        //            </members>
        //        </doc>
        //        """;

        //    System.IO.TextWriter tw = new System.IO.StringWriter(new System.Text.StringBuilder());
        //    var result1 = this.checker.RepaireMemberNode(tw, xmlInput);
        //    Assert.IsTrue(result1 == RepairResult.Repaired, "修復されているはずが修復されてません");

        //    string repaired = tw.ToString() ?? "";
        //    Assert.IsTrue(CompareXML(xmlCheck, repaired), "修復後のXMLが想定された文字列になってません");
        //}

        [TestMethod]
        public void TestRepaireXML()
        {
            string xmlInput = """
                <?xml version="1.0" encoding="utf-8"?>
                <doc>
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

            var result1 = this.checker.RepairXML(xmlInput, out var repired1);
            Assert.IsTrue(result1 == RepairResult.Pass, $"修復必要ないはずなのに{result1}になっています");
        }

        [TestMethod]
        public void TextNest()
        {
            string xmlInput = """
                <?xml version="1.0" encoding="utf-8"?>
                <doc>
                    <members>
                        <member >
                          <summary>AAA<see  />BBB</summary>
                        </member>
                    </members>
                </doc>
                """;

            var result1 = this.checker.RepairXML(xmlInput, out var repired1);
            Assert.IsTrue(result1 == RepairResult.Pass, $"修復必要ないはずなのに{result1}になっています");
        }

        [TestMethod("Memberより内側の壊れの修復")]
        public void TestRepaireMemberChildren()
        {
            string xmlInput = """
                <?xml version="1.0" encoding="utf-8"?>
                <doc>
                  <assembly>
                    <name>netstandard</name>
                  </assembly>
                  <members>
                    <member name="M:System.Collections.CaseInsensitiveComparer.Compare(System.Object,System.Object)">
                      <summary>Performs a case-insensitive comparison of two objects of the same type and returns a value indicating whether one is less than, equal to, or greater than the other.</summary>
                      <param name="a">The first object to compare.</param>
                      <param name="b">The second object to compare.</param>
                      <returns><p sourcefile="netstandard.yml" sourcestartlinenumber="1" sourceendlinenumber="2"><p sourcefile="netstandard.yml" sourcestartlinenumber="1" sourceendlinenumber="1">A signed integer that indicates the relative values of <code data-dev-comment-type="paramref">a</code> and <code data-dev-comment-type="paramref">b</code>, as shown in the following table.  </p>
                 <table><thead><tr><th> Value  <p>
                <p sourcefile="netstandard.yml" sourcestartlinenumber="4" sourceendlinenumber="4"> </th><th> Meaning  <p>
                <p sourcefile="netstandard.yml" sourcestartlinenumber="6" sourceendlinenumber="6"> </th></tr></thead><tbody><tr><td> Less than zero  <p>
                <p sourcefile="netstandard.yml" sourcestartlinenumber="8" sourceendlinenumber="8"> </td><td><code data-dev-comment-type="paramref">a</code> is less than <code data-dev-comment-type="paramref">b</code>, with casing ignored.  <p>
                <p sourcefile="netstandard.yml" sourcestartlinenumber="10" sourceendlinenumber="10"> </td></tr><tr><td> Zero  <p>
                <p sourcefile="netstandard.yml" sourcestartlinenumber="12" sourceendlinenumber="12"> </td><td><code data-dev-comment-type="paramref">a</code> equals <code data-dev-comment-type="paramref">b</code>, with casing ignored.  <p>
                <p sourcefile="netstandard.yml" sourcestartlinenumber="14" sourceendlinenumber="14"> </td></tr><tr><td> Greater than zero  <p>
                <p sourcefile="netstandard.yml" sourcestartlinenumber="16" sourceendlinenumber="16"> </td><td><code data-dev-comment-type="paramref">a</code> is greater than <code data-dev-comment-type="paramref">b</code>, with casing ignored.  <p>
                <p sourcefile="netstandard.yml" sourcestartlinenumber="18" sourceendlinenumber="18"> </td></tr></tbody></table></p>
                </returns>
                      <exception cref="T:System.ArgumentException">Neither <paramref name="a">a</paramref> nor <paramref name="b">b</paramref> implements the <see cref="T:System.IComparable"></see> interface.   -or-  <paramref name="a">a</paramref> and <paramref name="b">b</paramref> are of different types.</exception>
                    </member>
                  </members>
                </doc>
                """;

            string xmlCheck = """
                <?xml version="1.0" encoding="utf-8"?>
                <doc>
                  <assembly>
                    <name>netstandard</name>
                  </assembly>
                  <members>
                    <member name="M:System.Collections.CaseInsensitiveComparer.Compare(System.Object,System.Object)">
                      <summary>Performs a case-insensitive comparison of two objects of the same type and returns a value indicating whether one is less than, equal to, or greater than the other.</summary>
                      <param name="a">The first object to compare.</param>
                      <param name="b">The second object to compare.</param>

                      <?broken 
                      <returns><p sourcefile="netstandard.yml" sourcestartlinenumber="1" sourceendlinenumber="2"><p sourcefile="netstandard.yml" sourcestartlinenumber="1" sourceendlinenumber="1">A signed integer that indicates the relative values of <code data-dev-comment-type="paramref">a</code> and <code data-dev-comment-type="paramref">b</code>, as shown in the following table.  </p>
                 <table><thead><tr><th> Value  <p>
                <p sourcefile="netstandard.yml" sourcestartlinenumber="4" sourceendlinenumber="4"> </th><th> Meaning  <p>
                <p sourcefile="netstandard.yml" sourcestartlinenumber="6" sourceendlinenumber="6"> </th></tr></thead><tbody><tr><td> Less than zero  <p>
                <p sourcefile="netstandard.yml" sourcestartlinenumber="8" sourceendlinenumber="8"> </td><td><code data-dev-comment-type="paramref">a</code> is less than <code data-dev-comment-type="paramref">b</code>, with casing ignored.  <p>
                <p sourcefile="netstandard.yml" sourcestartlinenumber="10" sourceendlinenumber="10"> </td></tr><tr><td> Zero  <p>
                <p sourcefile="netstandard.yml" sourcestartlinenumber="12" sourceendlinenumber="12"> </td><td><code data-dev-comment-type="paramref">a</code> equals <code data-dev-comment-type="paramref">b</code>, with casing ignored.  <p>
                <p sourcefile="netstandard.yml" sourcestartlinenumber="14" sourceendlinenumber="14"> </td></tr><tr><td> Greater than zero  <p>
                <p sourcefile="netstandard.yml" sourcestartlinenumber="16" sourceendlinenumber="16"> </td><td><code data-dev-comment-type="paramref">a</code> is greater than <code data-dev-comment-type="paramref">b</code>, with casing ignored.  <p>
                <p sourcefile="netstandard.yml" sourcestartlinenumber="18" sourceendlinenumber="18"> </td></tr></tbody></table></p>
                </returns> 
                        ?>

                      <exception cref="T:System.ArgumentException">Neither <paramref name="a">a</paramref> nor <paramref name="b">b</paramref> implements the <see cref="T:System.IComparable"></see> interface.   -or-  <paramref name="a">a</paramref> and <paramref name="b">b</paramref> are of different types.</exception>
                    </member>
                  </members>
                </doc>
                """;

            var result1 = this.checker.RepairXML(xmlInput, out var repired1);
            Assert.IsTrue((result1 & RepairResult.Repaired) == RepairResult.Repaired, $"修復必要なはずなのに{result1}になっています");
            Assert.IsTrue(CompareXML(xmlCheck, repired1), "修復後のXMLが想定された文字列になってません");
        }
    }
}