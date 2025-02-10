namespace Gekka.Language.IntelliSenseXMLTranslator.DB
{
    enum DBType
    {
        [Util.EnumParameterDescription("元英文と日本語文のペアをテキスト形式で保存します")]
        Text,

        [Util.EnumParameterDescription("SQliteを辞書ファイルとして元英文をキーとしたテーブルに保存します")]
        Sqlite,
    }
}
