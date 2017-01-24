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
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.CompilerServices;
using System.Windows.Forms;

using Epanet.Enums;
using Epanet.Network.Structures;

using EpanetNetwork = Epanet.Network.Network;

namespace Epanet.UI {

    internal sealed class NetworkPanel : Panel {
        private const float NODE_DIAM = 5f;
        private const float TANK_DIAM = NODE_DIAM * 2;

        

        // private readonly SizeF _nodeSize = new SizeF(NODE_DIAM, NODE_DIAM);
        // private readonly SizeF _reservoirSize = new SizeF(RESERVOIR_DIAM, RESERVOIR_DIAM);
        // private readonly SizeF _tankSize = new SizeF(TANK_DIAM, TANK_DIAM);

        private static readonly Brush tankBrush = Brushes.LightGray;
        private static readonly Color tankPenColor = Color.DimGray;

        private static readonly Brush reservoirsBrush = Brushes.DimGray;
        private static readonly Color reservoirsColor = Color.LightGray;

        private static readonly Color pipePenColor = Color.Blue;

        private static readonly Color nodePenColor = Color.Green;
        private static readonly Brush nodeBrush = Brushes.Tan;

        private static readonly Color labelColor = Color.Fuchsia;

        private const float ZOOM_SIZE_MAX = 100000;
        private const float ZOOM_SIZE_MIN = 10;

        private float dxo;
        private float dyo;

        /// <summary>Loaded hydraulic network.</summary>
        private EpanetNetwork net;

        private PointF panPoint;
        private float zoom = 0.9f;

        public float Zoom {
            get { return this.zoom; }
            private set {
                if (this.networkBounds.IsEmpty) {
                    this.zoom = 1f;
                    return;
                }

                float sizeMax = Math.Max(this.networkBounds.Height, this.networkBounds.Width);
                float sizeNew = sizeMax * value;

                if (sizeNew > ZOOM_SIZE_MAX) {
                    this.zoom = ZOOM_SIZE_MAX / sizeMax;
                }
                else if (sizeNew < ZOOM_SIZE_MIN) {
                    this.zoom = ZOOM_SIZE_MIN / sizeMax;
                }
                else {
                    this.zoom = value;
                }
            }
        }

        private RectangleF networkBounds = RectangleF.Empty;

        /// <summary>Reference to the selected node in network map.</summary>
        private Node selNode;

        public NetworkPanel() {
            this.SetStyle(
                ControlStyles.AllPaintingInWmPaint |
                ControlStyles.UserPaint |
                // ControlStyles.DoubleBuffer |
                ControlStyles.OptimizedDoubleBuffer |
                ControlStyles.Selectable,
                true);

            this.TabStop = true;

            base.DoubleBuffered = true;
            base.ResizeRedraw = true;
            this.DrawNodes = this.DrawPipes = this.DrawTanks = true;
        }

        public PointF MousePoint { get; private set; }

        [DefaultValue(null)]
        public EpanetNetwork Net {
            get { return this.net; }
            set {
                if (value == this.net) return;

                this.net = value;
                this.ZoomAll();
            }
        }

        [DefaultValue(true)]
        public bool DrawNodes { get; set; }

        [DefaultValue(true)]
        public bool DrawPipes { get; set; }

        [DefaultValue(true)]
        public bool DrawTanks { get; set; }

        private Node PeekNearest(PointF pt) {
            if (this.net == null)
                return null;

            Node nearest = null;
            double distance = double.MaxValue;
            
            foreach (Node n in this.net.Nodes) {
                var pos = n.Position;
                if (pos.IsInvalid) continue;
                
                double distMin = (n.Type > NodeType.JUNC)
                    ? TANK_DIAM / this.Zoom
                    : NODE_DIAM / this.Zoom;

                distMin *= distMin;


                // squared distance
                double dist = Math.Pow(pos.X - pt.X, 2) + Math.Pow(pos.Y - pt.Y, 2);
                if (dist < distMin && dist < distance) {
                    nearest = n;
                    distance = dist;
                }
            }

            return nearest;
        }

