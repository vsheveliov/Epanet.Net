using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;

using Epanet.Enums;
using Epanet.Network;

namespace Epanet.Quality {

    ///<summary>Binary quality file reader class.</summary>
    public sealed class QualityReader:IEnumerable<QualityReader.Step>, IDisposable {
        ///<summary>Units conversion map.</summary>
        private readonly FieldsMap _fMap;

        ///<summary>Number of links.</summary>
        private readonly int _linkCount;

        ///<summary>Number of nodes.</summary>
        private readonly int _nodeCount;

        ///<summary>Number of report periods stored in this file.</summary>
        private readonly int _nPeriods;

        ///<summary>input stream</summary>
        private BinaryReader _reader;

        ///<summary>Class constructor.</summary>
        public QualityReader(string qualFile, FieldsMap fMap) {
            _fMap = fMap;

            // Read the last 4 bytes which contain the number of periods
            _reader = new BinaryReader(File.OpenRead(qualFile));
            _reader.BaseStream.Seek(-sizeof(int), SeekOrigin.End);
            _nPeriods = _reader.ReadInt32();
            _reader.BaseStream.Position = 0;

            _nodeCount = _reader.ReadInt32();
            _linkCount = _reader.ReadInt32();
            
        }

        public Step this[int index] {
            get {
                if(_reader == null)
                    throw new ObjectDisposedException(GetType().FullName);

                if ((index >= _nPeriods) || (index < 0))
                    throw new ArgumentOutOfRangeException();

                long recordSize = sizeof(float) * (_nodeCount + _linkCount);
                long position = sizeof(int) * 2 + recordSize * (index - 1);

                if (position + recordSize > _reader.BaseStream.Length)
                    throw new ArgumentOutOfRangeException();

                _reader.BaseStream.Position = position;

                return new Step(_reader, _fMap, _linkCount, _nodeCount);
            }
        }

        /// <summary>Get the number of links in the file.</summary>
        /// <value>Number of links.</value>
        public int Links {
            get { return _linkCount; }
        }

        /// <summary>Get the number of nodes in the file.</summary>
        /// <value>Number of nodes.</value>
        public int Nodes {
            get { return _nodeCount; }
        }

        /// <summary>Get the number of reported quality step snapshots in the file.</summary>
        /// <value>Number of periods.</value>
        public int Periods {
            get { return _nPeriods; }
        }
        public IEnumerator<Step> GetEnumerator() { return Steps().GetEnumerator(); }
        IEnumerator IEnumerable.GetEnumerator() { return GetEnumerator(); }

        private IEnumerable<Step> Steps() {
            if (_reader == null)
                throw new ObjectDisposedException(GetType().FullName);

            lock (_reader) {
                _reader.BaseStream.Position = sizeof(int) * 2;

                for (int i = 0; i < Periods; i++)
                    yield return new Step(_reader, _fMap, Links, Nodes);

            }
        }

        /// <summary>Close the inputStream.</summary>
        public void Close() { Dispose(); }

        /// <summary> Single step of the quality simulation, with the quality value for each link and node.</summary>
        public sealed class Step {
            /// <summary> Species quality values in the links</summary>
            private readonly float[] _linkQ;

            /// <summary> Species quality values in the nodes</summary>
            private readonly float[] _nodeQ;

            private readonly FieldsMap _fMap;

            /// <summary>Constructor</summary>
            /// <param name="linkCount">Number of links.</param>
            /// <param name="nodeCount">Number of nodes.</param>
            /// <param name="br">Input reader.</param>
            /// <param name="fMap">Units report properties & conversion object.</param>
            internal Step(BinaryReader br, FieldsMap fMap, int linkCount, int nodeCount) {
                _fMap = fMap;
                _linkQ = new float[linkCount];
                _nodeQ = new float[nodeCount];

                for(int i = 0; i < _nodeQ.Length; i++)
                    _nodeQ[i] = br.ReadSingle();

                for(int i = 0; i < _linkQ.Length; i++)
                    _linkQ[i] = br.ReadSingle();

            }

            /// <summary>Get link quality values in user units.</summary>
            /// <param name="id">Link sequential identification number.</param>
            /// <returns>Species concentration, trace or age value in user units.</returns>
            public float GetLinkQuality(int id) { return (float)_fMap.RevertUnit(FieldType.QUALITY, _linkQ[id]); }

            /// <summary>Get node quality values in user units.</summary>
            /// <param name="id">Link sequential identification number.</param>
            /// <returns>Specie concentration, trace or age value in user units.</returns>
            public float GetNodeQuality(int id) { return (float)_fMap.RevertUnit(FieldType.QUALITY, _nodeQ[id]); }

        }

        #region IDisposable pattern

        //Implement IDisposable.
        public void Dispose() {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing) {
            if (_reader == null)
                return;

            if (disposing) {
                // Free other state (managed objects).
                var rdr = _reader;
                _reader = null;

                if (rdr != null)
                    rdr.Close();
            }
            // Free your own state (unmanaged objects).
            // Set large fields to null.
            
        }

        // Use C# destructor syntax for finalization code.
        ~QualityReader() { Dispose(false); }

        #endregion
    }

}
