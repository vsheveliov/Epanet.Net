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
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

using Epanet.Enums;
using Epanet.Hydraulic.IO;
using Epanet.Hydraulic.Structures;
using Epanet.Network.Structures;
using Epanet.Quality.Structures;
using Epanet.Util;

using EpanetNetwork = Epanet.Network.Network;

namespace Epanet.Quality {

    ///<summary>Single species water quality simulation class.</summary>
    public class QualitySim {
        ///<summary>Bulk reaction units conversion factor.</summary>
        private readonly double _bucf;
        ///<summary>Current hydraulic time counter [seconds].</summary>
        private long _htime;

        private readonly QualityNode[] _juncs;
        private readonly QualityLink[] _links;
        private readonly EpanetNetwork _net;
        private readonly QualityNode[] _nodes;
        ///<summary>Number of reported periods.</summary>
        private int _nperiods;

        ///<summary>Current quality time (sec)</summary>
        private long _qtime;

        ///<summary>Reaction indicator.</summary>
        private readonly bool _reactflag;
        ///<summary>Current report time counter [seconds].</summary>
        private long _rtime;
        ///<summary>Schmidt Number.</summary>
        private readonly double _sc;
        private readonly QualityTank[] _tanks;
        private readonly QualityNode _traceNode;
        ///<summary>Tank reaction units conversion factor.</summary>
        private readonly double _tucf;
        ///<summary>Avg. bulk reaction rate.</summary>
        private double _wbulk;
        ///<summary>Avg. mass inflow.</summary>
        private double _wsource;
        ///<summary>Avg. tank reaction rate.</summary>
        private double _wtank;
        ///<summary>Avg. wall reaction rate.</summary>
        private double _wwall;

        [NonSerialized]
        private readonly double _elevUnits;
        [NonSerialized]
        private readonly QualType _qualflag;


        ///<summary>Initializes WQ solver system</summary>

        public QualitySim(EpanetNetwork net, TraceSource ignored) {
//        this.log = log;
            _net = net;
            
            _nodes = net.Nodes.Select(QualityNode.Create).ToArray();
            _tanks = _nodes.OfType<QualityTank>().ToArray();
            _juncs = _nodes.Where(x => !(x is QualityTank)).ToArray();
            _links = net.Links.Select(n => new QualityLink(net.Nodes, _nodes, n)).ToArray();
            

            /*
            this.nodes = new List<QualityNode>(net.Nodes.Count);
            this.links = new List<QualityLink>(net.Links.Count);
            this.tanks = new List<QualityTank>(net.Tanks.Count());
            this.juncs = new List<QualityNode>(net.Junctions.Count());
            
            foreach (Node n in net.Nodes) {
                QualityNode qN = QualityNode.Create(n);

                this.nodes.Add(qN);

                var tank = qN as QualityTank;

                if (tank != null)
                    this.tanks.Add(tank);
                else
                    this.juncs.Add(qN);
            }

            foreach (Link n  in  net.Links)
                this.links.Add(new QualityLink(net.Nodes, this.nodes, n));

            */

            _bucf = 1.0;
            _tucf = 1.0;
            _reactflag = false;

            _qualflag = _net.QualFlag;

            if (_qualflag != QualType.NONE) {
                if (_qualflag == QualType.TRACE) {
                    foreach (QualityNode qN  in  _nodes)
                        if (qN.Node.Name.Equals(_net.TraceNode, StringComparison.OrdinalIgnoreCase)) {
                            _traceNode = qN;
                            _traceNode.Quality = 100.0;
                            break;
                        }
                }

                if (_net.Diffus > 0.0)
                    _sc = _net.Viscos / _net.Diffus;
                else
                    _sc = 0.0;

                _bucf = GetUcf(_net.BulkOrder);
                _tucf = GetUcf(_net.TankOrder);

                _reactflag = GetReactflag();
            }


            _wbulk = 0.0;
            _wwall = 0.0;
            _wtank = 0.0;
            _wsource = 0.0;

            _htime = 0;
            _rtime = _net.RStart;
            _qtime = 0;
            _nperiods = 0;
            _elevUnits = _net.FieldsMap.GetUnits(FieldType.ELEV);
        }

