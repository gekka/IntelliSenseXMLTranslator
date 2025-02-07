#if DEBUG
[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("TestProject")]
#endif

namespace Gekka.Language.IntelliSenseXMLTranslator
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    using Gekka.Language.IntelliSenseXMLTranslator.Util;
    using Gekka.Language.IntelliSenseXMLTranslator.Work;
    using Gekka.Language.Translator;
    using Gekka.Language.Translator.Interfaces;

    internal class Program
    {
        static async Task Main(string[] args)
        {
            if (args.Length == 0)
            {
                return;
            }

            using System.Threading.CancellationTokenSource cts = new System.Threading.CancellationTokenSource();
            try
            {
                Parameters parameters = Parameters.Parse(args);

                if (parameters.Help)
                {
                    CommandLineParser.WriteHelp(parameters, System.Console.Out);
                    WriteFactoryNames("翻訳機の名前", Gekka.Language.Translator.Factory.GetTranslatorFactories());
                    return;
                }
                else if (parameters.Test)
                {
                    //対象ファイルを列挙するだけ
                    DumpFiles(parameters);
                    return;
                }
                else
                {
                    Task t1 = MainAsync(parameters, cts.Token);
                    Task t2 = ConsoleUtil.WaitCTRL_C(cts.Token);
                    await Task.WhenAny(t1, t2);
                    cts.Cancel();
                    await Task.WhenAll(t1, t2);
                }
            }
            catch (System.Threading.Tasks.TaskCanceledException)
            {
            }
            catch (Exception ex)
            {
                if (ex is AggregateException ae)
                {
                    ex = ae.InnerException ?? ex;
                }
                System.Console.ForegroundColor = ConsoleColor.Red;
                System.Console.WriteLine(ex.Message);
                System.Console.ResetColor();
                Environment.ExitCode = 1;
            }
            finally
            {
                System.Console.ResetColor();

                if (cts.IsCancellationRequested)
                {
                    System.Console.WriteLine("キャンセル");
                }

#if DEBUG
                Console.Write("Press Any Key");
                Console.ReadKey();
#endif
            }
        }

        static async Task MainAsync(Parameters parameters, System.Threading.CancellationToken token)
        {
            var fs = Gekka.Language.Translator.Factory.GetTranslatorFactories().ToArray();
            ITranslatorFactory? factory = null;
            if (parameters.TestXML)
            {
                factory = Gekka.Language.Translator.Factory.GetDummyTranslator();
            }
            else
            {
                factory = fs.Where(f => f.TranslatorName.ToLower() == parameters.Translator!.ToLower()).FirstOrDefault();
            }
            
            if (factory == null)
            {
                WriteFactoryNames("翻訳機が見つかりません", fs);
                return;
            }

            if (factory is Gekka.Language.Translator.Interfaces.IBrowserTranslatorFactory bfactory)
            {
                var dfs = Factory.GetDriverFactories();
                var df = dfs.Where(_ => _.BrowserName.ToLower() == parameters.Browser.ToString().ToLower()).FirstOrDefault();

                if (df == null)
                {
                    System.Console.WriteLine("Broser missing");
                    foreach (var dfx in dfs)
                    {
                        System.Console.WriteLine("  " + dfx.BrowserName);
                    }
                    return;
                }

                bfactory.DriverFactory = df;
            }


            var outputDir = new System.IO.DirectoryInfo(parameters.OutputDir);
            if (!outputDir.Exists)
            {
                outputDir.Create();
            }


            DB.IStringDictionary dictionary;
            if (parameters.DictionaryType == DB.DBType.Text)
            {
                dictionary = new DB.StringDictionary(parameters.Dictionary, true);
            }
            else
            {
                dictionary = new DB.SQLStringDictionary(parameters.Dictionary, "Text");
            }

            using (dictionary as IDisposable)
            {
                using (ITranslator translator = factory.Create())
                {
                    Worker worker = new Worker(dictionary, translator, token);
                    worker.InsertPoint = parameters.InsertPoint;
                    worker.IsCheckXMLOnly = parameters.TestXML;

                    var targetXMLFiles = parameters.GetFiles();

                    await worker.RunAsync(targetXMLFiles, parameters.OutputDir);

                    System.Console.WriteLine("終了");
                }
            }
        }

        static void WriteFactoryNames(string caption, IEnumerable<Gekka.Language.Translator.Interfaces.ITranslatorFactory> fs)
        {
            System.Console.WriteLine(caption);
            foreach (var f in fs)
            {
                System.Console.WriteLine("  " + f.TranslatorName);
            }
        }

        static void DumpFiles(Parameters parameters)
        {
            List<long> sizes = new List<long>();
            foreach (var f in parameters.GetFiles())
            {
                var outputXML = f.GetLangXMLPath(parameters.OutputDir, "ja");

                System.Console.ForegroundColor = ConsoleColor.Green;
                System.Console.WriteLine(f.Input);
                if (System.IO.File.Exists(outputXML))
                {
                    System.Console.ForegroundColor = ConsoleColor.Gray;
                }
                else
                {
                    System.Console.ForegroundColor = ConsoleColor.Cyan;
                    sizes.Add(new System.IO.FileInfo(f.Input).Length);
                }
                System.Console.WriteLine("> " + outputXML);
                System.Console.WriteLine();
            }

            long total = sizes.Sum();

            Console.ResetColor();
            Console.WriteLine($"合計ファイル数= {sizes.Count}");
            Console.Write($"合計ファイルサイズ= ");

            if (total >= 1024)
            {
                double totalKB = (long)Math.Ceiling((total / 1024.0));
                Console.WriteLine($"{totalKB:N0} KB");
            }
            else
            {
                Console.WriteLine($"{total:N} B");
            }
        }
    }
}
