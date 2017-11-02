using Codegen.DataModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace codegen.JsonGenerator
{
    [Codegen.Generator]
    public class Generator
    {
        public static Regex regex = new Regex(@"^[a-zA-Z0-9_\.]+$");
        public static bool NeedsQuotes(string value)
        {
            return !regex.IsMatch(value);
        }
        public static string GetAttributes(Dictionary<string, string> atts, bool appendSpace = false)
        {
            var att_string = new StringBuilder();
            var prefix = "";
            foreach (var att in atts)
            {
                if (string.IsNullOrWhiteSpace(att.Value))
                {
                    att_string.Append($"{prefix} {att.Key}");
                }
                else
                {
                    if (NeedsQuotes(att.Value))
                    {
                        att_string.Append($"{prefix} {att.Key} = \"{att.Value}\"");
                    }
                    else
                    {
                        att_string.Append($"{prefix} {att.Key} = {att.Value}");
                    }
                }
                prefix = ",";
            }
            var result = att_string.ToString().Trim();
            if (!string.IsNullOrWhiteSpace(result))
            {
                result = "[" + result + "]" + (appendSpace ? " " : "");
            }
            return result;
        }

        public static string Build(Global g)
        {
            var result = new StringBuilder();

            if (g.Objects.Count > 0)
            {
                foreach (var o in g.Objects)
                {
                    result.AppendLine($"object {o.Name} {GetAttributes(o.Attributes, true)}{{");

                    if (o.Properties.Count > 0)
                    {
                        var nameLength = o.Properties.Max(p => p.Name.Length + (p.Optional ? 1 : 0));
                        var typeLength = o.Properties.Max(p => p.Type.Name.Length + (p.Type.IsList ? 5 : 0));
                        var defaultLength = o.Properties.Max(p => p.Default.Trim().Length);
                        var hasDefault = o.Properties.Any(p => !string.IsNullOrWhiteSpace(p.Default));

                        foreach (var p in o.Properties)
                        {
                            var propString = new StringBuilder();
                            if (p.Optional)
                            {
                                propString.Append($"?{p.Name}".PadRight(nameLength));
                            }
                            else
                            {
                                propString.Append($"{p.Name}".PadRight(nameLength));
                            }
                            propString.Append(" : ");
                            if (p.Type.IsList)
                            {
                                propString.Append($"List {p.Type.Name}".PadRight(typeLength));
                            }
                            else
                            {
                                propString.Append($"{p.Type.Name}".PadRight(typeLength));
                            }

                            if (!string.IsNullOrWhiteSpace(p.Default.Trim()))
                            {
                                propString.Append(" = ");
                                propString.Append(p.Default.Trim().PadRight(defaultLength));
                            }
                            else
                            {
                                propString.Append("".PadRight(defaultLength + (hasDefault ? 3 : 0)));
                            }
                            propString.Append($" {GetAttributes(p.Attributes)}");
                            result.AppendLine(propString.ToString().Trim(), 1);
                        }


                        result.AppendLine("}");
                        result.AppendLine();
                    }
                }
            }

            if (g.ExternalTypes.Count > 0)
            {
                var externalNameLength = g.ExternalTypes.Max(t => t.Name.Length);

                foreach (var t in g.ExternalTypes)
                {
                    var extString = new StringBuilder();
                    extString.Append("external ");
                    extString.Append(t.Name.PadRight(externalNameLength));
                    extString.Append($" {GetAttributes(t.Attributes)}");
                    result.AppendLine(extString.ToString().Trim());
                }
            }

            return result.ToString();
        }
        public static GenerationResult Execute(Global g, List<string> args)
        {
            return new GenerationResult()
            {
                Content = Build(g),
                FileName = $"{g.Namespace}.cdl"
            };
        }
    }
    public static class StringBuilderExtensions
    {
        public const int INDENTATION = 2;
        public const char INDENTATION_CHAR = ' ';

        public static StringBuilder AppendLine(this StringBuilder sb, string value, int level)
        {
            return sb.AppendLine(value.PadLeft(INDENTATION * level + value.Length, INDENTATION_CHAR));
        }
        public static StringBuilder Append(this StringBuilder sb, string value, int level)
        {
            return sb.Append(value.PadLeft(INDENTATION * level + value.Length, INDENTATION_CHAR));
        }
        public static StringBuilder AppendFormat(this StringBuilder sb, string format, int level, params object[] args)
        {
            var str = string.Format(format, args);
            return sb.Append(str.PadLeft(INDENTATION * level + str.Length, INDENTATION_CHAR));
        }
    }
}
