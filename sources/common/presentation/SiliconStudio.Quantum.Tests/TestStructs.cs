﻿using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using SiliconStudio.Quantum.Contents;

namespace SiliconStudio.Quantum.Tests
{
    [TestFixture]
    public class TestStructs
    {
        public struct SimpleStruct
        {
            public string Name { get; set; }
            public int Value { get; set; }
            public override string ToString() => $"{{SimpleStruct: ({Value}), {Name}}}";
        }

        public struct StructWithCollection
        {
            public List<string> Strings { get; set; }
            public override string ToString() => $"{{StructWithCollection: ({Strings})}}";
        }

        public struct FirstNestingStruct
        {
            public SecondNestingStruct Struct1 { get; set; }
            public override string ToString() => $"{{FirstNestingStruct: {Struct1}}}";
        }

        public struct SecondNestingStruct
        {
            public SimpleStruct Struct2 { get; set; }
            public override string ToString() => $"{{SecondNestingStruct: {Struct2}}}";
        }

        public class StructContainer
        {
            public SimpleStruct Struct { get; set; }
            public override string ToString() => $"{{StructContainer: {Struct}}}";
        }

        public class StructWithCollectionContainer
        {
            public StructWithCollection Struct { get; set; }
            public override string ToString() => $"{{StructWithCollectionContainer: {Struct}}}";
        }

        public class NestingStructContainer
        {
            public FirstNestingStruct Struct { get; set; }
            public override string ToString() => $"{{NestingStructContainer: {Struct}}}";
        }

        [Test]
        public void TestSimpleStruct()
        {
            var nodeContainer = new NodeContainer();
            var container = new StructContainer { Struct = new SimpleStruct { Name = "Test", Value = 1 } };
            var containerNode = nodeContainer.GetOrCreateNode(container);
            Helper.TestNonCollectionObjectNode(containerNode, container, 1);

            var memberNode = containerNode[nameof(StructContainer.Struct)];
            Helper.TestMemberNode(containerNode, memberNode, container, container.Struct, nameof(StructContainer.Struct), true);
            Helper.TestNonNullObjectReference(memberNode.TargetReference, container.Struct, false);
            var structMember1Node = memberNode.Target[nameof(SimpleStruct.Name)];
            Helper.TestMemberNode(memberNode.Target, structMember1Node, container.Struct, container.Struct.Name, nameof(SimpleStruct.Name), false);
            var structMember2Node = memberNode.Target[nameof(SimpleStruct.Value)];
            Helper.TestMemberNode(memberNode.Target, structMember2Node, container.Struct, container.Struct.Value, nameof(SimpleStruct.Value), false);
        }

        [Test]
        public void TestSimpleStructUpdate()
        {
            var nodeContainer = new NodeContainer();
            var container = new StructContainer { Struct = new SimpleStruct { Name = "Test", Value = 1 } };
            var containerNode = nodeContainer.GetOrCreateNode(container);
            var memberNode = containerNode[nameof(StructContainer.Struct)];
            var targetNode = memberNode.Target;
            var structMember1Node = targetNode[nameof(SimpleStruct.Name)];
            var structMember2Node = targetNode[nameof(SimpleStruct.Value)];
            memberNode.Update(new SimpleStruct { Name = "Test2", Value = 2 });

            Assert.AreEqual("Test2", container.Struct.Name);
            Assert.AreEqual(2, container.Struct.Value);
            Assert.AreEqual(targetNode, memberNode.Target);
            Assert.AreEqual(structMember1Node, memberNode.Target[nameof(SimpleStruct.Name)]);
            Assert.AreEqual(structMember2Node, memberNode.Target[nameof(SimpleStruct.Value)]);
            structMember1Node = memberNode.Target[nameof(SimpleStruct.Name)];
            Helper.TestMemberNode(memberNode.Target, structMember1Node, container.Struct, container.Struct.Name, nameof(SimpleStruct.Name), false);
            structMember2Node = memberNode.Target[nameof(SimpleStruct.Value)];
            Helper.TestMemberNode(memberNode.Target, structMember2Node, container.Struct, container.Struct.Value, nameof(SimpleStruct.Value), false);
        }

