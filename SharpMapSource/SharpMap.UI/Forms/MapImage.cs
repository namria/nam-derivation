#if !UseMapBoxAsMapImage
// Copyright 2005, 2006 - Morten Nielsen (www.iter.dk)
//
// This file is part of SharpMap.
// SharpMap is free software; you can redistribute it and/or modify
// it under the terms of the GNU Lesser General Public License as published by
// the Free Software Foundation; either version 2 of the License, or
// (at your option) any later version.
// 
// SharpMap is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU Lesser General Public License for more details.

// You should have received a copy of the GNU Lesser General Public License
// along with SharpMap; if not, write to the Free Software
// Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA  02111-1307  USA 






using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;
using SharpMap.Data;
using SharpMap.Geometries;
using SharpMap.Layers;
using Point = SharpMap.Geometries.Point;

namespace SharpMap.Forms
{
    /// <summary>
    /// MapImage Class - MapImage control for Windows forms
    /// </summary>
    /// <remarks>
    /// The MapImage control adds basic functionality to a Windows Form, such as dynamic pan, zoom and data query.
    /// </remarks>
    [DesignTimeVisible(true)]
    public class MapImage : PictureBox
    {
        #region Tools enum

        /// <summary>
        /// Map tools enumeration
        /// </summary>
        public enum Tools
        {
            /// <summary>
            /// Pan
            /// </summary>
            Pan,
            /// <summary>
            /// Zoom in
            /// </summary>
            ZoomIn,
            /// <summary>
            /// Zoom out
            /// </summary>
            ZoomOut,
            /// <summary>
            /// Query tool
            /// </summary>
            Query,
            /// <summary>
            /// No active tool
            /// </summary>
            None
        }

        #endregion

        private Tools _Activetool;
        private double _fineZoomFactor = 10;
        private bool _isCtrlPressed = false;
        private Map _Map;
        private int _queryLayerIndex;

        private double _wheelZoomMagnitude = 2;
        private System.Drawing.Point mousedrag;
        private bool mousedragging = false;
        private Image mousedragImg;

        /// <summary>
        /// Initializes a new map
        /// </summary>
        public MapImage()
        {
            _Map = new Map(base.Size);
            _Activetool = Tools.None;
            base.MouseMove += new System.Windows.Forms.MouseEventHandler(MapImage_MouseMove);
            base.MouseUp += new System.Windows.Forms.MouseEventHandler(MapImage_MouseUp);
            base.MouseDown += new System.Windows.Forms.MouseEventHandler(MapImage_MouseDown);
            base.MouseWheel += new System.Windows.Forms.MouseEventHandler(MapImage_Wheel);
            Cursor = Cursors.Cross;
            DoubleBuffered = true;
        }

        [Description("The amount which a single movement of the mouse wheel zooms by.")]
        [DefaultValue(2)]
        [Category("Behavior")]
        public double WheelZoomMagnitude
        {
            get { return _wheelZoomMagnitude; }
            set { _wheelZoomMagnitude = value; }
        }

        [Description("The amount which the WheelZoomMagnitude is divided by " +
                     "when the Control key is pressed. A number greater than 1 decreases " +
                     "the zoom, and less than 1 increases it. A negative number reverses it.")]
        [DefaultValue(10)]
        [Category("Behavior")]
        public double FineZoomFactor
        {
            get { return _fineZoomFactor; }
            set { _fineZoomFactor = value; }
        }

        /// <summary>
        /// Map reference
        /// </summary>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Map Map
        {
            get { return _Map; }
            set
            {
                _Map = value;
                if (_Map != null)
                    Refresh();
            }
        }

        /// <summary>
        /// Gets or sets the index of the active query layer 
        /// </summary>
        public int QueryLayerIndex
        {
            get { return _queryLayerIndex; }
            set { _queryLayerIndex = value; }
        }


