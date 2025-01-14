﻿using System;
using System.IO;
using System.Windows.Threading;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.UI.Core;

namespace VRCHapticsLite
{
    /*
    struct Point
    {
        public int X { get; }
        public int Y { get; }

        public Point(int x, int y) { X = x; Y = y; }
    }
    */

    struct RotorPointSet
    {
        public RotorPosition Position { get; }
        public Point[] Points { get; }

        public RotorPointSet(RotorPosition position, Point[] points)
        {
            Position = position;
            Points = points;
        }
    }

    struct RotorBridgeParameters
    {
        public int Width { get; }
        public int Height { get; }
        public RotorPointSet[] PointSets { get; }

        public RotorBridgeParameters(int width, int height, RotorPointSet[] pointSets)
        {
            Width = width;
            Height = height;
            PointSets = pointSets;
        }
    }

    class RotorBridge
    {
        private const int COLOR_TOLERANCE = 16;

        private RotorBridgeParameters _parameters;
        private ModuleConfigViewModel _config;
        private ColorRangeViewModel _activeRange;
        private ColorRangeViewModel _inactiveRange;
        private RotorPlayer _player;

        public RotorBridge(
            RotorBridgeParameters parameters,
            ModuleConfigViewModel config,
            ColorRangeViewModel activeRange,
            ColorRangeViewModel inactiveRange,
            RotorPlayer hapticsPlayer)
        {
            _parameters = parameters;
            _config = config;
            _activeRange = activeRange;
            _inactiveRange = inactiveRange;
            _player = hapticsPlayer;
        }

        private bool IsActive(int r, int g, int b)
        {
            int minR = _activeRange.MinR.Value, maxR = _activeRange.MaxR.Value;
            int minG = _activeRange.MinG.Value, maxG = _activeRange.MaxG.Value;
            int minB = _activeRange.MinB.Value, maxB = _activeRange.MaxB.Value;
            if (r < minR || maxR < r) { return false; }
            if (g < minG || maxG < g) { return false; }
            if (b < minB || maxB < b) { return false; }
            return true;
        }

        private bool IsInactive(int r, int g, int b)
        {
            int minR = _inactiveRange.MinR.Value, maxR = _inactiveRange.MaxR.Value;
            int minG = _inactiveRange.MinG.Value, maxG = _inactiveRange.MaxG.Value;
            int minB = _inactiveRange.MinB.Value, maxB = _inactiveRange.MaxB.Value;
            if (r < minR || maxR < r) { return false; }
            if (g < minG || maxG < g) { return false; }
            if (b < minB || maxB < b) { return false; }
            return true;
        }

        public void OnDisabled()
        {
            foreach (var ps in _parameters.PointSets)
            {
                int n = ps.Points.Length;
                var data = new byte[n];
                for (int i = 0; i < n; ++i)
                {
                    var p = ps.Points[i];
                    data[i] = 0;
                }
                _ = _player.SendMessage(ps.Position, data, true);
            }
        }

        public void OnFrameArrived(CapturedBitmap frame)
        {
            var x = _config.X.Value;
            var y = _config.Y.Value;
            var w = _config.Width.Value;
            var h = _config.Height.Value;
            if (w == 0 || h == 0) { return; }
            var bytes = frame.GetPixelBytes(x, y, w, h);
            if (_config.Enabled.Value)
            {
                // Calculate coordinates
                var pw = _parameters.Width;
                var ph = _parameters.Height;
                var xs = new int[pw];
                var ys = new int[ph];
                for (int i = 0; i < pw; ++i) { xs[i] = (int)Math.Round(w * (i + 0.5) / pw); }
                for (int i = 0; i < ph; ++i) { ys[i] = (int)Math.Round(h * (i + 0.5) / ph); }
                // Make a decimated bitmap
                bool isValid = true;
                var active = new bool[ph, pw];
                for (int i = 0; i < ph; ++i)
                {
                    for (int j = 0; j < pw; ++j)
                    {
                        int r = bytes[(ys[i] * w + xs[j]) * 4 + 2];
                        int g = bytes[(ys[i] * w + xs[j]) * 4 + 1];
                        int b = bytes[(ys[i] * w + xs[j]) * 4 + 0];
                        if (IsActive(r, g, b))
                        {
                            active[i, j] = true;
                        }
                        else if (IsInactive(r, g, b))
                        {
                            active[i, j] = false;
                        }
                        else
                        {
                            isValid = false;
                        }
                    }
                }
                // Send messages for each position
                int power = _config.Power.Value;
                foreach (var ps in _parameters.PointSets)
                {
                    int n = ps.Points.Length;
                    var data = new byte[n];
                    for (int i = 0; i < n; ++i)
                    {
                        var p = ps.Points[i];
                        data[i] = (byte)(isValid && active[p.Y, p.X] ? power : 0);
                    }
                    _ = _player.SendMessage(ps.Position, data, false);
                }
            }
            // Update preview image
            if (bytes.Length > 0)
            {
                var bitmap = BitmapSource.Create(
                    w, h, 96.0, 96.0, PixelFormats.Bgra32, null, bytes, w * 4);
                _config.Image.Value = bitmap;
            }
        }
    }
}
