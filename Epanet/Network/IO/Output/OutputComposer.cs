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

using Epanet.Enums;

namespace Epanet.Network.IO.Output {

    ///<summary>Abstract class with factory for INP and XLSX composers.</summary>
    public abstract class OutputComposer {

        ///<summary>Composer creation method.</summary>
        /// <param name="type">Composer type.</param>
        /// <returns>Composer reference.</returns>
        public static OutputComposer Create(FileType type) {
            switch (type) {
            case FileType.INP_FILE:       return new InpComposer();
            case FileType.XML_FILE:       return new XMLComposer(false);
            case FileType.XML_GZ_FILE:    return new XMLComposer(true);
            }
            return null;
        }

        ///<summary>Abstract method to implement the output file creation.</summary>
        /// <param name="net">Hydraulic network reference.</param>
        /// <param name="fileName">File name reference.</param>
        public abstract void Composer(Network net, string fileName);

    }

}