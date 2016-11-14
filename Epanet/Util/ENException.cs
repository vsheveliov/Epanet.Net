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

namespace org.addition.epanet.util {

    ///<summary>Epanet exception codes handler.</summary>
    public class ENException:Exception {

        ///<summary>Array of arguments to be used in the error string creation.</summary>
        private readonly object[] arguments;

        ///<summary>Epanet error code.</summary>
        private readonly ErrorCode codeID;

        ///<summary>Get error code.</summary>
        /// <returns>Code id.</returns>
        public ErrorCode getCodeID() { return codeID; }

        ///<summary>Contructor from error code id.</summary>
        ///<param name="id">Error code id.</param>

        public ENException(ErrorCode id) {
            arguments = null;
            codeID = id;
        }

        /// <summary>Contructor from error code id and multiple arguments.</summary>
        ///  <param name="id">Error code id.</param>
        /// <param name="arg">Extra arguments.</param>
        ///  
        public ENException(ErrorCode id, params object[] arg) {
            codeID = id;
            arguments = arg;
        }

        ///<summary>Contructor from other exception and multiple arguments.</summary>
        public ENException(ENException e, params object[] arg) {
            arguments = arg;
            codeID = e.getCodeID();
        }

        ///<summary>Get arguments array.</summary>
        public object[] Arguments { get { return this.arguments; } }

        ///<summary>Handles the exception string conversion.</summary>
        /// <returns>Final error string.</returns>
        public override string Message {
            get {
                string str;
                string name = "ERR" + (int)this.codeID;

                try {
                    str = Epanet.Properties.Error.ResourceManager.GetString(name);
                }
                catch (Exception) {
                    str = null;
                }


                if (str == null)
                    return string.Format("Unknown error message ({0})", codeID);

                if (arguments != null)
                    return string.Format(str, arguments);

                return str;
            }
        }
    }

}