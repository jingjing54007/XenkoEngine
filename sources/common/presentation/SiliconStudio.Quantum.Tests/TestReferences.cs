﻿using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using SiliconStudio.Quantum.Contents;

namespace SiliconStudio.Quantum.Tests
{
    [TestFixture]
    public class TestReferences
    {
        public class TestObject
        {
            public string Name;
            public override string ToString() => $"{{TestObject: {Name}}}";
        }

        public class ObjectContainer
        {
            public object Instance { get; set; }
            public override string ToString() => "{ObjectContainer}";
        }

        public class ObjectsContainer
        {
            public TestObject Instance1 { get; set; }
            public TestObject Instance2 { get; set; }
            public override string ToString() => $"{{ObjectsContainer: {Instance1}, {Instance2}}}";
        }

        public class MultipleObjectContainer
        {
            public List<TestObject> Instances { get; set; } = new List<TestObject>();
            public override string ToString() => $"{{MultipleObjectContainer: {string.Join(", ", Instances.Select(x => x.ToString()))}}}";
        }

        /// <summary>
        /// This test creates an object referencing another and verifies that the reference is properly created in the node graph.
        /// </summary>
        [Test]
        public void TestObjectReference()
        {
            var nodeContainer = new NodeContainer();
            var instance = new TestObject { Name = "Test" };
            var container = new ObjectContainer { Instance = instance };
            var containerNode = nodeContainer.GetOrCreateNode(container);
            Helper.TestNonCollectionObjectNode(containerNode, container, 1);

            var memberNode = containerNode.Members.First();
            Helper.TestMemberNode(containerNode, memberNode, container, instance, nameof(ObjectContainer.Instance), true);
            Helper.TestNonNullObjectReference(memberNode.TargetReference, instance, false);
            var instanceNode = nodeContainer.GetNode(instance);
            Helper.TestNonNullObjectReference(memberNode.TargetReference, instanceNode, instance);

            memberNode = instanceNode.Members.First();
            Helper.TestMemberNode(instanceNode, memberNode, instance, instance.Name, nameof(TestObject.Name), false);
        }

        /// <summary>
        /// This test creates an object with a reference that is null and verifies that the node graph is consistent.
        /// </summary>
        [Test]
        public void TestNullObjectReference()
        {
            var nodeContainer = new NodeContainer();
            var container = new ObjectContainer { Instance = null };
            var containerNode = nodeContainer.GetOrCreateNode(container);
            Helper.TestNonCollectionObjectNode(containerNode, container, 1);

            var memberNode = containerNode.Members.First();
            Helper.TestMemberNode(containerNode, memberNode, container, null, nameof(ObjectContainer.Instance), true);
            Helper.TestNullObjectReference(memberNode.TargetReference);
        }

        /// <summary>
        /// This test creates an object referencing another and verifies that the reference is properly created in the node graph.
        /// </summary>
        [Test]
        public void TestObjectReferenceUpdate()
        {
            var nodeContainer = new NodeContainer();
            var instance = new TestObject { Name = "Test" };
            var container = new ObjectContainer { Instance = instance };
            var containerNode = nodeContainer.GetOrCreateNode(container);
            var memberNode = containerNode.Members.First();
            var instanceNode = memberNode.TargetReference.TargetNode;

            // Update to a new instance
            var newInstance = new TestObject { Name = "Test2" };
            memberNode.Update(newInstance);
            Helper.TestMemberNode(containerNode, memberNode, container, newInstance, nameof(ObjectContainer.Instance), true);
            Helper.TestNonNullObjectReference(memberNode.TargetReference, newInstance, false);

            var newInstanceNode = nodeContainer.GetNode(newInstance);
            Helper.TestNonNullObjectReference(memberNode.TargetReference, newInstanceNode, newInstance);
            Assert.AreNotEqual(instanceNode.Guid, newInstanceNode.Guid);

            // Update to null
            memberNode.Update(null);
            Helper.TestMemberNode(containerNode, memberNode, container, null, nameof(ObjectContainer.Instance), true);
            Helper.TestNullObjectReference(memberNode.TargetReference);

            // Update back to the initial instance
            memberNode.Update(instance);
            Helper.TestMemberNode(containerNode, memberNode, container, instance, nameof(ObjectContainer.Instance), true);
            Helper.TestNonNullObjectReference(memberNode.TargetReference, instanceNode, instance);
        }

