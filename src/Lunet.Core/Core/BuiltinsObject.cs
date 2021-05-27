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
            Head = parent.Scripts.CompileAnonymous("include '_builtins/head.sbn-html'");

            // Add our own to_rfc822
            var dateTimeFunctions = (DateTimeFunctions) Site.Scripts.ScribanBuiltins["date"];
            dateTimeFunctions.Import("to_rfc822", (Func<DateTime, string>)ToRFC822);

            // Add log object
            LogObject = new DynamicObject<BuiltinsObject>(this);
            SetValue("log", LogObject, true);
            LogObject.SetValue("info", DelegateCustomFunction.Create((Action<string>)LogInfo), true);
            LogObject.SetValue("error", DelegateCustomFunction.Create((Action<string>)LogError), true);
            LogObject.SetValue("warn", DelegateCustomFunction.Create((Action<string>)LogWarning), true);
            LogObject.SetValue("debug", DelegateCustomFunction.Create((Action<string>)LogDebug), true);
            LogObject.SetValue("trace", DelegateCustomFunction.Create((Action<string>)LogTrace), true);
            LogObject.SetValue("fatal", DelegateCustomFunction.Create((Action<string>)LogFatal), true);

            // Setup global Lunet object
            LunetObject = new LunetObject(Site);
            SetValue("lunet", LunetObject, true);
        }

        public DynamicObject LogObject { get; }

        public LunetObject LunetObject { get; }

        public void Initialize()
        {
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

            Site.Scripts.TryImportScript(helpers, "internal_helpers", this, ScriptFlags.AllowSiteFunctions, out _);
        }

        private void LogInfo(string message) => Site.Info(message);
        private void LogError(string message) => Site.Error(message);
        private void LogFatal(string message) => Site.Fatal(message);
        private void LogWarning(string message) => Site.Warning(message);
        private void LogTrace(string message) => Site.Trace(message);
        private void LogDebug(string message) => Site.Debug(message);



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