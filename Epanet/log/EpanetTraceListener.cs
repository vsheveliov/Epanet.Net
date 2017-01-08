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
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;

namespace Epanet.Log {

    public sealed class EpanetTraceListener : TextWriterTraceListener {
        
        private sealed class RollingFileStream : FileStream {

            public RollingFileStream(string path, long maxFileLength, int maxFileCount, FileMode mode)
                : base(path, BaseFileMode(mode), FileAccess.Write) {
                this.Init(path, maxFileLength, maxFileCount, mode);
            }

            public RollingFileStream(string path, long maxFileLength, int maxFileCount, FileMode mode, FileShare share)
                : base(path, BaseFileMode(mode), FileAccess.Write, share) {
                this.Init(path, maxFileLength, maxFileCount, mode);
            }

            public RollingFileStream(
                string path,
                long maxFileLength,
                int maxFileCount,
                FileMode mode,
                FileShare share,
                int bufferSize)
                : base(path, BaseFileMode(mode), FileAccess.Write, share, bufferSize) {
                this.Init(path, maxFileLength, maxFileCount, mode);
            }

            public RollingFileStream(
                string path,
                long maxFileLength,
                int maxFileCount,
                FileMode mode,
                FileShare share,
                int bufferSize,
                bool isAsync)
                : base(path, BaseFileMode(mode), FileAccess.Write, share, bufferSize, isAsync) {
                this.Init(path, maxFileLength, maxFileCount, mode);
            }

            public override bool CanRead { get { return false; } }

            public override void Write(byte[] array, int offset, int count) {
                while(true) {
                    int actualCount = Math.Min(count, array.GetLength(0));

                    if(this.Position + actualCount <= this.MaxFileLength) {
                        base.Write(array, offset, count);
                        break;
                    }

                    if(this.CanSplitData) {
                        int partialCount = (int)(Math.Max(this.MaxFileLength, this.Position) - this.Position);
                        base.Write(array, offset, partialCount);
                        offset += partialCount;
                        count = actualCount - partialCount;
                    }
                    else {
                        if(count > this.MaxFileLength) {
                            throw new ArgumentOutOfRangeException("count", "Buffer size exceeds maximum file length");
                        }
                    }
                    this.BackupAndResetStream();
                }
            }

            public long MaxFileLength { get; private set; }
            public int MaxFileCount { get; private set; }
            public bool CanSplitData { get; set; }

            private void Init(string path, long maxFileLength, int maxFileCount, FileMode mode) {
                if(maxFileLength <= 0)
                    throw new ArgumentOutOfRangeException("maxFileLength", "Invalid maximum file length");
                if(maxFileCount <= 0)
                    throw new ArgumentOutOfRangeException("maxFileCount", "Invalid maximum file count");

                this.MaxFileLength = maxFileLength;
                this.MaxFileCount = maxFileCount;
                // this.CanSplitData = true;

                string fullPath = Path.GetFullPath(path);
                this.fileDir = Path.GetDirectoryName(fullPath);
                this.fileBase = Path.GetFileNameWithoutExtension(fullPath);
                this.fileExt = Path.GetExtension(fullPath);

                this.fileDecimals = 1;
                int decimalBase = 10;

                while(decimalBase < this.MaxFileCount) {
                    this.fileDecimals++;
                    decimalBase *= 10;
                }

                switch(mode) {
                case FileMode.Create:
                case FileMode.CreateNew:
                case FileMode.Truncate:
                // Delete old files
                for(int iFile = 0; iFile < this.MaxFileCount; ++iFile) {
                    string file = this.GetBackupFileName(iFile);
                    if(File.Exists(file))
                        File.Delete(file);
                }

                break;

                default:
                // Position file pointer to the last backup file
                for(int iFile = 0; iFile < this.MaxFileCount; ++iFile) {
                    if(File.Exists(this.GetBackupFileName(iFile)))
                        this.nextFileIndex = iFile + 1;
                }

                if(this.nextFileIndex == this.MaxFileCount)
                    this.nextFileIndex = 0;
                this.Seek(0, SeekOrigin.End);
                break;
                }
            }

            private void BackupAndResetStream() {
                this.Flush();
                File.Copy(this.Name, this.GetBackupFileName(this.nextFileIndex), true);
                this.SetLength(0);

                this.nextFileIndex++;

                if(this.nextFileIndex >= this.MaxFileCount)
                    this.nextFileIndex = 0;
            }

            private string GetBackupFileName(int index) {
                string sIndex = index.ToString("D{0}" + this.fileDecimals);

                string path2 = string.IsNullOrEmpty(this.fileExt)
                    ? string.Format("{0}{1}", this.fileBase, sIndex)
                    : string.Format("{0}{1}{2}", this.fileBase, sIndex, this.fileExt);

                return Path.Combine(this.fileDir, path2);
            }