        ///<summary>Accumulates mass flow at nodes and updates nodal quality.</summary>
        /// <param name="dt">Step duration in seconds.</param>
        private void Accumulate(long dt) {
            //  Re-set memory used to accumulate mass & volume
            foreach (QualityNode qN  in  _nodes) {
                qN.VolumeIn = 0;
                qN.MassIn = 0;
                qN.SourceContribution = 0;
            }

            foreach (QualityLink qL  in  _links) {
                QualityNode j = qL.DownStreamNode; //  Downstream node
                if (qL.Segments.Count > 0) //  Accumulate concentrations
                {
                    j.MassIn = j.MassIn + qL.Segments.First.Value.C;
                    j.VolumeIn = j.VolumeIn + 1;
                }
                j = qL.UpStreamNode;
                if (qL.Segments.Count > 0) // Upstream node
                { // Accumulate concentrations
                    j.MassIn = j.MassIn + qL.Segments.Last.Value.C;
                    j.VolumeIn = j.VolumeIn + 1;
                }
            }

            foreach (QualityNode qN  in  _nodes)
                if (qN.VolumeIn > 0.0)
                    qN.SourceContribution = qN.MassIn / qN.VolumeIn;

            //  Move mass from first segment of each pipe into downstream node
            foreach (QualityNode qN  in  _nodes) {
                qN.VolumeIn = 0;
                qN.MassIn = 0;
            }

            foreach (QualityLink qL  in  _links) {
                QualityNode j = qL.DownStreamNode;
                double v = Math.Abs(qL.Flow) * dt;


                while (v > 0.0) {
                    if (qL.Segments.Count == 0)
                        break;

                    QualitySegment seg = qL.Segments.First.Value;

                    // Volume transported from this segment is
                    // minimum of flow volume & segment volume
                    // (unless leading segment is also last segment)
                    double vseg = seg.V;
                    vseg = Math.Min(vseg, v);

                    if (qL.Segments.Count == 1)
                        vseg = v;

                    double cseg = seg.C;
                    j.VolumeIn = j.VolumeIn + vseg;
                    j.MassIn = j.MassIn + vseg * cseg;

                    v -= vseg;

                    // If all of segment's volume was transferred, then
                    // replace leading segment with the one behind it
                    // (Note that the current seg is recycled for later use.)
                    if (v >= 0.0 && vseg >= seg.V) {
                        qL.Segments.RemoveFirst();
                    }
                    else {
                        seg.V -= vseg;
                    }
                }
            }
        }

        ///<summary>Computes average quality in link.</summary>
        double Avgqual(QualityLink ql) {
            double vsum = 0.0,
                   msum = 0.0;

            if (_qualflag == QualType.NONE)
                return 0.0;

            foreach (QualitySegment seg  in  ql.Segments) {
                vsum += seg.V;
                msum += seg.C * seg.V;
            }

            if (vsum > 0.0)
                return msum / vsum;
            else
                return (ql.FirstNode.Quality + ql.SecondNode.Quality) / 2.0;
        }

        ///<summary>Computes bulk reaction rate (mass/volume/time).</summary>
        private double Bulkrate(double c, double kb, double order) {
            double c1;

            if (order == 0.0)
                c = 1.0;
            else if (order < 0.0) {
                c1 = _net.CLimit + Utilities.GetSignal(kb) * c;
                if (Math.Abs(c1) < Constants.TINY) c1 = Utilities.GetSignal(c1) * Constants.TINY;
                c /= c1;
            }
            else {
                if (_net.CLimit == 0.0)
                    c1 = c;
                else
                    c1 = Math.Max(0.0, Utilities.GetSignal(kb) * (_net.CLimit - c));

                if (order == 1.0)
                    c = c1;
                else if (order == 2.0)
                    c = c1 * c;
                else
                    c = c1 * Math.Pow(Math.Max(0.0, c), order - 1.0);
            }


            if (c < 0) c = 0;
            return kb * c;
        }


        ///<summary>Retrieves hydraulic solution and hydraulic time step for next hydraulic event.</summary>
        private void GetHyd(BinaryWriter outStream, HydraulicReader hydSeek) {
            AwareStep step = hydSeek.GetStep(_htime);
            LoadHydValues(step);

            _htime += step.Step;

            if (_htime >= _rtime) {
                SaveOutput(outStream);
                _nperiods++;
                _rtime += _net.RStep;
            }


            if (_qualflag != QualType.NONE && _qtime < _net.Duration) {
                if (_reactflag && _qualflag != QualType.AGE)
                    Ratecoeffs();

                if (_qtime == 0)
                    Initsegs();
                else
                    Reorientsegs();
            }
        }

        public long Qtime { get { return _qtime; } }

