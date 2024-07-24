using NUnit.Framework;
using SaveLoadSystem.Core;
using SaveLoadSystem.Utility;

namespace Tests
{
    public class EdgeCaseTests
    {
        [Test]
        public void EmptyDataPass()
        {
            CoreTests.DataContainer serialiseData = new CoreTests.DataContainer();
        
            SaveLoadUtility.Save(serialiseData);

            CoreTests.DataContainer deserializeData = SaveLoadUtility.LoadSecure<CoreTests.DataContainer>();
        
            for (var i = 0; i < serialiseData.Data.Count; i++)
            {
                Assert.AreEqual(serialiseData.Data[i].Item1, deserializeData.Data[i].Item1);
                Assert.AreEqual(serialiseData.Data[i].Item2, deserializeData.Data[i].Item2);
            }
        }
    }
}
