using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace PlainText2MindMap {
    class MindMap {
        static List<Word> words = new List<Word>();
        static Dictionary<String, int> freq = new Dictionary<string, int>();
        static Dictionary<String, List<int>> source = new Dictionary<String, List<int>>();
        static Dictionary<String, List<String>> dicNames = new Dictionary<string, List<String>>();

        public void build(string fileName)
        {
            var text = File.ReadAllText(fileName);
            if (String.IsNullOrEmpty(text)) return;

            int i = 0;
            foreach (var abz in Regex.Split(text, "\r\n\r\n"))
                prepareData(abz.Replace("\r\n", " "), i++);

            freq = clearObject().OrderByDescending(t => t.Value).Take(100).ToDictionary(t => t.Key, t => t.Value);

            List<Clust> clust = new List<Clust>();
            foreach (var s in freq)
            {
                Clust c = new Clust();
                foreach (var t in source[s.Key]) c.sources.Add(t, 1);
                c.words.Add(s.Key, 1);
                clust.Add(c);
            }

            clust = clustering(clust);
            Word root = new Word
            {
                key = "root",
                parent = null,
                lev = 0,
                name = "root"
            };

            mapping(root, clust);

            string res = "<map version=\"1.0.1\">\r\n" + printMM(root, "") + "\r\n</map>";
            
            File.WriteAllText(Path.ChangeExtension(fileName, "mm"), res);
            File.WriteAllText(Path.ChangeExtension(fileName, "xml"), res);
        }

        private static void prepareData(string text, int num)
        {
            foreach (var sent in Regex.Split(text, "[^a-zA-Z0-9 ]"))
            {
                if (string.IsNullOrEmpty(sent)) continue;

                List<String> tail = new List<string>();
                List<String> srcTail = new List<string>();
                string str = ""; int swcnt = 0;
                foreach (var word in Regex.Split(sent, "[^a-zA-Z0-9]"))
                {
                    string key = Lingvo.getKey(word);

                    if (String.IsNullOrEmpty(key) || Lingvo.isNotNoun(word))
                    {
                        /*
                                 str = (str + " " + word).Trim();
                                 swcnt++;
                                             if (swcnt > 2)
                                             {
                                                 tail.Clear();
                                                 srcTail.Clear();
                                                 str = "";
                                                 swcnt = 0;
                                             }
                                             */
                        continue;
                    }
                    if (srcTail.Count > 0) srcTail[srcTail.Count - 1] = (srcTail[srcTail.Count - 1] + " " + str).Trim();
                    str = ""; swcnt = 0;
                    srcTail.Add(word);

                    while (tail.Count() > 4) tail.Remove(tail[0]); //!!!
                    tail.Add(key);

                    string comlpexName = "";
                    string dicName = "";
                    for (int i = 1; i <= tail.Count; i++)
                    {
                        comlpexName = tail[tail.Count - i] + " " + comlpexName;
                        comlpexName = comlpexName.Trim();

                        string comlpexKey = "";
                        foreach (var s in comlpexName.Split(' ').OrderBy(t => t))
                            comlpexKey += s + " ";
                        comlpexKey = comlpexKey.Trim();

                        if (freq.ContainsKey(comlpexKey)) freq[comlpexKey]++;
                        else freq.Add(comlpexKey, 1);

                        /*
                        if (!XWord.Contains(comlpexKey)) XWord.Add(comlpexKey);
                                    int xid = XWord.IndexOf(comlpexKey);
                                    if (!XRel[num].Contains(xid)) XRel[num].Add(xid);
                                    */

                        if (!source.ContainsKey(comlpexKey)) source.Add(comlpexKey, new List<int>());
                        if (!source[comlpexKey].Contains(num)) source[comlpexKey].Add(num);

                        dicName = srcTail[srcTail.Count - i] + " " + dicName;
                        dicName = dicName.Trim();
                        List<string> list = new List<string>();
                        if (!dicNames.ContainsKey(comlpexKey)) dicNames.Add(comlpexKey, list);
                        else list = dicNames[comlpexKey];

                        if (!list.Contains(dicName)) list.Add(dicName);
                        //if (!xray[num].Contains(comlpexKey)) xray[num].Add(comlpexKey);
                    }
                }
            }
        }

        private static void mapping(Word parent, List<Clust> clust)
        {
            if (parent.lev > 3) return;

            foreach (var c in clust)
            {
                if (c.words.Count <= 2) //Если осталось меньше трех элементов
                {
                    foreach (var a in c.words)
                    {
                        Word root = new Word
                        {
                            key = a.Key,
                            name = dicNames[a.Key].First(),
                            parent = parent,
                            lev = parent.lev + 1
                        };
                        words.Add(root);
                    }
                }
                else
                {
                    var sel = from w in c.words
                              join f in freq on w.Key equals f.Key
                              orderby f.Value descending //w.Value * 10000 + 
                              select w.Key;


                    Word root = new Word
                    {
                        key = sel.First(),
                        name = dicNames[sel.First()].First(),
                        parent = parent,
                        lev = parent.lev + 1
                    };
                    words.Add(root);

                    List<Clust> new_clust = new List<Clust>();
                    foreach (var a in c.words)
                    {
                        if (a.Key == root.key) continue;

                        Clust n = new Clust();
                        foreach (var t in source[a.Key]) n.sources.Add(t, 1);
                        n.words.Add(a.Key, 1);
                        new_clust.Add(n);
                    }

                    new_clust = clustering(new_clust);
                    mapping(root, new_clust);
                }
            }
        }

        private static List<Clust> clustering(List<Clust> clust)
        {
            //Кластеризация
            int size = clust.Count();
            int cicle = 0;
            while (true)
            {
                if (cicle++ > 1000) break;

                int imax = 0, jmax = 0;
                float max = 0;
                for (int i = 0; i < clust.Count(); i++)
                {
                    if (clust[i].words.Count() > Math.Max(2, size / 10)) continue;
                    for (int j = i + 1; j < clust.Count(); j++)
                    {
                        if (clust[j].words.Count() > Math.Max(2, size / 10)) continue;

                        var sel = from s1 in clust[i].sources
                                  join s2 in clust[j].sources on s1.Key equals s2.Key
                                  select s1.Value + s2.Value;
                        float cnt = sel.Sum(t => t);
                        cnt = cnt / (clust[i].sources.Sum(t => t.Value) + clust[j].sources.Sum(t => t.Value)) * 100;

                        if (cnt > max)
                        {
                            max = cnt;
                            imax = i;
                            jmax = j;
                        }
                    }
                }
                if (max <= 10) break; // %

                foreach (var c in clust[jmax].words)
                    clust[imax].words.Add(c.Key, 1);
                foreach (var c in clust[jmax].sources)
                    if (clust[imax].sources.ContainsKey(c.Key))
                        clust[imax].sources[c.Key] += c.Value;
                    else clust[imax].sources.Add(c.Key, c.Value);

                Dictionary<string, int> tmpWord = new Dictionary<string, int>();
                foreach (var c in clust[imax].words)
                {
                    var sel =
                       from s in source[c.Key]
                       join r in clust[imax].sources on s equals r.Key
                       select s;
                    tmpWord[c.Key] = sel.Count();
                }
                foreach (var c in tmpWord) clust[imax].words[c.Key] = c.Value;

                clust.Remove(clust[jmax]);
            }

            return clust;
        }

        private static Dictionary<string, int> clearObject()
        {
            //Зачищаем лишнее
            List<string> forDel = new List<string>();
            Dictionary<string, int> forChange = new Dictionary<string, int>();
            foreach (var dic in freq)
                if (dic.Value < 2) forDel.Add(dic.Key);
            foreach (var d in forDel) freq.Remove(d);
            forDel.Clear();

            foreach (var dic in freq)
            {
                var d = dic.Key.Split(' ');

                var sel =
                  from f in freq
                  where f.Value * 100 >= dic.Value && !dic.Key.Equals(f.Key) && (f.Key.Contains(dic.Key) || d.All(s => f.Key.Split(' ').Contains(s)))
                  select f;

                if (sel.Count() > 0)
                {
                    foreach (var s in sel)
                    {
                        int val = Math.Max(0, dic.Value - s.Value);
                        if (val < 0) continue;
                        if (forChange.ContainsKey(s.Key)) forChange[s.Key] += val;
                        else forChange.Add(s.Key, val);

                        foreach (var src in source[dic.Key])
                            if (!source[s.Key].Contains(src)) source[s.Key].Add(src);
                    }
                    forDel.Add(dic.Key);
                }
            }
            foreach (var d in forDel) freq.Remove(d);

            foreach (var d in forChange)
                if (freq.ContainsKey(d.Key))
                    freq[d.Key] += d.Value;

            return freq;
        }

        private static string printMM(Word w, string shift)
        {
            string child = "";
            string key = w.name;
            var sel = words.Where(t => t.parent == w);
            if (w.lev == 1 && sel.Count() == 0) return "";

            foreach (var s in sel)
            {
                var c = printMM(s, shift + "    ");
                if (!String.IsNullOrEmpty(c)) child += c + "\r\n";
            }

            if (!string.IsNullOrEmpty(child))
                child = shift + "\r\n" + child + shift;

            return shift + String.Format("<node text=\"{0}\">{1}</node>", toSentCase(key), child);
        }

        private static string toSentCase(string c)
        {
            return c[0].ToString().ToUpper() + c.Substring(1);
        }

    }

    class Word {
        public Word parent;
        public string key;
        public int lev;
        public string name;
    }

    class Clust {
        public Dictionary<String, int> words = new Dictionary<String, int>();
        public Dictionary<int, int> sources = new Dictionary<int, int>();
    }

}