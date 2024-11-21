using System;
using System.IO;
using OsmSharp.Streams;

namespace OsmSharp.IO.Binary.v0_1
{

    /// <summary>
    /// A stream source that just reads objects in binary format.
    /// </summary>
    public class BinaryOsmStreamSource : OsmStreamSource
    {
        private readonly Stream _stream;
        private readonly byte[] _buffer;
        private readonly long? _initialPosition;

        /// <summary>
        /// Creates a new binary stream source.
        /// </summary>
        public BinaryOsmStreamSource(Stream stream)
        {
            _stream = stream;
            if (_stream.CanSeek)
            {
                _initialPosition = _stream.Position;
            }
            _buffer = new byte[1024];
        }

        /// <summary>
        /// Returns true if this source can be reset.
        /// </summary>
        public override bool CanReset => _stream.CanSeek;

        /// <summary>
        /// Returns the current object.
        /// </summary>
        /// <returns></returns>
        public override OsmGeo Current()
        {
            return _current;
        }

        private OsmGeo _current;
        private long? _firstWayPosition;
        private long? _firstRelationPosition;

        /// <summary>
        /// Move to the next object in this stream source.
        /// </summary>
        public override bool MoveNext(bool ignoreNodes, bool ignoreWays, bool ignoreRelations)
        {
            if (_stream.CanSeek)
            {
                if (_stream.Length == _stream.Position + 1)
                {
                    return false;
                }

                // move to first way/relation position if they are known and nodes and or ways are to be skipped.
                if (_firstWayPosition != null && ignoreNodes && !ignoreWays)
                {
                    // if nodes have to be ignored, there was already a first pass and ways are not to be ignored jump to the first way.
                    if (_stream.Position <= _firstWayPosition)
                    {
                        // only just to the first way if that hasn't happened yet.
                        _stream.Seek(_firstWayPosition.Value, SeekOrigin.Begin);
                    }
                }

                if (_firstRelationPosition != null && ignoreNodes && ignoreWays && !ignoreRelations)
                {
                    // if nodes and ways have to be ignored, there was already a first pass and ways are not be ignored jump to the first relation.
                    if (_stream.Position < _firstRelationPosition)
                    {
                        // only just to the first relation if that hasn't happened yet.
                        _stream.Seek(_firstRelationPosition.Value, SeekOrigin.Begin);
                    }
                }
            }

            long? positionBefore = null;
            if (_stream.CanSeek) positionBefore = _stream.Position;
            var osmGeo = this.DoMoveNext();
            while (osmGeo != null)
            {
                switch (osmGeo.Type)
                {
                    case OsmGeoType.Node:
                        if (!ignoreNodes)
                        {
                            _current = osmGeo;
                            return true;
                        }

                        break;
                    case OsmGeoType.Way:
                        if (_firstWayPosition == null && positionBefore != null) _firstWayPosition = positionBefore;
                        if (!ignoreWays)
                        {
                            _current = osmGeo;
                            return true;
                        }

                        break;
                    case OsmGeoType.Relation:
                        if (_firstRelationPosition == null && positionBefore != null)
                            _firstRelationPosition = positionBefore;
                        if (!ignoreRelations)
                        {
                            _current = osmGeo;
                            return true;
                        }

                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }

                osmGeo = this.DoMoveNext();
            }

            return false;
        }

        private OsmGeo DoMoveNext()
        {
            return BinarySerializer.ReadOsmGeo(_stream, _buffer);
        }

        /// <summary>
        /// Resets this stream.
        /// </summary>
        public override void Reset()
        {
            if (_initialPosition == null) throw new NotSupportedException(
                $"Cannot reset this stream, source stream is not seekable, check {nameof(this.CanReset)} before calling {nameof(this.Reset)}");

            _current = null;
            _stream.Seek(_initialPosition.Value, SeekOrigin.Begin);
        }
    }
}