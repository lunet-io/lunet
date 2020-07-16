using System;
using System.IO;
using Lunet.Core;
using Scriban.Parsing;
using Scriban.Runtime;
using SharpYaml;
using SharpYaml.Events;
using SharpYaml.Schemas;
using Parser = SharpYaml.Parser;

namespace Lunet.Yaml
{
    /// <summary>
    /// Helper functions to decode a YAML into a Scriban <see cref="ScriptObject"/> or <see cref="ScriptArray"/>.
    /// </summary>
    public static class YamlUtil
    {
        private static readonly ExtendedSchema DefaultSchema = new ExtendedSchema();

        /// <summary>
        /// Extracts a YAML object from the YAML frontmatter and return the index of a string after the `---` end of the YAML frontmatter.
        /// </summary>
        /// <param name="yamlText">The input YAML expected to be a frontmatter, starting with a `---`</param>
        /// <param name="position">the index of a string after the `---` end of the YAML frontmatter</param>
        /// <returns>The parsed YAML frontmatter to a Scriban <see cref="ScriptObject"/></returns>
        public static object FromYamlFrontMatter(string yamlText, out TextPosition position, string yamlFile = null)
        {
            return FromYaml(yamlText, yamlFile, true, out position);
        }

        /// <summary>
        /// Extracts a YAML object from the YAML text.
        /// </summary>
        /// <param name="yamlText">An input yaml text</param>
        /// <returns>The parsed YAML object to a Scriban <see cref="ScriptObject"/> or <see cref="ScriptArray"/></returns>
        public static object FromYaml(string yamlText, string yamlFile = null)
        {
            TextPosition position;
            return FromYaml(yamlText, yamlFile, false, out position);
        }

        private static object FromYaml(string yamlText, string yamlFile, bool expectOnlyFrontMatter, out TextPosition position)
        {
            try
            {
                position = new TextPosition();
                if (yamlText == null)
                {
                    return null;
                }

                var parser = new Parser(new StringReader(yamlText));
                var reader = new EventReader(parser);

                if (!reader.Accept<StreamStart>())
                {
                    return null;
                }

                reader.Expect<StreamStart>();
                var docStart = reader.Expect<DocumentStart>();
                var hasDocumentStart = true;

                object result = null;
                ScriptArray objects = null;

                // If we expect to read multiple documents, we will return an array of result
                if (expectOnlyFrontMatter && docStart.IsImplicit)
                {
                    return null;
                }

                Mark endPosition;

                while (true)
                {
                    if (reader.Accept<StreamEnd>())
                    {
                        var evt = reader.Expect<StreamEnd>();
                        endPosition = evt.End;
                        break;
                    }

                    if (hasDocumentStart && reader.Accept<DocumentEnd>())
                    {
                        reader.Expect<DocumentEnd>();

                        hasDocumentStart = false;

                        if (expectOnlyFrontMatter)
                        {
                            reader.Accept<DocumentStart>();
                            // Don't consume the token as the parser will try to parse
                            // the following characters and could hit non YAML syntax (in Markdown)
                            // and would throw a parser exception
                            var nextDocStart = reader.Peek<DocumentStart>();
                            endPosition = nextDocStart.End;
                            break;
                        }

                        continue;
                    }

                    if (reader.Accept<DocumentStart>())
                    {
                        reader.Expect<DocumentStart>();
                        hasDocumentStart = true;
                    }

                    var obj = ReadEvent(reader);

                    if (result == null)
                    {
                        result = obj;
                    }
                    else
                    {
                        if (objects == null)
                        {
                            objects = new ScriptArray {result};
                            result = objects;
                        }

                        objects.Add(obj);
                    }
                }

                position = new TextPosition(endPosition.Index, endPosition.Line, endPosition.Column);
                return result;
            }
            catch (Exception ex)
            {
                throw new LunetException($"Error while parsing {yamlFile}. {ex.Message}");
            }
        }

        private static object ReadEvent(EventReader reader)
        {
            // Read a plain scalar and decode it to a C# value
            if (reader.Accept<Scalar>())
            {
                var scalar = reader.Expect<Scalar>();
                
                // We try to parse scalar with an extended YamlSchema
                // If we find a int,double... -> convert it to the proper C# type
                string defaultTag;
                object value;
                return DefaultSchema.TryParse(scalar, true, out defaultTag, out value) ? value : scalar.Value;
            }

            // Read a YAML sequence to a ScriptArray
            if (reader.Accept<SequenceStart>())
            {
                var array = new ScriptArray();
                reader.Expect<SequenceStart>();
                while (!reader.Accept<SequenceEnd>())
                {
                    array.Add(ReadEvent(reader));
                }
                reader.Expect<SequenceEnd>();
                return array;
            }

            // Read a YAML mapping to a ScriptObject
            if (reader.Accept<MappingStart>())
            {
                var obj = new ScriptObject();
                reader.Expect<MappingStart>();
                while (!reader.Accept<MappingEnd>())
                {
                    var key = ReadEvent(reader).ToString();
                    var value = ReadEvent(reader);
                    try
                    {
                        obj.Add(key, value);
                    }
                    catch (ArgumentException err)
                    {
                        throw new YamlException(reader.Parser.Current.Start, reader.Parser.Current.End, "Duplicate key", err);
                    }
                }
                reader.Expect<MappingEnd>();
                return obj;
            }

            throw new YamlException(reader.Parser.Current.Start, reader.Parser.Current.End, $"Unsupported Yaml Event {reader.Parser.Current}");
        }
    }
}