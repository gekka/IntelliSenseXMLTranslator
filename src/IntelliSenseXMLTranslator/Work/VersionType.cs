namespace Gekka.Language.IntelliSenseXMLTranslator.Work
{
    enum VersionType
    {
        [Util.EnumParameterDescription("検出可能なすべてのバージョンのファイルを翻訳します")]
        All,

        [Util.EnumParameterDescription("検出可能なバージョンのうち、最新と考えられるのみ翻訳します")]
        Latest
    }
}