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
using org.addition.epanet.network;

namespace org.addition.epanet.quality {

    ///<summary>Binary quality file reader class.</summary>
    public class QualityReader:IEnumerable<QualityReader.Step>, IDisposable {

        public IEnumerator<Step> GetEnumerator() { return this.Steps().GetEnumerator(); }
        IEnumerator IEnumerable.GetEnumerator() { return this.GetEnumerator(); }

        private IEnumerable<Step> Steps() {
            if (this._inputStream == null)
                throw new ObjectDisposedException(this.GetType().FullName);

            lock (this._inputStream) {

                this._inputStream.BaseStream.Position = sizeof(int) * 2;
                this._qStep = new Step(this._fMap, this._inputStream, this.Links, this.Nodes);

                for (int i = 0; i < this.Periods; i++) {
                    this._qStep.Read();
                    yield return this._qStep;

                }
            }
        }

        public Step this[int index] {
            get {
                if (index >= this._nPeriods || index < 0)
                    throw new ArgumentOutOfRangeException();

                long recordSize = sizeof(float) * (this._nodeCount + this._linkCount);
                long position = (sizeof(int) * 2) + recordSize * (index - 1);

                if (position + recordSize > this._inputStream.BaseStream.Length) {
                    throw new ArgumentOutOfRangeException();
                }

                this._inputStream.BaseStream.Position = position;

                this._qStep.Read();

                return this._qStep;
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
            this._fMap = fMap;
            this.Open(qualFile);
        }

        /// <summary>Close the inputStream.</summary>
        public void Close() { this._inputStream.Close(); }

        /// <summary>Get the number of links in the file.</summary>
        /// <value>Number of links.</value>
        public int Links { get { return this._linkCount; } }

        /// <summary>Get the number of nodes in the file.</summary>
        /// <value>Number of nodes.</value>
        public int Nodes { get { return this._nodeCount; } }

        /// <summary>Get the number of reported quality step snapshots in the file.</summary>
        /// <value>Number of periods.</value>
        public int Periods { get { return this._nPeriods; } }

        /// <param name="qualFile">Path to the quality file.</param>
        private void Open(string qualFile) {

            // Read the last 4 bytes which contain the number of periods
            this._inputStream = new BinaryReader(File.OpenRead(qualFile));
            this._inputStream.BaseStream.Position = qualFile.Length - sizeof(int);
            this._nPeriods = this._inputStream.ReadInt32();
            this._inputStream.Close();

            this._inputStream = new BinaryReader(File.OpenRead(qualFile));
            this._nodeCount = this._inputStream.ReadInt32();
            this._linkCount = this._inputStream.ReadInt32();
            this._qStep = new Step(this._fMap, this._inputStream, this._linkCount, this._nodeCount);
        }

        #region IDisposable pattern

        //Implement IDisposable.
        public void Dispose() {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing) {
            if (this._inputStream == null)
                return;

            if (disposing) {
                // Free other state (managed objects).
                var rdr = this._inputStream;
                this._inputStream = null;

                if (rdr != null)
                    rdr.Close();

            }
            // Free your own state (unmanaged objects).
            // Set large fields to null.

            this._qStep = null;

        }

        // Use C# destructor syntax for finalization code.
        ~QualityReader() { this.Dispose(false); }

        #endregion

        /// <summary> Single step of the quality simulation, with the quality value for each link and node.</summary>
        public sealed class Step {

            /// <summary> Species quality values in the links</summary>
            private readonly float[] _linkQ;

            private readonly FieldsMap _fld;

            /// <summary> Species quality values in the nodes</summary>
            private readonly float[] _nodeQ;

            private readonly BinaryReader _reader;

            /// <summary>Constructor</summary>
            /// <param name="reader"></param>
            /// <param name="linkCount">Number of links.</param>
            /// <param name="nodeCount">Number of nodes.</param>
            /// <param name="fld">Units report properties & conversion object.</param>
            internal Step(FieldsMap fld, BinaryReader reader, int linkCount, int nodeCount) {
                this._fld = fld;
                this._reader = reader;
                this._linkQ = new float[linkCount];
                this._nodeQ = new float[nodeCount];
            }

            /// <summary>Get link quality values in user units.</summary>
            /// <param name="id">Link sequential identification number.</param>
            /// <returns>Species concentration, trace or age value in user units.</returns>
            public float GetLinkQuality(int id) { return (float)_fld.RevertUnit(FieldsMap.FieldType.QUALITY, this._linkQ[id]); }

            /// <summary>Get node quality values in user units.</summary>
            /// <param name="id">Link sequential identification number.</param>
            /// <returns>Specie concentration, trace or age value in user units.</returns>
            public float GetNodeQuality(int id) { return (float)_fld.RevertUnit(FieldsMap.FieldType.QUALITY, this._nodeQ[id]); }

            /// <summary> Read quality data from file stream.</summary>
            internal void Read() {
                for (int i = 0; i < this._nodeQ.Length; i++) {
                    this._nodeQ[i] = this._reader.ReadSingle();
                }

                for (int i = 0; i < this._linkQ.Length; i++) {
                    this._linkQ[i] = this._reader.ReadSingle();
                }
            }

        }
    }

}