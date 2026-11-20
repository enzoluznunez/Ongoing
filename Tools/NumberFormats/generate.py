"""Writes Assets/AccountingImmersiveAnalytics/Text/NumberFormats.cs from Unicode CLDR (through Babel).

Every language in Text/Language.cs gets its decimal and group signs, minus sign, digit grouping, native digits,
percent pattern and short number words ("12.3K", "1,23 Mio.", "1.2万"), so numbers never depend on the culture data
of the device the app runs on.

Run:  python3 -m venv .venv && .venv/bin/pip install babel && .venv/bin/python generate.py
"""
import os
import re
from babel import Locale

HERE = os.path.dirname(os.path.abspath(__file__))
PROJECT = os.path.abspath(os.path.join(HERE, "..", ".."))
LANGUAGE = os.path.join(PROJECT, "Assets/AccountingImmersiveAnalytics/Text/Language.cs")
OUT = os.path.join(PROJECT, "Assets/AccountingImmersiveAnalytics/Text/NumberFormats.cs")

# App code -> CLDR locale. Norwegian is written Bokmål; Kurdish is CLDR's "ku", Kurmanji in Latin letters.
CLDR = {"zh-Hans": "zh_Hans", "zh-Hant": "zh_Hant", "pt-BR": "pt", "pt-PT": "pt_PT", "no": "nb"}

DIGITS = {
    "latn": None,
    "arab": "٠١٢٣٤٥٦٧٨٩",
    "arabext": "۰۱۲۳۴۵۶۷۸۹",
    "beng": "০১২৩৪৫৬৭৮৯",
    "deva": "०१२३४५६७८९",
    "mymr": "၀၁၂၃၄၅၆၇၈၉",
}


def cs(text):
    out = []
    for ch in text:
        if ch in '"\\':
            out.append("\\" + ch)
        elif 0x20 <= ord(ch) < 0x7F:
            out.append(ch)
        else:
            out.append("\\u%04x" % ord(ch)) if ord(ch) <= 0xFFFF else out.append("\\U%08x" % ord(ch))
    return '"' + "".join(out) + '"'


def symbol(locale, system, key, default):
    # A numbering system with no symbols of its own (Assamese's Bengali digits, for one) uses the Latin ones.
    for name in (system, "latn"):
        try:
            return locale.number_symbols[name][key]
        except KeyError:
            continue
    return default


def unquote(text):
    # CLDR quotes literal characters in patterns ('.' for a period); '' is a quote mark.
    return text.replace("''", "\0").replace("'", "").replace("\0", "'")


def units(locale):
    short = locale.compact_decimal_formats.get("short", {})
    other = short.get("other", {})
    one = short.get("one", {})
    found = []
    for key in sorted(other, key=lambda k: int(k)):
        threshold = int(key)
        if threshold >= 10 ** 15:
            continue
        pattern = other[key]
        zeros = pattern.int_prec[0]
        if pattern.pattern.strip("0") == "" or zeros == 0:
            found.append((threshold, 1, "", "", None, None))
            continue
        divisor = threshold // 10 ** (zeros - 1)
        prefix, suffix = unquote(pattern.prefix[0]), unquote(pattern.suffix[0])
        one_prefix = one_suffix = None
        if key in one and (unquote(one[key].prefix[0]), unquote(one[key].suffix[0])) != (prefix, suffix):
            one_prefix, one_suffix = unquote(one[key].prefix[0]), unquote(one[key].suffix[0])
        found.append((threshold, divisor, prefix, suffix, one_prefix, one_suffix))
    merged = []
    for unit in found:
        if merged and merged[-1][1:] == unit[1:]:
            continue
        merged.append(unit)
    return merged


def main():
    source = open(LANGUAGE, encoding="utf-8").read()
    block = source[source.index("Supported"):source.index("};")]
    languages = re.findall(r'\["([\w-]+)"\] = "([^"]+)"', block)
    lines = []
    for code, name in languages:
        locale = Locale.parse(CLDR.get(code, code.replace("-", "_")))
        system = locale._data.get("default_numbering_system", "latn")
        if system not in DIGITS:
            raise SystemExit(f"{code}: numbering system {system} has no digits in DIGITS")
        decimal_pattern = locale.decimal_formats[None]
        primary, secondary = decimal_pattern.grouping
        percent = locale.percent_formats[None]
        sign = symbol(locale, system, "percentSign", "%")
        before = unquote(percent.prefix[0]).replace("%", sign)
        after = unquote(percent.suffix[0]).replace("%", sign)
        digits = DIGITS[system]
        rows = []
        for threshold, divisor, prefix, suffix, one_prefix, one_suffix in units(locale):
            one = "" if one_prefix is None else f", {cs(one_prefix)}, {cs(one_suffix)}"
            rows.append(f"new NumberUnit({threshold:.0e}, {divisor:.0e}, {cs(prefix)}, {cs(suffix)}{one})".replace("e+0", "e").replace("e+", "e"))
        lines.append(
            f'            ["{code}"] = new NumberStyle({cs(symbol(locale, system, "decimal", "."))}, {cs(symbol(locale, system, "group", ","))}, {cs(symbol(locale, system, "minusSign", "-"))}, '
            f'{primary}, {secondary}, {cs(digits) if digits else "null"}, {cs(before)}, {cs(after)}, new[]\n'
            f'            {{\n                ' + ",\n                ".join(rows) + "\n            }),")
    text = (
        "using System.Collections.Generic;\n"
        "using AccountingImmersiveAnalytics.UI.Charts.Scales;\n\n"
        "namespace AccountingImmersiveAnalytics.Text\n{\n"
        "    public static class NumberFormats\n    {\n"
        "        public static readonly IReadOnlyDictionary<string, NumberStyle> ByLanguage = new Dictionary<string, NumberStyle>\n"
        "        {\n" + "\n".join(lines) + "\n        };\n    }\n}\n")
    open(OUT, "w", encoding="utf-8").write(text)
    print(f"Wrote {len(languages)} languages to {OUT}")


main()
