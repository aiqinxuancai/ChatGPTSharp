using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;

namespace ChatGPTSharp.Utils
{
    /// <summary>
    /// This code refers to the js library gpt-3-encoder
    /// </summary>
    public class GPT3Token
    {
        static Dictionary<string, int> encoder;
        static Dictionary<int, string> decoder;
        static Dictionary<int, char> byte_encoder;
        static Dictionary<char, int> byte_decoder;
        static Dictionary<Tuple<string, string>, int> bpe_ranks;

        static Dictionary<string, string> cache = new Dictionary<string, string>();

        static GPT3Token()
        {
            encoder = JsonConvert.DeserializeObject<Dictionary<string, int>>(ResHelper.GetTokenResString("encoder"));
            decoder = encoder.ToDictionary(x => x.Value, x => x.Key);
            byte_encoder = BytesToUnicode();
            byte_decoder = byte_encoder.ToDictionary(x => x.Value, x => x.Key);

            string bpe_file = ResHelper.GetTokenResString("vocab");

            string[] lines = bpe_file.Split('\n');

            List<string[]> bpeMerges = lines.Skip(1).Take(lines.Length - 2)
                    .Select(x => x.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries))
                    .ToList();

            Dictionary<string[], int>? bpeRanksCache = DictZip(bpeMerges, Enumerable.Range(0, bpeMerges.Count));

            Dictionary<Tuple<string, string>, int> bpe_ranks2 = bpeRanksCache.ToDictionary(
                    kvp => Tuple.Create(kvp.Key[0], kvp.Key[1]),
                    kvp => kvp.Value
                );

            bpe_ranks = bpe_ranks2;
        }

        public static Dictionary<T1, T2> DictZip<T1, T2>(IEnumerable<T1> x, IEnumerable<T2> y)
        {
            var result = new Dictionary<T1, T2>();
            var xArray = x.ToArray();
            var yArray = y.ToArray();
            for (int i = 0; i < xArray.Length; i++)
            {
                result.Add(xArray[i], yArray[i]);
            }
            return result;
        }


        static HashSet<string[]> GetPairs(List<string> word)
        {
            var pairs = new HashSet<string[]>();
            var prevChar = word[0];
            for (int i = 1; i < word.Count; i++)
            {
                var currChar = word[i];
                pairs.Add(new string[] { prevChar.ToString(), currChar.ToString() });
                prevChar = currChar;
            }
            return pairs;
        }

        static int[] Range(int x, int y)
        {
            return Enumerable.Range(x, y - x).ToArray();
        }

        static int Ord(char x)
        {
            return Convert.ToInt32(x);
        }

        static char Chr(int x)
        {
            return Convert.ToChar(x);
        }

        static int[] EncodeStr(string str)
        {
            var textEncoder = Encoding.UTF8;
            return textEncoder.GetBytes(str).Select(x => Convert.ToInt32(x)).ToArray();
        }

        static string DecodeStr(int[] arr)
        {
            var textDecoder = Encoding.UTF8;
            return textDecoder.GetString(arr.Select(x => Convert.ToByte(x)).ToArray());
        }

        static Dictionary<int, char> BytesToUnicode()
        {
            List<int> bs = Enumerable.Range((int)'!', (int)'~' - (int)'!' + 1)
                .Concat(Enumerable.Range((int)'¡', (int)'¬' - (int)'¡' + 1))
                .Concat(Enumerable.Range((int)'®', (int)'ÿ' - (int)'®' + 1)).ToList();

            List<int> cs = new List<int>(bs);
            int n = 0;
            for (int b = 0; b < Math.Pow(2, 8); b++)
            {
                if (!bs.Contains(b))
                {
                    bs.Add(b);
                    cs.Add((int)Math.Pow(2, 8) + n);
                    n++;
                }
            }

            cs = cs.Select(x => (int)x).ToList();

            Dictionary<int, char> result = new Dictionary<int, char>();
            for (int i = 0; i < bs.Count; i++)
            {
                result[bs[i]] = (char)cs[i];
            }

            return result;
        }

        public static List<int> Encode(string text)
        {
            var bpe_tokens = new List<int>();
            string pattern = @"'s|'t|'re|'ve|'m|'ll|'d| ?\p{L}+| ?\p{N}+| ?[^\s\p{L}\p{N}]+|\s+(?!\S)|\s+";

            var matches = Regex.Matches(text, pattern, RegexOptions.Compiled | RegexOptions.Multiline).Select(x => x.Value).ToArray();
            foreach (var token in matches)
            {
                var encodedBytes = EncodeStr(token).Select(x => byte_encoder[x]).ToList();
                var encodedToken = string.Join("", encodedBytes);
                var newTokens = bpe(encodedToken).Split(' ').Select(x => encoder[x]).ToList();
                bpe_tokens.AddRange(newTokens);
            }
            return bpe_tokens;
        }

        //public static string Decode(List<int> tokens)
        //{
        //    List<string> text = tokens.Select(x => decoder[x]).ToList();
        //    text = DecodeStr(text.Select(x => byte_decoder[x.ToCharArray()]).ToList());
        //    return new string(text.ToArray());
        //}

        //应该传入其他的值
        private static string bpe(string token)
        {
            if (cache.ContainsKey(token))
            {
                return cache[token];
            }
            //string[] chars = 
            List<string> word = token.ToCharArray().Select(c => c.ToString()).ToList();

            var pairs = GetPairs(word);

            if (pairs == null || pairs.Count == 0)
            {
                return token;
            }

            while (true)
            {
                var minPairs = new Dictionary<int, string[]>();
                foreach (var pair in pairs)
                {
                    Tuple<string, string> tuple = Tuple.Create(pair[0], pair[1]);

                    if (bpe_ranks.ContainsKey(tuple))
                    {
                        var rank = bpe_ranks[tuple];
                        minPairs[double.IsNaN(rank) ? int.MaxValue : (int)rank] = pair;
                    }
                    else
                    {
                        minPairs[int.MaxValue] = pair;
                    }

                }

                var bigram = minPairs[minPairs.Keys.Min()];
                Tuple<string, string> tupleBigram = Tuple.Create(bigram[0], bigram[1]);
                if (!bpe_ranks.ContainsKey(tupleBigram))
                {
                    break;
                }

                var first = bigram[0];
                var second = bigram[1];
                var new_word = new List<string>();
                var i = 0;


                while (i < word.Count)
                {

                    var j = word.IndexOf(first, i);
                    if (j == -1)
                    {
                        new_word.AddRange(word.GetRange(i, word.Count - i));
                        break;
                    }
                    new_word.AddRange(word.GetRange(i, j));
                    i = j;

                    if (word[i] == first && i < word.Count - 1 && word[i + 1] == second)
                    {
                        new_word.Add(first.ToString() + second.ToString());
                        i = i + 2;
                    }
                    else
                    {
                        new_word.Add(word[i].ToString());
                        i = i + 1;
                    }
                }

                word = new_word;
                if (word.Count == 1)
                {
                    break;
                }
                else
                {
                    pairs = GetPairs(word);
                }
            }

            var result = string.Join(" ", word);
            cache[token] = result;

            return result;
        }

    }
}
