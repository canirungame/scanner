using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace CanIRunGame
{
    static class Json
    {
        public static string Write(object value, bool pretty)
        {
            StringBuilder sb = new StringBuilder();
            WriteValue(sb, value, pretty, 0);
            return sb.ToString();
        }

        static void Indent(StringBuilder sb, bool pretty, int level)
        {
            if (!pretty) return;
            sb.Append("\r\n");
            sb.Append(' ', level * 2);
        }

        static void WriteValue(StringBuilder sb, object value, bool pretty, int level)
        {
            if (value == null)
            {
                sb.Append("null");
            }
            else if (value is string)
            {
                WriteString(sb, (string)value);
            }
            else if (value is bool)
            {
                sb.Append((bool)value ? "true" : "false");
            }
            else if (value is double)
            {
                sb.Append(((double)value).ToString("0.###", CultureInfo.InvariantCulture));
            }
            else if (value is int || value is long)
            {
                sb.Append(Convert.ToString(value, CultureInfo.InvariantCulture));
            }
            else if (value is IDictionary<string, object>)
            {
                IDictionary<string, object> dict = (IDictionary<string, object>)value;
                sb.Append('{');
                bool first = true;
                foreach (KeyValuePair<string, object> pair in dict)
                {
                    if (!first) sb.Append(',');
                    first = false;
                    Indent(sb, pretty, level + 1);
                    WriteString(sb, pair.Key);
                    sb.Append(pretty ? ": " : ":");
                    WriteValue(sb, pair.Value, pretty, level + 1);
                }
                if (!first) Indent(sb, pretty, level);
                sb.Append('}');
            }
            else if (value is IEnumerable)
            {
                sb.Append('[');
                bool first = true;
                foreach (object item in (IEnumerable)value)
                {
                    if (!first) sb.Append(',');
                    first = false;
                    Indent(sb, pretty, level + 1);
                    WriteValue(sb, item, pretty, level + 1);
                }
                if (!first) Indent(sb, pretty, level);
                sb.Append(']');
            }
            else
            {
                WriteString(sb, Convert.ToString(value, CultureInfo.InvariantCulture));
            }
        }

        static void WriteString(StringBuilder sb, string s)
        {
            sb.Append('"');
            foreach (char c in s)
            {
                switch (c)
                {
                    case '"': sb.Append("\\\""); break;
                    case '\\': sb.Append("\\\\"); break;
                    case '\n': sb.Append("\\n"); break;
                    case '\r': sb.Append("\\r"); break;
                    case '\t': sb.Append("\\t"); break;
                    default:
                        if (c < 32 || c > 126) sb.Append("\\u").Append(((int)c).ToString("x4"));
                        else sb.Append(c);
                        break;
                }
            }
            sb.Append('"');
        }
    }
}