        /// <summary>
        /// Sets the active map tool
        /// </summary>
        public Tools ActiveTool
        {
            get { return _Activetool; }
            set
            {
                bool fireevent = (value != _Activetool);
                _Activetool = value;
                if (value == Tools.Pan)
                    Cursor = Cursors.Hand;
                else
                    Cursor = Cursors.Cross;
                if (fireevent)
                    if (ActiveToolChanged != null)
                        ActiveToolChanged(value);
            }
        }

        /// <summary>
        /// Refreshes the map
        /// </summary>
        public override void Refresh()
        {
            if (_Map != null)
            {
                _Map.Size = Size;
                if (_Map.Layers == null || _Map.Layers.Count == 0)
                    Image = null;
                else
                    Image = _Map.GetMap();
                base.Refresh();
                if (MapRefreshed != null)
                    MapRefreshed(this, null);
            }
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            _isCtrlPressed = e.Control;
            Debug.WriteLine(String.Format("Ctrl: {0}", _isCtrlPressed));

            base.OnKeyDown(e);
        }

        protected override void OnKeyUp(KeyEventArgs e)
        {
            _isCtrlPressed = e.Control;
            Debug.WriteLine(String.Format("Ctrl: {0}", _isCtrlPressed));

            base.OnKeyUp(e);
        }

        protected override void OnMouseHover(EventArgs e)
        {
            if (!Focused)
            {
                bool isFocused = Focus();
                Debug.WriteLine(isFocused);
            }

            base.OnMouseHover(e);
        }

        private void MapImage_Wheel(object sender, MouseEventArgs e)
        {
            if (_Map != null)
            {
                double scale = ((double)e.Delta / 120.0);
                double scaleBase = 1 + (_wheelZoomMagnitude / (10 * ((double)(_isCtrlPressed ? _fineZoomFactor : 1))));

                _Map.Zoom *= Math.Pow(scaleBase, scale);

                if (MapZoomChanged != null)
                    MapZoomChanged(_Map.Zoom);

                Refresh();
            }
        }

        private void MapImage_MouseDown(object sender, MouseEventArgs e)
        {
            if (_Map != null)
            {
                if (e.Button == MouseButtons.Left) //dragging
                    mousedrag = e.Location;
                if (MouseDown != null)
                    MouseDown(_Map.ImageToWorld(new System.Drawing.Point(e.X, e.Y)), e);
            }
        }



        private void MapImage_MouseMove(object sender, MouseEventArgs e)
        {
            if (_Map != null)
            {
                Point p = _Map.ImageToWorld(new System.Drawing.Point(e.X, e.Y));

                if (MouseMove != null)
                    MouseMove(p, e);

                if (Image != null && e.Location != mousedrag && !mousedragging && e.Button == MouseButtons.Left)
                {
                    mousedragImg = Image.Clone() as Image;
                    mousedragging = true;
                }

                if (mousedragging)
                {
                    if (MouseDrag != null)
                        MouseDrag(p, e);

                    if (ActiveTool == Tools.Pan)
                    {
                        Image img = new Bitmap(Size.Width, Size.Height);
                        Graphics g = Graphics.FromImage(img);
                        g.Clear(Color.Transparent);
                        g.DrawImageUnscaled(mousedragImg,
                                            new System.Drawing.Point(e.Location.X - mousedrag.X,
                                                                     e.Location.Y - mousedrag.Y));
                        g.Dispose();
                        Image = img;
                    }
                    else if (ActiveTool == Tools.ZoomIn || ActiveTool == Tools.ZoomOut)
                    {
                        Image img = new Bitmap(Size.Width, Size.Height);
                        Graphics g = Graphics.FromImage(img);
                        g.Clear(Color.Transparent);
                        float scale = 0;
                        if (e.Y - mousedrag.Y < 0) //Zoom out
                            scale = (float)Math.Pow(1 / (float)(mousedrag.Y - e.Y), 0.5);
                        else //Zoom in
                            scale = 1 + (e.Y - mousedrag.Y) * 0.1f;
                        RectangleF rect = new RectangleF(0, 0, Width, Height);
                        if (_Map.Zoom / scale < _Map.MinimumZoom)
                            scale = (float)Math.Round(_Map.Zoom / _Map.MinimumZoom, 4);
                        rect.Width *= scale;
                        rect.Height *= scale;
                        rect.Offset(Width / 2 - rect.Width / 2, Height / 2 - rect.Height / 2);
                        g.DrawImage(mousedragImg, rect);
                        g.Dispose();
                        Image = img;
                        if (MapZooming != null)
                            MapZooming(scale);
                    }
                }
            }
        }

