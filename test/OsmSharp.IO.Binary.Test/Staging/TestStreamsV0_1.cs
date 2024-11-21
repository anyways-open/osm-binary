using System;
using System.IO;
using OsmSharp.Tags;

namespace OsmSharp.IO.Binary.Test.Staging
{
    internal static class TestStreamsV0_1
    {
        public static Stream GetNodeTestStream()
        {
            var stream = new MemoryStream();
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

            v0_1.TestBinarySerializer.Append(stream, node1);

            stream.Seek(0, SeekOrigin.Begin);
            return stream;
        }
        
        public static Stream GetWayTestStream()
        {
            var stream = new MemoryStream();
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

            v0_1.TestBinarySerializer.Append(stream, way1);

            stream.Seek(0, SeekOrigin.Begin);
            return stream;
        }
        
        public static Stream GetRelationTestStream()
        {
            var stream = new MemoryStream();
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

            v0_1.TestBinarySerializer.Append(stream, relation1);

            stream.Seek(0, SeekOrigin.Begin);
            return stream;
        }
        
        public static Stream GetNodeWayAndRelationTestStream()
        {
            var stream = new MemoryStream();
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

            v0_1.TestBinarySerializer.Append(stream, node1);
            
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
            
            v0_1.TestBinarySerializer.Append(stream, way1);
            
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

            v0_1.TestBinarySerializer.Append(stream, relation1);

            stream.Seek(0, SeekOrigin.Begin);
            return stream;
        }
    }
}