        [Test]
        public void TestSimpleStructMemberUpdate()
        {
            var nodeContainer = new NodeContainer();
            var container = new StructContainer { Struct = new SimpleStruct { Name = "Test", Value = 1 } };
            var containerNode = nodeContainer.GetOrCreateNode(container);
            var memberNode = containerNode[nameof(StructContainer.Struct)];
            var targetNode = memberNode.Target;
            var structMember1Node = targetNode[nameof(SimpleStruct.Name)];
            var structMember2Node = targetNode[nameof(SimpleStruct.Value)];

            Helper.TestNonCollectionObjectNode(containerNode, container, 1);
            Helper.TestMemberNode(containerNode, memberNode, container, container.Struct, nameof(StructContainer.Struct), true);
            Helper.TestNonNullObjectReference(memberNode.TargetReference, container.Struct, false);
            Helper.TestMemberNode(memberNode.Target, structMember1Node, container.Struct, container.Struct.Name, nameof(SimpleStruct.Name), false);

            structMember1Node.Update("Test2");
            Assert.AreEqual("Test2", container.Struct.Name);
            Assert.AreEqual(targetNode, memberNode.Target);
            Assert.AreEqual(structMember1Node, memberNode.Target[nameof(SimpleStruct.Name)]);
            Assert.AreEqual(structMember2Node, memberNode.Target[nameof(SimpleStruct.Value)]);
            Helper.TestMemberNode(memberNode.Target, structMember1Node, container.Struct, container.Struct.Name, nameof(SimpleStruct.Name), false);
            Helper.TestMemberNode(memberNode.Target, memberNode.Target.Members.First(), container.Struct, container.Struct.Name, nameof(SimpleStruct.Name), false);
        }

        [Test]
        public void TestSimpleStructCollectionUpdate()
        {
            var nodeContainer = new NodeContainer();
            var container = new StructWithCollectionContainer { Struct = new StructWithCollection { Strings = new List<string> { "aaa", "bbb" } } };
            var containerNode = nodeContainer.GetOrCreateNode(container);
            var memberNode = containerNode[nameof(StructWithCollectionContainer.Struct)];
            var targetNode = memberNode.Target;
            var structMemberNode = targetNode[nameof(StructWithCollection.Strings)];

            Helper.TestNonCollectionObjectNode(containerNode, container, 1);
            Helper.TestMemberNode(containerNode, memberNode, container, container.Struct, nameof(StructWithCollectionContainer.Struct), true);
            Helper.TestNonNullObjectReference(memberNode.TargetReference, container.Struct, false);
            Helper.TestMemberNode(targetNode, structMemberNode, container.Struct, container.Struct.Strings, nameof(StructWithCollection.Strings), false);

            structMemberNode.Update("ddd", new Index(1));
            Assert.AreEqual("ddd", container.Struct.Strings[1]);
            Assert.AreEqual(targetNode, memberNode.Target);
            Assert.AreEqual(structMemberNode, targetNode[nameof(StructWithCollection.Strings)]);
            Helper.TestMemberNode(targetNode, structMemberNode, container.Struct, container.Struct.Strings, nameof(StructWithCollection.Strings), false);
        }

