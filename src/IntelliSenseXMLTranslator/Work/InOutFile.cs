namespace Gekka.Language.IntelliSenseXMLTranslator.Work
{
    class InOutFile
    {
        public InOutFile(string input)
        {
            this.Input = input;
            //this.Output = output;
        }
        public string Input { get;}
        //private string Output { get; set; }

        public string? ListSource { get; set; } = null;
        public LongVersion? Ver { get; set; }

        public InOutFile TrySetListSource(string path)
        {
            if (string.IsNullOrWhiteSpace(this.ListSource))
            {
                this.ListSource = path;
            }
            return this;
        }

        /// <summary>
        /// dllのあるフォルダの下に作られる言語フォルダに作られるxmlファイルのパス
        /// </summary>
        /// <param name="outputBaseDir"></param>
        /// <param name="lang"></param>
        /// <returns></returns>
        public string GetLangXMLPath(string outputBaseDir, string lang)
        {
            //if (string.IsNullOrWhiteSpace(Output))
            //{
            //    return Output;
            //}

            var output = System.IO.Path.Combine(outputBaseDir, System.IO.Path.GetFullPath(Input).Replace(System.IO.Path.VolumeSeparatorChar, '：'));

            return System.IO.Path.Combine(System.IO.Path.GetDirectoryName(output) ?? "", lang, System.IO.Path.GetFileName(output));
        }
    }
}
