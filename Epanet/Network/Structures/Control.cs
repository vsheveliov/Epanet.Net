/*
 * Copyright (C) 2012  Addition, Lda. (addition at addition dot pt)
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
///<summary>Control statement</summary>
public class Control
{
    ///<summary>Control condition type</summary>
    public enum ControlType {
        LOWLEVEL = 0, // act when grade above set level
        HILEVEL = 1, // act when grade below set level
        TIMER = 2, // act when time of day occurs
        TIMEOFDAY = 3, // act when set time reached
    }

    ///<summary>Control grade.</summary>
    private double      Grade;
    ///<summary>Assigned link reference.</summary>
    private Link        Link;
    ///<summary>Assigned node reference.</summary>
    private Node        Node;
    ///<summary>New link setting.</summary>
    private double      Setting;
    ///<summary>New link status.</summary>
    private Link.StatType    Status;
    ///<summary>Control time (in seconds).</summary>
    private long        Time;
    ///<summary>Control type</summary>
    private ControlType Type;


    public double getGrade() {
        return Grade;
    }

    public Link getLink() {
        return Link;
    }

    public Node getNode() {
        return Node;
    }

    public double getSetting() {
        return Setting;
    }

    public Link.StatType getStatus() {
        return Status;
    }

    public long getTime() {
        return Time;
    }

    public ControlType getType() {
        return Type;
    }

    public void setGrade(double grade) {
        Grade = grade;
    }

    public void setLink(Link link) {
        Link = link;
    }

    public void setNode(Node node) {
        Node = node;
    }

    public void setSetting(double setting) {
        Setting = setting;
    }

    public void setStatus(Link.StatType status) {
        Status = status;
    }

    public void setTime(long time) {
        Time = time;
    }

    public void setType(ControlType type) {
        Type = type;
    }

}
}