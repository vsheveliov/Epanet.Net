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
                Init(path, maxFileLength, maxFileCount, mode);
            }

            public RollingFileStream(string path, long maxFileLength, int maxFileCount, FileMode mode, FileShare share)
                : base(path, BaseFileMode(mode), FileAccess.Write, share) {
                Init(path, maxFileLength, maxFileCount, mode);
            }

            public RollingFileStream(
                string path,
                long maxFileLength,
                int maxFileCount,
                FileMode mode,
                FileShare share,
                int bufferSize)
                : base(path, BaseFileMode(mode), FileAccess.Write, share, bufferSize) {
                Init(path, maxFileLength, maxFileCount, mode);
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
                Init(path, maxFileLength, maxFileCount, mode);
            }

            public override bool CanRead { get { return false; } }

            public override void Write(byte[] array, int offset, int count) {
                while(true) {
                    int actualCount = Math.Min(count, array.GetLength(0));

                    if(Position + actualCount <= MaxFileLength) {
                        base.Write(array, offset, count);
                        break;
                    }

                    if(CanSplitData) {
                        int partialCount = (int)(Math.Max(MaxFileLength, Position) - Position);
                        base.Write(array, offset, partialCount);
                        offset += partialCount;
                        count = actualCount - partialCount;
                    }
                    else {
                        if(count > MaxFileLength) {
                            throw new ArgumentOutOfRangeException("count", "Buffer size exceeds maximum file length");
                        }
                    }
                    BackupAndResetStream();
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

                MaxFileLength = maxFileLength;
                MaxFileCount = maxFileCount;
                // this.CanSplitData = true;

                string fullPath = Path.GetFullPath(path);
                _fileDir = Path.GetDirectoryName(fullPath);
                _fileBase = Path.GetFileNameWithoutExtension(fullPath);
                _fileExt = Path.GetExtension(fullPath);

                _fileDecimals = 1;
                int decimalBase = 10;

                while(decimalBase < MaxFileCount) {
                    _fileDecimals++;
                    decimalBase *= 10;
                }

                switch(mode) {
                case FileMode.Create:
                case FileMode.CreateNew:
                case FileMode.Truncate:
                // Delete old files
                for(int iFile = 0; iFile < MaxFileCount; ++iFile) {
                    string file = GetBackupFileName(iFile);
                    if(File.Exists(file))
                        File.Delete(file);
                }

                break;

                default:
                // Position file pointer to the last backup file
                for(int iFile = 0; iFile < MaxFileCount; ++iFile) {
                    if(File.Exists(GetBackupFileName(iFile)))
                        _nextFileIndex = iFile + 1;
                }

                if(_nextFileIndex == MaxFileCount)
                    _nextFileIndex = 0;
                Seek(0, SeekOrigin.End);
                break;
                }
            }

            private void BackupAndResetStream() {
                Flush();
                File.Copy(Name, GetBackupFileName(_nextFileIndex), true);
                SetLength(0);

                _nextFileIndex++;

                if(_nextFileIndex >= MaxFileCount)
                    _nextFileIndex = 0;
            }

            private string GetBackupFileName(int index) {
                string sIndex = index.ToString("D{0}" + _fileDecimals);

                string path2 = string.IsNullOrEmpty(_fileExt)
                    ? string.Format("{0}{1}", _fileBase, sIndex)
                    : string.Format("{0}{1}{2}", _fileBase, sIndex, _fileExt);

                return Path.Combine(_fileDir, path2);
            }

            private static FileMode BaseFileMode(FileMode mode) {
                return mode == FileMode.Append ? FileMode.OpenOrCreate : mode;
            }

            private string _fileDir;
            private string _fileBase;
            private string _fileExt;
            private int _fileDecimals;
            private int _nextFileIndex;

        }
        
        private sealed class InvariantStreamWriter : StreamWriter {
            public InvariantStreamWriter(Stream s, Encoding enc) : base(s, enc) { }

            public InvariantStreamWriter(string path, bool append, Encoding encoding) : base(path, append, encoding) { }

            public override IFormatProvider FormatProvider { get { return CultureInfo.InvariantCulture; } }
        }

        private bool _printDate = true;
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
        public bool PrintDate { get { return _printDate; } set { _printDate = value; } }

        public override void WriteLine(string message) {
            base.Write(DateTime.Now.ToString(base.Writer.FormatProvider));
            base.Write(": ");
            base.WriteLine(message);
        }

        private void WriteHeader(string source, TraceEventType eventType, int id) {
            if(PrintDate)
                Write(DateTime.Now.ToString("G", CultureInfo.InvariantCulture) + ": ");

            // Write(String.Format(CultureInfo.InvariantCulture, "{0} {1}: {2} : ", source, eventType, id.ToString(CultureInfo.InvariantCulture)));
        }

        public override void TraceEvent(TraceEventCache eventCache, string source, TraceEventType eventType, int id, string message) {

            if(Filter != null && !Filter.ShouldTrace(eventCache, source, eventType, id, message, null, null, null))
                return;

            WriteHeader(source, eventType, id);
            WriteLine(message);
            // WriteFooter(eventCache);
        }

        public override void TraceEvent(TraceEventCache eventCache, string source, TraceEventType eventType, int id, string format, params object[] args) {
            if(Filter != null && !Filter.ShouldTrace(eventCache, source, eventType, id, format, args, null, null))
                return;

            WriteHeader(source, eventType, id);
            if(args != null && args.Length > 0)
                WriteLine(string.Format(CultureInfo.InvariantCulture, format, args));
            else
                WriteLine(format);

            // WriteFooter(eventCache);
        }

        public override void TraceData(TraceEventCache eventCache, string source, TraceEventType eventType, int id, object data) {
            if(Filter != null && !Filter.ShouldTrace(eventCache, source, eventType, id, null, null, data, null))
                return;

            WriteHeader(source, eventType, id);
            string datastring = data == null ? string.Empty : data.ToString();

            WriteLine(datastring);
            // WriteFooter(eventCache);
        }

        public override void TraceData(TraceEventCache eventCache, string source, TraceEventType eventType, int id, params object[] data) {

            if(Filter != null && !Filter.ShouldTrace(eventCache, source, eventType, id, null, null, null, data))
                return;

            WriteHeader(source, eventType, id);

            StringBuilder sb = new StringBuilder();

            if(data != null) {
                for(int i = 0; i < data.Length; i++) {
                    if(i != 0)
                        sb.Append(", ");
                    if(data[i] != null)
                        sb.Append(data[i]);
                }
            }

            WriteLine(sb.ToString());

            // WriteFooter(eventCache);
        }

    }

}
