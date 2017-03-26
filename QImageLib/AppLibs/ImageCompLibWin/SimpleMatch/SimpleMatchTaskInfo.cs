using System.Collections.Generic;
using System.Linq;

namespace ImageCompLibWin.SimpleMatch
{
    public class SimpleMatchTaskInfo
    {
        public SimpleMatchTaskInfo(int index1, int index2)
        {
            Index1 = index1;
            Index2 = index2;
        }

        public int Index1 { get; }
        public int Index2 { get; }

        public static IEnumerable<SimpleMatchTaskInfo> GenerateTaskSequence(int n)
        {
            var delist = new LinkedList<int>();
            yield return new SimpleMatchTaskInfo(0, 1);
            delist.AddLast(0);
            delist.AddLast(1);
            var forward = false;
            for (var i = 2; i < n; i++)
            {
                var js = forward ? delist : delist.Reverse();
                foreach (var j in js)
                {
                    yield return new SimpleMatchTaskInfo(j, i);
                }
                if (forward)
                {
                    delist.AddLast(i);
                }
                else
                {
                    delist.AddFirst(i);
                }
                forward = !forward;
            }
        }

        public static IEnumerable<SimpleMatchTaskInfo> GenerateTaskSequenceLR(int l, int r)
        {
            var forward = true;
            for (var i = 0; i < l; i++)
            {
                if (forward)
                {
                    for (var j = 0; j < r; j++)
                    {
                        yield return new SimpleMatchTaskInfo(i, j);
                    }
                }
                else
                {
                    for (var j = r-1; j>=0; j--)
                    {
                        yield return new SimpleMatchTaskInfo(i, j);
                    }
                }
                forward = !forward;
            }
        }
    }
}
