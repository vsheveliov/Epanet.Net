using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace org.addition.epanet.log {
    /// <summary>
    /// Shortcut methods for <see cref="TraceSource"/> logger
    /// </summary>
    public static class TraceExtensions {

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
