using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Gekka.Language.Translator.Interfaces;
using Gekka.Language.Translator.Translators;

namespace Gekka.Language.Translator
{
    public static class Factory
    {
        public static IEnumerable<ITranslatorFactory> GetTranslatorFactories()
        {
            return new ITranslatorFactory[]
            {
                new BrowserTranslatorFactory(),
                new DeepLWebTranslatorFactory(),
                //new DeepLAPITranslatorFactory(),
                new DummyTranslatorFactory(),
            };
        }

        public static IList<IWebDriverFactory> GetDriverFactories()
        {
            return new IWebDriverFactory[]
            {
                new Browser.Edge(),
                new Browser.Chrome(),
            };
        }

        public static ITranslatorFactory GetDummyTranslator()
        {
            return new DummyTranslatorFactory();
        }
    }
}
