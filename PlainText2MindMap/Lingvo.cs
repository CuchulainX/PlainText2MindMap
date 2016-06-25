using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

public static class Lingvo {
    private static List<string> stopword;
    static Stemmer enStem = new Stemmer();
    static Dictionary<String, String> cache = new Dictionary<string, string>();
    static List<String> notNounSuffix = new List<string>();
    static List<String> notNounPrefix = new List<string>();

    static Lingvo()
    {
        init();
    }

    public static string stem(string word)
    {
        return enStem.stem(word);
    }

    public static string getKey(string word)
    {
        word = word.ToLower().Trim();
        if (word.Length < 3) return "";
        if (isNotNoun(word)) return "";

        if (cache.ContainsKey(word)) return cache[word];
        string res = stem(word);
        if (isSW(res)) res = "";
        //if (isDigit(res)) res = "";
        cache[word] = res;
        return res;
    }

    public static bool isDigit(string res)
    {
        foreach (char c in res)
            if (!Char.IsDigit(c)) return false;

        return true;
    }

    public static string getHash(string msg)
    {
        string str = msg.ToLower().Trim();
        string[] words = Regex.Split(str, "[^a-zA-Z]");

        List<String> k = new List<string>();
        foreach (var s in words)
        {
            string key = Lingvo.getKey(s);
            if (String.IsNullOrEmpty(key)) continue;
            k.Add(key);
        }
        k.Sort();
        string keys = "";
        foreach (string s in k) keys += s.Trim();

        return keys;
    }

    public static String StripHTML(String source)
    {
        String result;

        result = source.Replace("\r", " ");
        result = result.Replace("\n", " ");
        result = result.Replace("\t", " ");

        result = Regex.Replace(result, "( )+", " ");
        result = Regex.Replace(result, "<( )*?head([^>])*?>", "<head>");
        result = Regex.Replace(result, "(<( )*?(/)( )*?head( )*?>)", "</head>");
        result = Regex.Replace(result, "(<head>).*?(</head>)", "");

        // remove all scripts (prepare first by clearing attributes)
        result = Regex.Replace(result, "<( )*?script([^>])*?>", "<script>");
        result = Regex.Replace(result, "(<( )*?(/)( )*?script( )*?>)", "</script>");
        result = Regex.Replace(result, "(<script>).*?(</script>)", "");

        // remove all styles (prepare first by clearing attributes)
        result = Regex.Replace(result, "<( )*?style([^>])*?>", "<style>");
        result = Regex.Replace(result, "(<( )*?(/)( )*?style( )*?>)", "</style>");
        result = Regex.Replace(result, "(<style>).*?(</style>)", "");

        // insert tabs in spaces of <td> tags
        result = Regex.Replace(result, "<( )*?td([^>])*?>", "\t");

        // insert line breaks in places of <BR> and <LI> tags
        result = Regex.Replace(result, "<( )*?br( )*?>", "\r");
        result = Regex.Replace(result, "<( )*?li( )*?>", "\r");

        // insert line paragraphs (double line breaks) in place
        // if <P>, <DIV> and <TR> tags
        result = Regex.Replace(result, "<( )*?div([^>])*?>", "\r\r");
        result = Regex.Replace(result, "<( )*?tr([^>])*?>", "\r\r");
        result = Regex.Replace(result, "<( )*?p([^>])*?>", "\r\r");

        // Remove remaining tags like <a>, links, images,
        // comments etc - anything thats enclosed inside < >
        result = Regex.Replace(result, "<[^>]*?>", "");

        // replace special characters:
        result = Regex.Replace(result, "&nbsp;", " ");
        result = Regex.Replace(result, "&bull;", " *? ");
        result = Regex.Replace(result, "&lsaquo;", "<");
        result = Regex.Replace(result, "&rsaquo;", ">");
        result = Regex.Replace(result, "&trade;", "(tm)");
        result = Regex.Replace(result, "&frasl;", "/");
        result = Regex.Replace(result, "&lt;", "<");
        result = Regex.Replace(result, "&gt;", ">");
        result = Regex.Replace(result, "&copy;", "(c)");
        result = Regex.Replace(result, "&reg;", "(r)");
        result = Regex.Replace(result, "&#39;", "\'");

        // Remove all others. More can be added, see
        // http://hotwired.lycos.com/webmonkey/reference/special_characters/
        result = Regex.Replace(result, "&(.{2,6});", " ");
        result = Regex.Replace(result, "( )+", " ");
        result = Regex.Replace(result, "(\r)+", "\r");
        result = Regex.Replace(result, "(\n)+", "\n");
        result = Regex.Replace(result, "([a-zA-Z]*)'(a-zA-Z)*", "$1$2");

        return result.Trim();
    }

    public static int distance( //Расстояние Ливенштейна
                                     String s1,
                                     String s2)
    {
        const int ValueOfReplace = 1; // Цена замены символа строки
        const int ValueOfDelete = 1; // Цена удаления символа строки
        const int ValueOfInsert = 1; // Цена вставки символа строки

        int i, j;
        int[,] D = new int[s1.Length + 1, s2.Length + 1];

        for (i = 0; i <= s1.Length; i++)
            D[i, 0] = i;
        for (j = 0; j <= s2.Length; j++)
            D[0, j] = j;

        for (i = 1; i <= s1.Length; i++)
        {
            for (j = 1; j <= s2.Length; j++)
            {
                // Если символы совпадают то зачем с ними что-то делать?!
                int Difference = (s1[i - 1] == s2[j - 1]) ? 0 : 1;

                D[i, j] = Math.Min(Math.Min(
                             D[i - 1, j] + ValueOfDelete,
                             D[i, j - 1] + ValueOfInsert),
                             D[i - 1, j - 1] + ValueOfReplace * Difference);
            }
        }

        return D[s1.Length, s2.Length];
    }

    public static void init()
    {
        stopword = new List<string>();

        var lines = File.ReadLines(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "2000.txt"));
        foreach (var line in lines)
        {
            stopword.Add(line);
        }

        notNounSuffix.Add("al");
        notNounSuffix.Add("ent");
        notNounSuffix.Add("ive");
        notNounSuffix.Add("ous");
        notNounSuffix.Add("ful");
        notNounSuffix.Add("less");
        notNounSuffix.Add("able");
        notNounSuffix.Add("ise");
        notNounSuffix.Add("ate");
        notNounSuffix.Add("fy");
        notNounSuffix.Add("en");
        notNounSuffix.Add("ed");
        notNounSuffix.Add("ible");
        notNounSuffix.Add("ial");
        notNounSuffix.Add("ical");
        notNounSuffix.Add("ant");
        notNounSuffix.Add("ient");
        notNounSuffix.Add("ar");
        notNounSuffix.Add("ary");
        notNounSuffix.Add("ier");
        notNounSuffix.Add("est");
        notNounSuffix.Add("iest");
        notNounSuffix.Add("fold");
        notNounSuffix.Add("ify");
        notNounSuffix.Add("ile");
        notNounSuffix.Add("ish");
        notNounSuffix.Add("ly");
        notNounSuffix.Add("ose");
        notNounSuffix.Add("ward");
        notNounSuffix.Add("wise");
    }

    public static bool isNotNoun(String word)
    {
        foreach (var suffix in notNounSuffix)
            if (word.EndsWith(suffix)) return true;

        return false;
    }

    public static bool isSW(string word)
    {
        if (word.Length < 2 | word.Length > 30) return true;

        return stopword.Contains(word);
    }
}