        ///<summary>Checks if reactive chemical being simulated.</summary>
        private bool GetReactflag() {
            switch (_qualflag) {
                case QualType.TRACE:
                    return false;
                case QualType.AGE:
                    return true;
                default:
                    foreach (QualityLink qL  in  _links) {
                        if (qL.Link.Type <= LinkType.PIPE) {
                            if (qL.Link.Kb != 0.0 || qL.Link.Kw != 0.0)
                                return true;
                        }
                    }
                    foreach (QualityTank qT  in  _tanks)
                        if (((Tank)qT.Node).Kb != 0.0)
                            return true;

                    break;
            }

            return false;
        }

        ///<summary>Local method to compute unit conversion factor for bulk reaction rates.</summary>
        private static double GetUcf(double order) {
            if (order < 0.0) order = 0.0;
            return order == 1.0 ? 1.0 : 1.0 / Math.Pow(Constants.LperFT3, order - 1.0);
        }

        ///<summary>Initializes water quality segments.</summary>
        private void Initsegs() {
            foreach (QualityLink qL  in  _links) {
                qL.FlowDir = true;

                if (qL.Flow < 0.0)
                    qL.FlowDir = false;

                qL.Segments.Clear();

                double c;

                // Find quality of downstream node
                QualityNode j = qL.DownStreamNode;
               
                if (j is QualityTank) c = ((QualityTank)j).Concentration;
                else c = j.Quality;

                // Fill link with single segment with this quality
                qL.Segments.AddLast(new QualitySegment(qL.LinkVolume, c));
            }

            // Initialize segments in tanks that use them
            foreach (QualityTank qT  in  _tanks) {
                Tank tank = (Tank)qT.Node;

                // Skip reservoirs & complete mix tanks
                if(tank.Type == NodeType.RESERV || tank.MixModel == MixType.MIX1)
                    continue;

                double c = qT.Concentration;

                qT.Segments.Clear();

                // Add 2 segments for 2-compartment model
                if(tank.MixModel == MixType.MIX2) {
                    double v = Math.Max(0, qT.Volume - tank.V1Max);
                    qT.Segments.AddLast(new QualitySegment(v, c));
                    v = qT.Volume - v;
                    qT.Segments.AddLast(new QualitySegment(v, c));
                }
                else {
                    // Add one segment for FIFO & LIFO models
                    double v = qT.Volume;
                    qT.Segments.AddLast(new QualitySegment(v, c));
                }
            }
        }

        ///<summary>Load hydraulic simulation data to the water quality structures.</summary>
        private void LoadHydValues(AwareStep step) {
            int count = 0;
            foreach (QualityNode qN  in  _nodes) {
                qN.Demand = step.GetNodeDemand(count++, qN.Node, null);
            }

            count = 0;
            foreach (QualityLink qL  in  _links) {
                qL.Flow = step.GetLinkFlow(count++, qL.Link, null);
            }
        }

        ///<summary>Updates WQ conditions until next hydraulic solution occurs (after tstep secs.)</summary>
        private long Nextqual(BinaryWriter outStream) {
            long hydstep = _htime - _qtime;

            if (_qualflag != QualType.NONE && hydstep > 0)
                Transport(hydstep);

            long tstep = hydstep;
            _qtime += hydstep;

            if (tstep == 0)
                SaveFinaloutput(outStream);

            return tstep;
        }

        private long Nextqual() {
            long hydstep = _htime - _qtime;

            if (_qualflag != QualType.NONE && hydstep > 0)
                Transport(hydstep);

            long tstep = hydstep;
            _qtime += hydstep;

            return tstep;
        }

        ///<summary>Finds wall reaction rate coeffs.</summary>
        private double Piperate(QualityLink ql) {
            double a, d, u, kf, kw, y, re, sh;

            d = ql.Link.Diameter;

            if (_sc == 0.0) {
                if (_net.WallOrder == 0.0)
                    return Constants.BIG;
                else
                    return ql.Link.Kw * (4.0 / d) / _elevUnits;
            }

            a = Math.PI * d * d / 4.0;
            u = Math.Abs(ql.Flow) / a;
            re = u * d / _net.Viscos;

            if (re < 1.0)
                sh = 2.0;

            else if (re >= 2300.0)
                sh = 0.0149 * Math.Pow(re, 0.88) * Math.Pow(_sc, 0.333);
            else {
                y = d / ql.Link.Lenght * re * _sc;
                sh = 3.65 + 0.0668 * y / (1.0 + 0.04 * Math.Pow(y, 0.667));
            }


            kf = sh * _net.Diffus / d;


            if (_net.WallOrder == 0.0) return kf;


            kw = ql.Link.Kw / _elevUnits;
            kw = 4.0 / d * kw * kf / (kf + Math.Abs(kw));
            return kw;
        }

