using System;
using System.Collections.Generic;
using System.Linq;
using Lunet.Yaml;
using NUnit.Framework;
using Scriban.Parsing;
using Scriban.Runtime;

namespace Lunet.Tests
{
    public class TestYamlUtil
    {
        [Test]
        public void TestYamlFrontMatter()
        {
            TextPosition position;
            var input = @"---
name: yes
intvalue: 12351235
floatvalue: 12.3
boooo: 
  - a
  - b
  - c
---
Yo
";
            var result = YamlUtil.FromYamlFrontMatter(input, out position);

            Assert.AreEqual(new TextPosition(87, 8, 3), position);

            Assert.IsInstanceOf<ScriptObject>(result);
            var scriptObject = (ScriptObject)result;
            Assert.AreEqual(4, scriptObject.Keys.Count);
            var keys = scriptObject.Keys.ToList();
            keys.Sort();
            Assert.AreEqual(new List<string>()
            {
                "boooo",
                "floatvalue",
                "intvalue",
                "name"                
            }, keys);

            Assert.AreEqual(true, scriptObject["name"]);
            Assert.AreEqual(12351235, scriptObject["intvalue"]);
            Assert.AreEqual(12.3, scriptObject["floatvalue"]);
            Assert.AreEqual(new ScriptArray()
            {
                "a",
                "b",
                "c"
            }, scriptObject["boooo"]);

            var remaining = input.Substring(position.Offset).Trim();
            Assert.AreEqual("Yo", remaining);
        }
    }
}