        /// <summary>
        /// This test creates an object containing a collection of references to other objects and verifies that
        /// the references are properly created in the node graph.
        /// </summary>
        [Test]
        public void TestEnumerableReference()
        {
            var nodeContainer = new NodeContainer();
            var instance1 = new TestObject { Name = "Test1" };
            var instance2 = new TestObject { Name = "Test2" };
            var container = new MultipleObjectContainer { Instances = { instance1, instance2 } };
            var containerNode = nodeContainer.GetOrCreateNode(container);
            Helper.TestNonCollectionObjectNode(containerNode, container, 1);

            var memberNode = containerNode.Members.First();
            Helper.TestMemberNode(containerNode, memberNode, container, container.Instances, nameof(MultipleObjectContainer.Instances), true);
            Helper.TestReferenceEnumerable(memberNode.ItemReferences, container.Instances);

            Assert.AreEqual(container.Instances, memberNode.Retrieve());
            Assert.AreEqual(instance1, memberNode.Retrieve(new Index(0)));
            Assert.AreEqual(instance2, memberNode.Retrieve(new Index(1)));

            var reference1 = memberNode.ItemReferences.First();
            Helper.TestMemberNode(reference1.TargetNode, reference1.TargetNode.Members.First(), instance1, instance1.Name, nameof(TestObject.Name), false);

            var reference2 = memberNode.ItemReferences.Last();
            Helper.TestMemberNode(reference2.TargetNode, reference2.TargetNode.Members.First(), instance2, instance2.Name, nameof(TestObject.Name), false);
        }

        /// <summary>
        /// This test creates an object containing a collection of references to null and verifies that
        /// the references in the node graph are consistent.
        /// </summary>
        [Test]
        public void TestNullEnumerableReference()
        {
            var nodeContainer = new NodeContainer();
            var container = new MultipleObjectContainer { Instances = { null, null } };
            var containerNode = nodeContainer.GetOrCreateNode(container);
            Helper.TestNonCollectionObjectNode(containerNode, container, 1);

            var memberNode = containerNode.Members.First();
            Helper.TestMemberNode(containerNode, memberNode, container, container.Instances, nameof(MultipleObjectContainer.Instances), true);
            Helper.TestReferenceEnumerable(memberNode.ItemReferences, container.Instances);

            Assert.AreEqual(container.Instances, memberNode.Retrieve());
            Assert.AreEqual(null, memberNode.Retrieve(new Index(0)));
            Assert.AreEqual(null, memberNode.Retrieve(new Index(1)));
        }

        /// <summary>
        /// This test creates an object containing a collection of references to other objects and verifies that
        /// the references are properly created in the node graph.
        /// </summary>
        [Test]
        public void TestEnumerableReferenceUpdate()
        {
            var nodeContainer = new NodeContainer();
            var instance1 = new TestObject { Name = "Test1" };
            var instance2 = new TestObject { Name = "Test2" };
            var container = new MultipleObjectContainer { Instances = { instance1, instance2 } };
            var containerNode = nodeContainer.GetOrCreateNode(container);
            var memberNode = containerNode.Members.First();
            var reference = memberNode.ItemReferences;
            var reference1 = reference.First();
            var reference2 = reference.Last();

            // Update item 0 to a new instance and item 1 to null
            var newInstance = new TestObject { Name = "Test3" };
            memberNode.Update(newInstance, new Index(0));
            memberNode.Update(null, new Index(1));
            Assert.AreEqual(container.Instances, memberNode.Retrieve());
            Assert.AreEqual(newInstance, memberNode.Retrieve(new Index(0)));
            Assert.AreEqual(null, memberNode.Retrieve(new Index(1)));
            Helper.TestReferenceEnumerable(memberNode.ItemReferences, container.Instances);

            var newReference = memberNode.ItemReferences;
            Assert.AreEqual(reference, newReference);
            Assert.AreEqual(2, newReference.Count);
            var newReference1 = newReference.First();
            var newReference2 = newReference.Last();

            Assert.AreNotEqual(reference1, newReference1);
            Assert.AreNotEqual(reference1.TargetGuid, newReference1.TargetGuid);
            Assert.AreNotEqual(reference1.TargetNode, newReference1.TargetNode);
            Assert.AreNotEqual(reference1.ObjectValue, newReference1.ObjectValue);
            Assert.AreNotEqual(reference2, newReference2);
            Assert.AreNotEqual(reference2.TargetGuid, newReference2.TargetGuid);
            Assert.AreNotEqual(reference2.TargetNode, newReference2.TargetNode);
            Assert.AreNotEqual(reference2.ObjectValue, newReference2.ObjectValue);
        }