        ///<summary>Computes new quality in a pipe segment after reaction occurs.</summary>
        private double Pipereact(QualityLink ql, double c, double v, long dt) {
            double cnew, dc, dcbulk, dcwall, rbulk, rwall;


            if (_qualflag == QualType.AGE) return c + dt / 3600.0;


            rbulk = Bulkrate(c, ql.Link.Kb, _net.BulkOrder) * _bucf;
            rwall = Wallrate(c, ql.Link.Diameter, ql.Link.Kw, ql.FlowResistance);


            dcbulk = rbulk * dt;
            dcwall = rwall * dt;


            if (_htime >= _net.RStart) {
                _wbulk += Math.Abs(dcbulk) * v;
                _wwall += Math.Abs(dcwall) * v;
            }


            dc = dcbulk + dcwall;
            cnew = c + dc;
            cnew = Math.Max(0.0, cnew);
            return cnew;
        }

        ///<summary>Determines wall reaction coeff. for each pipe.</summary>
        private void Ratecoeffs() {
            foreach (QualityLink ql  in  _links) {
                double kw = ql.Link.Kw;
                if (kw != 0.0)
                    kw = Piperate(ql);

                ql.FlowResistance = kw;
                //ql.setReactionRate(0.0);
            }
        }

        ///<summary>Creates new segments in outflow links from nodes.</summary>
        /// <param name="dt">Step duration in seconds.</param>
        private void Release(long dt) {
            foreach (QualityLink qL  in  _links) {
                if (qL.Flow == 0.0)
                    continue;

                // Find flow volume released to link from upstream node
                // (NOTE: Flow volume is allowed to be > link volume.)
                QualityNode qN = qL.UpStreamNode;
                double q = Math.Abs(qL.Flow);
                double v = q * dt;

                // Include source contribution in quality released from node.
                double c = qN.Quality + qN.SourceContribution;

                // If link has a last seg, check if its quality
                // differs from that of the flow released from node.
                if (qL.Segments.Count > 0) {
                    QualitySegment seg = qL.Segments.Last.Value;

                    // Quality of seg close to that of node
                    if (Math.Abs(seg.C - c) < _net.Ctol) {
                        seg.C = (seg.C * seg.V + c * v) / (seg.V + v);
                        seg.V += v;
                    }
                    else // Otherwise add a new seg to end of link
                        qL.Segments.AddLast(new QualitySegment(v, c));
                }
                else // If link has no segs then add a new one.
                    qL.Segments.AddLast(new QualitySegment(qL.LinkVolume, c));
            }
        }

        ///<summary>Re-orients segments (if flow reverses).</summary>
        private void Reorientsegs() {
            foreach (QualityLink qL  in  _links) {
                bool newdir = true;

                if (qL.Flow == 0.0)
                    newdir = qL.FlowDir;
                else if (qL.Flow < 0.0)
                    newdir = false;

                if (newdir != qL.FlowDir) {
                    qL.Segments.Reverse();
                    qL.FlowDir = newdir;
                }
            }
        }

        ///<summary>Write the number of report periods written in the binary output file.</summary>
        private void SaveFinaloutput(BinaryWriter bw) {
            bw.Write((int)_nperiods);
        }

        ///<summary>Save links and nodes species concentrations for the current step.</summary>
        private void SaveOutput(BinaryWriter bw) {
            foreach (QualityNode qN  in  _nodes)
                bw.Write((float)qN.Quality);

            foreach (QualityLink qL  in  _links)
                bw.Write((float)Avgqual(qL));
        }

        /// <summary>Run the water quality simulation.</summary>
        /// <param name="hydFile">The hydraulic output file name generated previously.</param>
        /// <param name="qualFile">The output file name were the water quality simulation results will be written.</param>  
        public void Simulate(string hydFile, string qualFile) {

            using (var bos = File.OpenWrite(qualFile))
                Simulate(hydFile, bos);

        }

        /// <summary>Run the water quality simulation.</summary>
        /// <param name="hydFile">The hydraulic output file generated previously.</param>
        /// <param name="out">The output stream were the water quality simulation results will be written.</param>
        ///  
        void Simulate(string hydFile, Stream @out) {
            BinaryWriter bw = new BinaryWriter(@out);
            bw.Write((int)_net.Nodes.Count);
            bw.Write((int)_net.Links.Count);
            long tstep;

            using (var hydraulicReader = new HydraulicReader(new BinaryReader(File.OpenRead(hydFile))))
                do {
                    if (_qtime == _htime)
                        GetHyd(bw, hydraulicReader);


                    tstep = Nextqual(bw);
                } while (tstep > 0);

        }

