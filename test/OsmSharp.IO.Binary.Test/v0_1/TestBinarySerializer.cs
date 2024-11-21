using System;
using System.IO;

namespace OsmSharp.IO.Binary.Test.v0_1
{
    /// <summary>
    /// Contains all old binary formatting writing code only.
    ///
    /// The old format is only supported as a reader, we include this here as a reference.
    /// </summary>
    public static class TestBinarySerializer
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
    }
}