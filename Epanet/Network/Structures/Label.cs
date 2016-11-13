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

namespace org.addition.epanet.network.structures {

///<summary>Text label</summary>
public class Label {
    ///<summary>Label position.</summary>
    Point position;
    ///<summary>Label text.</summary>
    string text;

    public Label() {
        position = new Point();
        text = "";
    }

    public Point getPosition() {
        return position;
    }

    public string getText() {

        return text;
    }

    public void setPosition(Point position) {
        this.position = position;
    }

    public void setText(string text) {
        this.text = text;
    }
}
}