        private void MapImage_MouseUp(object sender, MouseEventArgs e)
        {
            if (_Map != null)
            {
                if (MouseUp != null)
                    MouseUp(_Map.ImageToWorld(new System.Drawing.Point(e.X, e.Y)), e);

                if (e.Button == MouseButtons.Left)
                {
                    if (ActiveTool == Tools.ZoomOut)
                    {
                        double scale = 0.5;
                        if (!mousedragging)
                        {
                            _Map.Center = _Map.ImageToWorld(new System.Drawing.Point(e.X, e.Y));
                            if (MapCenterChanged != null)
                                MapCenterChanged(_Map.Center);
                        }
                        else
                        {
                            if (e.Y - mousedrag.Y < 0) //Zoom out
                                scale = (float)Math.Pow(1 / (float)(mousedrag.Y - e.Y), 0.5);
                            else //Zoom in
                                scale = 1 + (e.Y - mousedrag.Y) * 0.1;
                        }
                        _Map.Zoom *= 1 / scale;
                        if (MapZoomChanged != null)
                            MapZoomChanged(_Map.Zoom);
                        Refresh();
                    }
                    else if (ActiveTool == Tools.ZoomIn)
                    {
                        double scale = 2;
                        if (!mousedragging)
                        {
                            _Map.Center = _Map.ImageToWorld(new System.Drawing.Point(e.X, e.Y));
                            if (MapCenterChanged != null)
                                MapCenterChanged(_Map.Center);
                        }
                        else
                        {
                            if (e.Y - mousedrag.Y < 0) //Zoom out
                                scale = (float)Math.Pow(1 / (float)(mousedrag.Y - e.Y), 0.5);
                            else //Zoom in
                                scale = 1 + (e.Y - mousedrag.Y) * 0.1;
                        }
                        _Map.Zoom *= 1 / scale;
                        if (MapZoomChanged != null)
                            MapZoomChanged(_Map.Zoom);
                        Refresh();
                    }
                    else if (ActiveTool == Tools.Pan)
                    {
                        if (mousedragging)
                        {
                            System.Drawing.Point pnt = new System.Drawing.Point(Width / 2 + (mousedrag.X - e.Location.X),
                                                                                Height / 2 + (mousedrag.Y - e.Location.Y));
                            _Map.Center = _Map.ImageToWorld(pnt);
                            if (MapCenterChanged != null)
                                MapCenterChanged(_Map.Center);
                        }
                        else
                        {
                            _Map.Center = _Map.ImageToWorld(new System.Drawing.Point(e.X, e.Y));
                            if (MapCenterChanged != null)
                                MapCenterChanged(_Map.Center);
                        }
                        Refresh();
                    }
                    else if (ActiveTool == Tools.Query)
                    {
                        if (_Map.Layers.Count > _queryLayerIndex && _queryLayerIndex > -1)
                        {
                            /*
                            if (_Map.Layers[_queryLayerIndex].GetType() == typeof(VectorLayer))
                            {
                                VectorLayer layer = _Map.Layers[_queryLayerIndex] as VectorLayer;
                                BoundingBox bbox =
                                    _Map.ImageToWorld(new System.Drawing.Point(e.X, e.Y)).GetBoundingBox().Grow(
                                        _Map.PixelSize * 5);
                                FeatureDataSet ds = new FeatureDataSet();
                                layer.DataSource.Open();
                                layer.DataSource.ExecuteIntersectionQuery(bbox, ds);
                                layer.DataSource.Close();
                                if (ds.Tables.Count > 0)
                                    if (MapQueried != null) MapQueried(ds.Tables[0]);
                                    else if (MapQueried != null) MapQueried(new FeatureDataTable());
                            }
                            */
                            if (_Map.Layers[_queryLayerIndex] is ICanQueryLayer)
                            {
                                ICanQueryLayer layer = _Map.Layers[_queryLayerIndex] as ICanQueryLayer;
                                BoundingBox bbox =
                                    _Map.ImageToWorld(new System.Drawing.Point(e.X, e.Y)).GetBoundingBox().Grow(
                                        _Map.PixelSize * 5);
                                FeatureDataSet ds = new FeatureDataSet();
                                layer.ExecuteIntersectionQuery(bbox, ds);
                                if (ds.Tables.Count > 0)
                                    if (MapQueried != null) MapQueried(ds.Tables[0]);
                                    else if (MapQueried != null) MapQueried(new FeatureDataTable());
                            }
                        }
                        else
                            MessageBox.Show("No active layer to query");
                    }
                }
                if (mousedragImg != null)
                {
                    mousedragImg.Dispose();
                    mousedragImg = null;
                }
                mousedragging = false;
            }
        }

