﻿// Copyright (c) 2014 Silicon Studio Corp. (http://siliconstudio.co.jp)
// This file is distributed under GPL v3. See LICENSE.md for details.

using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using SiliconStudio.Core;
using SiliconStudio.Quantum.Contents;
using SiliconStudio.Quantum.References;

namespace SiliconStudio.Quantum.Tests.Obsolete
{
    [TestFixture(Ignore = "Obsolete")]
    public class ObsoleteTestLists
    {
        #region Test class definitions
        public class SimpleClass
        {
            [DataMember(1)]
            public int FirstValue { get; set; }

            [DataMember(2)]
            public int SecondValue { get; set; }
        }

        public struct SimpleStruct
        {
            [DataMember(1)]
            public int FirstValue { get; set; }

            [DataMember(2)]
            public int SecondValue { get; set; }
        }

        public struct NestedStruct
        {
            [DataMember(1)]
            public SimpleStruct Struct { get; set; }
        }

        public class ClassWithLists
        {
            public ClassWithLists()
            {
                IntList = new List<int> { 1, 2, 3 };
                ClassList = new List<SimpleClass> { new SimpleClass() };
                SimpleStructList = new List<SimpleStruct> { new SimpleStruct(), new SimpleStruct() };
                NestedStructList = new List<NestedStruct> { new NestedStruct(), new NestedStruct() };
                ListOfSimpleStructLists = new List<List<SimpleStruct>> { new List<SimpleStruct> { new SimpleStruct() }, new List<SimpleStruct> { new SimpleStruct() } };
                ListOfNestedStructLists = new List<List<NestedStruct>> { new List<NestedStruct> { new NestedStruct() }, new List<NestedStruct> { new NestedStruct() } };
            }

            [DataMember(1)]
            public List<int> IntList { get; }

            [DataMember(2)]
            public List<SimpleClass> ClassList { get; }

            [DataMember(3)]
            public List<SimpleStruct> SimpleStructList { get; }

            [DataMember(4)]
            public List<NestedStruct> NestedStructList { get; }

            [DataMember(5)]
            public List<List<SimpleStruct>> ListOfSimpleStructLists { get; }

            [DataMember(6)]
            public List<List<NestedStruct>> ListOfNestedStructLists { get; }
        }

        public class ClassWithNullLists
        {
            [DataMember(1)]
            public List<int> IntList { get; set; }

            [DataMember(2)]
            public List<SimpleClass> ClassList { get; set; }

            [DataMember(3)]
            public List<SimpleStruct> SimpleStructList { get; set; }

            [DataMember(4)]
            public List<NestedStruct> NestedStructList { get; set; }

            [DataMember(5)]
            public List<List<SimpleStruct>> ListOfSimpleStructLists { get; set; }

            [DataMember(6)]
            public List<List<NestedStruct>> ListOfNestedStructLists { get; set; }
        }

        #endregion Test class definitions

        [Test]
        public void TestConstruction()
        {
            var obj = new ClassWithLists();
            var container = new NodeContainer();
            var model = container.GetOrCreateNode(obj);

            Assert.That(model["IntList"].Value, Is.SameAs(obj.IntList));
            Assert.That(model["IntList"].IsReference, Is.False);
            Assert.That(model["ClassList"].Value, Is.SameAs(obj.ClassList));
            //Assert.That(model["ClassList"].Reference, Is.AssignableFrom(typeof(ReferenceEnumerable)));
            Assert.That(model["SimpleStructList"].Value, Is.SameAs(obj.SimpleStructList));
            //Assert.That(model["SimpleStructList"].Reference, Is.AssignableFrom(typeof(ReferenceEnumerable)));
            Assert.That(model["NestedStructList"].Value, Is.SameAs(obj.NestedStructList));
            //Assert.That(model["NestedStructList"].Reference, Is.AssignableFrom(typeof(ReferenceEnumerable)));
            Assert.That(model["ListOfSimpleStructLists"].Value, Is.SameAs(obj.ListOfSimpleStructLists));
            //Assert.That(model["ListOfSimpleStructLists"].Reference, Is.AssignableFrom(typeof(ReferenceEnumerable)));
            foreach (var reference in model["ListOfSimpleStructLists"].ItemReferences)
            {
                Assert.That(reference, Is.AssignableFrom(typeof(ObjectReference)));
            }
            Assert.That(model["ListOfNestedStructLists"].Value, Is.SameAs(obj.ListOfNestedStructLists));
            //Assert.That(model["ListOfNestedStructLists"].Reference, Is.AssignableFrom(typeof(ReferenceEnumerable)));
            foreach (var reference in model["ListOfNestedStructLists"].ItemReferences)
            {
                Assert.That(reference, Is.AssignableFrom(typeof(ObjectReference)));
            }

            Assert.That(container.GetNode(obj.ClassList[0]), !Is.Null);
        }

