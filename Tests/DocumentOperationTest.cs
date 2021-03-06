﻿using System.Threading.Tasks;
using System.Collections;
using UnityEngine.TestTools;
using NUnit.Framework;
using UnityEngine;
using System.Collections.Generic;
using System;
using E7.Firebase;
using System.Linq;

namespace FirestormTesto
{

    public class DocumentOperationTest : FirestormTestBase
    {
        [UnityTest]
        public IEnumerator GetEmptyDocument()
        {
            yield return T().YieldWait(); async Task T()
            {
                var doc = await TestDocument1.GetSnapshotAsync();
                Assert.That(doc.IsEmpty);
            }
        }

        [UnityTest]
        public IEnumerator SetGetDocumentMultiple()
        {
            yield return T().YieldWait(); async Task T()
            {
                //See what TestDocument1 is in the FirestormTestBase
                var t1 = TestDocument1.SetAsync<TestDataAB>(new TestDataAB { a = 1, b = "x" });
                var t2 = TestDocument2.SetAsync<TestDataAB>(new TestDataAB { a = 2, b = "y" });
                var t3 = TestDocument3.SetAsync<TestDataAB>(new TestDataAB { a = 3, b = "z" });
                await Task.WhenAll(new Task[]{ t1, t2, t3 });

                var i1 = (await TestDocument1.GetSnapshotAsync()).ConvertTo<TestDataAB>();
                var i2 = (await TestDocument2.GetSnapshotAsync()).ConvertTo<TestDataAB>();
                var i3 = (await TestDocument3.GetSnapshotAsync()).ConvertTo<TestDataAB>();

                Assert.That(i1.a, Is.EqualTo(1));
                Assert.That(i1.b, Is.EqualTo("x"));

                Assert.That(i2.a, Is.EqualTo(2));
                Assert.That(i2.b, Is.EqualTo("y"));

                Assert.That(i3.a, Is.EqualTo(3));
                Assert.That(i3.b, Is.EqualTo("z"));
            }
        }

        [UnityTest]
        public IEnumerator SetGetDocumentNested()
        {
            yield return T().YieldWait(); async Task T()
            {
                var t1 = TestDocument2.SetAsync<TestDataAB>(new TestDataAB { a = 1, b = "x" });
                var t2 = TestDocument21.SetAsync<TestDataAB>(new TestDataAB { a = 2, b = "y" });
                var t3 = TestDocument22.SetAsync<TestDataAB>(new TestDataAB { a = 3, b = "z" });
                await Task.WhenAll(new Task[] { t1, t2, t3 });
                var i1 = (await TestDocument2.GetSnapshotAsync()).ConvertTo<TestDataAB>();
                var i2 = (await TestDocument21.GetSnapshotAsync()).ConvertTo<TestDataAB>();
                var i3 = (await TestDocument22.GetSnapshotAsync()).ConvertTo<TestDataAB>();

                Assert.That(i1.a, Is.EqualTo(1));
                Assert.That(i1.b, Is.EqualTo("x"));

                Assert.That(i2.a, Is.EqualTo(2));
                Assert.That(i2.b, Is.EqualTo("y"));

                Assert.That(i3.a, Is.EqualTo(3));
                Assert.That(i3.b, Is.EqualTo("z"));
            }
        }

        [UnityTest]
        public IEnumerator SetAsyncOverwriteNew()
        {
            yield return T().YieldWait(); async Task T()
            {
                await TestDocument1.SetAsync<TestDataAB>(new TestDataAB { a = 31, b = "hi" });
                //Check if the data is there on the server
                var snapshot = await TestDocument1.GetSnapshotAsync();
                Assert.That(snapshot.IsEmpty, Is.Not.True);
                var td = snapshot.ConvertTo<TestDataAB>();
                Assert.That(td.a, Is.EqualTo(31));
                Assert.That(td.b, Is.EqualTo("hi"));
            }
        }

        [UnityTest]
        public IEnumerator SetAsyncOverwriteUpdate()
        {
            yield return T().YieldWait(); async Task T()
            {
                await TestDocument1.SetAsync<TestDataAB>(new TestDataAB { a = 31, b = "hi" });
                await TestDocument1.SetAsync<TestDataAB>(new TestDataAB { a = 55, b = "yo" });

                var snapshot = await TestDocument1.GetSnapshotAsync();
                Assert.That(snapshot.IsEmpty, Is.Not.True);
                var td = snapshot.ConvertTo<TestDataAB>();
                Assert.That(td.a, Is.EqualTo(55));
                Assert.That(td.b, Is.EqualTo("yo"));
            }
        }

