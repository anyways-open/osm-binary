﻿// The MIT License (MIT)

// Copyright (c) 2017 Ben Abelshausen

// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:

// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.

// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

using OsmSharp.Tags;
using System;
using System.IO;
using System.Runtime.InteropServices;
using OsmSharp.Db;

namespace OsmSharp.IO.Binary
{
    /// <summary>
    /// Contains all binary formatting code.
    /// </summary>
    public static class BinarySerializer
    {
        /// <summary>
        /// Appends the header byte(s).
        /// </summary>
        public static int AppendHeader(this Stream stream, OsmGeo osmGeo)
        {
            // build header containing type and nullable flags.
            byte header = 1; // a node.
            if(osmGeo.Type == OsmGeoType.Way)
            {
                header = 2;
            }
            else if(osmGeo.Type == OsmGeoType.Relation)
            {
                header = 3;
            }
            if (!osmGeo.Id.HasValue) { header = (byte)(header | 4); }
            if (!osmGeo.ChangeSetId.HasValue) { header = (byte)(header | 8); }
            if (!osmGeo.TimeStamp.HasValue) { header = (byte)(header | 16); }
            if (!osmGeo.UserId.HasValue) { header = (byte)(header | 32); }
            if (!osmGeo.Version.HasValue) { header = (byte)(header | 64); }
            if (!osmGeo.Visible.HasValue) { header = (byte)(header | 128); }
            stream.WriteByte(header);

            return 1;
        }
        
        /// <summary>
        /// Writes the given node starting at the stream's current position.
        /// </summary>
        public static int Append(this Stream stream, Node node)
        {
            if (node == null) { throw new ArgumentNullException(nameof(node)); }

            // appends the header.
            var size = stream.AppendHeader(node);

            // write osm geo data.
            size += stream.AppendOsmGeo(node);

            // write lat/lon with nullable flags.
            byte header = 0;
            if (!node.Latitude.HasValue) { header = (byte)(header | 1); }
            if (!node.Longitude.HasValue) { header = (byte)(header | 2); }
            size += 1;
            stream.WriteByte(header);
            if (node.Latitude.HasValue) { size += stream.WriteDouble(node.Latitude.Value); }
            if (node.Longitude.HasValue) { size += stream.WriteDouble(node.Longitude.Value); }

            return size;
        }

        /// <summary>
        /// Writes the given way starting at the stream's current position.
        /// </summary>
        public static int Append(this Stream stream, Way way)
        {
            if (way == null) { throw new ArgumentNullException(nameof(way)); }

            // appends the header.
            var size = stream.AppendHeader(way);

            // write data.
            size += stream.AppendOsmGeo(way);
            
            if (way.Nodes == null ||
                way.Nodes.Length == 0)
            {
                size += WriteInt32(stream, 0);
            }
            else
            {
                size += WriteInt32(stream, way.Nodes.Length);
                for (var i = 0; i < way.Nodes.Length; i++)
                {
                    size += WriteInt64(stream, way.Nodes[i]);
                }
            }

            return size;
        }

        /// <summary>
        /// Writes the given relation starting at the stream's current position.
        /// </summary>
        public static int Append(this Stream stream, Relation relation)
        {
            if (relation == null) { throw new ArgumentNullException(nameof(relation)); }

            // appends the header.
            var size = stream.AppendHeader(relation);

            // write data.
            size += stream.AppendOsmGeo(relation);
            
            if (relation.Members == null ||
                relation.Members.Length == 0)
            {
                size += WriteInt32(stream, 0);
            }
            else
            {
                size += WriteInt32(stream, relation.Members.Length);
                for (var i = 0; i < relation.Members.Length; i++)
                {
                    size += WriteInt64(stream, relation.Members[i].Id);
                    size += stream.WriteWithSize(relation.Members[i].Role);
                    switch (relation.Members[i].Type)
                    {
                        case OsmGeoType.Node:
                            stream.WriteByte((byte)1);
                            break;
                        case OsmGeoType.Way:
                            stream.WriteByte((byte)2);
                            break;
                        case OsmGeoType.Relation:
                            stream.WriteByte((byte)3);
                            break;
                    }
                    size += 1;
                }
            }

            return size;
        }

