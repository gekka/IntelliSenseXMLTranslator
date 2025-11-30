namespace Gekka.Language.IntelliSenseXMLTranslator.DB
{
    public class SQLStringDictionary : SQLDictionary<string, string>, IStringDictionary
    {
        private static string conv(string v) => v;

        public SQLStringDictionary(string dbPath, string tableName) : base(dbPath, tableName, conv, conv, conv, conv)
        {
        }

        /// <inheritdoc/>
        public void SaveChanges()
        {
        }
    }
}