        [UnityTest]
        public IEnumerator SetAsyncOrphaned()
        {
            yield return T().YieldWait(); async Task T()
            {
                //Starts at deeper in the tree and it still works.
                await TestDocument21.SetAsync<TestDataAB>(new TestDataAB { a = 31, b = "hi" });

                var snapshot = await TestDocument21.GetSnapshotAsync();
                Assert.That(snapshot.IsEmpty, Is.Not.True);
                var td = snapshot.ConvertTo<TestDataAB>();
                Assert.That(td.a, Is.EqualTo(31));
                Assert.That(td.b, Is.EqualTo("hi"));
            }
        }

        [UnityTest]
        public IEnumerator SetAsyncOverwriteLess()
        {
            yield return T().YieldWait(); async Task T()
            {
                await TestDocument1.SetAsync<TestDataABC>(new TestDataABC { a = 31, b = "hi", c = 55.555 });
                await TestDocument1.SetAsync<TestDataAB>(new TestDataAB { a = 55, b = "yo" });

                var snapshot = await TestDocument1.GetSnapshotAsync();
                Assert.That(snapshot.IsEmpty, Is.Not.True);

                var ab = snapshot.ConvertTo<TestDataAB>();
                Assert.That(ab.a, Is.EqualTo(55));
                Assert.That(ab.b, Is.EqualTo("yo"));

                var abc = snapshot.ConvertTo<TestDataABC>();
                Assert.That(abc.a, Is.EqualTo(55));
                Assert.That(abc.b, Is.EqualTo("yo"));
                Assert.That(abc.c, Is.EqualTo(default(double)), "With overwrite the double field is removed from the server");
            }
        }

        [UnityTest]
        public IEnumerator SetAsyncOverwriteMore()
        {
            yield return T().YieldWait(); async Task T()
            {
                await TestDocument1.SetAsync<TestDataAB>(new TestDataAB { a = 55, b = "yo" });
                await TestDocument1.SetAsync<TestDataABC>(new TestDataABC { a = 31, b = "hi", c = 55.555 });

                var snapshot = await TestDocument1.GetSnapshotAsync();
                Assert.That(snapshot.IsEmpty, Is.Not.True);

                var abc = snapshot.ConvertTo<TestDataABC>();
                Assert.That(abc.a, Is.EqualTo(31));
                Assert.That(abc.b, Is.EqualTo("hi"));
                Assert.That(abc.c, Is.EqualTo(55.555));

                try
                {
                    var ab = snapshot.ConvertTo<TestDataAB>();
                    Assert.Fail("The type must be exact, it cannot be a subset of original data either");
                    // Assert.That(ab.a, Is.EqualTo(31), "It is fine to convert to data type with less fields");
                    // Assert.That(ab.b, Is.EqualTo("hi"), "It is fine to convert to data type with less fields");
                }
                catch(Exception)
                {
                }
            }
        }

        [UnityTest]
        public IEnumerator SetAsyncMergeAllNew()
        {
            yield return T().YieldWait(); async Task T()
            {
                await TestDocument1.UpdateAsync<TestDataAB>(new TestDataAB { a = 31, b = "hi" });
                //Check if the data is there on the server
                var snapshot = await TestDocument1.GetSnapshotAsync();
                Assert.That(snapshot.IsEmpty, Is.Not.True);
                var td = snapshot.ConvertTo<TestDataAB>();
                Assert.That(td.a, Is.EqualTo(31));
                Assert.That(td.b, Is.EqualTo("hi"));
            }
        }

        [UnityTest]
        public IEnumerator SetAsyncMergeAllUpdate()
        {
            yield return T().YieldWait(); async Task T()
            {
                await TestDocument1.UpdateAsync<TestDataABC>(new TestDataABC { a = 31, b = "hi", c = 55.555 });
                await TestDocument1.UpdateAsync<TestDataABC>(new TestDataABC { a = 66, b = "yo", c = 66.666 });

                var snapshot = await TestDocument1.GetSnapshotAsync();
                Assert.That(snapshot.IsEmpty, Is.Not.True);
                var td = snapshot.ConvertTo<TestDataABC>();
                Assert.That(td.a, Is.EqualTo(66));
                Assert.That(td.b, Is.EqualTo("yo"));
                Assert.That(td.c, Is.EqualTo(66.666));
            }
        }