        /// <summary>
        /// Writes the given osm geo object starting at the stream's current position.
        /// </summary>
        public static int Append(this Stream stream, OsmGeo osmGeo)
        {
            return osmGeo switch
            {
                Node node => stream.Append(node),
                Way way => stream.Append(way),
                Relation relation => stream.Append(relation),
                _ => throw new InvalidDataException($"Unknown {nameof(OsmGeo)} type.")
            };
        }
        
        private static int AppendOsmGeo(this Stream stream, OsmGeo osmGeo)
        {
            var size = 0;

            if (osmGeo.Id.HasValue) { size += WriteInt64(stream, osmGeo.Id.Value); }
            if (osmGeo.ChangeSetId.HasValue) { size += WriteInt64(stream, osmGeo.ChangeSetId.Value); }
            if (osmGeo.TimeStamp.HasValue) { size += stream.Write(osmGeo.TimeStamp.Value); }
            if (osmGeo.UserId.HasValue) { size += WriteInt64(stream, osmGeo.UserId.Value); }
            size += stream.WriteWithSize(osmGeo.UserName);
            if (osmGeo.Version.HasValue) { size += WriteInt32(stream, (int)osmGeo.Version.Value); }
            if (osmGeo.Visible.HasValue) { size += stream.Write(osmGeo.Visible.Value); }
            
            if (osmGeo.Tags == null ||
                osmGeo.Tags.Count == 0)
            {
                size += WriteInt32(stream, 0);
            }
            else
            {
                size += WriteInt32(stream, osmGeo.Tags.Count);
                foreach (var t in osmGeo.Tags)
                {
                    size += stream.WriteWithSize(t.Key);
                    size += stream.WriteWithSize(t.Value);
                }
            }

            return size;
        }

        /// <summary>
        /// Reads the header, returns the type, and outputs the flags.
        /// </summary>
        internal static bool TryReadOsmGeoHeader(this Stream stream, out OsmGeoType type, out bool hasId, out bool hasChangesetId, out bool hasTimestamp,
            out bool hasUserId, out bool hasVersion, out bool hasVisible)
        {
            var header = stream.ReadByte();
            if (header == -1)
            {
                hasId = false;
                hasVersion = false;
                hasChangesetId = false;
                hasTimestamp = false;
                hasUserId = false;
                hasVersion = false;
                hasVisible = false;
                type = OsmGeoType.Node;
                return false;
            }

            hasId = (header & 4) == 0;
            hasChangesetId = (header & 8) == 0;
            hasTimestamp = (header & 16) == 0;
            hasUserId = (header & 32) == 0;
            hasVersion = (header & 64) == 0;
            hasVisible = (header & 128) == 0;

            var typeNumber = header & 3;            
            switch (typeNumber)
            {
                case 1:
                    type = OsmGeoType.Node;
                    break;
                case 2:
                    type = OsmGeoType.Way;
                    break;
                case 3:
                    type = OsmGeoType.Relation;
                    break;
                default:
                    throw new Exception("Invalid header: cannot detect OsmGeoType.");
            }

            return true;
        }

        /// <summary>
        /// Reads only the OSM type and id for the current object.
        /// </summary>
        /// <param name="stream">The stream.</param>
        /// <param name="buffer">The buffer.</param>
        /// <returns>The id and type.</returns>
        public static (OsmGeoType type, long? id) ReadOsmGeoKey(this Stream stream, byte[] buffer = null)
        {            
            if (stream.CanSeek &&
                         stream.Length == stream.Position) throw new InvalidDataException("Could not read header.");;

            if (!stream.TryReadOsmGeoHeader(out var type, out var hasId, out var _, out var _,
                out var _, out var _, out var _)) throw new InvalidDataException("Could not read header.");
            
            buffer ??= new byte [1024];
            if (buffer.Length < 1024) throw new ArgumentException("Buffer needs be at least 1024 bytes.", nameof(buffer));

            // read the basics.
            long? id = null;
            if (hasId) { id = stream.ReadInt64(buffer); }

            return (type, id);
        }
        
