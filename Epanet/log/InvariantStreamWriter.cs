using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace org.addition.epanet.log {
    internal sealed class InvariantStreamWriter : StreamWriter {
        public InvariantStreamWriter(Stream s, Encoding enc) : base(s, enc) { }

        public InvariantStreamWriter(string path, bool append, Encoding encoding) : base(path, append, encoding) { }

        /// <summary>
        /// Returns new StreamWriter with FormatProvider = CultureInfo.Invariantculture and output encoding UTF8 (no BOM).
        /// </summary>
        /// <param name="stream"></param>
        public InvariantStreamWriter(Stream stream) : base(stream) { }

        public override IFormatProvider FormatProvider { get { return CultureInfo.InvariantCulture; } }
    }
}
