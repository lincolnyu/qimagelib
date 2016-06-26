using ImageCompLibWin.SimpleMatch;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;

namespace ImageCompLibWin.Tests
{
    [TestClass]
    public class SimpleMatchTaskInfoTest
    {
        [TestMethod]
        public void SequenceGenerationTest()
        {
            var expected6 = new SimpleMatchTaskInfo[] {
                new SimpleMatchTaskInfo(0,1),
                new SimpleMatchTaskInfo(1,2),
                new SimpleMatchTaskInfo(0,2),
                new SimpleMatchTaskInfo(2,3),
                new SimpleMatchTaskInfo(0,3),
                new SimpleMatchTaskInfo(1,3),
                new SimpleMatchTaskInfo(3,4),
                new SimpleMatchTaskInfo(1,4),
                new SimpleMatchTaskInfo(0,4),
                new SimpleMatchTaskInfo(2,4),
                new SimpleMatchTaskInfo(4,5),
                new SimpleMatchTaskInfo(2,5),
                new SimpleMatchTaskInfo(0,5),
                new SimpleMatchTaskInfo(1,5),
                new SimpleMatchTaskInfo(3,5),
            };

            var actual = SimpleMatchTaskInfo.GenerateTaskSequence(6).ToArray();
            Assert.AreEqual(expected6.Length, actual.Length);
            for (var i = 0; i < expected6.Length; i++)
            {
                Assert.AreEqual(expected6[i].Index1, actual[i].Index1);
                Assert.AreEqual(expected6[i].Index2, actual[i].Index2);
            }
        }
    }
}
