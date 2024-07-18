using System;
using System.Collections.Generic;
using NUnit.Framework;
using SaveLoadSystem.Core;
using SaveLoadSystem.Core.Attributes;
using SaveLoadSystem.Utility;
using UnityEngine;

namespace Tests
{
    public class CoreTests
    {
        [Serializable]
        public sealed class DataContainer
        {
            //TODO: what if the type doesnt match the object?
            public List<(Type, object)> Data { get; set; } = new List<(Type, object)>();
        }
        
        [Serializable]
        public sealed class DictionaryTest
        {
            public Dictionary<string, object> Data = new ();
        }

        private DictionaryTest DictionaryTestElement => new DictionaryTest()
        {
            Data = new Dictionary<string, object>
            {
                { "string", "string" }
            }
        };
        
        //nint: Avoid using nint and nuint for saved data due to platform dependency.
        private DataContainer UnmanagedData => new DataContainer
        {
            Data = new List<(Type, object)>
            {
                (typeof(sbyte), -128),
                (typeof(byte), 255),
                (typeof(short), -32768),
                (typeof(ushort), 65535),
                (typeof(int), -2147483648),
                (typeof(uint), 4294967295),
                (typeof(long), -9223372036854775808L),
                (typeof(ulong), 18446744073709551615UL),
                (typeof(char), 'A'),
                (typeof(float), 3.14f),
                (typeof(double), 3.141592653589793),
                (typeof(decimal), 79228162514264337593543950335M),
                (typeof(bool), true)
            }
        };

        [Serializable]
        public class OtherReference : IEquatable<OtherReference>
        {
            public int i = 5;

            public bool Equals(OtherReference other)
            {
                if (ReferenceEquals(null, other)) return false;
                if (ReferenceEquals(this, other)) return true;
                return i == other.i;
            }

            public override bool Equals(object obj)
            {
                if (ReferenceEquals(null, obj)) return false;
                if (ReferenceEquals(this, obj)) return true;
                if (obj.GetType() != this.GetType()) return false;
                return Equals((OtherReference)obj);
            }

            public override int GetHashCode()
            {
                return i;
            }
        }
        
        [Serializable]
        public struct OtherStruct : IEquatable<OtherStruct>
        {
            public int foo;

            public OtherStruct(int fo)
            {
                foo = fo;
            }

            public bool Equals(OtherStruct other)
            {
                return foo == other.foo;
            }

            public override bool Equals(object obj)
            {
                return obj is OtherStruct other && Equals(other);
            }

            public override int GetHashCode()
            {
                return foo;
            }
        }
        
        [Serializable]
        public class ReferenceTest : IEquatable<ReferenceTest>
        {
            public string randomString;
            public OtherReference otherReference;
            public OtherStruct otherStruct;
            public int i = 10;
            
            private const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";

            public ReferenceTest()
            {
                randomString = GenerateRandomString(5);
                otherReference = new OtherReference();
                otherStruct = new OtherStruct(i);
            }
            
            // Method to generate a random string of a given length
            private string GenerateRandomString(int length)
            {
                // Create an array to hold the generated characters
                char[] stringChars = new char[length];

                // Create an instance of the Random class
                System.Random random = new System.Random();

                // Loop to select random characters from the chars array
                for (int i = 0; i < stringChars.Length; i++)
                {
                    stringChars[i] = chars[random.Next(chars.Length)];
                }

                // Convert the character array to a string and return it
                return new string(stringChars);
            }

            public bool Equals(ReferenceTest other)
            {
                if (ReferenceEquals(null, other)) return false;
                if (ReferenceEquals(this, other)) return true;
                return randomString == other.randomString && Equals(otherReference, other.otherReference) && otherStruct.Equals(other.otherStruct) && i == other.i;
            }

            public override bool Equals(object obj)
            {
                if (ReferenceEquals(null, obj)) return false;
                if (ReferenceEquals(this, obj)) return true;
                if (obj.GetType() != this.GetType()) return false;
                return Equals((ReferenceTest)obj);
            }

            public override int GetHashCode()
            {
                return HashCode.Combine(randomString, otherReference, otherStruct, i);
            }
        }

        private bool CanBeAssigned(Type type, object value)
        {
            try
            {
                // Attempt to convert the value to the specified type
                var convertedValue = Convert.ChangeType(value, type);
                return true;
            }
            catch (Exception)
            {
                Debug.LogError($"Failed to assign {type} to {value}!");
                // If an exception occurs, the conversion is not possible
                return false;
            }
        }
        
        [Test]
        public void DictionaryPass()
        {
            DictionaryTest serialiseData = DictionaryTestElement;
        
            SaveLoadManager.Save(serialiseData);

            DictionaryTest deserializeData = SaveLoadManager.Load<DictionaryTest>();
            
            Assert.IsTrue(deserializeData.Data.Count == serialiseData.Data.Count);
        }
        
        [Test]
        public void UnmanagedIntegrityPass()
        {
            DataContainer serialiseData = UnmanagedData;
        
            SaveLoadManager.Save(serialiseData);

            DataContainer deserializeData = SaveLoadManager.Load<DataContainer>();
        
            for (var i = 0; i < serialiseData.Data.Count; i++)
            {
                Assert.IsTrue(CanBeAssigned(serialiseData.Data[i].Item1, serialiseData.Data[i].Item2));
                Assert.AreEqual(serialiseData.Data[i].Item1, deserializeData.Data[i].Item1);
                
                Assert.IsTrue(CanBeAssigned(deserializeData.Data[i].Item1, deserializeData.Data[i].Item2));
                Assert.AreEqual(serialiseData.Data[i].Item2, deserializeData.Data[i].Item2);
            }
        }
        
        [Test]
        public void ManagedReferencePass()
        {
            DataContainer serialiseData = new DataContainer
            {
                Data = new List<(Type, object)>
                {
                    (typeof(ReferenceTest), new ReferenceTest())
                }
            };
        
            SaveLoadManager.Save(serialiseData);

            DataContainer deserializeData = SaveLoadManager.Load<DataContainer>();
        
            for (var i = 0; i < serialiseData.Data.Count; i++)
            {
                Assert.IsTrue(CanBeAssigned(serialiseData.Data[i].Item1, serialiseData.Data[i].Item2));
                Assert.AreEqual(serialiseData.Data[i].Item1, deserializeData.Data[i].Item1);
                
                Assert.IsTrue(CanBeAssigned(deserializeData.Data[i].Item1, deserializeData.Data[i].Item2));
                Assert.AreEqual(serialiseData.Data[i].Item2, deserializeData.Data[i].Item2);
            }
        }

        public class AttributeTestType
        {
            [Savable] private string _privateString = "_privateString";
            [Savable] protected string protectedString = "protectedString";
            [Savable] public string publicString = "publicString";
        }
        
        [Test]
        public void SavableAttributeCollectorPass()
        {
            List<string> collectedSavableList = new List<string>();
            ReflectionUtility.GetFieldsAndPropertiesWithAttributeOnType<SavableAttribute>(typeof(AttributeTestType), ref collectedSavableList);
            
            Assert.Contains("_privateString", collectedSavableList, "Doesnt contain private string");
            Assert.Contains("protectedString", collectedSavableList, "Doesnt contain protected string");
            Assert.Contains("publicString", collectedSavableList, "Doesnt contain public string");
        }
    }
}