        /// <summary>
        /// This test creates an object referencing another twice and verifies that the reference is properly created in the node graph.
        /// </summary>
        [Test]
        public void TestObjectReferenceSameInstance()
        {
            var nodeContainer = new NodeContainer();
            var instance = new TestObject { Name = "Test" };
            var container = new ObjectsContainer { Instance1 = instance, Instance2 = instance };
            var containerNode = nodeContainer.GetOrCreateNode(container);
            Helper.TestNonCollectionObjectNode(containerNode, container, 2);

            var member1Node = containerNode.Members.First();
            Helper.TestMemberNode(containerNode, member1Node, container, instance, nameof(ObjectsContainer.Instance1), true);
            Helper.TestNonNullObjectReference(member1Node.TargetReference, instance, false);

            var member2Node = containerNode.Members.Last();
            Helper.TestMemberNode(containerNode, member2Node, container, instance, nameof(ObjectsContainer.Instance2), true);
            Helper.TestNonNullObjectReference(member2Node.TargetReference, instance, false);

            var reference1 = member1Node.TargetReference;
            var reference2 = member2Node.TargetReference;
            Assert.AreEqual(reference1.TargetGuid, reference2.TargetGuid);
            Assert.AreEqual(reference1.TargetNode, reference2.TargetNode);
            Assert.AreEqual(reference1.ObjectValue, reference2.ObjectValue);
        }

        /// <summary>
        /// This test creates an object containing a collection of references to the same object and verifies that
        /// the references are properly created in the node graph.
        /// </summary>
        [Test]
        public void TestEnumerableReferenceSameInstance()
        {
            var nodeContainer = new NodeContainer();
            var instance = new TestObject { Name = "Test" };
            var container = new MultipleObjectContainer { Instances = { instance, instance } };
            var containerNode = nodeContainer.GetOrCreateNode(container);
            Helper.TestNonCollectionObjectNode(containerNode, container, 1);

            var memberNode = containerNode.Members.First();
            Helper.TestMemberNode(containerNode, memberNode, container, container.Instances, nameof(MultipleObjectContainer.Instances), true);
            Helper.TestReferenceEnumerable(memberNode.ItemReferences, container.Instances);

            var reference = memberNode.ItemReferences;
            Assert.AreEqual(2, reference.Count);
            var reference1 = reference.First();
            var reference2 = reference.Last();
            Assert.AreEqual(reference1.ObjectValue, reference2.ObjectValue);
            Assert.AreEqual(reference1.TargetGuid, reference2.TargetGuid);
            Assert.AreEqual(reference1.TargetNode, reference2.TargetNode);
        }

        /// <summary>
        /// This test creates two objects, one referencing the other. It verifies that when constructing the node of the referenced object first,
        /// the referencer object will reuse the same node as target of the reference
        /// </summary>
        [Test]
        public void TestUseExistingNodeAsReference()
        {
            var nodeContainer = new NodeContainer();
            var instance = new TestObject { Name = "Test" };
            var container = new ObjectContainer { Instance = instance };
            var instanceNode = nodeContainer.GetOrCreateNode(instance);
            var containerNode = nodeContainer.GetOrCreateNode(container);
            Assert.AreEqual(1, containerNode.Members.Count);
            var memberNode = containerNode.Members.First();
            Assert.AreEqual(instance, memberNode.Retrieve());
            Assert.AreEqual(true, memberNode.IsReference);
            Assert.AreEqual(instanceNode, memberNode.TargetReference.TargetNode);
        }

        /// <summary>
        /// This test creates two objects, one referencing the other. It verifies that when constructing the node of the referencer object first,
        /// the referenced object will reuse the same node as target of the reference
        /// </summary>
        [Test]
        public void TestUseExistingReferenceAsNode()
        {
            var nodeContainer = new NodeContainer();
            var instance = new TestObject { Name = "Test" };
            var container = new ObjectContainer { Instance = instance };
            var containerNode = nodeContainer.GetOrCreateNode(container);
            var instanceNode = nodeContainer.GetOrCreateNode(instance);
            Assert.AreEqual(1, containerNode.Members.Count);
            var memberNode = containerNode.Members.First();
            Assert.AreEqual(instance, memberNode.Retrieve());
            Assert.AreEqual(true, memberNode.IsReference);
            Assert.AreEqual(instanceNode, memberNode.TargetReference.TargetNode);
        }

