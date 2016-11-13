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

namespace org.addition.epanet.msx
{


    public interface VariableInterface
    {
        double getValue(int id);
        int getIndex(string id);
    }

    public interface IVariable
    {
        double GetValue(int id);
        int GetIndex(string id);
    }

    public sealed class VariableContainer : IVariable
    {
        public delegate int GetIndexDelegate(string id);

        public delegate double GetValueDelegate(int id);

        private readonly GetIndexDelegate _getIndexPtr;
        private readonly GetValueDelegate _getValuePtr;

        public VariableContainer(GetValueDelegate pipeValuePtr, GetIndexDelegate getIndexPtr)
        {
            if (pipeValuePtr == null) throw new ArgumentNullException("pipeValue");
            if (getIndexPtr == null) throw new ArgumentNullException("tankValue");

            this._getValuePtr = pipeValuePtr;
            this._getIndexPtr = getIndexPtr;
        }

        public double GetValue(int id) { return this._getValuePtr(id); }
        public int GetIndex(string id) { return this._getIndexPtr(id); }
    }
}