        /// <summary>Simulate water quality during one hydraulic step.</summary>
        public bool SimulateSingleStep(List<SimulationNode> hydNodes, List<SimulationLink> hydLinks, long hydStep) {
            int count = 0;
            foreach (QualityNode qN  in  _nodes) {
                qN.Demand = hydNodes[count++].SimDemand;
            }

            count = 0;
            foreach (QualityLink qL  in  _links) {
                SimulationLink hL = hydLinks[count++];
                qL.Flow = hL.SimStatus <= StatType.CLOSED ? 0d : hL.SimFlow;
            }

            _htime += hydStep;

            if (_qualflag != QualType.NONE && _qtime < _net.Duration) {
                if (_reactflag && _qualflag != QualType.AGE)
                    Ratecoeffs();

                if (_qtime == 0)
                    Initsegs();
                else
                    Reorientsegs();
            }

            long tstep = Nextqual();

            return tstep != 0;
        }

        ///<summary>Computes contribution (if any) of mass additions from WQ sources at each node.</summary>
        /// <param name="dt">Step duration in seconds.</param>
        private void Sourceinput(long dt) {
            double qcutoff = 10.0 * Constants.TINY;

            foreach (QualityNode qN  in  _nodes)
                qN.SourceContribution = 0;


            if (_qualflag != QualType.CHEM)
                return;

            foreach (QualityNode qN  in  _nodes) {
                QualSource source = qN.Node.QualSource;

                // Skip node if no WQ source
                if (source == null)
                    continue;

                if (source.C0 == 0.0)
                    continue;

                // Find total flow volume leaving node
                double volout = qN.Node.Type == NodeType.JUNC ? qN.VolumeIn : qN.VolumeIn - qN.Demand * dt;

                double qout = volout / dt;

                double massadded = 0;

                // Evaluate source input only if node outflow > cutoff flow
                if (qout > qcutoff) {
                    // Mass added depends on type of source
                    double s = Sourcequal(source);

                    switch (source.Type) {
                    case SourceType.CONCEN:
                        // Only add source mass if demand is negative
                        if (qN.Demand < 0.0) {
                            massadded = -s * qN.Demand * dt;
                            if (qN.Node.Type > NodeType.JUNC)
                                qN.Quality = 0.0;
                        }
                        else
                            massadded = 0.0;
                        break;
                    // Mass Inflow Booster Source:
                    case SourceType.MASS:
                        massadded = s * dt;
                        break;
                    // Setpoint Booster Source:
                    // Mass added is difference between source
                    // & node concen. times outflow volume
                    case SourceType.SETPOINT:
                        if (s > qN.Quality)
                            massadded = (s - qN.Quality) * volout;
                        else
                            massadded = 0.0;
                        break;
                    // Flow-Paced Booster Source:
                    // Mass added = source concen. times outflow volume
                    case SourceType.FLOWPACED:
                        massadded = s * volout;
                        break;
                    }

                    // Source concen. contribution = (mass added / outflow volume)
                    qN.SourceContribution = massadded / volout;

                    // Update total mass added for time period & simulation
                    qN.MassRate = qN.MassRate + massadded;
                    if (_htime >= _net.RStart)
                        _wsource += massadded;
                }
            }

            // Add mass inflows from reservoirs to Wsource
            if (_htime >= _net.RStart) {
                foreach (QualityTank qT  in  _tanks) {
                    if (((Tank)qT.Node).Type == NodeType.RESERV) {
                        double volout = qT.VolumeIn - qT.Demand * dt;
                        if (volout > 0.0)
                            _wsource += volout * qT.Concentration;
                    }
                }
            }
        }

        ///<summary>Determines source concentration in current time period.</summary>
        private double Sourcequal(QualSource source) {
            double c = source.C0;

            if (source.Type == SourceType.MASS)
                c /= 60.0;
            else
                c /= _net.FieldsMap.GetUnits(FieldType.QUALITY);


            Pattern pat = source.Pattern;
            if (pat == null)
                return c;

            long k = (_qtime + _net.PStart) / _net.PStep % pat.Count;
            return c * pat[(int)k];
        }

