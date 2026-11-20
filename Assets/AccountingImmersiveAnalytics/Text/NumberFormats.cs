using System.Collections.Generic;
using AccountingImmersiveAnalytics.UI.Charts.Scales;

namespace AccountingImmersiveAnalytics.Text
{
    public static class NumberFormats
    {
        public static readonly IReadOnlyDictionary<string, NumberStyle> ByLanguage = new Dictionary<string, NumberStyle>
        {
            ["af"] = new NumberStyle(",", "\u00a0", "-", 3, 3, null, "", "%", new[]
            {
                new NumberUnit(1e3, 1e3, "", "\u00a0k"),
                new NumberUnit(1e6, 1e6, "", "\u00a0m"),
                new NumberUnit(1e9, 1e9, "", "\u00a0mjd"),
                new NumberUnit(1e12, 1e12, "", "\u00a0bn")
            }),
            ["ak"] = new NumberStyle(".", ",", "-", 3, 3, null, "", "%", new[]
            {
                new NumberUnit(1e3, 1e3, "", "K"),
                new NumberUnit(1e6, 1e6, "", "M"),
                new NumberUnit(1e9, 1e9, "", "G"),
                new NumberUnit(1e12, 1e12, "", "T")
            }),
            ["sq"] = new NumberStyle(",", "\u00a0", "-", 3, 3, null, "", "%", new[]
            {
                new NumberUnit(1e3, 1e3, "", "\u00a0mij\u00eb"),
                new NumberUnit(1e6, 1e6, "", "\u00a0mln"),
                new NumberUnit(1e9, 1e9, "", "\u00a0mld"),
                new NumberUnit(1e12, 1e12, "", "\u00a0bln")
            }),
            ["am"] = new NumberStyle(".", ",", "-", 3, 3, null, "", "%", new[]
            {
                new NumberUnit(1e3, 1e3, "", "\u00a0\u123a"),
                new NumberUnit(1e6, 1e6, "", "\u00a0\u121a"),
                new NumberUnit(1e9, 1e9, "", "\u00a0\u1262"),
                new NumberUnit(1e12, 1e12, "", "\u00a0\u1275")
            }),
            ["ar"] = new NumberStyle(".", ",", "\u200e-", 3, 3, null, "", "\u200e%\u200e", new[]
            {
                new NumberUnit(1e3, 1e3, "", "\u00a0\u0623\u0644\u0641"),
                new NumberUnit(1e6, 1e6, "", "\u00a0\u0645\u0644\u064a\u0648\u0646"),
                new NumberUnit(1e9, 1e9, "", "\u00a0\u0645\u0644\u064a\u0627\u0631"),
                new NumberUnit(1e12, 1e12, "", "\u00a0\u062a\u0631\u0644\u064a\u0648\u0646")
            }),
            ["hy"] = new NumberStyle(",", "\u00a0", "-", 3, 3, null, "", "%", new[]
            {
                new NumberUnit(1e3, 1e3, "", "\u00a0\u0570\u0566\u0580"),
                new NumberUnit(1e6, 1e6, "", "\u00a0\u0574\u056c\u0576"),
                new NumberUnit(1e9, 1e9, "", "\u00a0\u0574\u056c\u0580\u0564"),
                new NumberUnit(1e12, 1e12, "", "\u00a0\u057f\u0580\u056c\u0576")
            }),
            ["as"] = new NumberStyle(".", ",", "-", 3, 2, "\u09e6\u09e7\u09e8\u09e9\u09ea\u09eb\u09ec\u09ed\u09ee\u09ef", "", "%", new[]
            {
                new NumberUnit(1e3, 1e3, "", "\u00a0\u09b9\u09be\u099c\u09be\u09f0"),
                new NumberUnit(1e5, 1e5, "", "\u00a0\u09b2\u09be\u0996"),
                new NumberUnit(1e6, 1e6, "", "\u00a0\u09a8\u09bf\u09af\u09c1\u09a4"),
                new NumberUnit(1e8, 1e6, "", "\u00a0\u09a8\u09bf\u0983"),
                new NumberUnit(1e9, 1e9, "", "\u00a0\u09b6\u0983\u00a0\u0995\u09cb\u0983"),
                new NumberUnit(1e11, 1e9, "", "\u00a0\u09b6\u0983\u00a0\u0995\u0983"),
                new NumberUnit(1e12, 1e12, "", "\u00a0\u09b6\u0983\u00a0\u09aa\u0983")
            }),
            ["az"] = new NumberStyle(",", ".", "-", 3, 3, null, "", "%", new[]
            {
                new NumberUnit(1e3, 1e3, "", "K"),
                new NumberUnit(1e6, 1e6, "", "\u00a0mln"),
                new NumberUnit(1e9, 1e9, "", "\u00a0mlrd"),
                new NumberUnit(1e12, 1e12, "", "\u00a0trln")
            }),
            ["eu"] = new NumberStyle(",", ".", "\u2212", 3, 3, null, "%\u00a0", "", new[]
            {
                new NumberUnit(1e3, 1e0, "", ""),
                new NumberUnit(1e6, 1e6, "", "\u00a0M"),
                new NumberUnit(1e12, 1e12, "", "\u00a0B")
            }),
            ["be"] = new NumberStyle(",", "\u00a0", "-", 3, 3, null, "", "\u00a0%", new[]
            {
                new NumberUnit(1e3, 1e3, "", "\u00a0\u0442\u044b\u0441."),
                new NumberUnit(1e6, 1e6, "", "\u00a0\u043c\u043b\u043d"),
                new NumberUnit(1e9, 1e9, "", "\u00a0\u043c\u043b\u0440\u0434"),
                new NumberUnit(1e12, 1e12, "", "\u00a0\u0442\u0440\u043b\u043d")
            }),
            ["bn"] = new NumberStyle(".", ",", "-", 3, 2, "\u09e6\u09e7\u09e8\u09e9\u09ea\u09eb\u09ec\u09ed\u09ee\u09ef", "", "%", new[]
            {
                new NumberUnit(1e3, 1e3, "", "\u00a0\u09b9\u09be"),
                new NumberUnit(1e5, 1e5, "", "\u00a0\u09b2\u09be"),
                new NumberUnit(1e7, 1e7, "", "\u00a0\u0995\u09cb"),
                new NumberUnit(1e10, 1e9, "", "\u09b6\u09a4\u00a0\u0995\u09cb", "", "\u00a0\u09b6\u09a4\u00a0\u0995\u09cb"),
                new NumberUnit(1e11, 1e9, "", "\u0995\u09cb"),
                new NumberUnit(1e12, 1e12, "", "\u00a0\u09b2\u09be.\u0995\u09cb.")
            }),
            ["bs"] = new NumberStyle(",", ".", "-", 3, 3, null, "", "%", new[]
            {
                new NumberUnit(1e3, 1e3, "", "\u00a0hilj."),
                new NumberUnit(1e6, 1e6, "", "\u00a0mil."),
                new NumberUnit(1e9, 1e9, "", "\u00a0mlrd."),
                new NumberUnit(1e12, 1e12, "", "\u00a0bil.")
            }),
            ["bg"] = new NumberStyle(",", "\u00a0", "-", 3, 3, null, "", "%", new[]
            {
                new NumberUnit(1e3, 1e3, "", "\u00a0\u0445\u0438\u043b."),
                new NumberUnit(1e6, 1e6, "", "\u00a0\u043c\u043b\u043d."),
                new NumberUnit(1e9, 1e9, "", "\u00a0\u043c\u043b\u0440\u0434."),
                new NumberUnit(1e12, 1e12, "", "\u00a0\u0442\u0440\u043b\u043d.")
            }),
            ["my"] = new NumberStyle(".", ",", "-", 3, 3, "\u1040\u1041\u1042\u1043\u1044\u1045\u1046\u1047\u1048\u1049", "", "%", new[]
            {
                new NumberUnit(1e3, 1e3, "", "\u00a0\u1011\u1031\u102c\u1004\u103a"),
                new NumberUnit(1e4, 1e4, "", "\u00a0\u101e\u1031\u102c\u1004\u103a\u1038"),
                new NumberUnit(1e5, 1e5, "", "\u00a0\u101e\u102d\u1014\u103a\u1038"),
                new NumberUnit(1e6, 1e6, "", "\u00a0\u101e\u1014\u103a\u1038"),
                new NumberUnit(1e7, 1e7, "", "\u00a0\u1000\u102f\u100b\u1031"),
                new NumberUnit(1e9, 1e7, "", "\u00a0\u100b\u1031"),
                new NumberUnit(1e10, 1e10, "\u100b\u1031\u00a0", "\u00a0\u1011"),
                new NumberUnit(1e11, 1e11, "\u100b\u1031\u00a0", "\u00a0\u101e"),
                new NumberUnit(1e12, 1e12, "\u100b\u1031\u00a0", "\u00a0\u101e\u102d\u1014\u103a\u1038"),
                new NumberUnit(1e13, 1e13, "\u100b\u1031\u00a0", "\u00a0\u101e\u1014\u103a\u1038"),
                new NumberUnit(1e14, 1e14, "", "\u00a0\u1000\u1031\u102c\u100b\u102d")
            }),
            ["ca"] = new NumberStyle(",", ".", "-", 3, 3, null, "", "\u00a0%", new[]
            {
                new NumberUnit(1e3, 1e3, "", "\u00a0k"),
                new NumberUnit(1e6, 1e6, "", "\u00a0M"),
                new NumberUnit(1e10, 1e9, "", "\u00a0kM"),
                new NumberUnit(1e12, 1e12, "", "\u00a0B")
            }),
            ["ceb"] = new NumberStyle(".", ",", "-", 3, 3, null, "", "%", new[]
            {
                new NumberUnit(1e3, 1e3, "", "K"),
                new NumberUnit(1e6, 1e6, "", "M"),
                new NumberUnit(1e9, 1e9, "", "G"),
                new NumberUnit(1e12, 1e12, "", "T")
            }),
            ["zh-Hans"] = new NumberStyle(".", ",", "-", 3, 3, null, "", "%", new[]
            {
                new NumberUnit(1e3, 1e0, "", ""),
                new NumberUnit(1e4, 1e4, "", "\u4e07"),
                new NumberUnit(1e8, 1e8, "", "\u4ebf"),
                new NumberUnit(1e12, 1e12, "", "\u4e07\u4ebf")
            }),
            ["zh-Hant"] = new NumberStyle(".", ",", "-", 3, 3, null, "", "%", new[]
            {
                new NumberUnit(1e3, 1e0, "", ""),
                new NumberUnit(1e4, 1e4, "", "\u842c"),
                new NumberUnit(1e8, 1e8, "", "\u5104"),
                new NumberUnit(1e12, 1e12, "", "\u5146")
            }),
            ["hr"] = new NumberStyle(",", ".", "\u2212", 3, 3, null, "", "\u00a0%", new[]
            {
                new NumberUnit(1e3, 1e3, "", "\u00a0tis."),
                new NumberUnit(1e6, 1e6, "", "\u00a0mil."),
                new NumberUnit(1e9, 1e9, "", "\u00a0mlr."),
                new NumberUnit(1e12, 1e12, "", "\u00a0bil.")
            }),
            ["cs"] = new NumberStyle(",", "\u00a0", "-", 3, 3, null, "", "\u00a0%", new[]
            {
                new NumberUnit(1e3, 1e3, "", "\u00a0tis."),
                new NumberUnit(1e6, 1e6, "", "\u00a0mil."),
                new NumberUnit(1e9, 1e9, "", "\u00a0mld."),
                new NumberUnit(1e12, 1e12, "", "\u00a0bil.")
            }),
            ["da"] = new NumberStyle(",", ".", "-", 3, 3, null, "", "\u00a0%", new[]
            {
                new NumberUnit(1e3, 1e3, "", "\u00a0t"),
                new NumberUnit(1e6, 1e6, "", "\u00a0mio."),
                new NumberUnit(1e9, 1e9, "", "\u00a0mia."),
                new NumberUnit(1e12, 1e12, "", "\u00a0bio.")
            }),
            ["nl"] = new NumberStyle(",", ".", "-", 3, 3, null, "", "%", new[]
            {
                new NumberUnit(1e3, 1e3, "", "K"),
                new NumberUnit(1e6, 1e6, "", "\u00a0mln."),
                new NumberUnit(1e9, 1e9, "", "\u00a0mld."),
                new NumberUnit(1e12, 1e12, "", "\u00a0bln.")
            }),
            ["en"] = new NumberStyle(".", ",", "-", 3, 3, null, "", "%", new[]
            {
                new NumberUnit(1e3, 1e3, "", "K"),
                new NumberUnit(1e6, 1e6, "", "M"),
                new NumberUnit(1e9, 1e9, "", "B"),
                new NumberUnit(1e12, 1e12, "", "T")
            }),
            ["et"] = new NumberStyle(",", "\u00a0", "\u2212", 3, 3, null, "", "%", new[]
            {
                new NumberUnit(1e3, 1e3, "", "\u00a0tuh"),
                new NumberUnit(1e6, 1e6, "", "\u00a0mln"),
                new NumberUnit(1e9, 1e9, "", "\u00a0mld"),
                new NumberUnit(1e12, 1e12, "", "\u00a0trln")
            }),
            ["fo"] = new NumberStyle(",", ".", "\u2212", 3, 3, null, "", "\u00a0%", new[]
            {
                new NumberUnit(1e3, 1e3, "", "\u00a0t\u00fas."),
                new NumberUnit(1e6, 1e6, "", "\u00a0mi\u00f3."),
                new NumberUnit(1e9, 1e9, "", "\u00a0mia."),
                new NumberUnit(1e12, 1e12, "", "\u00a0bi\u00f3.")
            }),
            ["fil"] = new NumberStyle(".", ",", "-", 3, 3, null, "", "%", new[]
            {
                new NumberUnit(1e3, 1e3, "", "K"),
                new NumberUnit(1e6, 1e6, "", "M"),
                new NumberUnit(1e9, 1e9, "", "B"),
                new NumberUnit(1e12, 1e12, "", "T")
            }),
            ["fi"] = new NumberStyle(",", "\u00a0", "\u2212", 3, 3, null, "", "\u00a0%", new[]
            {
                new NumberUnit(1e3, 1e3, "", "\u00a0t."),
                new NumberUnit(1e6, 1e6, "", "\u00a0milj."),
                new NumberUnit(1e9, 1e9, "", "\u00a0mrd."),
                new NumberUnit(1e12, 1e12, "", "\u00a0bilj.")
            }),
            ["fr"] = new NumberStyle(",", "\u202f", "-", 3, 3, null, "", "\u00a0%", new[]
            {
                new NumberUnit(1e3, 1e3, "", "\u00a0k"),
                new NumberUnit(1e6, 1e6, "", "\u00a0M"),
                new NumberUnit(1e9, 1e9, "", "\u00a0Md"),
                new NumberUnit(1e12, 1e12, "", "\u00a0Bn")
            }),
            ["gl"] = new NumberStyle(",", ".", "-", 3, 3, null, "", "\u00a0%", new[]
            {
                new NumberUnit(1e3, 1e0, "", ""),
                new NumberUnit(1e6, 1e6, "", "\u00a0M"),
                new NumberUnit(1e12, 1e12, "", "\u00a0B")
            }),
            ["ka"] = new NumberStyle(",", "\u00a0", "-", 3, 3, null, "", "%", new[]
            {
                new NumberUnit(1e3, 1e3, "", "\u00a0\u10d0\u10d7."),
                new NumberUnit(1e6, 1e6, "", "\u00a0\u10db\u10da\u10dc."),
                new NumberUnit(1e9, 1e9, "", "\u00a0\u10db\u10da\u10e0\u10d3."),
                new NumberUnit(1e11, 1e9, "", "\u00a0\u10db\u10da\u10e0."),
                new NumberUnit(1e12, 1e12, "", "\u00a0\u10e2\u10e0\u10da.")
            }),
            ["de"] = new NumberStyle(",", ".", "-", 3, 3, null, "", "\u00a0%", new[]
            {
                new NumberUnit(1e3, 1e0, "", ""),
                new NumberUnit(1e6, 1e6, "", "\u00a0Mio."),
                new NumberUnit(1e9, 1e9, "", "\u00a0Mrd."),
                new NumberUnit(1e12, 1e12, "", "\u00a0Bio.")
            }),
            ["el"] = new NumberStyle(",", ".", "-", 3, 3, null, "", "%", new[]
            {
                new NumberUnit(1e3, 1e3, "", "\u00a0\u03c7\u03b9\u03bb."),
                new NumberUnit(1e6, 1e6, "", "\u00a0\u03b5\u03ba."),
                new NumberUnit(1e9, 1e9, "", "\u00a0\u03b4\u03b9\u03c3."),
                new NumberUnit(1e12, 1e12, "", "\u00a0\u03c4\u03c1\u03b9\u03c3.")
            }),
            ["gu"] = new NumberStyle(".", ",", "-", 3, 2, null, "", "%", new[]
            {
                new NumberUnit(1e3, 1e3, "", "\u00a0\u0ab9\u0a9c\u0abe\u0ab0"),
                new NumberUnit(1e5, 1e5, "", "\u00a0\u0ab2\u0abe\u0a96"),
                new NumberUnit(1e7, 1e7, "", "\u00a0\u0a95\u0ab0\u0acb\u0aa1"),
                new NumberUnit(1e9, 1e9, "", "\u00a0\u0a85\u0aac\u0a9c"),
                new NumberUnit(1e11, 1e11, "", "\u00a0\u0aa8\u0abf\u0a96\u0ab0\u0acd\u0ab5"),
                new NumberUnit(1e12, 1e12, "", "\u00a0\u0aae\u0ab9\u0abe\u0aaa\u0aa6\u0acd\u0aae"),
                new NumberUnit(1e13, 1e13, "", "\u00a0\u0ab6\u0a82\u0a95\u0ac1"),
                new NumberUnit(1e14, 1e14, "", "\u00a0\u0a9c\u0ab2\u0aa7\u0abf")
            }),
            ["ha"] = new NumberStyle(".", ",", "-", 3, 3, null, "", "%", new[]
            {
                new NumberUnit(1e3, 1e3, "", "K"),
                new NumberUnit(1e4, 1e3, "", "D"),
                new NumberUnit(1e6, 1e6, "", "M"),
                new NumberUnit(1e9, 1e9, "", "B"),
                new NumberUnit(1e12, 1e12, "", "T")
            }),
            ["he"] = new NumberStyle(".", ",", "\u200e-", 3, 3, null, "", "%", new[]
            {
                new NumberUnit(1e3, 1e3, "", "K\u200f"),
                new NumberUnit(1e6, 1e6, "", "M\u200f"),
                new NumberUnit(1e9, 1e9, "", "B\u200f"),
                new NumberUnit(1e12, 1e12, "", "T\u200f")
            }),
            ["hi"] = new NumberStyle(".", ",", "-", 3, 2, null, "", "%", new[]
            {
                new NumberUnit(1e3, 1e3, "", "\u00a0\u0939\u091c\u093c\u093e\u0930"),
                new NumberUnit(1e5, 1e5, "", "\u00a0\u0932\u093e\u0916"),
                new NumberUnit(1e7, 1e7, "", "\u00a0\u0915\u0970"),
                new NumberUnit(1e9, 1e9, "", "\u00a0\u0905\u0970"),
                new NumberUnit(1e11, 1e11, "", "\u00a0\u0916\u0970"),
                new NumberUnit(1e13, 1e13, "", "\u00a0\u0928\u0940\u0932")
            }),
            ["hu"] = new NumberStyle(",", "\u00a0", "-", 3, 3, null, "", "%", new[]
            {
                new NumberUnit(1e3, 1e3, "", "\u00a0E"),
                new NumberUnit(1e6, 1e6, "", "\u00a0M"),
                new NumberUnit(1e9, 1e9, "", "\u00a0Mrd"),
                new NumberUnit(1e12, 1e12, "", "\u00a0B")
            }),
            ["is"] = new NumberStyle(",", ".", "-", 3, 3, null, "", "%", new[]
            {
                new NumberUnit(1e3, 1e3, "", "\u00a0\u00fe."),
                new NumberUnit(1e6, 1e6, "", "\u00a0m."),
                new NumberUnit(1e9, 1e9, "", "\u00a0ma."),
                new NumberUnit(1e12, 1e12, "", "\u00a0bn")
            }),
            ["id"] = new NumberStyle(",", ".", "-", 3, 3, null, "", "%", new[]
            {
                new NumberUnit(1e3, 1e3, "", "\u00a0rb"),
                new NumberUnit(1e6, 1e6, "", "\u00a0jt"),
                new NumberUnit(1e9, 1e9, "", "\u00a0M"),
                new NumberUnit(1e12, 1e12, "", "\u00a0T")
            }),
            ["ga"] = new NumberStyle(".", ",", "-", 3, 3, null, "", "%", new[]
            {
                new NumberUnit(1e3, 1e3, "", "k"),
                new NumberUnit(1e6, 1e6, "", "M"),
                new NumberUnit(1e9, 1e9, "", "B"),
                new NumberUnit(1e12, 1e12, "", "T")
            }),
            ["it"] = new NumberStyle(",", ".", "-", 3, 3, null, "", "%", new[]
            {
                new NumberUnit(1e3, 1e0, "", ""),
                new NumberUnit(1e6, 1e6, "", "\u00a0Mln"),
                new NumberUnit(1e9, 1e9, "", "\u00a0Mld"),
                new NumberUnit(1e12, 1e12, "", "\u00a0Bln")
            }),
            ["ja"] = new NumberStyle(".", ",", "-", 3, 3, null, "", "%", new[]
            {
                new NumberUnit(1e3, 1e0, "", ""),
                new NumberUnit(1e4, 1e4, "", "\u4e07"),
                new NumberUnit(1e8, 1e8, "", "\u5104"),
                new NumberUnit(1e12, 1e12, "", "\u5146")
            }),
            ["kn"] = new NumberStyle(".", ",", "-", 3, 3, null, "", "%", new[]
            {
                new NumberUnit(1e3, 1e3, "", "\u0cb8\u0cbe"),
                new NumberUnit(1e6, 1e6, "", "\u0cae\u0cbf"),
                new NumberUnit(1e9, 1e9, "", "\u0cac\u0cbf"),
                new NumberUnit(1e12, 1e12, "", "\u0c9f\u0ccd\u0cb0\u0cbf")
            }),
            ["kk"] = new NumberStyle(",", "\u00a0", "-", 3, 3, null, "", "%", new[]
            {
                new NumberUnit(1e3, 1e3, "", "\u00a0\u043c\u044b\u04a3"),
                new NumberUnit(1e5, 1e3, "", "\u00a0\u043c."),
                new NumberUnit(1e6, 1e6, "", "\u00a0\u043c\u043b\u043d"),
                new NumberUnit(1e9, 1e9, "", "\u00a0\u043c\u043b\u0440\u0434"),
                new NumberUnit(1e12, 1e12, "", "\u00a0\u0442\u0440\u043b\u043d")
            }),
            ["km"] = new NumberStyle(".", ",", "-", 3, 3, null, "", "%", new[]
            {
                new NumberUnit(1e3, 1e3, "", "\u1796\u17b6\u1793\u17cb"),
                new NumberUnit(1e4, 1e3, "", "\u00a0\u1796\u17b6\u1793\u17cb"),
                new NumberUnit(1e6, 1e6, "", "\u00a0\u179b\u17b6\u1793"),
                new NumberUnit(1e9, 1e9, "", "\u00a0\u1794\u17ca\u17b8\u179b\u17b6\u1793"),
                new NumberUnit(1e12, 1e12, "", "\u00a0\u1791\u17d2\u179a\u17b8\u179b\u17b6\u1793")
            }),
            ["rw"] = new NumberStyle(",", ".", "-", 3, 3, null, "", "%", new[]
            {
                new NumberUnit(1e3, 1e3, "", "K"),
                new NumberUnit(1e6, 1e6, "", "M"),
                new NumberUnit(1e9, 1e9, "", "G"),
                new NumberUnit(1e12, 1e12, "", "T")
            }),
            ["ko"] = new NumberStyle(".", ",", "-", 3, 3, null, "", "%", new[]
            {
                new NumberUnit(1e3, 1e3, "", "\ucc9c"),
                new NumberUnit(1e4, 1e4, "", "\ub9cc"),
                new NumberUnit(1e8, 1e8, "", "\uc5b5"),
                new NumberUnit(1e12, 1e12, "", "\uc870")
            }),
            ["ku"] = new NumberStyle(",", ".", "-", 3, 3, null, "%", "", new[]
            {
                new NumberUnit(1e3, 1e3, "", "H"),
                new NumberUnit(1e6, 1e6, "", "MN"),
                new NumberUnit(1e9, 1e9, "", "MR"),
                new NumberUnit(1e12, 1e12, "", "TN")
            }),
            ["ky"] = new NumberStyle(",", "\u00a0", "-", 3, 3, null, "", "%", new[]
            {
                new NumberUnit(1e3, 1e3, "", "\u00a0\u043c\u0438\u04a3"),
                new NumberUnit(1e6, 1e6, "", "\u00a0\u043c\u043b\u043d"),
                new NumberUnit(1e9, 1e9, "", "\u00a0\u043c\u043b\u0434"),
                new NumberUnit(1e12, 1e12, "", "\u00a0\u0442\u0440\u043b\u043d")
            }),
            ["lo"] = new NumberStyle(",", ".", "-", 3, 3, null, "", "%", new[]
            {
                new NumberUnit(1e3, 1e3, "", "\u00a0\u0e9e\u0eb1\u0e99"),
                new NumberUnit(1e6, 1e6, "", "\u00a0\u0ea5\u0ec9\u0eb2\u0e99"),
                new NumberUnit(1e9, 1e9, "", "\u00a0\u0e95\u0eb7\u0ec9"),
                new NumberUnit(1e12, 1e12, "", "\u00a0\u0ea5\u0ec9\u0eb2\u0e99\u0ea5\u0ec9\u0eb2\u0e99"),
                new NumberUnit(1e13, 1e12, "", "\u0ea5\u0ea5")
            }),
            ["lv"] = new NumberStyle(",", "\u00a0", "-", 3, 3, null, "", "%", new[]
            {
                new NumberUnit(1e3, 1e3, "", "\u00a0t\u016bkst."),
                new NumberUnit(1e6, 1e6, "", "\u00a0milj."),
                new NumberUnit(1e9, 1e9, "", "\u00a0mljrd."),
                new NumberUnit(1e12, 1e12, "", "\u00a0trilj.")
            }),
            ["lt"] = new NumberStyle(",", "\u00a0", "\u2212", 3, 3, null, "", "\u00a0%", new[]
            {
                new NumberUnit(1e3, 1e3, "", "\u00a0t\u016bkst."),
                new NumberUnit(1e6, 1e6, "", "\u00a0mln."),
                new NumberUnit(1e9, 1e9, "", "\u00a0mlrd."),
                new NumberUnit(1e12, 1e12, "", "\u00a0trln.")
            }),
            ["mk"] = new NumberStyle(",", ".", "-", 3, 3, null, "", "\u00a0%", new[]
            {
                new NumberUnit(1e3, 1e3, "", "\u00a0\u0438\u043b\u0458."),
                new NumberUnit(1e6, 1e6, "", "\u00a0\u043c\u0438\u043b."),
                new NumberUnit(1e8, 1e6, "", "\u00a0\u041c"),
                new NumberUnit(1e9, 1e9, "", "\u00a0\u043c\u0438\u043b\u0458."),
                new NumberUnit(1e11, 1e9, "", "\u00a0\u043c\u0438.", "", "\u00a0\u043c\u0458."),
                new NumberUnit(1e12, 1e12, "", "\u00a0\u0431\u0438\u043b.")
            }),
            ["ms"] = new NumberStyle(".", ",", "-", 3, 3, null, "", "%", new[]
            {
                new NumberUnit(1e3, 1e3, "", "K"),
                new NumberUnit(1e6, 1e6, "", "J"),
                new NumberUnit(1e9, 1e9, "", "B"),
                new NumberUnit(1e12, 1e12, "", "T")
            }),
            ["ml"] = new NumberStyle(".", ",", "-", 3, 2, null, "", "%", new[]
            {
                new NumberUnit(1e3, 1e3, "", "K"),
                new NumberUnit(1e6, 1e6, "", "M"),
                new NumberUnit(1e9, 1e9, "", "B"),
                new NumberUnit(1e12, 1e12, "", "T")
            }),
            ["mt"] = new NumberStyle(".", ",", "-", 3, 3, null, "", "%", new[]
            {
                new NumberUnit(1e3, 1e3, "", "K"),
                new NumberUnit(1e6, 1e6, "", "M"),
                new NumberUnit(1e9, 1e9, "", "G"),
                new NumberUnit(1e12, 1e12, "", "T")
            }),
            ["mi"] = new NumberStyle(".", ",", "-", 3, 3, null, "", "%", new[]
            {
                new NumberUnit(1e3, 1e3, "", "K"),
                new NumberUnit(1e6, 1e6, "", "M"),
                new NumberUnit(1e9, 1e9, "", "G"),
                new NumberUnit(1e12, 1e12, "", "T")
            }),
            ["mr"] = new NumberStyle(".", ",", "-", 3, 2, "\u0966\u0967\u0968\u0969\u096a\u096b\u096c\u096d\u096e\u096f", "", "%", new[]
            {
                new NumberUnit(1e3, 1e3, "", "\u00a0\u0939"),
                new NumberUnit(1e5, 1e5, "", "\u00a0\u0932\u093e\u0916"),
                new NumberUnit(1e7, 1e7, "", "\u00a0\u0915\u094b\u091f\u0940"),
                new NumberUnit(1e9, 1e9, "", "\u00a0\u0905\u092c\u094d\u091c"),
                new NumberUnit(1e11, 1e11, "", "\u00a0\u0916\u0930\u094d\u0935"),
                new NumberUnit(1e13, 1e13, "", "\u00a0\u092a\u0926\u094d\u092e")
            }),
            ["mn"] = new NumberStyle(".", ",", "-", 3, 3, null, "", "%", new[]
            {
                new NumberUnit(1e3, 1e3, "", "\u00a0\u043c\u044f\u043d\u0433\u0430"),
                new NumberUnit(1e6, 1e6, "", "\u00a0\u0441\u0430\u044f"),
                new NumberUnit(1e9, 1e9, "", "\u00a0\u0442\u044d\u0440\u0431\u0443\u043c"),
                new NumberUnit(1e11, 1e9, "", "\u0422"),
                new NumberUnit(1e12, 1e12, "", "\u0418\u041d")
            }),
            ["ne"] = new NumberStyle(".", ",", "-", 3, 2, "\u0966\u0967\u0968\u0969\u096a\u096b\u096c\u096d\u096e\u096f", "", "%", new[]
            {
                new NumberUnit(1e3, 1e3, "", "\u00a0\u0939\u091c\u093e\u0930"),
                new NumberUnit(1e5, 1e5, "", "\u00a0\u0932\u093e\u0916"),
                new NumberUnit(1e7, 1e7, "", "\u00a0\u0915\u0930\u094b\u0921"),
                new NumberUnit(1e9, 1e9, "", "\u00a0\u0905\u0930\u092c"),
                new NumberUnit(1e11, 1e11, "", "\u00a0\u0916\u0930\u092c"),
                new NumberUnit(1e13, 1e13, "", "\u00a0\u0936\u0902\u0916")
            }),
            ["no"] = new NumberStyle(",", "\u00a0", "\u2212", 3, 3, null, "", "\u00a0%", new[]
            {
                new NumberUnit(1e3, 1e3, "", "k"),
                new NumberUnit(1e6, 1e6, "", "\u00a0mill."),
                new NumberUnit(1e9, 1e9, "", "\u00a0mrd."),
                new NumberUnit(1e12, 1e12, "", "\u00a0bill.")
            }),
            ["or"] = new NumberStyle(".", ",", "-", 3, 2, null, "", "%", new[]
            {
                new NumberUnit(1e3, 1e3, "", "\u0b39"),
                new NumberUnit(1e6, 1e6, "", "\u0b28\u0b3f"),
                new NumberUnit(1e9, 1e9, "", "\u0b2c\u0b3f"),
                new NumberUnit(1e12, 1e12, "", "\u0b1f\u0b4d\u0b30\u0b3f")
            }),
            ["om"] = new NumberStyle(".", ",", "-", 3, 3, null, "", "%", new[]
            {
                new NumberUnit(1e3, 1e3, "", "K"),
                new NumberUnit(1e6, 1e6, "", "M"),
                new NumberUnit(1e9, 1e9, "", "G"),
                new NumberUnit(1e12, 1e12, "", "T")
            }),
            ["ps"] = new NumberStyle("\u066b", "\u066c", "\u200e-\u200e", 3, 3, "\u06f0\u06f1\u06f2\u06f3\u06f4\u06f5\u06f6\u06f7\u06f8\u06f9", "", "\u066a", new[]
            {
                new NumberUnit(1e3, 1e3, "", "K"),
                new NumberUnit(1e6, 1e6, "", "M"),
                new NumberUnit(1e9, 1e9, "", "B"),
                new NumberUnit(1e11, 1e9, "", "B", "", "G"),
                new NumberUnit(1e12, 1e12, "", "T")
            }),
            ["fa"] = new NumberStyle("\u066b", "\u066c", "\u200e\u2212", 3, 3, "\u06f0\u06f1\u06f2\u06f3\u06f4\u06f5\u06f6\u06f7\u06f8\u06f9", "", "\u066a", new[]
            {
                new NumberUnit(1e3, 1e3, "", "\u00a0\u0647\u0632\u0627\u0631"),
                new NumberUnit(1e6, 1e6, "", "\u00a0\u0645\u06cc\u0644\u06cc\u0648\u0646"),
                new NumberUnit(1e9, 1e9, "", "\u00a0\u0645\u06cc\u0644\u06cc\u0627\u0631\u062f"),
                new NumberUnit(1e12, 1e12, "", "\u00a0\u062a\u0631\u06cc\u0644\u06cc\u0648\u0646")
            }),
            ["pl"] = new NumberStyle(",", "\u00a0", "-", 3, 3, null, "", "%", new[]
            {
                new NumberUnit(1e3, 1e3, "", "\u00a0tys."),
                new NumberUnit(1e6, 1e6, "", "\u00a0mln"),
                new NumberUnit(1e9, 1e9, "", "\u00a0mld"),
                new NumberUnit(1e12, 1e12, "", "\u00a0bln")
            }),
            ["pt-BR"] = new NumberStyle(",", ".", "-", 3, 3, null, "", "%", new[]
            {
                new NumberUnit(1e3, 1e3, "", "\u00a0mil"),
                new NumberUnit(1e6, 1e6, "", "\u00a0mi"),
                new NumberUnit(1e9, 1e9, "", "\u00a0bi"),
                new NumberUnit(1e12, 1e12, "", "\u00a0tri")
            }),
            ["pt-PT"] = new NumberStyle(",", "\u00a0", "-", 3, 3, null, "", "%", new[]
            {
                new NumberUnit(1e3, 1e3, "", "\u00a0mil"),
                new NumberUnit(1e6, 1e6, "", "\u00a0M"),
                new NumberUnit(1e9, 1e9, "", "\u00a0mM"),
                new NumberUnit(1e12, 1e12, "", "\u00a0Bi")
            }),
            ["pa"] = new NumberStyle(".", ",", "-", 3, 2, null, "", "%", new[]
            {
                new NumberUnit(1e3, 1e3, "", "\u00a0\u0a39\u0a1c\u0a3c\u0a3e\u0a30"),
                new NumberUnit(1e5, 1e5, "", "\u00a0\u0a32\u0a71\u0a16"),
                new NumberUnit(1e7, 1e7, "", "\u00a0\u0a15\u0a30\u0a4b\u0a5c"),
                new NumberUnit(1e9, 1e9, "", "\u00a0\u0a05\u0a30\u0a2c"),
                new NumberUnit(1e11, 1e11, "", "\u00a0\u0a16\u0a30\u0a2c"),
                new NumberUnit(1e13, 1e13, "", "\u00a0\u0a28\u0a40\u0a32")
            }),
            ["qu"] = new NumberStyle(".", ",", "-", 3, 3, null, "", "\u00a0%", new[]
            {
                new NumberUnit(1e3, 1e3, "", "K"),
                new NumberUnit(1e6, 1e6, "", "M"),
                new NumberUnit(1e9, 1e9, "", "G"),
                new NumberUnit(1e12, 1e12, "", "T")
            }),
            ["ro"] = new NumberStyle(",", ".", "-", 3, 3, null, "", "\u00a0%", new[]
            {
                new NumberUnit(1e3, 1e3, "", "\u00a0K"),
                new NumberUnit(1e6, 1e6, "", "\u00a0mil."),
                new NumberUnit(1e9, 1e9, "", "\u00a0mld."),
                new NumberUnit(1e12, 1e12, "", "\u00a0tril.")
            }),
            ["rm"] = new NumberStyle(".", "\u2019", "\u2212", 3, 3, null, "", "\u00a0%", new[]
            {
                new NumberUnit(1e3, 1e3, "", "K"),
                new NumberUnit(1e6, 1e6, "", "M"),
                new NumberUnit(1e9, 1e9, "", "G"),
                new NumberUnit(1e12, 1e12, "", "T")
            }),
            ["ru"] = new NumberStyle(",", "\u00a0", "-", 3, 3, null, "", "\u00a0%", new[]
            {
                new NumberUnit(1e3, 1e3, "", "\u00a0\u0442\u044b\u0441."),
                new NumberUnit(1e6, 1e6, "", "\u00a0\u043c\u043b\u043d"),
                new NumberUnit(1e9, 1e9, "", "\u00a0\u043c\u043b\u0440\u0434"),
                new NumberUnit(1e12, 1e12, "", "\u00a0\u0442\u0440\u043b\u043d")
            }),
            ["sr"] = new NumberStyle(",", ".", "-", 3, 3, null, "", "%", new[]
            {
                new NumberUnit(1e3, 1e3, "", "\u00a0\u0445\u0438\u0459."),
                new NumberUnit(1e6, 1e6, "", "\u00a0\u043c\u0438\u043b."),
                new NumberUnit(1e9, 1e9, "", "\u00a0\u043c\u043b\u0440\u0434."),
                new NumberUnit(1e12, 1e12, "", "\u00a0\u0431\u0438\u043b.")
            }),
            ["sd"] = new NumberStyle("\u066b", "\u066c", "\u061c-", 3, 3, "\u0660\u0661\u0662\u0663\u0664\u0665\u0666\u0667\u0668\u0669", "", "\u066a\u061c", new[]
            {
                new NumberUnit(1e3, 1e3, "", "\u00a0\u0647\u0632\u0627\u0631"),
                new NumberUnit(1e6, 1e6, "", "\u00a0\u0645\u0644\u064a\u0646"),
                new NumberUnit(1e9, 1e9, "", "\u00a0\u0628\u0644\u064a\u0646"),
                new NumberUnit(1e12, 1e12, "", "\u00a0\u067d\u0631\u0644\u064a\u0646")
            }),
            ["si"] = new NumberStyle(".", ",", "-", 3, 3, null, "", "%", new[]
            {
                new NumberUnit(1e3, 1e3, "\u0daf", ""),
                new NumberUnit(1e6, 1e6, "\u0db8\u0dd2", ""),
                new NumberUnit(1e9, 1e9, "\u0db6\u0dd2", ""),
                new NumberUnit(1e12, 1e12, "\u0da7\u0dca\u200d\u0dbb\u0dd2", "")
            }),
            ["sk"] = new NumberStyle(",", "\u00a0", "-", 3, 3, null, "", "\u00a0%", new[]
            {
                new NumberUnit(1e3, 1e3, "", "\u00a0tis."),
                new NumberUnit(1e6, 1e6, "", "\u00a0mil."),
                new NumberUnit(1e9, 1e9, "", "\u00a0mld."),
                new NumberUnit(1e12, 1e12, "", "\u00a0bil.")
            }),
            ["sl"] = new NumberStyle(",", ".", "\u2212", 3, 3, null, "", "\u00a0%", new[]
            {
                new NumberUnit(1e3, 1e3, "", "\u00a0tis."),
                new NumberUnit(1e6, 1e6, "", "\u00a0mio."),
                new NumberUnit(1e9, 1e9, "", "\u00a0mrd."),
                new NumberUnit(1e12, 1e12, "", "\u00a0bil.")
            }),
            ["so"] = new NumberStyle(".", ",", "-", 3, 3, null, "", "%", new[]
            {
                new NumberUnit(1e3, 1e3, "", "K"),
                new NumberUnit(1e6, 1e6, "", "M"),
                new NumberUnit(1e9, 1e9, "", "B"),
                new NumberUnit(1e12, 1e12, "", "T")
            }),
            ["st"] = new NumberStyle(".", ",", "-", 3, 3, null, "", "%", new[]
            {
                new NumberUnit(1e3, 1e3, "", "K"),
                new NumberUnit(1e6, 1e6, "", "M"),
                new NumberUnit(1e9, 1e9, "", "G"),
                new NumberUnit(1e12, 1e12, "", "T")
            }),
            ["es"] = new NumberStyle(",", ".", "-", 3, 3, null, "", "\u00a0%", new[]
            {
                new NumberUnit(1e3, 1e3, "", "\u00a0mil"),
                new NumberUnit(1e6, 1e6, "", "\u00a0M"),
                new NumberUnit(1e10, 1e9, "", "\u00a0mil\u00a0M"),
                new NumberUnit(1e12, 1e12, "", "\u00a0B")
            }),
            ["sw"] = new NumberStyle(".", ",", "-", 3, 3, null, "", "%", new[]
            {
                new NumberUnit(1e3, 1e3, "elfu\u00a0", ""),
                new NumberUnit(1e6, 1e6, "", "M"),
                new NumberUnit(1e9, 1e9, "", "B"),
                new NumberUnit(1e12, 1e12, "", "T")
            }),
            ["sv"] = new NumberStyle(",", "\u00a0", "\u2212", 3, 3, null, "", "\u00a0%", new[]
            {
                new NumberUnit(1e3, 1e3, "", "\u00a0tn"),
                new NumberUnit(1e6, 1e6, "", "\u00a0mn"),
                new NumberUnit(1e9, 1e9, "", "\u00a0md"),
                new NumberUnit(1e12, 1e12, "", "\u00a0bn")
            }),
            ["tg"] = new NumberStyle(",", "\u00a0", "-", 3, 3, null, "", "%", new[]
            {
                new NumberUnit(1e3, 1e3, "", "\u00a0\u04b3\u0437\u0440."),
                new NumberUnit(1e6, 1e6, "", "\u00a0\u043c\u043b\u043d."),
                new NumberUnit(1e9, 1e9, "", "\u00a0\u043c\u043b\u0440\u0434."),
                new NumberUnit(1e12, 1e12, "", "\u00a0\u0442\u0440\u043b\u043d.")
            }),
            ["ta"] = new NumberStyle(".", ",", "-", 3, 2, null, "", "%", new[]
            {
                new NumberUnit(1e3, 1e3, "", "\u0b86"),
                new NumberUnit(1e6, 1e6, "", "\u0bae\u0bbf"),
                new NumberUnit(1e9, 1e9, "", "\u0baa\u0bbf"),
                new NumberUnit(1e12, 1e12, "", "\u0b9f\u0bbf")
            }),
            ["te"] = new NumberStyle(".", ",", "-", 3, 2, null, "", "%", new[]
            {
                new NumberUnit(1e3, 1e3, "", "\u0c35\u0c47"),
                new NumberUnit(1e6, 1e6, "", "\u0c2e\u0c3f"),
                new NumberUnit(1e9, 1e9, "", "\u0c2c\u0c3f"),
                new NumberUnit(1e12, 1e12, "", "\u0c1f\u0c4d\u0c30\u0c3f")
            }),
            ["th"] = new NumberStyle(".", ",", "-", 3, 3, null, "", "%", new[]
            {
                new NumberUnit(1e3, 1e3, "", "K"),
                new NumberUnit(1e6, 1e6, "", "M"),
                new NumberUnit(1e9, 1e9, "", "B"),
                new NumberUnit(1e12, 1e12, "", "T")
            }),
            ["tn"] = new NumberStyle(".", "\u2019", "-", 3, 3, null, "", "%", new[]
            {
                new NumberUnit(1e3, 1e3, "", "K"),
                new NumberUnit(1e6, 1e6, "", "M"),
                new NumberUnit(1e9, 1e9, "", "G"),
                new NumberUnit(1e12, 1e12, "", "T")
            }),
            ["tr"] = new NumberStyle(",", ".", "-", 3, 3, null, "%", "", new[]
            {
                new NumberUnit(1e3, 1e3, "", "\u00a0B"),
                new NumberUnit(1e6, 1e6, "", "\u00a0Mn"),
                new NumberUnit(1e9, 1e9, "", "\u00a0Mr"),
                new NumberUnit(1e12, 1e12, "", "\u00a0Tn")
            }),
            ["tk"] = new NumberStyle(",", "\u00a0", "-", 3, 3, null, "", "\u00a0%", new[]
            {
                new NumberUnit(1e3, 1e3, "", "\u00a0m\u00fc\u0148"),
                new NumberUnit(1e6, 1e6, "", "\u00a0mln"),
                new NumberUnit(1e9, 1e9, "", "\u00a0mlrd"),
                new NumberUnit(1e12, 1e12, "", "\u00a0trln")
            }),
            ["uk"] = new NumberStyle(",", "\u00a0", "-", 3, 3, null, "", "%", new[]
            {
                new NumberUnit(1e3, 1e3, "", "\u00a0\u0442\u0438\u0441."),
                new NumberUnit(1e6, 1e6, "", "\u00a0\u043c\u043b\u043d"),
                new NumberUnit(1e9, 1e9, "", "\u00a0\u043c\u043b\u0440\u0434"),
                new NumberUnit(1e12, 1e12, "", "\u00a0\u0442\u0440\u043b\u043d")
            }),
            ["ur"] = new NumberStyle(".", ",", "\u200e-", 3, 3, null, "", "%", new[]
            {
                new NumberUnit(1e3, 1e3, "", "\u00a0\u06c1\u0632\u0627\u0631"),
                new NumberUnit(1e5, 1e5, "", "\u00a0\u0644\u0627\u06a9\u06be"),
                new NumberUnit(1e7, 1e7, "", "\u00a0\u06a9\u0631\u0648\u0691"),
                new NumberUnit(1e9, 1e9, "", "\u00a0\u0627\u0631\u0628"),
                new NumberUnit(1e11, 1e11, "", "\u00a0\u06a9\u06be\u0631\u0628"),
                new NumberUnit(1e13, 1e12, "", "\u00a0\u0679\u0631\u06cc\u0644\u06cc\u0646")
            }),
            ["uz"] = new NumberStyle(",", "\u00a0", "-", 3, 3, null, "", "%", new[]
            {
                new NumberUnit(1e3, 1e3, "", "\u00a0ming"),
                new NumberUnit(1e6, 1e6, "", "\u00a0mln"),
                new NumberUnit(1e9, 1e9, "", "\u00a0mlrd"),
                new NumberUnit(1e12, 1e12, "", "\u00a0trln")
            }),
            ["vi"] = new NumberStyle(",", ".", "-", 3, 3, null, "", "%", new[]
            {
                new NumberUnit(1e3, 1e3, "", "\u00a0N"),
                new NumberUnit(1e6, 1e6, "", "\u00a0Tr"),
                new NumberUnit(1e9, 1e9, "", "\u00a0T"),
                new NumberUnit(1e12, 1e12, "", "\u00a0NT")
            }),
            ["cy"] = new NumberStyle(".", ",", "-", 3, 3, null, "", "%", new[]
            {
                new NumberUnit(1e3, 1e3, "", "K"),
                new NumberUnit(1e6, 1e6, "", "M"),
                new NumberUnit(1e9, 1e9, "", "B"),
                new NumberUnit(1e12, 1e12, "", "T")
            }),
            ["fy"] = new NumberStyle(",", ".", "-", 3, 3, null, "", "%", new[]
            {
                new NumberUnit(1e3, 1e3, "", "K"),
                new NumberUnit(1e6, 1e6, "", "\u00a0mln."),
                new NumberUnit(1e9, 1e9, "", "\u00a0mld."),
                new NumberUnit(1e12, 1e12, "", "\u00a0bln.")
            }),
            ["wo"] = new NumberStyle(",", ".", "-", 3, 3, null, "", "%", new[]
            {
                new NumberUnit(1e3, 1e3, "", "K"),
                new NumberUnit(1e6, 1e6, "", "M"),
                new NumberUnit(1e9, 1e9, "", "B"),
                new NumberUnit(1e12, 1e12, "", "T")
            }),
            ["yo"] = new NumberStyle(".", ",", "-", 3, 3, null, "", "%", new[]
            {
                new NumberUnit(1e3, 1e3, "", "K"),
                new NumberUnit(1e6, 1e6, "", "M"),
                new NumberUnit(1e9, 1e9, "", "G"),
                new NumberUnit(1e12, 1e12, "", "T")
            }),
            ["zu"] = new NumberStyle(".", ",", "-", 3, 3, null, "", "%", new[]
            {
                new NumberUnit(1e3, 1e3, "", "K"),
                new NumberUnit(1e6, 1e6, "", "M"),
                new NumberUnit(1e9, 1e9, "", "B"),
                new NumberUnit(1e12, 1e12, "", "T")
            }),
        };
    }
}
