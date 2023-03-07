using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace ChatGPTSharp.Utils.tiktoken
{
    
    internal class BytePairEncoding2
    {
        internal static List<int> BytePairMerge(byte[] piece, Dictionary<byte[], int> ranks, Func<Range, int> f)
        {
            (int, int)[] parts = new (int, int)[piece.Length + 1];
            for (int i = 0; i < parts.Length; i++)
            {
                parts[i] = (i, int.MaxValue);
            }

            int? GetRank(int startIdx, int skip)
            {
                if (startIdx + skip + 2 < parts.Length)
                {
                    byte[] key = new byte[parts[startIdx + skip + 2].Item1 - parts[startIdx].Item1];
                    Array.Copy(piece, parts[startIdx].Item1, key, 0, key.Length);
                    return ranks.TryGetValue(key, out int rank) ? rank : default(int?);
                }
                else
                {
                    return null;
                }
            }


            for (int i = 0; i < parts.Length - 2; i++)
            {
                int? rank = GetRank(i, 0);
                if (rank.HasValue)
                {
                    Debug.Assert(rank != int.MaxValue);
                    parts[i] = (parts[i].Item1, rank.Value);
                }
            }

            while (parts.Length > 1)
            {
                int minRank = int.MaxValue;
                int minRankIndex = 0;
                for (int i = 0; i < parts.Length - 1; i++)
                {
                    if (parts[i].Item2 < minRank)
                    {
                        minRank = parts[i].Item2;
                        minRankIndex = i;
                    }
                }

                if (minRank != int.MaxValue)
                {
                    parts[minRankIndex] = (parts[minRankIndex].Item1, GetRank(minRankIndex, 1) ?? int.MaxValue);
                    if (minRankIndex > 0)
                    {
                        parts[minRankIndex - 1] = (parts[minRankIndex - 1].Item1, GetRank(minRankIndex - 1, 1) ?? int.MaxValue);
                    }

                    Array.Copy(parts, minRankIndex + 2, parts, minRankIndex + 1, parts.Length - minRankIndex - 2);
                    Array.Resize(ref parts, parts.Length - 1);
                }
                else
                {
                    break;
                }
            }

            List<int> outList = new List<int>(parts.Length - 1);
            for (int i = 0; i < parts.Length - 1; i++)
            {
                outList.Add(f(new Range(parts[i].Item1, parts[i + 1].Item1)));
            }
            return outList;
        }

        internal static List<int> BytePairEncode(byte[] piece, Dictionary<byte[], int> ranks)
        {
            if (piece.Length == 1)
            {
                return new List<int>() { ranks[piece] };
            }

            Func<Range, int> f = (range) => {
                var r = piece[range.Start.Value..range.End.Value];
                //var hasKey = ranks.ContainsKey(r);
                return ranks[r];
            };


            return BytePairMerge(piece, ranks, f);
        }


    }
}
