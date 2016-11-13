/*
 * Copyright (C) 2012  Addition, Lda. (addition at addition dot pt)
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

        IEnumerable<Step> Steps() {
            if (this.inputStream == null)
                throw new ObjectDisposedException(this.GetType().FullName);

            lock (this.inputStream) {

                this.inputStream.BaseStream.Position = sizeof(int) * 2;
                this.qStep = new Step(this.fMap, this.inputStream, this.getLinks(), this.getNodes());

                for (int i = 0; i < this.getPeriods(); i++) {
                    this.qStep.Read();
                    yield return this.qStep;

                }
            }
        }

        public Step this[int index] {
            get {
                if (index >= this.nPeriods || index < 0)
                    throw new ArgumentOutOfRangeException();

                long recordSize = sizeof(float) * (this.nodeCount + this.linkCount);
                long position = (sizeof(int) * 2) + recordSize * (index - 1);

                if (position + recordSize > this.inputStream.BaseStream.Length) {
                    throw new ArgumentOutOfRangeException();
                }

                this.inputStream.BaseStream.Position = position;

                this.qStep.Read();

                return this.qStep;
            }
        }

        ///<summary>Units conversion map.</summary>
        private readonly FieldsMap fMap;

        ///<summary>input stream</summary>
        private BinaryReader inputStream;

        ///<summary>Number of links.</summary>
        private int linkCount;

        ///<summary>Number of nodes.</summary>
        private int nodeCount;

        ///<summary>Number of report periods stored in this file.</summary>
        private int nPeriods;

        ///<summary>Current quality step snapshot.</summary>
        private Step qStep;

        ///<summary>Class constructor</summary>
        public QualityReader(string qualFile, FieldsMap fMap) {
            this.fMap = fMap;
            this.open(qualFile);
        }

        /**
         * Close the inputStream.
         *
         * @throws IOException
         */
        public void close() { inputStream.Close(); }

        /**
         * Get the number of links in the file.
         *
         * @return Number of links.
         */
        public int getLinks() { return linkCount; }

        /**
         * Get the number of nodes in the file.
         *
         * @return Number of nodes.
         */
        public int getNodes() { return nodeCount; }


        /**
         * Get the number of reported quality step snapshots in the file.
         *
         * @return Number of periods.
         */
        public int getPeriods() { return nPeriods; }


        ///<summary>@param qualFile Abstract representation of the quality file.</summary>
        private void open(string qualFile) {

            // Read the last 4 bytes which contain the number of periods
            inputStream = new BinaryReader(File.OpenRead(qualFile));
            inputStream.BaseStream.Position = qualFile.Length - sizeof(int);
            nPeriods = inputStream.ReadInt32();
            inputStream.Close();

            inputStream = new BinaryReader(File.OpenRead(qualFile));
            nodeCount = inputStream.ReadInt32();
            linkCount = inputStream.ReadInt32();
            qStep = new Step(this.fMap, this.inputStream, linkCount, nodeCount);
        }

        #region IDisposable pattern

        //Implement IDisposable.
        public void Dispose() {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing) {
            if (this.inputStream == null)
                return;

            if (disposing) {
                // Free other state (managed objects).
                var rdr = this.inputStream;
                this.inputStream = null;

                if (rdr != null)
                    rdr.Close();

            }
            // Free your own state (unmanaged objects).
            // Set large fields to null.

            this.qStep = null;

        }

        // Use C# destructor syntax for finalization code.
        ~QualityReader() { this.Dispose(false); }

        #endregion

        /// <summary> Single step of the quality simulation, with the quality value for each link and node.</summary>
        public sealed class Step {

            /// <summary> Species quality values in the links</summary>
            private readonly float[] linkQ;

            private readonly FieldsMap _fld;

            /// <summary> Species quality values in the nodes</summary>
            private readonly float[] nodeQ;

            private readonly BinaryReader _reader;

            /// <summary>Constructor</summary>
            /// <param name="reader"></param>
            /// <param name="linkCount">Number of links.</param>
            /// <param name="nodeCount">Number of nodes.</param>
            /// <param name="fld">Units report properties & conversion object.</param>
            internal Step(FieldsMap fld, BinaryReader reader, int linkCount, int nodeCount) {
                this._fld = fld;
                this._reader = reader;
                this.linkQ = new float[linkCount];
                this.nodeQ = new float[nodeCount];
            }

            /// <summary>Get link quality values in user units.</summary>
            /// <param name="id">Link sequential identification number.</param>
            /// <returns>Species concentration, trace or age value in user units.</returns>
            public float getLinkQuality(int id) { return (float)_fld.revertUnit(FieldsMap.Type.QUALITY, linkQ[id]); }

            /// <summary>Get node quality values in user units.</summary>
            /// <param name="id">Link sequential identification number.</param>
            /// <returns>Specie concentration, trace or age value in user units.</returns>
            public float getNodeQuality(int id) { return (float)_fld.revertUnit(FieldsMap.Type.QUALITY, nodeQ[id]); }

            /// <summary> Read quality data from file stream.</summary>
            internal void Read() {
                for (int i = 0; i < this.nodeQ.Length; i++) {
                    this.nodeQ[i] = this._reader.ReadSingle();
                }

                for (int i = 0; i < this.linkQ.Length; i++) {
                    this.linkQ[i] = this._reader.ReadSingle();
                }
            }

        }
    }

}