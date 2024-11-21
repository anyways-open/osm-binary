using System;
using System.IO;
using OsmSharp.Tags;

namespace OsmSharp.IO.Binary.v0_1
{
    /// <summary>
    /// Contains all binary formatting code.
    /// </summary>
    public static class BinarySerializer
    {
        private static System.Text.Encoder _encoder = (new System.Text.UnicodeEncoding()).GetEncoder();

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
            if (node.Latitude.HasValue) { size += stream.Write(node.Latitude.Value); }
            if (node.Longitude.HasValue) { size += stream.Write(node.Longitude.Value); }

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
                size += stream.Write(0);
            }
            else
            {
                size += stream.Write(way.Nodes.Length);
                for (var i = 0; i < way.Nodes.Length; i++)
                {
                    size += stream.Write(way.Nodes[i]);
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
                size += stream.Write(0);
            }
            else
            {
                size += stream.Write(relation.Members.Length);
                for (var i = 0; i < relation.Members.Length; i++)
                {
                    size += stream.Write(relation.Members[i].Id);
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
        
        private static int AppendOsmGeo(this Stream stream, OsmGeo osmGeo)
        {
            var size = 0;

            if (osmGeo.Id.HasValue) { size += stream.Write(osmGeo.Id.Value); }
            if (osmGeo.ChangeSetId.HasValue) { size += stream.Write(osmGeo.ChangeSetId.Value); }
            if (osmGeo.TimeStamp.HasValue) { size += stream.Write(osmGeo.TimeStamp.Value); }
            if (osmGeo.UserId.HasValue) { size += stream.Write(osmGeo.UserId.Value); }
            size += stream.WriteWithSize(osmGeo.UserName);
            if (osmGeo.Version.HasValue) { size += stream.Write((int)osmGeo.Version.Value); }
            if (osmGeo.Visible.HasValue) { size += stream.Write(osmGeo.Visible.Value); }
            
            if (osmGeo.Tags == null ||
                osmGeo.Tags.Count == 0)
            {
                size += stream.Write(0);
            }
            else
            {
                size += stream.Write(osmGeo.Tags.Count);
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
        public static OsmGeoType? ReadOsmGeoHeader(this Stream stream, out bool hasId, out bool hasChangesetId, out bool hasTimestamp,
            out bool hasUserId, out bool hasVersion, out bool hasVisible)
        {
            var header = stream.ReadByte();
            if (header == -1)
            {
                hasId = false;
                hasChangesetId = false;
                hasTimestamp = false;
                hasUserId = false;
                hasVersion = false;
                hasVisible = false;
                return null;
            }

            hasId = (header & 4) == 0;
            hasChangesetId = (header & 8) == 0;
            hasTimestamp = (header & 16) == 0;
            hasUserId = (header & 32) == 0;
            hasVersion = (header & 64) == 0;
            hasVisible = (header & 128) == 0;

            var type = header & 3;            
            switch (type)
            {
                case 1:
                    return OsmGeoType.Node;
                case 2:
                    return OsmGeoType.Way;
                case 3:
                    return OsmGeoType.Relation;
            }
            throw new Exception("Invalid header: cannot detect OsmGeoType.");
        }
        
        /// <summary>
        /// Reads an OSM object starting at the stream's current position.
        /// </summary>
        public static OsmGeo ReadOsmGeo(this Stream stream, byte[] buffer)
        {
            bool hasId, hasChangesetId, hasTimestamp, hasUserId, hasVersion, hasVisible;
            var type = stream.ReadOsmGeoHeader(out hasId, out hasChangesetId, out hasTimestamp, 
                out hasUserId, out hasVersion, out hasVisible);
            if (type == null) return null;

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

            OsmGeo osmGeo = null;
            switch (type)
            {
                case OsmGeoType.Node:
                    osmGeo = stream.ReadNode(buffer);
                    break;
                case OsmGeoType.Way:
                    osmGeo = stream.ReadWay(buffer);
                    break;
                case OsmGeoType.Relation:
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
        public static int Write(this Stream stream, int value)
        {
            stream.Write(BitConverter.GetBytes(value), 0, 4);
            return 4;
        }
        
        private static int Write(this Stream stream, float value)
        {
            stream.Write(BitConverter.GetBytes(value), 0, 4);
            return 4;
        }

        private static int Write(this Stream stream, double value)
        {
            stream.Write(BitConverter.GetBytes(value), 0, 8);
            return 8;
        }

        private static int Write(this Stream stream, long value)
        {
            stream.Write(BitConverter.GetBytes(value), 0, 8);
            return 8;
        }

        private static int Write(this Stream stream, ulong value)
        {
            stream.Write(BitConverter.GetBytes(value), 0, 8);
            return 8;
        }

        private static int Write(this Stream stream, DateTime value)
        {
            stream.Write(BitConverter.GetBytes(value.Ticks), 0, 8);
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
            stream.Read(buffer, 0, 8);
            return BitConverter.ToInt64(buffer, 0);
        }

        private static int ReadInt32(this Stream stream, byte[] buffer)
        {
            stream.Read(buffer, 0, 4);
            return BitConverter.ToInt32(buffer, 0);
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

        private static float ReadSingle(this Stream stream, byte[] buffer)
        {
            stream.Read(buffer, 0, 4);
            return BitConverter.ToSingle(buffer, 0);
        }

        private static double ReadDouble(this Stream stream, byte[] buffer)
        {
            stream.Read(buffer, 0, 8);
            return BitConverter.ToDouble(buffer, 0);
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


            return System.Text.UnicodeEncoding.Unicode.GetString(buffer, 0, size);
        }
    }
}