        /// <summary>
        /// This test creates a container object that reference multiples other object. It verifies that the same nodes are reused between the instances
        /// of objects and the references
        /// </summary>
        [Test]
        public void TestUseExistingNodesAsReference()
        {
            var nodeContainer = new NodeContainer();
            var instance1 = new TestObject { Name = "Test1" };
            var instance2 = new TestObject { Name = "Test2" };
            var container = new MultipleObjectContainer { Instances = { instance1, instance2 } };
            var instance1Node = nodeContainer.GetOrCreateNode(instance1);
            var containerNode = nodeContainer.GetOrCreateNode(container);
            var instance2Node = nodeContainer.GetOrCreateNode(instance2);
            Assert.AreEqual(1, containerNode.Members.Count);
            var memberNode = containerNode.Members.First();
            Assert.AreEqual(true, memberNode.IsReference);
            var reference = memberNode.ItemReferences;
            Assert.AreEqual(2, reference.Indices.Count);
            var reference1 = reference.First();
            var reference2 = reference.Last();
            Assert.AreEqual(instance1Node, reference1.TargetNode);
            Assert.AreEqual(instance2Node, reference2.TargetNode);
        }

        /// <summary>
        /// This test creates two objects referencing each other. It verifies that the same nodes are reused between instances and references.
        /// </summary>
        [Test]
        public void TestCircularReferences()
        {
            var nodeContainer = new NodeContainer();
            var instance1 = new ObjectContainer();
            var instance2 = new ObjectContainer();
            instance1.Instance = instance2;
            instance2.Instance = instance1;
            var instance1Node = nodeContainer.GetOrCreateNode(instance1);
            var instance2Node = nodeContainer.GetOrCreateNode(instance2);
            Assert.AreEqual(1, instance1Node.Members.Count);
            Assert.AreEqual(1, instance2Node.Members.Count);
            Assert.AreEqual(instance1Node.Members.First().TargetReference.TargetNode, instance2Node);
            Assert.AreEqual(instance2Node.Members.First().TargetReference.TargetNode, instance1Node);
        }

        /// <summary>
        /// This test creates two objects and updates them to referencing each other. It verifies that the same nodes are reused between instances and references.
        /// </summary>
        [Test]
        public void TestCircularReferencesUpdate()
        {
            var nodeContainer = new NodeContainer();
            var instance1 = new ObjectContainer();
            var instance2 = new ObjectContainer();
            var instance1Node = nodeContainer.GetOrCreateNode(instance1);
            var instance2Node = nodeContainer.GetOrCreateNode(instance2);
            instance1Node.Members.First().Update(instance2);
            instance2Node.Members.First().Update(instance1);
            Assert.AreEqual(instance1Node.Members.First().TargetReference.TargetNode, instance2Node);
            Assert.AreEqual(instance2Node.Members.First().TargetReference.TargetNode, instance1Node);
        }

        /// <summary>
        /// This test creates an object and make it reference itself. It verifies that the same nodes are reused.
        /// </summary>
        [Test]
        public void TestSelfReference()
        {
            var nodeContainer = new NodeContainer();
            var instance = new ObjectContainer();
            instance.Instance = instance;
            var instanceNode = nodeContainer.GetOrCreateNode(instance);
            Assert.AreEqual(1, instanceNode.Members.Count);
            Assert.AreEqual(instanceNode.Members.First().TargetReference.TargetNode, instanceNode);
        }

        /// <summary>
        /// This test creates an object and update it to make it reference itself. It verifies that the same nodes are reused.
        /// </summary>
        [Test]
        public void TestSelfReferenceUpdate()
        {
            var nodeContainer = new NodeContainer();
            var instance = new ObjectContainer();
            var instanceNode = nodeContainer.GetOrCreateNode(instance);
            instanceNode.Members.First().Update(instance);
            instance.Instance = instance;
            Assert.AreEqual(1, instanceNode.Members.Count);
            Assert.AreEqual(instanceNode.Members.First().TargetReference.TargetNode, instanceNode);
        }
    }
}