        [Test]
        public void TestNestedStruct()
        {
            var nodeContainer = new NodeContainer();
            var container = new NestingStructContainer { Struct = new FirstNestingStruct { Struct1 = new SecondNestingStruct { Struct2 = new SimpleStruct { Name = "Test", Value = 1 } } } };
            var containerNode = nodeContainer.GetOrCreateNode(container);
            var memberNode = containerNode[nameof(NestingStructContainer.Struct)];
            var firstTargetNode = memberNode.Target;
            var firstNestingMemberNode = firstTargetNode[nameof(FirstNestingStruct.Struct1)];
            var secondTargetNode = firstNestingMemberNode.Target;
            var secondNestingMemberNode = secondTargetNode[nameof(SecondNestingStruct.Struct2)];
            var structMember1Node = secondNestingMemberNode.Target[nameof(SimpleStruct.Name)];
            var structMember2Node = secondNestingMemberNode.Target[nameof(SimpleStruct.Value)];

            Helper.TestNonCollectionObjectNode(containerNode, container, 1);
            Helper.TestMemberNode(containerNode, memberNode, container, container.Struct, nameof(StructContainer.Struct), true);
            Helper.TestNonNullObjectReference(memberNode.TargetReference, container.Struct, false);
            Helper.TestMemberNode(memberNode.Target, firstNestingMemberNode, container.Struct, container.Struct.Struct1, nameof(FirstNestingStruct.Struct1), true);
            Helper.TestNonNullObjectReference(firstNestingMemberNode.TargetReference, container.Struct.Struct1, false);
            Helper.TestMemberNode(firstNestingMemberNode.Target, secondNestingMemberNode, container.Struct.Struct1, container.Struct.Struct1.Struct2, nameof(SecondNestingStruct.Struct2), true);
            Helper.TestNonNullObjectReference(secondNestingMemberNode.TargetReference, container.Struct.Struct1.Struct2, false);
            Helper.TestMemberNode(secondNestingMemberNode.Target, structMember1Node, container.Struct.Struct1.Struct2, container.Struct.Struct1.Struct2.Name, nameof(SimpleStruct.Name), false);
            Helper.TestMemberNode(secondNestingMemberNode.Target, structMember2Node, container.Struct.Struct1.Struct2, container.Struct.Struct1.Struct2.Value, nameof(SimpleStruct.Value), false);
        }

        [Test]
        public void TestFirstNestedStructUpdate()
        {
            var nodeContainer = new NodeContainer();
            var container = new NestingStructContainer { Struct = new FirstNestingStruct { Struct1 = new SecondNestingStruct { Struct2 = new SimpleStruct { Name = "Test", Value = 1 } } } };
            var containerNode = nodeContainer.GetOrCreateNode(container);
            var memberNode = containerNode[nameof(NestingStructContainer.Struct)];
            var firstTargetNode = memberNode.Target;
            var firstNestingMemberNode = firstTargetNode[nameof(FirstNestingStruct.Struct1)];
            var secondTargetNode = firstNestingMemberNode.Target;
            var secondNestingMemberNode = secondTargetNode[nameof(SecondNestingStruct.Struct2)];
            var structMember1Node = secondNestingMemberNode.Target[nameof(SimpleStruct.Name)];
            var structMember2Node = secondNestingMemberNode.Target[nameof(SimpleStruct.Value)];

            var newStruct = new FirstNestingStruct { Struct1 = new SecondNestingStruct { Struct2 = new SimpleStruct { Name = "Test2", Value = 2 } } };
            memberNode.Update(newStruct);
            Assert.AreEqual("Test2", container.Struct.Struct1.Struct2.Name);
            Assert.AreEqual(2, container.Struct.Struct1.Struct2.Value);
            Assert.AreEqual(firstTargetNode, memberNode.Target);
            Assert.AreEqual(firstNestingMemberNode, firstTargetNode[nameof(FirstNestingStruct.Struct1)]);
            Assert.AreEqual(secondTargetNode, firstNestingMemberNode.Target);
            Assert.AreEqual(secondNestingMemberNode, secondTargetNode[nameof(SecondNestingStruct.Struct2)]);
            Assert.AreEqual(structMember1Node, secondNestingMemberNode.Target[nameof(SimpleStruct.Name)]);
            Assert.AreEqual(structMember2Node, secondNestingMemberNode.Target[nameof(SimpleStruct.Value)]);

            Helper.TestNonCollectionObjectNode(containerNode, container, 1);
            Helper.TestMemberNode(containerNode, memberNode, container, container.Struct, nameof(StructContainer.Struct), true);
            Helper.TestNonNullObjectReference(memberNode.TargetReference, container.Struct, false);
            Helper.TestMemberNode(memberNode.Target, firstNestingMemberNode, container.Struct, container.Struct.Struct1, nameof(FirstNestingStruct.Struct1), true);
            Helper.TestNonNullObjectReference(firstNestingMemberNode.TargetReference, container.Struct.Struct1, false);
            Helper.TestMemberNode(firstNestingMemberNode.Target, secondNestingMemberNode, container.Struct.Struct1, container.Struct.Struct1.Struct2, nameof(SecondNestingStruct.Struct2), true);
            Helper.TestNonNullObjectReference(secondNestingMemberNode.TargetReference, container.Struct.Struct1.Struct2, false);
            Helper.TestMemberNode(secondNestingMemberNode.Target, structMember1Node, container.Struct.Struct1.Struct2, container.Struct.Struct1.Struct2.Name, nameof(SimpleStruct.Name), false);
            Helper.TestMemberNode(secondNestingMemberNode.Target, structMember2Node, container.Struct.Struct1.Struct2, container.Struct.Struct1.Struct2.Value, nameof(SimpleStruct.Value), false);
        }

