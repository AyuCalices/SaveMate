using NUnit.Framework;
using SaveLoadCore;

namespace Tests
{
    public class EdgeCaseTests
    {
        [Test]
        public void EmptyDataPass()
        {
            DataContainer serialiseData = new DataContainer();
        
            SaveLoadManager.Save(serialiseData);

            DataContainer deserializeData = SaveLoadManager.Load();
        
            for (var i = 0; i < serialiseData.Data.Count; i++)
            {
                Assert.AreEqual(serialiseData.Data[i].Item1, deserializeData.Data[i].Item1);
                Assert.AreEqual(serialiseData.Data[i].Item2, deserializeData.Data[i].Item2);
            }
        }
    }
}
