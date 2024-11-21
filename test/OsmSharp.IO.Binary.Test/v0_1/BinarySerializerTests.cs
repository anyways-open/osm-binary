using System;
using System.IO;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OsmSharp.Tags;
using OsmSharp.IO.Binary.v0_1;

namespace OsmSharp.IO.Binary.Test.v0_1
{
    [TestClass]
    public class BinarySerializerTests
    {
        /// <summary>
        /// Tests reading/writing a node.
        /// </summary>
        [TestMethod]
        public void TestReadWriteNode()
        {
            using (var stream = new MemoryStream())
            {
                var node1 = new Node()
                {
                    Id = 1,
                    ChangeSetId = 2,
                    Latitude = 10,
                    TimeStamp = DateTime.Now,
                    Longitude = 11,
                    Tags = new TagsCollection(new Tag("name", "hu?")),
                    UserId = 12,
                    UserName = "Ben",
                    Version = 123,
                    Visible = true
                };
                
                // tests the actual serialization code.
                TestBinarySerializer.Append(stream, node1);
                stream.Seek(0, SeekOrigin.Begin);

                // read again and compare.
                var osmGeo = Binary.v0_1.BinarySerializer.ReadOsmGeo(stream, new byte[1024]);
                Assert.IsNotNull(osmGeo);
                Assert.IsInstanceOfType(osmGeo, typeof(Node));
                var node2 = osmGeo as Node;

                Assert.AreEqual(node1.Id, node2.Id);
                Assert.AreEqual(node1.Latitude, node2.Latitude);
                Assert.AreEqual(node1.Longitude, node2.Longitude);
                Assert.AreEqual(node1.ChangeSetId, node2.ChangeSetId);
                Assert.AreEqual(node1.TimeStamp, node2.TimeStamp);
                Assert.AreEqual(node1.UserId, node2.UserId);
                Assert.AreEqual(node1.UserName, node2.UserName);
                Assert.AreEqual(node1.Version, node2.Version);
                Assert.AreEqual(node1.Visible, node2.Visible);
                ExtraAssert.AreEqual(node1.Tags.ToArray(), node2.Tags.ToArray());
            }
        }

        /// <summary>
        /// Tests reading/writing a way.
        /// </summary>
        [TestMethod]
        public void TestReadWriteWay()
        {
            using (var stream = new MemoryStream())
            {
                var way1 = new Way()
                {
                    Id = 1,
                    ChangeSetId = 1,
                    TimeStamp = DateTime.Now,
                    Tags = new TagsCollection(new Tag("name", "hu?")),
                    Nodes = new long[]
                    {
                        1,
                        2, 
                        3
                    },
                    UserId = 1,
                    UserName = "Ben",
                    Version = 1,
                    Visible = true
                };

                // tests the actual serialization code.
                TestBinarySerializer.Append(stream, way1);
                stream.Seek(0, SeekOrigin.Begin);

                // read again and compare.
                var osmGeo = Binary.v0_1.BinarySerializer.ReadOsmGeo(stream, new byte[1024]);
                Assert.IsNotNull(osmGeo);
                Assert.IsInstanceOfType(osmGeo, typeof(Way));
                var way2 = osmGeo as Way;

                Assert.AreEqual(way1.Id, way2.Id);
                Assert.AreEqual(way1.ChangeSetId, way2.ChangeSetId);
                Assert.AreEqual(way1.TimeStamp, way2.TimeStamp);
                Assert.AreEqual(way1.UserId, way2.UserId);
                Assert.AreEqual(way1.UserName, way2.UserName);
                Assert.AreEqual(way1.Version, way2.Version);
                Assert.AreEqual(way1.Visible, way2.Visible);
                Assert.AreEqual(way1.Nodes.Length, way2.Nodes.Length);
                ExtraAssert.AreEqual(way1.Nodes, way2.Nodes);
                ExtraAssert.AreEqual(way1.Tags.ToArray(), way2.Tags.ToArray());
            }
        }

        /// <summary>
        /// Tests reading/writing a relation.
        /// </summary>
        [TestMethod]
        public void TestReadWriteRelation()
        {
            using (var stream = new MemoryStream())
            {
                var relation1 = new Relation()
                {
                    Id = 1,
                    ChangeSetId = 1,
                    TimeStamp = DateTime.Now,
                    Tags = new TagsCollection(new Tag("name", "hu?")),
                    Members = new RelationMember[]
                    {
                        new RelationMember()
                        {
                            Id = 1,
                            Role = "node",
                            Type = OsmGeoType.Node
                        },
                        new RelationMember()
                        {
                            Id = 2,
                            Role = "way",
                            Type = OsmGeoType.Way
                        },
                        new RelationMember()
                        {
                            Id = 3,
                            Role = "relation",
                            Type = OsmGeoType.Relation
                        }
                    },
                    UserId = 1,
                    UserName = "Ben",
                    Version = 1,
                    Visible = true
                };

                // tests the actual serialization code.
                TestBinarySerializer.Append(stream, relation1);
                stream.Seek(0, SeekOrigin.Begin);

                // read again and compare.
                var osmGeo = Binary.v0_1.BinarySerializer.ReadOsmGeo(stream, new byte[1024]);
                Assert.IsNotNull(osmGeo);
                Assert.IsInstanceOfType(osmGeo, typeof(Relation));
                var relation2 = osmGeo as Relation;

                Assert.AreEqual(relation1.Id, relation2.Id);
                Assert.AreEqual(relation1.ChangeSetId, relation2.ChangeSetId);
                Assert.AreEqual(relation1.TimeStamp, relation2.TimeStamp);
                Assert.AreEqual(relation1.UserId, relation2.UserId);
                Assert.AreEqual(relation1.UserName, relation2.UserName);
                Assert.AreEqual(relation1.Version, relation2.Version);
                Assert.AreEqual(relation1.Visible, relation2.Visible);
                Assert.AreEqual(relation1.Members.Length, relation2.Members.Length);
                ExtraAssert.AreEqual(relation1.Members, relation2.Members, (m1, m2) =>
                {
                    Assert.IsNotNull(m2);
                    Assert.AreEqual(m1.Id, m2.Id);
                    Assert.AreEqual(m1.Role, m2.Role);
                    Assert.AreEqual(m1.Type, m2.Type);
                });
                ExtraAssert.AreEqual(relation1.Tags.ToArray(), relation2.Tags.ToArray());
            }
        }
    }
}