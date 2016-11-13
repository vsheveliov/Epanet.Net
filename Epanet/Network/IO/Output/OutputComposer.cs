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

using System.IO;

namespace org.addition.epanet.network.io.output {

///<summary>Abstract class with factory for INP and XLSX composers.</summary>
public abstract class OutputComposer {

    /**
     * Composer creation method.
     * @param type Composer type.
     * @return Composer reference.
     */
    public static OutputComposer create(Network.FileType type){
        switch (type) {
            case Network.FileType.INP_FILE:
                return new InpComposer();
            case Network.FileType.EXCEL_FILE:
                return new ExcelComposer();
            case Network.FileType.XML_FILE:
                return new XMLComposer(false);
            case Network.FileType.XML_GZ_FILE:
                return new XMLComposer(true);
        }
        return null;
    }

    /**
     * Abstract method to implement the output file creation.
     * @param net Hydraulic network reference.
     * @param f Abstract file reference.
     * @throws ENException
     */
    public abstract void composer(Network net, string fileName);

}
}