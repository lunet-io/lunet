using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Text;
using Lunet.Scripts;
using Scriban;
using Scriban.Functions;
using Scriban.Runtime;
using Zio;

namespace Lunet.Core
{
    public class BuiltinsObject : DynamicObject<SiteObject>
    {
        public string Name => SiteVariables.Builtins;

        public SiteObject Site { get; }

        public BuiltinsObject(SiteObject parent) : base(parent)
        {
            Site = parent;
            parent.SetValue(SiteVariables.Builtins, this, true);
            Head = parent.Scripts.CompileAnonymous("include 'builtins/head.sbn-html'");

            // Helpers used for declaring panels (e.g {{NOTE do}}This is a note.{{end}}
            var helpers = @"
# Defines the generic alert helper function
func ALERT
    `<div class='` + (($0 + ` ` + $.class) | string.rstrip ) + `' role='alert'>`
        `<div class='` + $0 + `-heading'>`
            `<span class='` + $0 + `-icon'></span><span class='` + $0 + `-heading-text'></span>`
        `</div>`
        `<div class='` + $0 + `-content'>` + '\n\n'
            $1
        `</div>`
    '</div>\n\n'
end

# Defines alert functions
func NOTE; ALERT 'lunet-alert-note' class:$.class @$0; end
func TIP; ALERT 'lunet-alert-tip' class:$.class @$0; end
func WARNING; ALERT 'lunet-alert-warning' class:$.class @$0; end
func IMPORTANT; ALERT 'lunet-alert-important' class:$.class @$0; end
func CAUTION; ALERT 'lunet-alert-caution' class:$.class @$0; end
func CALLOUT; ALERT 'lunet-alert-callout' class:$.class @$0; end
";

            parent.Scripts.TryImportScript(helpers, "internal_helpers", this, ScriptFlags.AllowSiteFunctions, out _);

            // Add our own to_rfc822
            var dateTimeFunctions = (DateTimeFunctions) Site.Scripts.Builtins["date"];
            dateTimeFunctions.Import("to_rfc822", (Func<DateTime, string>)ToRFC822);
        }

        public object Head
        {
            get => GetSafeValue<object>("Head");
            set => SetValue("Head", value);
        }

        private static string ToRFC822(DateTime date)
        {
            int offset = TimeZone.CurrentTimeZone.GetUtcOffset(DateTime.Now).Hours;
            string timeZone = "+" + offset.ToString(CultureInfo.InvariantCulture).PadLeft(2, '0');
            if (offset < 0)
            {
                int i = offset * -1;
                timeZone = "-" + i.ToString(CultureInfo.InvariantCulture).PadLeft(2, '0');
            }
            return date.ToString("ddd, dd MMM yyyy HH:mm:ss " + timeZone.PadRight(5, '0'));
        }
    }
}