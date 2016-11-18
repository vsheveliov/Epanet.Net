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

        private bool printDate = true;
        public EpanetTraceListener() { }

        public EpanetTraceListener(Stream stream) : base(stream) { }

        public EpanetTraceListener(string path, bool append, string name) : base(CreateWriter(path, append), name) { }

        public EpanetTraceListener(TextWriter writer) : base(writer) { }

        public EpanetTraceListener(Stream stream, string name) : base(stream, name) { }

        public EpanetTraceListener(string path, string name) : base(path, name) { }

        private static TextWriter CreateWriter(string path, bool append) {

            FileMode mode = append ? FileMode.Append : FileMode.Create;

            RollingFileStream fs = new RollingFileStream(path, 0x1000, 10, mode, FileShare.Read);

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
            if(args != null)
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
