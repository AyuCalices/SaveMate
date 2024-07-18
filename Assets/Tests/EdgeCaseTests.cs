using NUnit.Framework;
using SaveLoadCore;
using SaveLoadSystem.Core;

namespace Tests
{
    public class EdgeCaseTests
    {
        [Test]
        public void EmptyDataPass()
        {
            CoreTests.DataContainer serialiseData = new CoreTests.DataContainer();
        
            SaveLoadManager.Save(serialiseData);

            CoreTests.DataContainer deserializeData = SaveLoadManager.Load<CoreTests.DataContainer>();
        
            for (var i = 0; i < serialiseData.Data.Count; i++)
            {
                Assert.AreEqual(serialiseData.Data[i].Item1, deserializeData.Data[i].Item1);
                Assert.AreEqual(serialiseData.Data[i].Item2, deserializeData.Data[i].Item2);
            }
        }
    }
}
