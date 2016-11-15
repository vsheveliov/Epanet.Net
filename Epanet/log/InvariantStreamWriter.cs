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
using System.Globalization;
using System.IO;
using System.Text;

namespace Epanet.Log {
    public sealed class InvariantStreamWriter : StreamWriter {
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
