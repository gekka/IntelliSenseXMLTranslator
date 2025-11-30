namespace Gekka.Language.IntelliSenseXMLTranslator.DB
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;

    public class DiffStringDictionary : IDictionary<string, string>, IDisposable, IStringDictionary
    {
        public DiffStringDictionary(StringDictionary dic, StringDictionary newdic)
        {
            this.dic = dic;
            this.newdic = newdic;
        }
        public DiffStringDictionary(StringDictionary dic, string dbPath, bool autoSave = true)
            : this(dic, new StringDictionary(dbPath, autoSave))
        {
        }
        public DiffStringDictionary(StringDictionary dic, string dbPath, System.Text.Encoding enc, bool autoSave = true)
            : this(dic, new StringDictionary(dbPath, enc, autoSave))
        {
        }

        private StringDictionary dic;
        private StringDictionary newdic;

        /// <summary>保存</summary>
        public void SaveChanges() => newdic.SaveChanges();

        /// <inheritdoc/>
        public bool IsChanged => newdic.IsChanged;

        /// <inheritdoc />
        public void Dispose() => newdic.Dispose();

        #region


        /// <inheritdoc />
        public void Add(string key, string value) => newdic.Add(key, value);

        /// <inheritdoc />
        public bool ContainsKey(string key) => dic.ContainsKey(key) || newdic.ContainsKey(key);
        /// <inheritdoc />
        public bool Remove(string key) => newdic.Remove(key);
        /// <inheritdoc />
        public bool TryGetValue(string key, [System.Diagnostics.CodeAnalysis.MaybeNullWhen(false)] out string value)
        {
            return dic.TryGetValue(key, out value) || newdic.TryGetValue(key, out value);
        }
        /// <inheritdoc />
        public string this[string key]
        {
            get
            {
                if (dic.TryGetValue(key, out var value))
                {
                    return value;
                }
                return newdic[key];
            }
            set
            {
                newdic[key] = value;
            }
        }
        /// <inheritdoc />
        public ICollection<string> Keys => dic.Keys.Concat(newdic.Keys).ToArray();
        /// <inheritdoc />
        public ICollection<string> Values => dic.Values.Concat(newdic.Keys).ToArray();
        /// <inheritdoc />
        public void Add(KeyValuePair<string, string> item)
        {
            newdic.Add(item.Key, item.Value);
        }
        /// <inheritdoc />
        public void Clear()
        {
            newdic.Clear();
        }
        /// <inheritdoc />
        public bool Contains(KeyValuePair<string, string> item) => dic.Contains(item) || newdic.Contains(item);
        /// <inheritdoc />
        public void CopyTo(KeyValuePair<string, string>[] array, int arrayIndex) => ((ICollection<KeyValuePair<string, string>>)newdic).CopyTo(array, arrayIndex);
        /// <inheritdoc />
        public bool Remove(KeyValuePair<string, string> item)
        {
            return ((ICollection<KeyValuePair<string, string>>)newdic).Remove(item);
        }
        /// <inheritdoc />
        public int Count => dic.Count + newdic.Count;
        /// <inheritdoc />
        public bool IsReadOnly => ((ICollection<KeyValuePair<string, string>>)newdic).IsReadOnly;
        /// <inheritdoc />
        public IEnumerator<KeyValuePair<string, string>> GetEnumerator() => ((IEnumerable<KeyValuePair<string, string>>)dic).Concat((IEnumerable<KeyValuePair<string, string>>)newdic).GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();

        #endregion
    }
}