        [UnityTest]
        public IEnumerator SetAsyncMergeAllLess()
        {
            yield return T().YieldWait(); async Task T()
            {
                await TestDocument1.UpdateAsync<TestDataABC>(new TestDataABC { a = 31, b = "hi", c = 55.555 });
                await TestDocument1.UpdateAsync<TestDataAB>(new TestDataAB { a = 66, b = "yo" });

                var snapshot = await TestDocument1.GetSnapshotAsync();
                Assert.That(snapshot.IsEmpty, Is.Not.True);
                var td = snapshot.ConvertTo<TestDataABC>();
                Assert.That(td.a, Is.EqualTo(66));
                Assert.That(td.b, Is.EqualTo("yo"));
                Assert.That(td.c, Is.EqualTo(55.555), "Unlike Overwrite mode the non-intersecting field remain untouched");
            }
        }

        [UnityTest]
        public IEnumerator SetAsyncMergeAllMore()
        {
            yield return T().YieldWait(); async Task T()
            {
                await TestDocument1.UpdateAsync<TestDataAB>(new TestDataAB { a = 31, b = "hi" });
                await TestDocument1.UpdateAsync<TestDataABC>(new TestDataABC { a = 66, b = "yo", c = 55.555 });

                var snapshot = await TestDocument1.GetSnapshotAsync();
                Assert.That(snapshot.IsEmpty, Is.Not.True);
                var td = snapshot.ConvertTo<TestDataABC>();
                Assert.That(td.a, Is.EqualTo(66));
                Assert.That(td.b, Is.EqualTo("yo"));
                Assert.That(td.c, Is.EqualTo(55.555));
            }
        }

        [UnityTest]
        public IEnumerator SetAsyncNestedMergeAll()
        {
            yield return T().YieldWait(); async Task T()
            {
                var startingData = new TestDataNestedAB
                {
                    a = 11,
                    b = "22",
                    c = 33.333,
                    nested = new TestDataAB
                    {
                        a = 44,
                        b = "55",
                    },
                };
                var newData = new TestDataNestedAC
                {
                    a = 11,
                    b = "22",
                    c = 33.333,
                    nested = new TestDataAC
                    {
                        a = 66,
                        c = 77.777,
                    },
                };

                await TestDocument1.UpdateAsync(startingData);
                await TestDocument1.UpdateAsync(newData);
                var snapshot = await TestDocument1.GetSnapshotAsync();

                Assert.That(snapshot.IsEmpty, Is.Not.True);
                var td = snapshot.ConvertTo<TestDataNestedABC>();
                Assert.That(td.a, Is.EqualTo(11));
                Assert.That(td.b, Is.EqualTo("22"));
                Assert.That(td.c, Is.EqualTo(33.333));
                Assert.That(td.nested.a, Is.EqualTo(66));
                Assert.That(td.nested.b, Is.EqualTo("55"), "This inner field untouched because of merge!");
                Assert.That(td.nested.c, Is.EqualTo(77.777));
            }
        }

        [UnityTest]
        public IEnumerator SetAsyncNestedOverwrite()
        {
            yield return T().YieldWait(); async Task T()
            {
                var startingData = new TestDataNestedAB
                {
                    a = 11,
                    b = "22",
                    c = 33.333,
                    nested = new TestDataAB
                    {
                        a = 44,
                        b = "55",
                    },
                };
                var newData = new TestDataNestedAC
                {
                    a = 11,
                    b = "22",
                    c = 33.333,
                    nested = new TestDataAC
                    {
                        a = 66,
                        c = 77.777,
                    },
                };
                
                await TestDocument1.SetAsync(startingData);
                await TestDocument1.SetAsync(newData);
                var snapshot = await TestDocument1.GetSnapshotAsync();

                Assert.That(snapshot.IsEmpty, Is.Not.True);
                var td = snapshot.ConvertTo<TestDataNestedABC>();
                Assert.That(td.a, Is.EqualTo(11));
                Assert.That(td.b, Is.EqualTo("22"));
                Assert.That(td.c, Is.EqualTo(33.333));
                Assert.That(td.nested.a, Is.EqualTo(66));
                Assert.That(td.nested.b, Is.Null, "Overwritten");
                Assert.That(td.nested.c, Is.EqualTo(77.777));
            }
        }