        [Test]
        public void TestNullLists()
        {
            var obj = new ClassWithNullLists();
            var container = new NodeContainer();
            var model = container.GetOrCreateNode(obj);
        }

        [Test]
        public void TestPrimitiveItemUpdate()
        {
            var obj = new ClassWithLists();
            var container = new NodeContainer();
            var model = container.GetOrCreateNode(obj);
            Console.WriteLine(model.PrintHierarchy());
            ((List<int>)model["IntList"].Value)[1] = 42;
            ((List<int>)model["IntList"].Value).Add(26);
            Assert.That(obj.IntList.Count, Is.EqualTo(4));
            Assert.That(obj.IntList[1], Is.EqualTo(42));
            Assert.That(obj.IntList[3], Is.EqualTo(26));
        }

        [Test]
        public void TestObjectItemUpdate()
        {
            var obj = new ClassWithLists();
            var container = new NodeContainer();
            var model = container.GetOrCreateNode(obj);
            //var objRef = ((ReferenceEnumerable)model["ClassList"].Reference).First();
            //objRef.TargetNode["SecondValue"].Update(32);
            Assert.That(obj.ClassList[0].SecondValue, Is.EqualTo(32));
        }

        [Test]
        public void TestStructItemUpdate()
        {
            var obj = new ClassWithLists();
            var container = new NodeContainer();
            var model = container.GetOrCreateNode(obj);
            //var objRef = ((ReferenceEnumerable)model["SimpleStructList"].Reference).First();
            //objRef.TargetNode["SecondValue"].Update(32);
            Assert.That(obj.SimpleStructList[0].SecondValue, Is.EqualTo(32));
        }

        [Test]
        public void TestNestedStructItemUpdate()
        {
            var obj = new ClassWithLists();
            var container = new NodeContainer();
            var model = container.GetOrCreateNode(obj);
            //var objRef = ((ReferenceEnumerable)model["NestedStructList"].Reference).First();
            //var structNode = container.GetNode(((ObjectReference)objRef.TargetNode["Struct"].Reference).TargetGuid);
            //structNode["SecondValue"].Update(32);
            Assert.That(obj.NestedStructList[0].Struct.SecondValue, Is.EqualTo(32));
            //var visitor = new ModelConsistencyCheckVisitor(container.NodeBuilder);
            //visitor.Check((GraphNode)model, obj, typeof(ClassWithLists), true);
        }

        [Test]
        public void TestListOfSimpleStructListsUpdate()
        {
            var obj = new ClassWithLists();
            var container = new NodeContainer();
            var model = container.GetOrCreateNode(obj);
            //var listRef = ((ReferenceEnumerable)model["ListOfSimpleStructLists"].Reference).Last();
            //var objRef = ((ReferenceEnumerable)listRef.TargetNode.Reference).Last();
            //objRef.TargetNode["SecondValue"].Update(32);
            Assert.That(obj.ListOfSimpleStructLists[1][0].SecondValue, Is.EqualTo(32));
        }

        [Test]
        public void TestListOfNestedStructListsUpdate()
        {
            var obj = new ClassWithLists();
            var container = new NodeContainer();
            var model = container.GetOrCreateNode(obj);
            //var listRef = ((ReferenceEnumerable)model["ListOfNestedStructLists"].Reference).Last();
            //var objRef = ((ReferenceEnumerable)listRef.TargetNode.Reference).Last();
            //var structNode = container.GetNode(((ObjectReference)objRef.TargetNode["Struct"].Reference).TargetGuid);
            //structNode["SecondValue"].Update(32);
            Assert.That(obj.ListOfNestedStructLists[1][0].Struct.SecondValue, Is.EqualTo(32));
        }
    }
}