        [Test]
        public void TestSecondNestedStructUpdate()
        {
            var nodeContainer = new NodeContainer();
            var container = new NestingStructContainer { Struct = new FirstNestingStruct { Struct1 = new SecondNestingStruct { Struct2 = new SimpleStruct { Name = "Test", Value = 1 } } } };
            var containerNode = nodeContainer.GetOrCreateNode(container);
            var memberNode = containerNode[nameof(NestingStructContainer.Struct)];
            var firstTargetNode = memberNode.Target;
            var firstNestingMemberNode = firstTargetNode[nameof(FirstNestingStruct.Struct1)];
            var secondTargetNode = firstNestingMemberNode.Target;
            var secondNestingMemberNode = secondTargetNode[nameof(SecondNestingStruct.Struct2)];
            var structMember1Node = secondNestingMemberNode.Target[nameof(SimpleStruct.Name)];
            var structMember2Node = secondNestingMemberNode.Target[nameof(SimpleStruct.Value)];

            var newStruct = new SecondNestingStruct { Struct2 = new SimpleStruct { Name = "Test2", Value = 2 } };
            firstNestingMemberNode.Update(newStruct);
            Assert.AreEqual("Test2", container.Struct.Struct1.Struct2.Name);
            Assert.AreEqual(2, container.Struct.Struct1.Struct2.Value);
            Assert.AreEqual(firstTargetNode, memberNode.Target);
            Assert.AreEqual(firstNestingMemberNode, firstTargetNode[nameof(FirstNestingStruct.Struct1)]);
            Assert.AreEqual(secondTargetNode, firstNestingMemberNode.Target);
            Assert.AreEqual(secondNestingMemberNode, secondTargetNode[nameof(SecondNestingStruct.Struct2)]);
            Assert.AreEqual(structMember1Node, secondNestingMemberNode.Target[nameof(SimpleStruct.Name)]);
            Assert.AreEqual(structMember2Node, secondNestingMemberNode.Target[nameof(SimpleStruct.Value)]);

            Helper.TestNonCollectionObjectNode(containerNode, container, 1);
            Helper.TestMemberNode(containerNode, memberNode, container, container.Struct, nameof(StructContainer.Struct), true);
            Helper.TestNonNullObjectReference(memberNode.TargetReference, container.Struct, false);
            Helper.TestMemberNode(firstTargetNode, firstNestingMemberNode, container.Struct, container.Struct.Struct1, nameof(FirstNestingStruct.Struct1), true);
            Helper.TestNonNullObjectReference(firstNestingMemberNode.TargetReference, container.Struct.Struct1, false);
            Helper.TestMemberNode(secondTargetNode, secondNestingMemberNode, container.Struct.Struct1, container.Struct.Struct1.Struct2, nameof(SecondNestingStruct.Struct2), true);
            Helper.TestNonNullObjectReference(secondNestingMemberNode.TargetReference, container.Struct.Struct1.Struct2, false);
            Helper.TestMemberNode(secondNestingMemberNode.Target, structMember1Node, container.Struct.Struct1.Struct2, container.Struct.Struct1.Struct2.Name, nameof(SimpleStruct.Name), false);
            Helper.TestMemberNode(secondNestingMemberNode.Target, structMember2Node, container.Struct.Struct1.Struct2, container.Struct.Struct1.Struct2.Value, nameof(SimpleStruct.Value), false);
        }

