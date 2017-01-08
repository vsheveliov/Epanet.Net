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

using System.Diagnostics;

namespace Epanet.Log {
    /// <summary>
    /// Shortcut methods for <see cref="TraceSource"/> logger
    /// </summary>
    public static class TraceExtensions {

        private static object[] GetCaller() {
            var frame = new StackFrame(2,true);

            return new object[] {
                frame.GetMethod().Name,
                frame.GetFileName(),
                frame.GetFileLineNumber()
            };
        }

        #region Critical

        [Conditional("TRACE")]
        public static void Critical(this TraceSource src, string message) {
            src.TraceEvent(TraceEventType.Critical, 0, message);
        }

        [Conditional("TRACE")]
        public static void Critical(this TraceSource src, string format, params object[] args) {
            src.TraceEvent(TraceEventType.Critical, 0, format, args);
        }

        [Conditional("TRACE")]
        public static void Critical(this TraceSource src, object data) {
            src.TraceData(TraceEventType.Critical, 0, data);
        }

        [Conditional("TRACE")]
        public static void Critical(this TraceSource src, params object[] data) {
            src.TraceData(TraceEventType.Critical, 0, data);
        }

        #endregion

        #region Error

        [Conditional("TRACE")]
        public static void Error(this TraceSource src, string message) {
            src.TraceEvent(TraceEventType.Error, 0, message);
        }

        [Conditional("TRACE")]
        public static void Error(this TraceSource src, string format, params object[] args) {
            src.TraceEvent(TraceEventType.Error, 0, format, args);
        }

        [Conditional("TRACE")]
        public static void Error(this TraceSource src, string format, object arg) {
            src.TraceEvent(TraceEventType.Error, 0, format, arg);
        }

        [Conditional("TRACE")]
        public static void Error(this TraceSource src, object data) {
            src.TraceData(TraceEventType.Error, 0, data);
        }

        public static void Error(this TraceSource src, params object[] data) {
            src.TraceData(TraceEventType.Error, 0, data);
        }

        #endregion

        #region Warning

        [Conditional("TRACE")]
        public static void Warning(this TraceSource src, string message) {
            src.TraceEvent(TraceEventType.Warning, 0, message);
        }

        [Conditional("TRACE")]
        public static void Warning(this TraceSource src, string format, params object[] args) {
            src.TraceEvent(TraceEventType.Warning, 0, format, args);
        }

        [Conditional("TRACE")]
        public static void Warning(this TraceSource src, object data) {
            src.TraceData(TraceEventType.Warning, 0, data);
        }

        [Conditional("TRACE")]
        public static void Warning(this TraceSource src, params object[] data) {
            src.TraceData(TraceEventType.Warning, 0, data);
        }

        #endregion

        #region Information

        [Conditional("TRACE")]
        public static void Information(this TraceSource src, string message) {
            src.TraceEvent(TraceEventType.Information, 0, message);
        }

        [Conditional("TRACE")]
        public static void Information(this TraceSource src, string format, params object[] args) {
            src.TraceEvent(TraceEventType.Information, 0, format, args);
        }

        [Conditional("TRACE")]
        public static void Information(this TraceSource src, object data) {
            src.TraceData(TraceEventType.Information, 0, data);
        }

        [Conditional("TRACE")]
        public static void Information(this TraceSource src, params object[] data) {
            src.TraceData(TraceEventType.Information, 0, data);
        }

        #endregion

        #region Information

        [Conditional("TRACE")]
        public static void Verbose(this TraceSource src, string message) {
            src.TraceEvent(TraceEventType.Verbose, 0, message);
        }

        [Conditional("TRACE")]
        public static void Verbose(this TraceSource src, string format, params object[] args) {
            src.TraceEvent(TraceEventType.Verbose, 0, format, args);
        }

        [Conditional("TRACE")]
        public static void Verbose(this TraceSource src, object data) {
            src.TraceData(TraceEventType.Verbose, 0, data);
        }

        [Conditional("TRACE")]
        public static void Verbose(this TraceSource src, params object[] data) {
            src.TraceData(TraceEventType.Verbose, 0, data);
        }

        #endregion
    }
}