        /// <summary>
        /// Reads an OSM object starting at the stream's current position.
        /// </summary>
        public static OsmGeo ReadOsmGeo(this Stream stream, byte[] buffer = null)
        {
            if (stream.CanSeek &&
                stream.Length == stream.Position) return null;

            if (!stream.TryReadOsmGeoHeader(out var type, out var hasId, out var hasChangesetId, out var hasTimestamp,
                out var hasUserId, out var hasVersion, out var hasVisible)) return null; // couldn't read header.
            
            buffer ??= new byte [1024];
            if (buffer.Length < 1024) throw new ArgumentException("Buffer needs be at least 1024 bytes.", nameof(buffer));

            // read the basics.
            long? id = null;
            if (hasId) { id = stream.ReadInt64(buffer); }
            long? changesetId = null;
            if (hasChangesetId) { changesetId = stream.ReadInt64(buffer); }
            DateTime? timestamp = null;
            if (hasTimestamp) { timestamp = stream.ReadDateTime(buffer); }
            long? userId = null;
            if (hasUserId) { userId = stream.ReadInt64(buffer); }
            var username = stream.ReadWithSizeString(buffer);
            int? version = null;
            if (hasVersion) { version = stream.ReadInt32(buffer); }
            bool? visible = null;
            if (hasVisible) { visible = stream.ReadBool(); }

            // read tags.
            var tagsCount = stream.ReadInt32(buffer);
            TagsCollection tags = null;
            if (tagsCount > 0)
            {
                tags = new TagsCollection(tagsCount);
                for (var i = 0; i < tagsCount; i++)
                {
                    var key = stream.ReadWithSizeString(buffer);
                    var value = stream.ReadWithSizeString(buffer);
                    tags.AddOrReplace(key, value);
                }
            }

            OsmGeo osmGeo;
            switch (type)
            {
                case OsmGeoType.Node:
                    osmGeo = stream.ReadNode(buffer);
                    break;
                case OsmGeoType.Way:
                    osmGeo = stream.ReadWay(buffer);
                    break;
                default:
                    osmGeo = stream.ReadRelation(buffer);
                    break;
            }

            osmGeo.Id = id;
            osmGeo.ChangeSetId = changesetId;
            osmGeo.TimeStamp = timestamp;
            osmGeo.UserId = userId;
            osmGeo.UserName = username;
            osmGeo.Version = version;
            osmGeo.Visible = visible;
            osmGeo.Tags = tags;

            return osmGeo;
        }

        private static Node ReadNode(this Stream stream, byte[] buffer)
        {
            var node = new Node();

            var header = stream.ReadByte();
            var hasLatitude = (header & 1) == 0;
            var hasLongitude = (header & 2) == 0;

            if (hasLatitude) { node.Latitude = stream.ReadDouble(buffer); }
            if (hasLongitude) { node.Longitude = stream.ReadDouble(buffer); }

            return node;
        }

        private static Way ReadWay(this Stream stream, byte[] buffer)
        {
            var way = new Way();

            var nodeCount = stream.ReadInt32(buffer);
            if (nodeCount > 0)
            {
                var nodes = new long[nodeCount];
                for (var i = 0; i < nodeCount; i++)
                {
                    nodes[i] = stream.ReadInt64(buffer);
                }
                way.Nodes = nodes;
            }

            return way;
        }