        /// <summary>Complete mix tank model.</summary>
        ///  <param name="tank">Tank to be updated.</param>
        /// <param name="dt">Step duration in seconds.</param>
        private void Tankmix1(QualityTank tank, long dt) {
            // React contents of tank
            double c = Tankreact(tank.Concentration, tank.Volume, ((Tank)tank.Node).Kb, dt);

            // Determine tank & volumes
            double vold = tank.Volume;

            tank.Volume = tank.Volume + tank.Demand * dt;

            double vin = tank.VolumeIn;

            double cin;
            if (vin > 0.0)
                cin = tank.MassIn / vin;
            else
                cin = 0.0;

            // Compute inflow concen.
            double cmax = Math.Max(c, cin);

            // Mix inflow with tank contents
            if (vin > 0.0)
                c = (c * vold + cin * vin) / (vold + vin);
            c = Math.Min(c, cmax);
            c = Math.Max(c, 0.0);
            tank.Concentration = c;
            tank.Quality = tank.Concentration;
        }

        /// <summary>2-compartment tank model (seg1 = mixing zone,seg2 = ambient zone).</summary>
        /// <param name="tank">Tank to be updated.</param>
        /// <param name="dt">Step duration in seconds.</param>  
        private void Tankmix2(QualityTank tank, long dt) {
            if (tank.Segments.Count == 0)
                return;

            QualitySegment seg1 = tank.Segments.Last.Value;
            QualitySegment seg2 = tank.Segments.First.Value;

            seg1.C = Tankreact(seg1.C, seg1.V, ((Tank)tank.Node).Kb, dt);
            seg2.C = Tankreact(seg2.C, seg2.V, ((Tank)tank.Node).Kb, dt);

            // Find inflows & outflows
            double vnet = tank.Demand * dt;
            double vin = tank.VolumeIn;
            double cin = vin > 0.0 ? tank.MassIn / vin : 0.0;
            double v1Max = ((Tank)tank.Node).V1Max;

            // Tank is filling
            double vt = 0.0;
            if (vnet > 0.0) {
                vt = Math.Max(0.0, seg1.V + vnet - v1Max);
                if (vin > 0.0) {
                    seg1.C = (seg1.C * seg1.V + cin * vin) / (seg1.V + vin);
                }
                if (vt > 0.0) {
                    seg2.C = (seg2.C * seg2.V + seg1.C * vt) / (seg2.V + vt);
                }
            }

            // Tank is emptying
            if (vnet < 0.0) {
                if (seg2.V > 0.0) {
                    vt = Math.Min(seg2.V, -vnet);
                }
                if (vin + vt > 0.0) {
                    seg1.C = (seg1.C * seg1.V + cin * vin + seg2.C * vt) / (seg1.V + vin + vt);
                }
            }

            // Update segment volumes
            if (vt > 0.0) {
                seg1.V = v1Max;
                if (vnet > 0.0)
                    seg2.V += vt;
                else
                    seg2.V = Math.Max(0.0, seg2.V - vt);
            }
            else {
                seg1.V += vnet;
                seg1.V = Math.Min(seg1.V, v1Max);
                seg1.V = Math.Max(0.0, seg1.V);
                seg2.V = 0.0;
            }

            tank.Volume = Math.Max(tank.Volume + vnet, 0.0);
            // Use quality of mixed compartment (seg1) to
            // represent quality of tank since this is where
            // outflow begins to flow from
            tank.Concentration = seg1.C;
            tank.Quality = tank.Concentration;
        }

