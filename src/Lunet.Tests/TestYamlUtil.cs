using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Lunet.Helpers;
using Xunit;
using Scriban.Runtime;

namespace Lunet.Tests
{
    public class TestYamlUtil
    {
        [Fact]
        public void TestYamlFrontMatter()
        {
            int index;
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
            var result = YamlUtil.FromYamlFrontMatter(input, out index);

            var scriptObject = Assert.IsType<ScriptObject>(result);
            Assert.Equal(4, scriptObject.Keys.Count);
            var keys = scriptObject.Keys.ToList();
            keys.Sort();
            Assert.Equal(new List<string>()
            {
                "boooo",
                "floatvalue",
                "intvalue",
                "name"                
            }, keys);

            Assert.Equal(true, scriptObject["name"]);
            Assert.Equal(12351235, scriptObject["intvalue"]);
            Assert.Equal(12.3, scriptObject["floatvalue"]);
            Assert.Equal(new ScriptArray()
            {
                "a",
                "b",
                "c"
            }, scriptObject["boooo"]);

            Assert.True(index > 0);
            var remaining = input.Substring(index).Trim();
            Assert.Equal("Yo", remaining);
        }
    }
}