        [UnityTest]
        public IEnumerator ServerTimestampTest()
        {
            yield return T().YieldWait(); async Task T()
            {
                var st = new ServerTimestamper
                {
                    needServerTime = DateTime.MinValue,
                    myOwnTime = DateTime.MinValue,
                };
                await TestDocument1.SetAsync<ServerTimestamper>(st);
                var getBack = (await TestDocument1.GetSnapshotAsync()).ConvertTo<ServerTimestamper>();
                Assert.That((getBack.needServerTime - getBack.myOwnTime).TotalSeconds, Is.Not.Zero, "The same value sent came back different! Wow!");
            }
        }

        public class ServerTimestamper
        {
            [ServerTimestamp] public DateTime needServerTime;
            public DateTime myOwnTime;
        }

        [UnityTest]
        public IEnumerator AllSupportedTypesSurvivalTest()
        {
            yield return T().YieldWait(); async Task T()
            {
                //Test if all value type can go and come back from the server correctly
                var ts = new TestStruct();
                ts.typeTimestamp = DateTime.MinValue;
                ts.typeString = "CYCLONEMAGNUM";
                ts.typeNumber = 55.55;
                ts.typeNumberInt = 555;
                ts.typeBoolean = false;
                ts.typeEnum = TestEnum.B;
                ts.typeBytes = new byte[] { 0x41, 0x42, 0x43 };

                var minPlus2 = DateTime.MinValue + TimeSpan.FromHours(2);
                ts.typeMap = new TestStructInner
                {
                    typeTimestampMap = minPlus2,
                    typeBooleanMap = true,
                    typeNumberMap = 777.88,
                    typeStringMap = "nemii"
                };

                ts.typeArray = new List<object>();
                ts.typeArray.Add("5argonTheGod");
                ts.typeArray.Add(6789);
                DateTime dt = DateTime.MinValue + TimeSpan.FromHours(1);
                ts.typeArray.Add(dt);
                ts.typeArray.Add(true);
                ts.typeArray.Add(11.111);


                await TestDocument1.SetAsync<TestStruct>(ts);
                var getBack = (await TestDocument1.GetSnapshotAsync()).ConvertTo<TestStruct>();

                Assert.That(getBack.typeTimestamp.Year, Is.EqualTo(DateTime.MinValue.Year));
                Assert.That(getBack.typeTimestamp.Month, Is.EqualTo(DateTime.MinValue.Month));
                Assert.That(getBack.typeTimestamp.Day, Is.EqualTo(DateTime.MinValue.Day));
                Assert.That(getBack.typeTimestamp.TimeOfDay.TotalHours, Is.EqualTo(DateTime.MinValue.TimeOfDay.TotalHours));

                Assert.That(getBack.typeMap.typeTimestampMap.Year, Is.EqualTo(minPlus2.Year));
                Assert.That(getBack.typeMap.typeTimestampMap.Month, Is.EqualTo(minPlus2.Month));
                Assert.That(getBack.typeMap.typeTimestampMap.Day, Is.EqualTo(minPlus2.Day));
                Assert.That(getBack.typeMap.typeTimestampMap.TimeOfDay.TotalHours, Is.EqualTo(minPlus2.TimeOfDay.TotalHours));
                Assert.That(getBack.typeMap.typeBooleanMap, Is.EqualTo(true));
                Assert.That(getBack.typeMap.typeNumberMap, Is.EqualTo(777.88));
                Assert.That(getBack.typeMap.typeStringMap, Is.EqualTo("nemii"));

                Assert.That(getBack.typeString, Is.EqualTo("CYCLONEMAGNUM"));
                Assert.That(getBack.typeNumber, Is.EqualTo(55.55));
                Assert.That(getBack.typeNumberInt, Is.EqualTo(555));
                Assert.That(getBack.typeBoolean, Is.EqualTo(false));
                Assert.That(getBack.typeEnum, Is.EqualTo(TestEnum.B));
                Assert.That(getBack.typeBytes, Is.EquivalentTo(new byte[] { 0x41, 0x42, 0x43 }));

                Assert.That((string)getBack.typeArray[0], Is.EqualTo("5argonTheGod"));
                Assert.That(getBack.typeArray[1], Is.EqualTo(6789));
                var timeInArray = DateTime.Parse((string)getBack.typeArray[2]).ToUniversalTime();

                Assert.That(timeInArray.Year, Is.EqualTo(dt.Year));
                Assert.That(timeInArray.Month, Is.EqualTo(dt.Month));
                Assert.That(timeInArray.Day, Is.EqualTo(dt.Day));
                Assert.That(timeInArray.TimeOfDay.TotalHours, Is.EqualTo(dt.TimeOfDay.TotalHours));

                Assert.That((bool)getBack.typeArray[3], Is.EqualTo(true));
                Assert.That((double)getBack.typeArray[4], Is.EqualTo(11.111));

            }
        }