        #region Events

        #region Delegates

        /// <summary>
        /// Eventtype fired when the map tool is changed
        /// </summary>
        /// <param name="tool"></param>
        public delegate void ActiveToolChangedHandler(Tools tool);

        /// <summary>
        /// Eventtype fired when the center has changed
        /// </summary>
        /// <param name="center"></param>
        public delegate void MapCenterChangedHandler(Point center);

        /// <summary>
        /// Eventtype fired when the map is queried
        /// </summary>
        /// <param name="data"></param>
        public delegate void MapQueryHandler(FeatureDataTable data);

        /// <summary>
        /// Eventtype fired when the zoom was or are being changed
        /// </summary>
        /// <param name="zoom"></param>
        public delegate void MapZoomHandler(double zoom);

        /// <summary>
        /// MouseEventtype fired from the MapImage control
        /// </summary>
        /// <param name="WorldPos"></param>
        /// <param name="ImagePos"></param>
        public delegate void MouseEventHandler(Point WorldPos, MouseEventArgs ImagePos);

        #endregion

        /// <summary>
        /// Fires when mouse moves over the map
        /// </summary>
        public new event MouseEventHandler MouseMove;

        /// <summary>
        /// Fires when map received a mouseclick
        /// </summary>
        public new event MouseEventHandler MouseDown;

        /// <summary>
        /// Fires when mouse is released
        /// </summary>		
        public new event MouseEventHandler MouseUp;

        /// <summary>
        /// Fired when mouse is dragging
        /// </summary>
        public event MouseEventHandler MouseDrag;

        /// <summary>
        /// Fired when the map has been refreshed
        /// </summary>
        public event EventHandler MapRefreshed;

        /// <summary>
        /// Fired when the zoom value has changed
        /// </summary>
        public event MapZoomHandler MapZoomChanged;

        /// <summary>
        /// Fired when the map is being zoomed
        /// </summary>
        public event MapZoomHandler MapZooming;

        /// <summary>
        /// Fired when the map is queried
        /// </summary>
        public event MapQueryHandler MapQueried;


        /// <summary>
        /// Fired when the center of the map has changed
        /// </summary>
        public event MapCenterChangedHandler MapCenterChanged;

        /// <summary>
        /// Fired when the active map tool has changed
        /// </summary>
        public event ActiveToolChangedHandler ActiveToolChanged;

        #endregion
    }
}
#endif