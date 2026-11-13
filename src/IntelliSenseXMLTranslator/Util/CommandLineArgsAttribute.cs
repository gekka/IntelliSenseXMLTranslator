
namespace Gekka.Language.IntelliSenseXMLTranslator.Util
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;

    /// <summary>
    /// コマンドラインの引数を受け取るプロパティの属性
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    class CommandLineArgsAttribute : Attribute
    {
        public CommandLineArgsAttribute(string @long, string @short, string description = "", bool hasParameter = true)
        {
            @long = @long.Trim();
            if (string.IsNullOrWhiteSpace(@long))
            {
                throw new ArgumentException(nameof(@long));
            }

            Long = @long;
            Short = @short;
            //Short = string.IsNullOrWhiteSpace(@short) ? @long.Substring(0, 1) : @short;
            HasParameter = hasParameter;
            Description = description;
        }

        public CommandLineArgsAttribute()
        {
            IsMissingList = true;
        }

        public string Long { get; } = "";
        public string Short { get; } = "";
        public bool HasParameter { get; set; } = true;
        public bool IsRequired { get; set; }
        public bool IsMissingList { get; set; } = false;
        public string Default { get; set; } = "";

        public string Description { get; set; } = "";
    }

    /// <summary>
    /// コマンドラインの引数をクラスのプロパティにセットする
    /// </summary>
    class CommandLineParser
    {
        public static void WriteHelp<T>(T _, System.IO.TextWriter w) where T : new()
        {
            WriteHelp<T>(w);
        }

        public static void WriteHelp<T>(System.IO.TextWriter writer) where T : new()
        {
            var def = new T();

            string offset1 = "  ";
            string offset2 = "    ";

            var pps = PropertyInfoPair.Get<T>().ToArray();
            if (pps.Length > 0)
            {
                writer.WriteLine("コマンドライン引数");

                foreach (var pp in pps)
                {
                    //System.Text.StringBuilder sb = new System.Text.StringBuilder();
                    if (pp.Setting.IsMissingList)
                    {
                        WriteLines(1, "[arguments]");
                    }
                    else
                    {
                        writer.Write(offset1 + string.Join(" ", new[] { pp.Setting.Long, pp.Setting.Short }.Distinct().Where(_ => !string.IsNullOrWhiteSpace(_)).Select(_ => "/" + _)));
                        if (pp.Setting.HasParameter)
                        {
                            writer.Write(" @value");
                        }
                        writer.WriteLine();
                    }



                    if (pp.Setting.HasParameter)
                    {
                        WriteLines(2, "@value");
                        var pt = pp.pi.PropertyType;
                        if (pt.IsEnum)
                        {
                            bool isFlag = pt.GetCustomAttributes(typeof(FlagsAttribute), true).Any();
                            if (isFlag)
                            {
                                WriteLines(3, "Flag = " + string.Join(",", Enum.GetNames(pt)));
                            }
                            else
                            {
                                WriteLines(3, "Switch= " + string.Join("|", Enum.GetNames(pt)));
                            }
                        }

                        if (!pp.Setting.IsMissingList && pp.pi.PropertyType.IsValueType)
                        {
                            var defvalue = pp.pi.GetValue(def)?.ToString();
                            if (!string.IsNullOrEmpty(defvalue))
                            {
                                WriteLines(3, "Default=" + defvalue);
                            }
                        }
                    }

                    if (!string.IsNullOrWhiteSpace(pp.Setting.Description))
                    {
                        writer.WriteLine();
                        WriteLines(2, pp.Setting.Description);
                    }

                    if (pp.pi.PropertyType.IsEnum)
                    {
                        var enumtype = pp.pi.PropertyType;
                        var enumfiels = enumtype.GetFields().Select(_ => new { PI = _, Attr = _.GetCustomAttribute<EnumParameterDescriptionAttribute>() }).Where(_ => _.Attr != null).ToArray();
                        if (enumfiels.Length > 0)
                        {
                            writer.WriteLine();
                            WriteLines(2, "各スイッチの説明");
                            foreach (var temp in enumfiels)
                            {
                                writer.WriteLine();
                                WriteLines(3, temp.PI.Name);
                                WriteLines(4, temp.Attr!.Text);
                            }
                        }
                    }

                    writer.WriteLine();
                }
            }

            void WriteLines(int lv, string text)
            {
                string offset = new string(' ', lv * 2);
                using var sr = new System.IO.StringReader(text);
                while (sr.Peek() != -1)
                {
                    writer.WriteLine(offset + sr.ReadLine());
                }
            }
        }


        public static void Parse<T>(T target, string[] args) where T : class
        {
            IList<PropertyInfoPair> pps = PropertyInfoPair.Get<T>().ToList();
            HashSet<PropertyInfoPair> requires = pps.Where(_ => _.Setting.IsRequired).ToHashSet();

            var missings = pps.Where(_ => _.Setting.IsMissingList).ToArray();

            if (missings.Count() > 1)
            {
                throw new ApplicationException("Invalid Settings");
            }

            bool hasMissing = missings.Length == 1;

            var isMissing = pps.Where(_ => _.Setting.IsMissingList).LastOrDefault();
            IList<string> list = isMissing?.pi.GetValue(target) as IList<string> ?? new List<string>();

            var dicL = pps.Where(_ => !_.Setting.IsMissingList && !string.IsNullOrWhiteSpace(_.Setting.Long)).ToDictionary(_ => _.Setting.Long.ToLower());
            var dicS = pps.Where(_ => !_.Setting.IsMissingList && !string.IsNullOrWhiteSpace(_.Setting.Short)).ToDictionary(_ => _.Setting.Short.ToLower());

            (string key, Dictionary<string, PropertyInfoPair>[] dics)[] tagStarts =
                [
                 ("--", [dicL]),
                 ("-", [dicS]),
                 ("/", [dicL,dicS])
                 ];

            for (int i = 0; i < args.Length; i++)
            {
                var part = args[i];
                int splitIndex = part.IndexOf(":");

                string tag = (splitIndex <= 0)
                    ? part
                    : part.Substring(0, splitIndex);
                tag = tag.ToLower();

                PropertyInfoPair? piPair = null;

                var ts = tagStarts.FirstOrDefault(_ => tag.StartsWith(_.key));
                if (!string.IsNullOrWhiteSpace(ts.key) && ts.dics.Length > 0)
                {
                    var tagkey = tag.Substring(ts.key.Length);
                    if (!string.IsNullOrWhiteSpace(tagkey))
                    {
                        foreach (var dic in ts.dics)
                        {
                            if (dic.TryGetValue(tagkey, out piPair) && piPair != null)
                            {
                                break;
                            }
                        }

                        if (piPair == null && hasMissing && (missings[0].Setting.Long.ToLower() == tagkey || missings[0].Setting.Short.ToLower() == tagkey))
                        {
                            piPair = missings[0];
                            if (i < args.Length - 1)
                            {
                                i++;
                                list.Add(args[i]);
                                requires.Remove(missings[0]);
                                continue;
                            }
                            else
                            {
                                throw new ApplicationException($"CommandLine Parameter Missing : {piPair.Setting.Long},{piPair.Setting.Short}");
                            }
                        }
                    }
                }
                else
                {
                    list.Add(part);
                    if (hasMissing)
                    {
                        requires.Remove(missings[0]);
                    }
                    continue;
                }

                //if (tagStart == "--")
                //{
                //    dicL.TryGetValue(tagKey, out piPair);
                //}
                //else if (tag.StartsWith("-"))
                //{
                //    dicS.TryGetValue(tagKey, out piPair);
                //}
                //else if (tag.StartsWith("/"))
                //{
                //    dicL.TryGetValue(tagKey, out piPair);
                //    if (piPair == null)
                //    {
                //        dicS.TryGetValue(tagKey, out piPair);
                //    }
                //}
                //else
                //{
                //    list.Add(part);
                //    if (missings.Count() == 1)
                //    {
                //        requires.Remove(missings[0]);
                //    }
                //    continue;
                //}

                if (piPair == null)
                {
                    throw new ApplicationException("Invalid Command Parameter " + part);
                }

                if (piPair.Setting.HasParameter)
                {
                    string param = "";
                    if (splitIndex > 0)
                    {
                        param = part.Substring(splitIndex + 1);
                    }
                    else if (i < args.Length - 1)
                    {
                        i++;
                        param = args[i];
                    }
                    else
                    {
                        throw new ApplicationException($"CommandLine Parameter Missing : {piPair.Setting.Long},{piPair.Setting.Short}");
                    }

                    Setter(piPair.pi, target, param);
                }
                else
                {
                    if (piPair.pi.PropertyType == typeof(bool))
                    {
                        bool flag = true;
                        if (splitIndex > 0 && !Boolean.TryParse(part.Substring(splitIndex + 1), out flag))
                        {
                            throw new ApplicationException($"CommandLine Parameter Invalid flag : {piPair.Setting.Long},{piPair.Setting.Short}");
                        }

                        piPair.pi.SetValue(target, flag);

                    }
                    else
                    {
                        piPair.pi.SetValue(target, piPair.Setting.Default);
                    }
                }

                if (piPair.Setting.IsRequired)
                {
                    requires.Remove(piPair);
                }
            }


            if (requires.Count > 0)
            {
                throw new ApplicationException("Parameter Missing: " + string.Join(",", requires.Select(_ => _.Setting.Long)));
            }
        }

        private static void Setter(PropertyInfo pi, object o, string s)
        {
            var t = Nullable.GetUnderlyingType(pi.PropertyType);
            if (t == null)
            {
                t = pi.PropertyType;
            }
            try
            {
                if (t == typeof(string)) { pi.SetValue(o, s); }
                else if (t == typeof(bool)) { pi.SetValue(o, bool.Parse(s)); }
                else if (t == typeof(sbyte)) { pi.SetValue(o, byte.Parse(s)); }
                else if (t == typeof(sbyte)) { pi.SetValue(o, sbyte.Parse(s)); }
                else if (t == typeof(short)) { pi.SetValue(o, short.Parse(s)); }
                else if (t == typeof(ushort)) { pi.SetValue(o, ushort.Parse(s)); }
                else if (t == typeof(int)) { pi.SetValue(o, int.Parse(s)); }
                else if (t == typeof(uint)) { pi.SetValue(o, uint.Parse(s)); }
                else if (t == typeof(long)) { pi.SetValue(o, long.Parse(s)); }
                else if (t == typeof(ulong)) { pi.SetValue(o, ulong.Parse(s)); }
                else if (t == typeof(float)) { pi.SetValue(o, float.Parse(s)); }
                else if (t == typeof(double)) { pi.SetValue(o, double.Parse(s)); }
                else if (t == typeof(decimal)) { pi.SetValue(o, decimal.Parse(s)); }
                else if (t.IsEnum)
                {
                    var names = Enum.GetNames(t);
                    bool ignoreCase = names.Select(_ => _.ToLower()).Distinct().Count() == names.Count();

                    object? e = null;
                    try
                    {
                        e = Enum.Parse(t, s, ignoreCase);
                    }
                    catch
                    {
                        throw new ApplicationException($"Parameter value is not supported : {s}");
                    }
                    pi.SetValue(o, e);
                }
                else
                {
                    throw new ApplicationException($"Parameter Type is not supported : {pi.Name}");
                }
            }
            catch
            {
                throw new ApplicationException($"Invalid Parameter Value: {pi.Name}");
            }
        }

        class PropertyInfoPair
        {
            public CommandLineArgsAttribute Setting;
            public PropertyInfo pi;

            public PropertyInfoPair(CommandLineArgsAttribute a, PropertyInfo pi)
            {
                Setting = a;
                this.pi = pi;
            }

            public static IEnumerable<PropertyInfoPair> Get<T>()
            {
                return Get(typeof(T));
            }
            public static IEnumerable<PropertyInfoPair> Get(Type t)
            {
                foreach (var pi in t.GetProperties())
                {
                    foreach (var a in pi.GetCustomAttributes(true).OfType<CommandLineArgsAttribute>())
                    {
                        yield return new PropertyInfoPair(a, pi);
                    }
                }
            }

            public override string ToString()
            {
                return $"{pi.Name},{Setting.Long},{Setting.Short},{Setting.IsRequired}";
            }
        }
    }


    [System.AttributeUsage(System.AttributeTargets.Field)]
    class EnumParameterDescriptionAttribute : System.Attribute
    {
        public EnumParameterDescriptionAttribute(string text)
        {
            this.Text = text;
        }
        public string Text { get; }
    }
}