        [Test]
        public void TestNestedStructMemberUpdate()
        {
            var nodeContainer = new NodeContainer();
            var container = new NestingStructContainer { Struct = new FirstNestingStruct { Struct1 = new SecondNestingStruct { Struct2 = new SimpleStruct { Name = "Test", Value = 1 } } } };
            var containerNode = nodeContainer.GetOrCreateNode(container);
            var memberNode = containerNode[nameof(NestingStructContainer.Struct)];
            var firstTargetNode = memberNode.Target;
            var firstNestingMemberNode = firstTargetNode[nameof(FirstNestingStruct.Struct1)];
            var secondTargetNode = firstNestingMemberNode.Target;
            var secondNestingMemberNode = secondTargetNode[nameof(SecondNestingStruct.Struct2)];
            var structMember1Node = secondNestingMemberNode.Target[nameof(SimpleStruct.Name)];
            var structMember2Node = secondNestingMemberNode.Target[nameof(SimpleStruct.Value)];

            structMember1Node.Update("Test2");
            structMember2Node.Update(2);
            Assert.AreEqual("Test2", container.Struct.Struct1.Struct2.Name);
            Assert.AreEqual(2, container.Struct.Struct1.Struct2.Value);
            Assert.AreEqual(firstTargetNode, memberNode.Target);
            Assert.AreEqual(firstNestingMemberNode, firstTargetNode[nameof(FirstNestingStruct.Struct1)]);
            Assert.AreEqual(secondTargetNode, firstNestingMemberNode.Target);
            Assert.AreEqual(secondNestingMemberNode, secondTargetNode[nameof(SecondNestingStruct.Struct2)]);
            Assert.AreEqual(structMember1Node, secondNestingMemberNode.Target[nameof(SimpleStruct.Name)]);
            Assert.AreEqual(structMember2Node, secondNestingMemberNode.Target[nameof(SimpleStruct.Value)]);

            Helper.TestNonCollectionObjectNode(containerNode, container, 1);
            Helper.TestMemberNode(containerNode, memberNode, container, container.Struct, nameof(StructContainer.Struct), true);
            Helper.TestNonNullObjectReference(memberNode.TargetReference, container.Struct, false);
            Helper.TestMemberNode(firstTargetNode, firstNestingMemberNode, container.Struct, container.Struct.Struct1, nameof(FirstNestingStruct.Struct1), true);
            Helper.TestNonNullObjectReference(firstNestingMemberNode.TargetReference, container.Struct.Struct1, false);
            Helper.TestMemberNode(secondTargetNode, secondNestingMemberNode, container.Struct.Struct1, container.Struct.Struct1.Struct2, nameof(SecondNestingStruct.Struct2), true);
            Helper.TestNonNullObjectReference(secondNestingMemberNode.TargetReference, container.Struct.Struct1.Struct2, false);
            Helper.TestMemberNode(secondNestingMemberNode.Target, structMember1Node, container.Struct.Struct1.Struct2, container.Struct.Struct1.Struct2.Name, nameof(SimpleStruct.Name), false);
            Helper.TestMemberNode(secondNestingMemberNode.Target, structMember2Node, container.Struct.Struct1.Struct2, container.Struct.Struct1.Struct2.Value, nameof(SimpleStruct.Value), false);
        }
    }
}
