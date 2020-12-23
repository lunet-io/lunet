using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using Microsoft.DocAsCode.DataContracts.ManagedReference;

namespace Lunet.Api.DotNet.Extractor
{
    public partial class JsonSerializer
    {
        private bool _isFirstKeyValue;
        private readonly TextWriter _writer;
        private static readonly Dictionary<Type, Action<JsonSerializer, object>> _serializers = new Dictionary<Type, Action<JsonSerializer, object>>();
        private int _level;
        private bool _prettyOutput;
        private bool _newLine;

        public JsonSerializer(TextWriter writer)
        {
            this._writer = writer ?? throw new ArgumentNullException(nameof(writer));
        }

        public bool PrettyOutput
        {
            get => _prettyOutput;
            set => _prettyOutput = value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Write(object value)
        {
            WriteJsonValue(value);
        }

        private void WriteIndent()
        {
            if (_prettyOutput && _newLine)
            {
                for (int i = 0; i < _level; i++)
                {
                    _writer.Write('\t');
                }

                _newLine = false;
            }
        }

        private void WriteOutput(char value)
        {
            WriteIndent();
            _writer.Write(value);
        }


        private void WriteOutput(string value)
        {
            WriteIndent();
            _writer.Write(value);
        }

        private void WriteLine()
        {
            if (_prettyOutput)
            {
                WriteOutput('\n');
                _newLine = true;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void StartObject()
        {
            WriteOutput('{');
            WriteLine();
            _level++;
            _isFirstKeyValue = true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void EndObject()
        {
            if (!_isFirstKeyValue)
            {
                WriteLine();
            }
            _level--;
            WriteOutput('}');
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void WriteJsonKeyValue( object key, object value)
        {
            if (value is null) return;
            WriteKey(key);
            if (_prettyOutput)
            {
                WriteOutput(' ');
            }
            WriteJsonValue(value);
            WriteLine();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void WriteJsonKeyValue(object key, string value)
        {
            if (value is null) return;
            WriteKey(key);
            WriteJsonString(value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void WriteJsonKeyValue(object key, bool value)
        {
            WriteKey(key);
            WriteJsonBoolean(value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void WriteJsonKeyValue(object key, int value)
        {
            WriteKey(key);
            WriteJsonInt32(value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void WriteJsonKeyValue(object key, IEnumerable value)
        {
            if (value is null) return;
            WriteKey(key);
            WriteJsonArrayValue(value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void WriteKey(object key)
        {
            if (!_isFirstKeyValue)
            {
                WriteOutput(',');
                if (_prettyOutput)
                {
                    WriteLine();
                }
            }
            _isFirstKeyValue = false;
            WriteJsonValue(key);
            WriteOutput(":");
        }

        private void StartArray()
        {
            WriteOutput('[');
            WriteLine();
            _level++;
        }

        private void EndArray()
        {
            _level--;
            WriteOutput(']');
        }

        private void WriteJsonArrayValue(IEnumerable values)
        {
            if (values == null)
            {
                WriteOutput("null");
                return;
            }

            StartArray();
            bool isFirst = true;
            try
            {
                foreach (var item in values)
                {
                    if (!isFirst)
                    {
                        WriteOutput(',');
                        WriteLine();
                    }

                    isFirst = false;
                    WriteJsonValue(item);
                }
            }
            catch (Exception ex)
            {
                var supportedInterfaces = values.GetType().GetInterfaces();
                
                throw new InvalidOperationException($"Error while trying to serialize {values.GetType()}. Reason: {ex.Message}. Supported interfaces: {string.Join("\n", supportedInterfaces.Select(x=> x.FullName))}", ex);
            }

            if (_prettyOutput && !isFirst)
            {
                WriteLine();
            }
            EndArray();
        }

        private void WriteJsonValue(object value)
        {
            if (value is null)
            {
                WriteOutput("null");
                return;
            }

            if (_serializers.TryGetValue(value.GetType(), out var valueSerializer))
            {
                valueSerializer(this, value);
                return;
            }

            if (value is bool b)
            {
                WriteJsonBoolean(b);
            }
            else if (value is int i32)
            {
                WriteJsonInt32(i32);
            }
            else if (value is string str)
            {
                WriteJsonString(str);
            }
            else if (value is IDictionary dictionary)
            {
                var it = dictionary.GetEnumerator();
                StartObject();
                while (it.MoveNext())
                {
                    WriteJsonKeyValue(it.Key, it.Value);
                }

                EndObject();
            }
            else if (!(value is string) && value is IEnumerable it)
            {
                WriteJsonArrayValue(it);
            }
            else
            {
                var type = value.GetType();
                if (value is Enum e)
                {
                    WriteJsonString(e.ToString());
                }
                else
                {
                    throw new InvalidOperationException($"The serializer for the type {value.GetType()} is out of date. You must re-run the serializer gen.");
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void WriteJsonInt32(int i32)
        {
            WriteOutput(i32.ToString(CultureInfo.InvariantCulture));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void WriteJsonBoolean(bool b)
        {
            WriteOutput(b ? "true" : "false");
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void WriteJsonString(string str)
        {
            var newStr = JsonStringEncode(str);
            WriteOutput(newStr);
        }

        private static bool CharRequiresJsonEncoding(char c) => c < ' ' || c == '"' || c == '\\';
        
        private static string JsonStringEncode(string value)
        {
            if (string.IsNullOrEmpty(value))
                return "\"\"";
            var builder = new StringBuilder(value.Length + 5);
            builder.Append('"');
            int startIndex = 0;
            int count = 0;
            for (int index = 0; index < value.Length; ++index)
            {
                char c = value[index];
                if (CharRequiresJsonEncoding(c))
                {
                    if (count > 0)
                        builder.Append(value, startIndex, count);
                    startIndex = index + 1;
                    count = 0;
                    switch (c)
                    {
                        case '\b':
                            builder.Append("\\b");
                            continue;
                        case '\t':
                            builder.Append("\\t");
                            continue;
                        case '\n':
                            builder.Append("\\n");
                            continue;
                        case '\f':
                            builder.Append("\\f");
                            continue;
                        case '\r':
                            builder.Append("\\r");
                            continue;
                        case '"':
                            builder.Append("\\\"");
                            continue;
                        case '\\':
                            builder.Append("\\\\");
                            continue;
                        default:
                            AppendCharAsUnicodeJavaScript(builder, c);
                            continue;
                    }
                }
                else
                    ++count;
            }
            if (count > 0)
                builder.Append(value, startIndex, count);
            builder.Append('"');
            return builder.ToString();
        }

        private static void AppendCharAsUnicodeJavaScript(StringBuilder builder, char c)
        {
            builder.Append("\\u");
            builder.Append(((int)c).ToString("x4", (IFormatProvider)CultureInfo.InvariantCulture));
        }
    }
}