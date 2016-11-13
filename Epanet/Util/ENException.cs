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
using System.Resources;

namespace org.addition.epanet.util {

    ///<summary>Epanet exception codes handler.</summary>
    public class ENException:System.Exception {

        ///<summary>Array of arguments to be used in the error string creation.</summary>
        private readonly object[] arguments;

        ///<summary>Epanet error code.</summary>
        private readonly ErrorCode codeID;

        /**
     * Get error code.
     * @return Code id.
     */
        public ErrorCode getCodeID() { return codeID; }

        /**
     * Contructor from error code id.
     * @param id Error code id.
     */

        public ENException(ErrorCode id) {
            arguments = null;
            codeID = id;
        }

        /**
     * Contructor from error code id and multiple arguments.
     * @param id Error code id.
     * @param arg Extra arguments.
     */

        public ENException(ErrorCode id, params object[] arg) {
            codeID = id;
            arguments = arg;
        }

        /**
     * Contructor from other exception and multiple arguments.
     * @param e
     * @param arg
     */

        public ENException(ENException e, params object[] arg) {
            arguments = arg;
            codeID = e.getCodeID();
        }

        ///<summary>Get arguments array.</summary>
        public object[] getArguments() { return arguments; }

        /**
     * Handles the exception string conversion.
     * @return Final error string.
     */

        public override string ToString() {
            string str;
            string name = "ERR" + (int)this.codeID;

            try {
                str = Epanet.Properties.Error.ResourceManager.GetString(name);
            }
            catch (Exception) {
                str = null;
            }
        

        if(str==null)
            return string.Format("Unknown error message ({0})",codeID);

        if(arguments!=null)
            return string.Format(str,arguments);

        return str;
    }

}
}