            private static FileMode BaseFileMode(FileMode mode) {
                return mode == FileMode.Append ? FileMode.OpenOrCreate : mode;
            }

            private string fileDir;
            private string fileBase;
            private string fileExt;
            private int fileDecimals;
            private int nextFileIndex;

        }
        
        private sealed class InvariantStreamWriter : StreamWriter {
            public InvariantStreamWriter(Stream s, Encoding enc) : base(s, enc) { }

            public InvariantStreamWriter(string path, bool append, Encoding encoding) : base(path, append, encoding) { }

            /// <summary>
            /// Returns new StreamWriter with FormatProvider = CultureInfo.Invariantculture and output encoding UTF8 (no BOM).
            /// </summary>
            /// <param name="stream"></param>
            public InvariantStreamWriter(Stream stream) : base(stream) { }

            public override IFormatProvider FormatProvider { get { return CultureInfo.InvariantCulture; } }
        }

        private bool printDate = true;
        public EpanetTraceListener() { }

        public EpanetTraceListener(Stream stream) : base(stream) { }

        public EpanetTraceListener(string path, bool append, string name) : base(CreateWriter(path, append), name) { }
        public EpanetTraceListener(string path, bool append) : base(CreateWriter(path, append), path) { }

        public EpanetTraceListener(TextWriter writer) : base(writer) { }

        public EpanetTraceListener(Stream stream, string name) : base(stream, name) { }

        public EpanetTraceListener(string path, string name) : base(path, name) { }

        private static TextWriter CreateWriter(string path, bool append) {

            FileMode mode = append ? FileMode.Append : FileMode.Create;

            RollingFileStream fs = new RollingFileStream(path, 0x100000, 10, mode, FileShare.Read);

            InvariantStreamWriter writer = new InvariantStreamWriter(fs, Encoding.Default);

            return writer;
        }

        public EpanetTraceListener(TextWriter writer, string name) : base(writer, name) { }
        public bool PrintDate { get { return this.printDate; } set { this.printDate = value; } }

        public override void WriteLine(string message) {
            base.Write(DateTime.Now.ToString(base.Writer.FormatProvider));
            base.Write(": ");
            base.WriteLine(message);
        }

        private void WriteHeader(string source, TraceEventType eventType, int id) {
            if(this.PrintDate)
                this.Write(DateTime.Now.ToString("G", CultureInfo.InvariantCulture) + ": ");

            // Write(String.Format(CultureInfo.InvariantCulture, "{0} {1}: {2} : ", source, eventType, id.ToString(CultureInfo.InvariantCulture)));
        }

        public override void TraceEvent(TraceEventCache eventCache, string source, TraceEventType eventType, int id, string message) {

            if(this.Filter != null && !this.Filter.ShouldTrace(eventCache, source, eventType, id, message, null, null, null))
                return;

            this.WriteHeader(source, eventType, id);
            this.WriteLine(message);
            // WriteFooter(eventCache);
        }

        public override void TraceEvent(TraceEventCache eventCache, string source, TraceEventType eventType, int id, string format, params object[] args) {
            if(this.Filter != null && !this.Filter.ShouldTrace(eventCache, source, eventType, id, format, args, null, null))
                return;

            this.WriteHeader(source, eventType, id);
            if(args != null && args.Length > 0)
                this.WriteLine(string.Format(CultureInfo.InvariantCulture, format, args));
            else
                this.WriteLine(format);

            // WriteFooter(eventCache);
        }

        public override void TraceData(TraceEventCache eventCache, string source, TraceEventType eventType, int id, object data) {
            if(this.Filter != null && !this.Filter.ShouldTrace(eventCache, source, eventType, id, null, null, data, null))
                return;

            this.WriteHeader(source, eventType, id);
            string datastring = data == null ? string.Empty : data.ToString();

            this.WriteLine(datastring);
            // WriteFooter(eventCache);
        }

        public override void TraceData(TraceEventCache eventCache, string source, TraceEventType eventType, int id, params object[] data) {

            if(this.Filter != null && !this.Filter.ShouldTrace(eventCache, source, eventType, id, null, null, null, data))
                return;

            this.WriteHeader(source, eventType, id);

            StringBuilder sb = new StringBuilder();

            if(data != null) {
                for(int i = 0; i < data.Length; i++) {
                    if(i != 0)
                        sb.Append(", ");
                    if(data[i] != null)
                        sb.Append(data[i]);
                }
            }

            this.WriteLine(sb.ToString());

            // WriteFooter(eventCache);
        }

    }

}
