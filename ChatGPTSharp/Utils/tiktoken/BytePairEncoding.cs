using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace ChatGPTSharp.Utils.tiktoken
{

    public class BytePairEncoding
    {
        static List<T> BytePairMerge<T>(byte[] piece, Dictionary<byte[], int> ranks, Func<Range, T> f)
        {
            var parts = Enumerable.Range(0, piece.Length + 1).Select(i => (i, int.MaxValue)).ToList();
            int? GetRank(int startIdx, int skip = 0)
            {
                if (startIdx + skip + 2 < parts.Count)
                {
                    var slice = piece[parts[startIdx].Item1..parts[startIdx + skip + 2].Item1];
                    if (ranks.TryGetValue(slice, out var rank))
                    {
                        return rank;
                    }
                }
                return null;
            }
            for (int i = 0; i < parts.Count - 2; i++)
            {
                var rank = GetRank(i);
                if (rank != null)
                {
                    System.Diagnostics.Debug.Assert(rank.Value != int.MaxValue);
                    parts[i] = (parts[i].Item1, rank.Value);
                }
            }
            while (parts.Count > 1)
            {
                var minRank = (int.MaxValue, 0);
                for (int i = 0; i < parts.Count - 1; i++)
                {
                    if (parts[i].Item2 < minRank.Item1)
                    {
                        minRank = (parts[i].Item2, i);
                    }
                }
                if (minRank.Item1 != int.MaxValue)
                {
                    int i = minRank.Item2;
                    parts[i] = (parts[i].Item1, GetRank(i, 1) ?? int.MaxValue);
                    if (i > 0)
                    {
                        parts[i - 1] = (parts[i - 1].Item1, GetRank(i - 1, 1) ?? int.MaxValue);
                    }
                    parts.RemoveAt(i + 1);
                }
                else
                {
                    break;
                }
            }
            var outList = new List<T>(parts.Count - 1);
            for (int i = 0; i < parts.Count - 1; i++)
            {
                outList.Add(f(parts[i].Item1..parts[i + 1].Item1));
            }
            return outList;
        }

        public static List<int> BytePairEncode(byte[] piece, Dictionary<byte[], int> ranks)
        {
            if (piece.Length == 1)
            {
                return new List<int> { ranks[piece] };
            }
            return BytePairMerge(piece, ranks, p => ranks[piece[p.Start..p.End]]);
        }

        public static List<byte[]> BytePairSplit(byte[] piece, Dictionary<byte[], int> ranks)
        {
            if (piece.Length == 1)
            {
                return new List<byte[]> { piece };
            }
            return BytePairMerge(piece, ranks, p => piece[p.Start..p.End]);
        }



        //private static List<(int, int)> BytePairMerge<T>(byte[] piece, Dictionary<byte[], int> ranks, Func<Range, T> f)
        //{
        //    var parts = Enumerable.Range(0, piece.Length + 1).Select(i => (i, int.MaxValue)).ToList();

        //    int? GetRank(int startIdx, int skip = 0)
        //    {
        //        if (startIdx + skip + 2 < parts.Count)
        //        {
        //            return ranks.TryGetValue(piece.Skip(parts[startIdx].Item1).Take(parts[startIdx + skip + 2].Item1 - parts[startIdx].Item1).ToArray(), out int rank)
        //                ? rank
        //                : (int?)null;
        //        }
        //        else
        //        {
        //            return null;
        //        }
        //    }

        //    for (int i = 0; i < parts.Count - 2; i++)
        //    {
        //        int? rank = GetRank(i);
        //        if (rank.HasValue)
        //        {
        //            Debug.Assert(rank.Value != int.MaxValue);
        //            parts[i] = (parts[i].Item1, rank.Value);
        //        }
        //    }

        //    while (parts.Count > 1)
        //    {
        //        if (parts.Count == 1)
        //        {
        //            break;
        //        }

        //        (int, int) minRank = (int.MaxValue, 0);
        //        for (int i = 0; i < parts.Count - 1; i++)
        //        {
        //            if (parts[i].Item2 < minRank.Item1)
        //            {
        //                minRank = (parts[i].Item2, i);
        //            }
        //        }

        //        if (minRank.Item1 != int.MaxValue)
        //        {
        //            int i = minRank.Item2;
        //            parts[i] = (parts[i].Item1, GetRank(i, 1) ?? int.MaxValue);
        //            if (i > 0)
        //            {
        //                parts[i - 1] = (parts[i - 1].Item1, GetRank(i - 1, 1) ?? int.MaxValue);
        //            }

        //            parts.RemoveAt(i + 1);
        //        }
        //        else
        //        {
        //            break;
        //        }
        //    }

        //    var outList = new List<T>(parts.Count - 1);
        //    for (int i = 0; i < parts.Count - 1; i++)
        //    {
        //        outList.Add(f(new Range(parts[i].Item1, parts[i + 1].Item1)));
        //    }
        //    return parts;
        //}

        //public static List<int> BytePairEncode(byte[] piece, Dictionary<byte[], int> ranks)
        //{
        //    if (piece.Length == 1)
        //    {
        //        return new List<int> { ranks[piece] };
        //    }
        //    return BytePairMerge(piece, ranks, p => ranks[piece[p.Start..p.End]]);
        //}

        //public static List<byte[]> BytePairSplit(byte[] piece, Dictionary<byte[], int> ranks)
        //{
        //    if (piece.Length == 1)
        //    {
        //        return new List<byte[]> { piece };
        //    }
        //    return BytePairMerge(piece, ranks, p => piece[p.Start..p.End]);
        //}
    }

}
