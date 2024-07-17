using System;
using System.Collections.Generic;
using UnityEngine;

namespace SaveLoadCore
{
    public class CircleReference : MonoBehaviour
    {
        [SavableMember] private CircleObject Test { get; set; }

        private void Awake()
        {
            CircleObject circleElement1;
            if (Test == null)
            {
                circleElement1 = new CircleObject();
                CircleObject circleElement2 = new CircleObject();
                
                circleElement1.circleElement = circleElement2;
                circleElement2.circleElement = circleElement1;
            }
            else
            {
                return;
            }
            
            foreach (CircleReference savableTest in GetComponents<CircleReference>())
            {
                savableTest.Test = circleElement1;
            }
        }

        [ContextMenu("Remove")]
        public void Remove()
        {
            Test = null;
        }
    }

    [Serializable]
    public class CircleObject
    {
        [SavableMember] public CircleObject circleElement;
    }
}