        /// <summary>First-In-First-Out (FIFO) tank model.</summary>
        /// <param name="tank">Tank to be updated.</param>
        /// <param name="dt">Step duration in seconds.</param>
        ///  
        private void Tankmix3(QualityTank tank, long dt) {

            if (tank.Segments.Count == 0)
                return;

            // React contents of each compartment
            if (_reactflag) {
                foreach (QualitySegment seg  in  tank.Segments) {
                    seg.C = Tankreact(seg.C, seg.V, ((Tank)tank.Node).Kb, dt);
                }
            }

            // Find inflows & outflows
            double vnet = tank.Demand * dt;
            double vin = tank.VolumeIn;
            double vout = vin - vnet;
            double cin;

            if (vin > 0.0)
                cin = tank.MassIn / tank.VolumeIn;
            else
                cin = 0.0;

            tank.Volume = Math.Max(tank.Volume + vnet, 0.0);

            // Withdraw flow from first segment
            double vsum = 0.0;
            double csum = 0.0;

            while (vout > 0.0) {
                if (tank.Segments.Count == 0)
                    break;

                QualitySegment seg = tank.Segments.First.Value;
                double vseg = seg.V; // Flow volume from leading seg
                vseg = Math.Min(vseg, vout);
                if (tank.Segments.Count == 1) vseg = vout;
                vsum += vseg;
                csum += seg.C * vseg;
                vout -= vseg; // Remaining flow volume
                if (vout >= 0.0 && vseg >= seg.V) {
                    tank.Segments.RemoveFirst();
                }
                else {
                    // Remaining volume in segment
                    seg.V -= vseg;
                }
            }

            // Use quality withdrawn from 1st segment
            // to represent overall quality of tank
            if (vsum > 0.0)
                tank.Concentration = csum / vsum;
            else
                tank.Concentration = tank.Segments.First.Value.C;

            tank.Quality = tank.Concentration;

            // Add new last segment for new flow entering tank
            if (vin > 0.0) {
                if (tank.Segments.Count > 0) {
                    QualitySegment seg = tank.Segments.Last.Value;

                    // Quality is the same, so just add flow volume to last seg
                    if (Math.Abs(seg.C - cin) < _net.Ctol)
                        seg.V += vin;
                    else // Otherwise add a new seg to tank
                        tank.Segments.AddLast(new QualitySegment(vin, cin));
                }
                else //  If no segs left then add a new one.
                    tank.Segments.AddLast(new QualitySegment(vin, cin));
            }
        }

        /// <summary>Last In-First Out (LIFO) tank model.</summary>
        ///  <param name="tank">Tank to be updated.</param>
        /// <param name="dt">Step duration in seconds.</param>
        private void Tankmix4(QualityTank tank, long dt) {
            if (tank.Segments.Count == 0)
                return;

            // React contents of each compartment
            if (_reactflag) {
                for (LinkedListNode<QualitySegment> el = tank.Segments.Last; el != null; el = el.Previous) {
                    QualitySegment seg = el.Value;
                    seg.C = Tankreact(seg.C, seg.V, ((Tank)tank.Node).Kb, dt);
                }
            }

            // Find inflows & outflows
            double vnet = tank.Demand * dt;
            double vin = tank.VolumeIn;
            double cin;

            if (vin > 0.0)
                cin = tank.MassIn / tank.VolumeIn;
            else
                cin = 0.0;

            tank.Volume = Math.Max(0.0, tank.Volume + vnet);
            tank.Concentration = tank.Segments.Last.Value.C;

            // If tank filling, then create new last seg
            if (vnet > 0.0) {
                if (tank.Segments.Count > 0) {
                    QualitySegment seg = tank.Segments.Last.Value;
                    // Quality is the same, so just add flow volume to last seg
                    if (Math.Abs(seg.C - cin) < _net.Ctol)
                        seg.V += vnet;
                    // Otherwise add a new last seg to tank
                    // Which points to old last seg
                    else
                        tank.Segments.AddLast(new QualitySegment(vin, cin));
                }
                else
                    tank.Segments.AddLast(new QualitySegment(vin, cin));

                tank.Concentration = tank.Segments.Last.Value.C;
            }
            // If net emptying then remove last segments until vnet consumed
            else if (vnet < 0.0) {
                double vsum = 0.0;
                double csum = 0.0;
                vnet = -vnet;

                while (vnet > 0.0) {

                    if (tank.Segments.Count == 0)
                        break;

                    QualitySegment seg = tank.Segments.Last.Value;
                    if (seg == null)
                        break;

                    double vseg = seg.V;
                    vseg = Math.Min(vseg, vnet);
                    if (tank.Segments.Count == 1)
                        vseg = vnet;

                    vsum += vseg;
                    csum += seg.C * vseg;
                    vnet -= vseg;

                    if (vnet >= 0.0 && vseg >= seg.V) {
                        tank.Segments.RemoveLast(); //(2.00.12 - LR)
                    }
                    else {
                        // Remaining volume in segment
                        seg.V -= vseg;
                    }
                }
                // Reported tank quality is mixture of flow released and any inflow
                tank.Concentration = (csum + tank.MassIn) / (vsum + vin);
            }
            tank.Quality = tank.Concentration;
        }

        ///<summary>Computes new quality in a tank after reaction occurs.</summary>
        private double Tankreact(double c, double v, double kb, long dt) {
            if (!_reactflag)
                return c;

            if (_qualflag == QualType.AGE)
                return c + dt / 3600.0;

            double rbulk = Bulkrate(c, kb, _net.TankOrder) * _tucf;

            double dc = rbulk * dt;
            if (_htime >= _net.RStart)
                _wtank += Math.Abs(dc) * v;
            double cnew = c + dc;
            cnew = Math.Max(0.0, cnew);

            return cnew;
        }

