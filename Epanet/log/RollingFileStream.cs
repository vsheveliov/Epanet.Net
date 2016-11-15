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
using System.IO;
using System.Text;

namespace Epanet.Log {
    public sealed class RollingFileStream : FileStream {

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
            this._fileDir = Path.GetDirectoryName(fullPath);
            this._fileBase = Path.GetFileNameWithoutExtension(fullPath);
            this._fileExt = Path.GetExtension(fullPath);

            this._fileDecimals = 1;
            int decimalBase = 10;
            while(decimalBase < this.MaxFileCount) {
                ++this._fileDecimals;
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
                    this._nextFileIndex = iFile + 1;
            }
            if(this._nextFileIndex == this.MaxFileCount)
                this._nextFileIndex = 0;
            this.Seek(0, SeekOrigin.End);
            break;
            }
        }

        private void BackupAndResetStream() {
            this.Flush();
            File.Copy(this.Name, this.GetBackupFileName(this._nextFileIndex), true);
            this.SetLength(0);

            ++this._nextFileIndex;
            if(this._nextFileIndex >= this.MaxFileCount)
                this._nextFileIndex = 0;
        }

        private string GetBackupFileName(int index) {
            StringBuilder format = new StringBuilder();
            format.AppendFormat("D{0}", this._fileDecimals);
            StringBuilder sb = new StringBuilder();
            if(this._fileExt.Length > 0)
                sb.AppendFormat("{0}{1}{2}", this._fileBase, index.ToString(format.ToString()), this._fileExt);
            else
                sb.AppendFormat("{0}{1}", this._fileBase, index.ToString(format.ToString()));
            return Path.Combine(this._fileDir, sb.ToString());
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
}
