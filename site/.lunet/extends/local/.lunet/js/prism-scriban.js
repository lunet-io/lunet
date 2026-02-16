(function (Prism) {
    if (!Prism || !Prism.languages) {
        return;
    }

    Prism.languages.scriban = {
        "comment": [
            {
                pattern: /(^|[^\\])#.*/,
                lookbehind: true,
                greedy: true
            }
        ],
        "template-delimiter": {
            pattern: /\{\{[-~]?|[-~]?\}\}/,
            alias: "punctuation"
        },
        "string": [
            {
                pattern: /@?"(?:\\.|[^"\\\r\n])*"/,
                greedy: true
            },
            {
                pattern: /'(?:\\.|[^'\\\r\n])*'/,
                greedy: true
            }
        ],
        "interpolation": {
            pattern: /(^|[^\\])\$\{(?:[^{}]|{[^{}]*})*\}/,
            lookbehind: true,
            inside: {
                "interpolation-punctuation": {
                    pattern: /^\$\{|\}$/,
                    alias: "punctuation"
                }
            }
        },
        "keyword": /\b(?:as|break|case|capture|continue|do|else|end|for|func|if|import|in|readonly|ret|return|tablerow|when|while|with)\b/,
        "boolean": /\b(?:false|null|true)\b/,
        "builtin": /\b(?:empty)\b/,
        "number": /\b(?:0x[\da-f]+|(?:\d+\.?\d*|\.\d+)(?:e[+-]?\d+)?)\b/i,
        "function": /\b[a-z_]\w*(?=\s*(?:\())/i,
        "variable": /\$[a-z_]\w*/i,
        "operator": /\?\?|\.\.|=>|==|!=|<=|>=|&&|\|\||[+\-*/%!?=<>|&^~]/,
        "punctuation": /[()[\]{}.,:;]/
    };

    Prism.languages.sbn = Prism.languages.scriban;
    Prism.languages["scriban-html"] = Prism.languages.scriban;
    Prism.languages["scriban-md"] = Prism.languages.scriban;
}(Prism));
