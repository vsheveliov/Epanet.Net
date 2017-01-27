/*
 * Copyright (C) 2016 Vyacheslav Shevelyov (slavash at aha dot ru)
 *
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with this program.  If not, see http://www.gnu.org/licenses/.
 */

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;

using Epanet.Enums;
using Epanet.Network;

namespace Epanet.Quality {

    ///<summary>Binary quality file reader class.</summary>
    public class QualityReader:IEnumerable<QualityReader.Step>, IDisposable {
        public IEnumerator<Step> GetEnumerator() { return Steps().GetEnumerator(); }
        IEnumerator IEnumerable.GetEnumerator() { return GetEnumerator(); }

        private IEnumerable<Step> Steps() {
            if (_inputStream == null)
                throw new ObjectDisposedException(GetType().FullName);

            lock (_inputStream) {

                _inputStream.BaseStream.Position = sizeof(int) * 2;
                _qStep = new Step(_fMap, _inputStream, Links, Nodes);

                for (int i = 0; i < Periods; i++) {
                    _qStep.Read();
                    yield return _qStep;

                }
            }
        }

        public Step this[int index] {
            get {
                if (index >= _nPeriods || index < 0)
                    throw new ArgumentOutOfRangeException();

                long recordSize = sizeof(float) * (_nodeCount + _linkCount);
                long position = (sizeof(int) * 2) + recordSize * (index - 1);

                if (position + recordSize > _inputStream.BaseStream.Length) {
                    throw new ArgumentOutOfRangeException();
                }

                _inputStream.BaseStream.Position = position;

                _qStep.Read();

                return _qStep;
            }
        }

        ///<summary>Units conversion map.</summary>
        private readonly FieldsMap _fMap;

        ///<summary>input stream</summary>
        private BinaryReader _inputStream;

        ///<summary>Number of links.</summary>
        private int _linkCount;

        ///<summary>Number of nodes.</summary>
        private int _nodeCount;

        ///<summary>Number of report periods stored in this file.</summary>
        private int _nPeriods;

        ///<summary>Current quality step snapshot.</summary>
        private Step _qStep;

        ///<summary>Class constructor.</summary>
        public QualityReader(string qualFile, FieldsMap fMap) {
            _fMap = fMap;
            Open(qualFile);
        }

        /// <summary>Close the inputStream.</summary>
        public void Close() { _inputStream.Close(); }

        /// <summary>Get the number of links in the file.</summary>
        /// <value>Number of links.</value>
        public int Links { get { return _linkCount; } }

        /// <summary>Get the number of nodes in the file.</summary>
        /// <value>Number of nodes.</value>
        public int Nodes { get { return _nodeCount; } }

        /// <summary>Get the number of reported quality step snapshots in the file.</summary>
        /// <value>Number of periods.</value>
        public int Periods { get { return _nPeriods; } }

        /// <param name="qualFile">Path to the quality file.</param>
        private void Open(string qualFile) {

            // Read the last 4 bytes which contain the number of periods
            _inputStream = new BinaryReader(File.OpenRead(qualFile));
            _inputStream.BaseStream.Seek(-sizeof(int), SeekOrigin.End);
            _nPeriods = _inputStream.ReadInt32();
            _inputStream.BaseStream.Position = 0;

            _nodeCount = _inputStream.ReadInt32();
            _linkCount = _inputStream.ReadInt32();
            _qStep = new Step(_fMap, _inputStream, _linkCount, _nodeCount);
        }

        #region IDisposable pattern

        //Implement IDisposable.
        public void Dispose() {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing) {
            if (_inputStream == null)
                return;

            if (disposing) {
                // Free other state (managed objects).
                var rdr = _inputStream;
                _inputStream = null;

                if (rdr != null)
                    rdr.Close();

            }
            // Free your own state (unmanaged objects).
            // Set large fields to null.

            _qStep = null;

        }

        // Use C# destructor syntax for finalization code.
        ~QualityReader() { Dispose(false); }

        #endregion

        /// <summary> Single step of the quality simulation, with the quality value for each link and node.</summary>
        public sealed class Step {

            /// <summary> Species quality values in the links</summary>
            private readonly float[] linkQ;

            private readonly FieldsMap fld;

            /// <summary> Species quality values in the nodes</summary>
            private readonly float[] nodeQ;

            private readonly BinaryReader reader;

            /// <summary>Constructor</summary>
            /// <param name="reader"></param>
            /// <param name="linkCount">Number of links.</param>
            /// <param name="nodeCount">Number of nodes.</param>
            /// <param name="fld">Units report properties & conversion object.</param>
            internal Step(FieldsMap fld, BinaryReader reader, int linkCount, int nodeCount) {
                this.fld = fld;
                this.reader = reader;
                linkQ = new float[linkCount];
                nodeQ = new float[nodeCount];
            }

            /// <summary>Get link quality values in user units.</summary>
            /// <param name="id">Link sequential identification number.</param>
            /// <returns>Species concentration, trace or age value in user units.</returns>
            public float GetLinkQuality(int id) { return (float)fld.RevertUnit(FieldType.QUALITY, linkQ[id]); }

            /// <summary>Get node quality values in user units.</summary>
            /// <param name="id">Link sequential identification number.</param>
            /// <returns>Specie concentration, trace or age value in user units.</returns>
            public float GetNodeQuality(int id) { return (float)fld.RevertUnit(FieldType.QUALITY, nodeQ[id]); }

            /// <summary> Read quality data from file stream.</summary>
            internal void Read() {
                for (int i = 0; i < nodeQ.Length; i++) {
                    nodeQ[i] = reader.ReadSingle();
                }

                for (int i = 0; i < linkQ.Length; i++) {
                    linkQ[i] = reader.ReadSingle();
                }
            }

        }
    }

}