        private void ZoomToPoint(float zoomScale, Point zoomPoint) {
            PointF pt = this.GetLogPoint(zoomPoint);
            this.Zoom = zoomScale;
            this.dxo = -pt.X + zoomPoint.X / this.Zoom;
            this.dyo = pt.Y + zoomPoint.Y / this.Zoom;
            this.Refresh();
        }

        private static PointF ToPointF(EnPoint that) {
            return new PointF((float)that.X, (float)that.Y);
        }

        /// <summary>Calculates bounding rectangle for Network Nodes and Links</summary>
        /// <returns></returns>
        private RectangleF GetNetworkBounds() {
            var result = RectangleF.Empty;

            if (this.net == null)
                return RectangleF.Empty;

            bool firstPass = true;

            foreach (Node node in this.net.Nodes) {
                if (!node.Position.IsInvalid) {
                    var rect = new RectangleF(ToPointF(node.Position), SizeF.Empty);
                    if (firstPass) {
                        result = rect;
                        firstPass = false;
                    }
                    else {
                        result = RectangleF.Union(result, rect);
                    }
                }
            }

            foreach (Link link in this.net.Links) {
                foreach (var position in link.Vertices) {
                    if (!position.IsInvalid) {
                        var rect = new RectangleF(ToPointF(position), SizeF.Empty);
                        if (firstPass) {
                            result = rect;
                            firstPass = false;
                        }
                        else {
                            result = RectangleF.Union(result, rect);
                        }
                    }
                }
            }

            return result;
        }

        private float GetExtentsZoom() {
            var rect = this.GetNetworkBounds();

            float w = this.Width;
            float h = this.Height;

            float result = (rect.Width / rect.Height) < (w / h)
                ? h / this.networkBounds.Height
                : w / this.networkBounds.Width;

            return result * 0.95f;

        }

        public void ZoomAll() {
            if (this.net == null) return;

            float w = this.Width;
            float h = this.Height;
            this.networkBounds = this.GetNetworkBounds();
            if (this.networkBounds.IsEmpty) return;

            this.Zoom = (this.networkBounds.Width / this.networkBounds.Height) < (w / h)
                ? h / this.networkBounds.Height
                : w / this.networkBounds.Width;

            this.dxo = -this.networkBounds.X;
            this.dyo = this.networkBounds.Bottom;

            this.Zoom *= 0.95f;

            this.dxo += w * 0.5f / this.Zoom - this.networkBounds.Width * 0.5f;
            this.dyo += h * 0.5f / this.Zoom - this.networkBounds.Height * 0.5f;

            this.Invalidate();
        }

        public void ZoomStep(int steps) {
            if (steps == 0) return;
            float scale = steps > 0 ? this.Zoom * 1.5f * steps : this.Zoom / (1.5f * -steps);
            var pt = new Point(this.Width >> 1, this.Height >> 1);
            this.ZoomToPoint(scale, pt);
            this.Invalidate();
        }

        private PointF GetLogPoint(PointF pt) {
            return new PointF(-this.dxo + pt.X / this.Zoom, this.dyo - pt.Y / this.Zoom);
        }

        #region DrawNetwork methods

        private void _DrawTanks(Graphics g) {
            float tankDiam = TANK_DIAM / this.Zoom;

            using (Pen pen = new Pen(tankPenColor, -1f))
            using (Pen reservoirPen = new Pen(reservoirsColor, -1f))
            using(Pen tankPen = new Pen(tankPenColor, -1f)) {
                foreach (var tank in this.net.Tanks) {
                    var pos = tank.Position;
                    if (pos.IsInvalid)
                        continue;
       
                    var rect = new RectangleF((float)pos.X, (float)pos.Y, tankDiam, tankDiam);
                    rect.Offset(tankDiam * -0.5f, tankDiam * -0.5f);

                    g.FillRectangle(tankBrush, rect);
                    g.DrawRectangle(tankPen, rect.X, rect.Y, rect.Width, rect.Height);

                    var fillRect = new RectangleF((float)pos.X, (float)pos.Y, tankDiam, tankDiam);
                    fillRect.Offset(tankDiam * -0.5f, tankDiam * -0.5f);

                    g.FillRectangle(tankBrush, fillRect);
                    g.DrawRectangle(pen, fillRect.X, fillRect.Y, fillRect.Width, fillRect.Height);
                }

                foreach(Tank tank in this.net.Reservoirs) {
                    var pos = tank.Position;
                    if (pos.IsInvalid)
                        continue;
                   
                    var rect = new RectangleF((float)pos.X, (float)pos.Y, tankDiam, tankDiam);
                    rect.Offset(tankDiam * -0.5f, tankDiam * -0.5f);

                    g.FillRectangle(reservoirsBrush, rect);
                    g.DrawRectangle(reservoirPen, rect.X, rect.Y, rect.Width, rect.Height);

                    var fillRect = new RectangleF((float)pos.X, (float)pos.Y, tankDiam, tankDiam);
                    fillRect.Offset(tankDiam * -0.5f, tankDiam * -0.5f);

                    g.FillRectangle(tankBrush, fillRect);
                    g.DrawRectangle(pen, fillRect.X, fillRect.Y, fillRect.Width, fillRect.Height);
                }
            }

        }

