
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

            var isMissing = pps.Where(_ => _.Setting.IsMissingList).LastOrDefault();
            IList<string> list = isMissing?.pi.GetValue(target) as IList<string> ?? new List<string>();

            var dicL = pps.Where(_ => !_.Setting.IsMissingList && !string.IsNullOrWhiteSpace(_.Setting.Long)).ToDictionary(_ => _.Setting.Long.ToLower());
            var dicS = pps.Where(_ => !_.Setting.IsMissingList && !string.IsNullOrWhiteSpace(_.Setting.Short)).ToDictionary(_ => _.Setting.Short.ToLower());

            for (int i = 0; i < args.Length; i++)
            {
                var part = args[i];
                int splitIndex = part.IndexOf(":");

                string tag = part.ToLower();

                if (splitIndex > 0)
                {
                    tag = part.Substring(0, splitIndex);
                }

                PropertyInfoPair? temp = null;
                if (tag.StartsWith("--"))
                {
                    dicL.TryGetValue(tag.Substring(2), out temp);
                }
                else if (tag.StartsWith("-"))
                {
                    dicS.TryGetValue(tag.Substring(1), out temp);
                }
                else if (tag.StartsWith("/"))
                {
                    dicL.TryGetValue(tag.Substring(1), out temp);
                    if (temp == null)
                    {
                        dicS.TryGetValue(tag.Substring(1), out temp);
                    }
                }
                else
                {
                    list.Add(part);
                    continue;
                }

                if (temp == null)
                {
                    throw new ApplicationException("Invalid Command Parameter " + part);
                }

                if (temp.Setting.HasParameter)
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
                        throw new ApplicationException($"CommandLine Parameter Missing : {temp.Setting.Long},{temp.Setting.Short}");
                    }

                    Setter(temp.pi, target, param);
                }
                else
                {
                    if (temp.pi.PropertyType == typeof(bool))
                    {
                        temp.pi.SetValue(target, true);
                    }
                    else
                    {
                        temp.pi.SetValue(target, temp.Setting.Default);
                    }
                }
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

                    if (Enum.TryParse(t, s, ignoreCase, out object? e))
                    {
                        pi.SetValue(o, e);
                    }
                    else
                    {
                        throw new ApplicationException($"Parameter value is not supported : {s}");
                    }

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