        private static Relation ReadRelation(this Stream stream, byte[] buffer)
        {
            var relation = new Relation();
            
            var memberCount = stream.ReadInt32(buffer);
            if (memberCount > 0)
            {
                var members = new RelationMember[memberCount];
                for(var i = 0; i< memberCount; i++)
                {
                    var id = stream.ReadInt64(buffer);
                    var role = stream.ReadWithSizeString(buffer);
                    var typeId = stream.ReadByte();
                    var type = OsmGeoType.Node;
                    switch(typeId)
                    {
                        case 2:
                            type = OsmGeoType.Way;
                            break;
                        case 3:
                            type = OsmGeoType.Relation;
                            break;
                    }
                    members[i] = new RelationMember()
                    {
                        Id = id,
                        Role = role,
                        Type = type
                    };
                }
                relation.Members = members;
            }

            return relation;
        }

        /// <summary>
        /// Writes the given value to the stream.
        /// </summary>
        public static int WriteInt32(this Stream stream, int value)
        {
            BitCoder.WriteInt32(stream, value);
            return 4;
        }

        private static int WriteDouble(this Stream stream, double value)
        {
            BitCoder.WriteInt64(stream, BitConverter.DoubleToInt64Bits(value));
            //stream.Write(BitConverter.GetBytes(value), 0, 8);
            return 8;
        }

        private static int WriteInt64(this Stream stream, long value)
        {
            BitCoder.WriteInt64(stream, value);
            //stream.Write(BitConverter.GetBytes(value), 0, 8);
            return 8;
        }

        private static int Write(this Stream stream, DateTime value)
        {
            BitCoder.WriteInt64(stream, value.Ticks);
            //stream.Write(BitConverter.GetBytes(value.Ticks), 0, 8);
            return 8;
        }

        private static int Write(this Stream stream, bool value)
        {
            if (value)
            {
                stream.WriteByte(1);
            }
            else
            {
                stream.WriteByte(0);
            }
            return 1;
        }

        private static int WriteWithSize(this Stream stream, string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                stream.WriteByte(0);
                return 1;
            }
            else
            { // TODO: improve this based on the protobuf way of handling this kind of variable info.
                var bytes = System.Text.Encoding.Unicode.GetBytes(value);
                var position = 0;
                while(bytes.Length - position >= 255)
                { // write in blocks of 255.
                    stream.WriteByte(255);
                    stream.Write(bytes, position, 255);
                    position += 256; // data + size
                }
                stream.WriteByte((byte)(bytes.Length - position));
                if (bytes.Length - position > 0)
                {
                    stream.Write(bytes, position, bytes.Length - position);
                }
                return bytes.Length + 1;
            }
        }

        private static DateTime ReadDateTime(this Stream stream, byte[] buffer)
        {
            return new DateTime(stream.ReadInt64(buffer));
        }

        private static long ReadInt64(this Stream stream, byte[] buffer)
        {
            return stream.ReadInt64();
            //var c = stream.Read(buffer, 0, 8);
            //return BitConverter.ToInt64(buffer, 0);
        }

        private static int ReadInt32(this Stream stream, byte[] buffer)
        {
            return stream.ReadInt32();
            //stream.Read(buffer, 0, 4);
            //return BitConverter.ToInt32(buffer, 0);
        }

        private static bool ReadBool(this Stream stream)
        {
            var v = stream.ReadByte();
            if (v == 0)
            {
                return false;
            }
            else if (v == 1)
            {
                return true;
            }
            else
            {
                throw new InvalidDataException("Cannot deserialize bool.");
            }
        }

        private static double ReadDouble(this Stream stream, byte[] buffer)
        {
            var value = stream.ReadInt64();
            return BitConverter.Int64BitsToDouble(value);
            //stream.Read(buffer, 0, 8);
            //return BitConverter.ToDouble(buffer, 0);
        }

        private static string ReadWithSizeString(this System.IO.Stream stream, byte[] buffer)
        {
            var size = stream.ReadByte();
            var position = 0;
            while (size == 255)
            {
                stream.Read(buffer, position, (int)size);
                size = stream.ReadByte();
                position += 256;
            }
            if (size > 0)
            {
                stream.Read(buffer, position, (int)size);
            }


            return System.Text.Encoding.Unicode.GetString(buffer, 0, size);
        }
    }
}