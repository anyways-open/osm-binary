using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OsmSharp.IO.Binary.v0_1;
using OsmSharp.Tags;

namespace OsmSharp.IO.Binary.Test.v0_1
{
    /// <summary>
    /// Contains tests for the binary serializer core functions.
    /// </summary>
    [TestClass]
    public class BinaryOsmStreamTargetTests
    {
        [TestMethod]
        public void BinaryOsmStreamTarget_ShouldWriteNode()
        {
            using (var stream = new MemoryStream())
            {
                var targetStream = new BinaryOsmStreamTarget(stream);
                targetStream.Initialize();
                targetStream.AddNode(new Node()
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
                });

                stream.Seek(0, SeekOrigin.Begin);
                var sourceStream = new BinaryOsmStreamSource(stream);
                var nodes = sourceStream.ToList();
                
                Assert.AreEqual(1, nodes.Count);
            }
        }
        
        [TestMethod]
        public void BinaryOsmStreamTarget_ShouldWriteWay()
        {
            using (var stream = new MemoryStream())
            {
                var targetStream = new BinaryOsmStreamTarget(stream);
                targetStream.Initialize();
                targetStream.AddWay(new Way()
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
                });

                stream.Seek(0, SeekOrigin.Begin);
                var sourceStream = new BinaryOsmStreamSource(stream);
                var osmGeos = sourceStream.ToList();
                
                Assert.AreEqual(1, osmGeos.Count);
            }
        }
        
        [TestMethod]
        public void BinaryOsmStreamTarget_ShouldWriteRelation()
        {
            using (var stream = new MemoryStream())
            {
                var targetStream = new BinaryOsmStreamTarget(stream);
                targetStream.Initialize();
                targetStream.AddRelation(new Relation()
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
                });

                stream.Seek(0, SeekOrigin.Begin);
                var sourceStream = new BinaryOsmStreamSource(stream);
                var osmGeos = sourceStream.ToList();
                
                Assert.AreEqual(1, osmGeos.Count);
            }
        }

        [TestMethod]
        public void BinaryOsmStreamTarget_ShouldWriteToDeflateStream()
        {
            using (var stream = new MemoryStream())
            using (var streamCompressed = new System.IO.Compression.DeflateStream(stream, CompressionMode.Compress))
            {
                var targetStream = new BinaryOsmStreamTarget(streamCompressed);
                targetStream.Initialize();
                targetStream.AddRelation(new Relation()
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
                });
            }
        }
    }
}