        private void _DrawPipes(Graphics g) {
            using (Pen pen = new Pen(pipePenColor, -1f)) {
                foreach (Link link in this.net.Links) {
                    PointF[] points = new PointF[link.Vertices.Count + 2];
                    int i = 0;
                    points[i++] = ToPointF(link.FirstNode.Position);

                    foreach (var p in link.Vertices) {
                        points[i++] = ToPointF(p);
                    }

                    points[i] = ToPointF(link.SecondNode.Position);

                    g.DrawLines(pen, points);
                }
            }
        }

        private void _DrawNodes(Graphics g) {
            float diam = NODE_DIAM / this.Zoom;

            using(Pen pen = new Pen(nodePenColor, -1f)) {
                foreach (Node node in this.net.Junctions) {
                    var pos = node.Position;
                    if (pos.IsInvalid) continue;
                    
                    var nodeRect = new RectangleF((float)pos.X, (float)pos.Y, diam, diam);
                    nodeRect.Offset(diam * -0.5f, diam * -0.5f);

                    g.FillEllipse(nodeBrush, nodeRect);
                    g.DrawEllipse(pen, nodeRect);
                }
            }

        }

        private void _DrawLabels(Graphics g) {
            GraphicsState gs = g.Save();
            
            g.ScaleTransform(1f, -1f);

            using(var sf = new StringFormat(StringFormatFlags.NoWrap))
            using(Brush b = new SolidBrush(labelColor)) 
            using (Pen pen = new Pen(SystemColors.Info, -1f))
            {
                foreach (var l in this.net.Labels) {

                    var pt = l.Position;
                    
#if false
                    Node anchor = string.IsNullOrEmpty(l.AnchorNodeId)
                        ? null
                        : this.net.GetNode(l.AnchorNodeId);

                    if (anchor != null) {
                        var apt = anchor.Position;
                        if (!apt.IsInvalid) {
                            pt = apt;
                        }
                    }
#endif

                    if (pt.IsInvalid) continue;

                    PointF ptf = new PointF((float)pt.X,(float)(-pt.Y));
                    
                    SizeF sz = g.MeasureString(l.Text, this.Font, ptf, sf);
                    RectangleF rect = new RectangleF(ptf, sz);
                    
                    if (rect.IsEmpty) continue;

                    //var pos = l.getPosition();
                    //g.DrawEllipse(Pens.Red, (float)pos.getX(), (float)(-pos.getY()), 10f,10f);

                    g.DrawString(l.Text, this.Font, b, rect);
                   // rect.Inflate(0.5f / this.Zoom, 0.5f / this.Zoom);
                    //rect.Inflate(0.5f, 0.5f);
                    //g.DrawRectangle(pen, rect.X, rect.Y, rect.Width, rect.Height);
                }

            }

            g.Restore(gs);

        }