        protected class TestArray
        {
            public List<object> array1; 
            public List<object> array2; 
            public List<object> array3; 
        }
        
        [UnityTest]
        public IEnumerator ArrayAppend()
        {
            yield return T().YieldWait(); async Task T()
            {
                var ta = new TestArray{
                    array1 = new List<object> { 11 , "hey" },
                    array2 = new List<object> { 44 , "yo" },
                    array3 = new List<object> { 66 , "wow" },
                };
                await TestDocument1.SetAsync<TestArray>(ta);
                await TestDocument1.ArrayAppendAsync("array2", new object[] { 55, 555 });
                var returned = (await TestDocument1.GetSnapshotAsync()).ConvertTo<TestArray>();

                Assert.That(returned.array1.Count, Is.EqualTo(2));
                Assert.That(returned.array2.Count, Is.EqualTo(4));
                Assert.That(returned.array3.Count, Is.EqualTo(2));

                Assert.That(returned.array2[0], Is.EqualTo(44));
                Assert.That(returned.array2[1], Is.EqualTo("yo"));
                Assert.That(returned.array2[2], Is.EqualTo(55));
                Assert.That(returned.array2[3], Is.EqualTo(555));

                await TestDocument1.ArrayAppendAsync("array2", new object[] { 666 });
                returned = (await TestDocument1.GetSnapshotAsync()).ConvertTo<TestArray>();

                Assert.That(returned.array2.Count, Is.EqualTo(5));
                Assert.That(returned.array2[3], Is.EqualTo(555));
                Assert.That(returned.array2[4], Is.EqualTo(666));

                await TestDocument1.ArrayAppendAsync("array2", new object[] { 666 });
                returned = (await TestDocument1.GetSnapshotAsync()).ConvertTo<TestArray>();

                Assert.That(returned.array2.Count, Is.EqualTo(5));
                Assert.That(returned.array2[3], Is.EqualTo(555));
                Assert.That(returned.array2[4], Is.EqualTo(666), "Array append is not able to add duplicate element (but array data structure itself can hold dup element.");
            }
        }

        [UnityTest]
        public IEnumerator ArrayRemove()
        {
            yield return T().YieldWait(); async Task T()
            {
                var ta = new TestArray{
                    array1 = new List<object> { 11 , "hey" },
                    array2 = new List<object> { 44, "yo", 55, 66, 77, 88 },
                    array3 = new List<object> { 66 , "wow" },
                };
                await TestDocument1.SetAsync<TestArray>(ta);
                await TestDocument1.ArrayRemoveAsync("array2", new object[] { 66, 77 });
                var returned = (await TestDocument1.GetSnapshotAsync()).ConvertTo<TestArray>();

                Assert.That(returned.array1.Count, Is.EqualTo(2));
                Assert.That(returned.array2.Count, Is.EqualTo(4));
                Assert.That(returned.array3.Count, Is.EqualTo(2));

                Assert.That(returned.array2[0], Is.EqualTo(44));
                Assert.That(returned.array2[1], Is.EqualTo("yo"));
                Assert.That(returned.array2[2], Is.EqualTo(55));
                Assert.That(returned.array2[3], Is.EqualTo(88));

                await TestDocument1.ArrayRemoveAsync("array2", new object[] { 555555 });
                returned = (await TestDocument1.GetSnapshotAsync()).ConvertTo<TestArray>();

                Assert.That(returned.array2.Count, Is.EqualTo(4), "Removing something that is not in array cause nothing.");
                Assert.That(returned.array2[0], Is.EqualTo(44));
                Assert.That(returned.array2[1], Is.EqualTo("yo"));
                Assert.That(returned.array2[2], Is.EqualTo(55));
                Assert.That(returned.array2[3], Is.EqualTo(88));
            }
        }

    }
}
