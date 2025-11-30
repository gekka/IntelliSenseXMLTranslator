using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gekka.Language.IntelliSenseXMLTranslator.DB
{
    internal interface IStringDictionary : System.Collections.Generic.IDictionary<string, string>,IDisposable
    {
         /// <summary>保存</summary>
        void SaveChanges();
    }
}