        ///<summary>Transports constituent mass through pipe network under a period of constant hydraulic conditions.</summary>
        private void Transport(long tstep) {
            long qt = 0;
            while (qt < tstep) {
                long dt = Math.Min(_net.QStep, tstep - qt);
                qt += dt;
                if (_reactflag) Updatesegs(dt);
                Accumulate(dt);
                Updatenodes(dt);
                Sourceinput(dt);
                Release(dt);
            }
            Updatesourcenodes(tstep);
        }

        ///<summary>
        /// Updates concentration at all nodes to mixture of accumulated inflow from connecting pipes.
        /// </summary>
        /// <param name="dt">Step duration in seconds.</param>
        private void Updatenodes(long dt) {
            foreach (QualityNode qN  in  _juncs) {
                if (qN.Demand < 0.0)
                    qN.VolumeIn = qN.VolumeIn - qN.Demand * dt;
                if (qN.VolumeIn > 0.0)
                    qN.Quality = qN.MassIn / qN.VolumeIn;
                else
                    qN.Quality = qN.SourceContribution;
            }

            //  Update tank quality
            UpdateTanks(dt);

            // For flow tracing, set source node concen. to 100.
            if (_qualflag == QualType.TRACE)
                _traceNode.Quality = 100.0;
        }

        ///<summary>Reacts material in pipe segments up to time t.</summary>
        /// <param name="dt">Step duration in seconds.</param>
        private void Updatesegs(long dt) {
            foreach (QualityLink qL  in  _links) {
                double rsum = 0.0;
                double vsum = 0.0;
                if (qL.Link.Lenght == 0.0)
                    continue;

                foreach (QualitySegment seg  in  qL.Segments) {
                    double cseg = seg.C;
                    seg.C = Pipereact(qL, seg.C, seg.V, dt);

                    if (_qualflag == QualType.CHEM) {
                        rsum += Math.Abs(seg.C - cseg) * seg.V;
                        vsum += seg.V;
                    }
                }

                if (vsum > 0.0)
                    qL.FlowResistance = rsum / vsum / dt * Constants.SECperDAY;
                else
                    qL.FlowResistance = 0.0;
            }
        }

        ///<summary>Updates quality at source nodes.</summary>
        ///<param name="dt">step duration in seconds.</param>
        private void Updatesourcenodes(long dt) {
            QualSource source;

            if (_qualflag != QualType.CHEM) return;


            foreach (QualityNode qN  in  _nodes) {
                source = qN.Node.QualSource;
                if (source == null)
                    continue;

                qN.Quality = qN.Quality + qN.SourceContribution;


                if (qN.Node.Type == NodeType.TANK) {
                    qN.Quality = ((Tank)qN.Node).C;
                }

                qN.MassRate /= dt;
            }
        }

        ///<summary>Updates tank volumes & concentrations.</summary>
        ///<param name="dt">step duration in seconds.</param>
        private void UpdateTanks(long dt) {
            // Examine each reservoir & tank
            foreach (QualityTank tank  in  _tanks) {

                // Use initial quality for reservoirs
                if (tank.Node.Type == NodeType.RESERV) {
                    tank.Quality = tank.Node.C0;
                }

                // Update tank WQ based on mixing model
                else
                    switch (((Tank)tank.Node).MixModel) {
                    case MixType.MIX2:
                        Tankmix2(tank, dt);
                        break;
                    case MixType.FIFO:
                        Tankmix3(tank, dt);
                        break;
                    case MixType.LIFO:
                        Tankmix4(tank, dt);
                        break;
                    default:
                        Tankmix1(tank, dt);
                        break;
                    }
            }
        }

        ///<summary>Computes wall reaction rate.</summary>
        private double Wallrate(double c, double d, double kw, double kf) {
            if (kw == 0.0 || d == 0.0)
                return 0.0;

            if (_net.WallOrder == 0.0) {
                kf = Utilities.GetSignal(kw) * c * kf;
                kw *= Math.Pow(_elevUnits, 2);
                if (Math.Abs(kf) < Math.Abs(kw))
                    kw = kf;
                return kw * 4.0 / d;
            }

            return c * kf;
        }

        public QualityNode[] NNodes { get { return _nodes; } }

        public QualityLink[] NLinks { get { return _links; } }
    }

}