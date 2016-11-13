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
using System.Windows.Forms;
using org.addition.epanet.network;
using org.addition.epanet.network.structures;
using Label = System.Windows.Forms.Label;
using Point = System.Drawing.Point;

namespace EpaTool {

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

        private float _dxo;
        private float _dyo;

        /// <summary>Loaded hydraulic network.</summary>
        private Network _net;

        private PointF _panPoint;
        private float _zoom = 0.9f;

        public float Zoom {
            get { return this._zoom; }
            private set {
                if (this._networkBounds.IsEmpty) {
                    this._zoom = 1f;
                    return;
                }

                float sizeMax = Math.Max(this._networkBounds.Height, this._networkBounds.Width);
                float sizeNew = sizeMax * value;

                if (sizeNew > ZOOM_SIZE_MAX) {
                    this._zoom = ZOOM_SIZE_MAX / sizeMax;
                }
                else if (sizeNew < ZOOM_SIZE_MIN) {
                    this._zoom = ZOOM_SIZE_MIN / sizeMax;
                }
                else {
                    this._zoom = value;
                }
            }
        }

        private RectangleF _networkBounds = RectangleF.Empty;

        /// <summary>Reference to the selected node in network map.</summary>
        private Node _selNode;

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
        public Network Net {
            get { return this._net; }
            set {
                if (value == this._net) return;

                this._net = value;
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
            if (this._net == null)
                return null;

            Node nearest = null;
            double distance = double.MaxValue;
            
            foreach (Node n in this._net.getNodes()) {
                var pos = n.getPosition();
                if (pos == null) continue;
                
                double distMin = (n is Tank)
                    ? TANK_DIAM / this.Zoom
                    : NODE_DIAM / this.Zoom;

                distMin *= distMin;


                // squared distance
                double dist = Math.Pow(pos.getX() - pt.X, 2) + Math.Pow(pos.getY() - pt.Y, 2);
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
            this._dxo = -pt.X + zoomPoint.X / this.Zoom;
            this._dyo = pt.Y + zoomPoint.Y / this.Zoom;
            this.Refresh();
        }

        private static PointF ToPointF(org.addition.epanet.network.structures.Point that) {
            return new PointF((float)that.getX(), (float)that.getY());
        }

        /// <summary>Calculates bounding rectangle for Network Nodes and Links</summary>
        /// <returns></returns>
        private RectangleF GetNetworkBounds() {
            var result = RectangleF.Empty;

            if (this._net == null)
                return RectangleF.Empty;

            bool firstPass = true;

            foreach (Node node in this._net.getNodes()) {
                if (node.getPosition() != null) {
                    var rect = new RectangleF(ToPointF(node.getPosition()), SizeF.Empty);
                    if (firstPass) {
                        result = rect;
                        firstPass = false;
                    }
                    else {
                        result = RectangleF.Union(result, rect);
                    }
                }
            }

            foreach (Link link in this._net.getLinks()) {
                foreach (var position in link.getVertices()) {
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

            float zoom = (rect.Width / rect.Height) < (w / h)
                ? h / this._networkBounds.Height
                : w / this._networkBounds.Width;

            return zoom * 0.95f;

        }

        public void ZoomAll() {
            if (this._net == null) return;

            float w = this.Width;
            float h = this.Height;
            this._networkBounds = this.GetNetworkBounds();
            if (this._networkBounds.IsEmpty) return;

            this.Zoom = (this._networkBounds.Width / this._networkBounds.Height) < (w / h)
                ? h / this._networkBounds.Height
                : w / this._networkBounds.Width;

            this._dxo = -this._networkBounds.X;
            this._dyo = this._networkBounds.Bottom;

            this.Zoom *= 0.95f;

            this._dxo += w * 0.5f / this.Zoom - this._networkBounds.Width * 0.5f;
            this._dyo += h * 0.5f / this.Zoom - this._networkBounds.Height * 0.5f;

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
            return new PointF(-this._dxo + pt.X / this.Zoom, this._dyo - pt.Y / this.Zoom);
        }

        #region DrawNetwork methods

        private void _DrawTanks(Graphics g) {
            float tankDiam = TANK_DIAM / this.Zoom;

            using (Pen pen = new Pen(tankPenColor, -1f))
            using (Pen reservoirPen = new Pen(reservoirsColor, -1f))
            using(Pen tankPen = new Pen(tankPenColor, -1f)) {
                foreach (Tank tank in this._net.getTanks()) {
                    var pos = tank.getPosition();
                    if (pos == null)
                        continue;


                    if (tank.IsReservoir) {
                        // Reservoir

                        var rect = new RectangleF((float)pos.getX(), (float)pos.getY(), tankDiam, tankDiam);
                        rect.Offset(tankDiam * -0.5f, tankDiam * -0.5f);

                        g.FillRectangle(reservoirsBrush, rect);
                        g.DrawRectangle(reservoirPen, rect.X, rect.Y, rect.Width, rect.Height);


                    }
                    else {
                        // Tank
                        var rect = new RectangleF((float)pos.getX(), (float)pos.getY(), tankDiam, tankDiam);
                        rect.Offset(tankDiam * -0.5f, tankDiam * -0.5f);

                        g.FillRectangle(tankBrush, rect);
                        g.DrawRectangle(tankPen, rect.X, rect.Y, rect.Width, rect.Height);

                    }

                    var fillRect = new RectangleF((float)pos.getX(), (float)pos.getY(), tankDiam, tankDiam);
                    fillRect.Offset(tankDiam * -0.5f, tankDiam * -0.5f);

                    g.FillRectangle(tankBrush, fillRect);
                    g.DrawRectangle(pen, fillRect.X, fillRect.Y, fillRect.Width, fillRect.Height);

                }


            }

        }

        private void _DrawPipes(Graphics g) {
            using (Pen pen = new Pen(pipePenColor, -1f)) {
                foreach (Link link in this._net.getLinks()) {
                    PointF[] points = new PointF[link.getVertices().Count + 2];
                    int i = 0;
                    points[i++] = ToPointF(link.getFirst().getPosition());

                    foreach (var p in link.getVertices()) {
                        points[i++] = ToPointF(p);
                    }

                    points[i] = ToPointF(link.getSecond().getPosition());

                    g.DrawLines(pen, points);
                }
            }
        }

        private void _DrawNodes(Graphics g) {
            float diam = NODE_DIAM / this.Zoom;

            using(Pen pen = new Pen(nodePenColor, -1f)) {
                foreach (Node node in this._net.getNodes()) {
                    if (node is Tank) continue;
                    var pos = node.getPosition();
                    if (pos == null) continue;
                    
                    var nodeRect = new RectangleF((float)pos.getX(), (float)pos.getY(), diam, diam);
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
                foreach (var l in this._net.getLabels()) {

                    var pt = l.getPosition();
                    
#if false
                    Node anchor = string.IsNullOrEmpty(l.AnchorNodeId)
                        ? null
                        : this._net.getNode(l.AnchorNodeId);

                    if (anchor != null) {
                        var apt = anchor.getPosition();
                        if (apt != null) {
                            pt = apt;
                        }
                    }
#endif

                    if (pt == null) continue;

                    PointF ptf = new PointF((float)pt.getX(),(float)(-pt.getY()));
                    
                    SizeF sz = g.MeasureString(l.getText(), this.Font, ptf, sf);
                    RectangleF rect = new RectangleF(ptf, sz);
                    
                    if (rect.IsEmpty) continue;

                    //var pos = l.getPosition();
                    //g.DrawEllipse(Pens.Red, (float)pos.getX(), (float)(-pos.getY()), 10f,10f);

                    g.DrawString(l.getText(), this.Font, b, rect);
                   // rect.Inflate(0.5f / this.Zoom, 0.5f / this.Zoom);
                    //rect.Inflate(0.5f, 0.5f);
                    //g.DrawRectangle(pen, rect.X, rect.Y, rect.Width, rect.Height);
                }

            }

            g.Restore(gs);

        }

        [Conditional("DEBUG")]
        private void _DrawLabels2(Graphics g) {
            GraphicsState gs = g.Save();

            using (var sf = new StringFormat(StringFormatFlags.NoWrap))
            using (var path = new GraphicsPath()) {
                foreach (var l in this._net.getLabels()) {
                    var pos = l.getPosition();
                    if (pos.IsInvalid) continue;

                    PointF pt = new PointF((float)pos.getX(), (float)pos.getY() * -1);

                    path.AddString(l.getText(), this.Font.FontFamily, 0, this.Font.Size * 2, pt, sf);
                    SizeF sz = g.MeasureString(l.getText(), this.Font, pt, sf);
                    path.AddRectangle(new RectangleF(pt, sz));

                    //g.DrawRectangle(Pens.Green, (int)pt.X, (int)pt.Y, 100, -100);
                }

                using (var matrix = new Matrix()) {
                    matrix.Scale(1f, -1f);
                    path.Transform(matrix);
                }



                using (Brush b = new SolidBrush(labelColor)) {
                    g.FillPath(b, path);
                }

            }
        }


        private void _DrawSelection(Graphics g) {
            if (this._selNode == null) return;

            PointF point = ToPointF(this._selNode.getPosition());
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
                    this._selNode.getId(),
                    this._selNode.getPosition().getX(),
                    this._selNode.getPosition().getY());

                path.AddString(s, this.Font.FontFamily, 0, this.Font.Size * 2 / this.Zoom, pt, sf);
                matrix.Scale(1f, -1f);
                path.Transform(matrix);
                g.FillPath(Brushes.Red, path);
            }
        }

        private void DrawNetwork(Graphics g) {
            if (this.Net == null || this._networkBounds.IsEmpty)
                return;

            try {
                g.PageUnit = GraphicsUnit.Pixel;
                g.InterpolationMode = InterpolationMode.High;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.PixelOffsetMode = PixelOffsetMode.HighQuality;
                g.PageScale = this.Zoom;
                g.TranslateTransform(this._dxo, this._dyo);
                g.ScaleTransform(1, -1);
            }
            catch {
                return;
            }

            float rectSize = Math.Max(this._networkBounds.Height, this._networkBounds.Width) * this.Zoom;
  
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
                    new RectangleF(this._networkBounds.Location, new SizeF(rectSize, rectSize)));

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

            if (this._net == null)
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
                if (!Equals(nd, this._selNode)) {
                    this._selNode = nd;
                    this.Invalidate();
                }

                break;
            case MouseButtons.Middle:
                this._panPoint = e.Location;
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
                var pt = new PointF(e.X - this._panPoint.X, e.Y - this._panPoint.Y);
                this._dxo += pt.X / this.Zoom;
                this._dyo += pt.Y / this.Zoom;
                this._panPoint = e.Location;
                this.Invalidate();
            }
                break;
                //case MouseButtons.Right: break;
            }
        }

        #endregion
    }

}