        private void _DrawSelection(Graphics g) {
            if (this.selNode == null) return;

            PointF point = ToPointF(this.selNode.Position);
            float size = (20f / this.Zoom);
            point.X -= size * 0.5f;
            point.Y -= size * 0.5f;


            using(Pen pen = new Pen(Color.Red, -1)) {
                g.DrawEllipse(pen, new RectangleF(point, new SizeF(size, size)));
            }

            using(var path = new GraphicsPath())
            using(var matrix = new Matrix())
            using(var sf = new StringFormat(StringFormatFlags.NoWrap)) {

                var pt = new PointF(point.X + size, -(point.Y + size));

                string s = string.Format(
                    "{0}: x={1:f};y={2:f}",
                    this.selNode.Name,
                    this.selNode.Position.X,
                    this.selNode.Position.Y);

                path.AddString(s, this.Font.FontFamily, 0, this.Font.Size * 2 / this.Zoom, pt, sf);
                matrix.Scale(1f, -1f);
                path.Transform(matrix);
                g.FillPath(Brushes.Red, path);
            }
        }

        private void DrawNetwork(Graphics g) {
            if (this.Net == null || this.networkBounds.IsEmpty)
                return;

            try {
                g.PageUnit = GraphicsUnit.Pixel;
                g.InterpolationMode = InterpolationMode.High;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.PixelOffsetMode = PixelOffsetMode.HighQuality;
                g.PageScale = this.Zoom;
                g.TranslateTransform(this.dxo, this.dyo);
                g.ScaleTransform(1, -1);
            }
            catch {
                return;
            }

            float rectSize = Math.Max(this.networkBounds.Height, this.networkBounds.Width) * this.Zoom;
  
            Debug.Print("rectSize1={0}", rectSize);
            if (rectSize > 4) {
                //links
                if (this.DrawPipes) this._DrawPipes(g);
                //tanks
                if (this.DrawTanks) this._DrawTanks(g);
                //nodes
                if (this.DrawNodes) this._DrawNodes(g);
                this._DrawLabels(g);
            }
            else {
                rectSize = 4f / this.Zoom;
                Debug.Print("rectSize2={0}", rectSize);
                g.FillRectangle(
                    Brushes.LightSlateGray,
                    new RectangleF(this.networkBounds.Location, new SizeF(rectSize, rectSize)));

            }

            this._DrawSelection(g);
        }

        #endregion

        #region Overrides of Control

        // private const int WHEEL_DELTA = 120;

        protected override void OnPaint(PaintEventArgs e) {
            base.OnPaint(e);

            e.Graphics.DrawLine(Pens.Red, -100, 0, 100, 0);
            e.Graphics.DrawLine(Pens.Red, 0, -100, 0, 100);

            if (this.net == null)
                return;

            try {
                this.DrawNetwork(e.Graphics);
            }
            catch (Exception ex) {
                Debug.Print(ex.Message);
            }
        }

        protected override void OnMouseWheel(MouseEventArgs e) {
            base.OnMouseWheel(e);

            float scale = e.Delta > 0 ? this.Zoom * 1.5f : this.Zoom / 1.5f;
            this.ZoomToPoint(scale, e.Location);
            this.OnMouseMove(e);
        }

        protected override void OnDoubleClick(EventArgs e) {
            base.OnDoubleClick(e);
            this.ZoomAll();
        }

        protected override void OnMouseDown(MouseEventArgs e) {
            this.Focus();
            base.OnMouseDown(e);

            switch (e.Button) {
            case MouseButtons.Left:
                Node nd = this.PeekNearest(this.GetLogPoint(e.Location));
                if (!Equals(nd, this.selNode)) {
                    this.selNode = nd;
                    this.Invalidate();
                }

                break;
            case MouseButtons.Middle:
                this.panPoint = e.Location;
                break;
            case MouseButtons.Right:
                break;
            }
        }

        protected override void OnMouseMove(MouseEventArgs e) {
            base.OnMouseMove(e);
            this.MousePoint = this.GetLogPoint(e.Location);

            switch (e.Button) {
                // case MouseButtons.None: break;
                // case MouseButtons.Left: break;
            case MouseButtons.Middle: {
                var pt = new PointF(e.X - this.panPoint.X, e.Y - this.panPoint.Y);
                this.dxo += pt.X / this.Zoom;
                this.dyo += pt.Y / this.Zoom;
                this.panPoint = e.Location;
                this.Invalidate();
            }
                break;
                //case MouseButtons.Right: break;
            }
        }

        #endregion
    }

}
