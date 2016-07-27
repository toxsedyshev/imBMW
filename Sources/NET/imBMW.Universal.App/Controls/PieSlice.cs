// Reference: https://github.com/jpoon/RotaryWheel/blob/master/RotaryWheelControl/PieSlicePath.cs

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Shapes;

namespace imBMW.Universal.App.Controls
{
    public class PieSlice : Path
    {
        private bool _isLoaded;

        public static readonly DependencyProperty StartAngleProperty =
            DependencyProperty.Register("StartAngle", typeof(double), typeof(PieSlice),
                new PropertyMetadata(default(double), (s, e) => { Changed(s as PieSlice); }));

        public static readonly DependencyProperty AngleProperty =
            DependencyProperty.Register("Angle", typeof(double), typeof(PieSlice),
                new PropertyMetadata(DependencyProperty.UnsetValue, (s, e) => { Changed(s as PieSlice); }));

        public static readonly DependencyProperty DiameterProperty =
            DependencyProperty.Register("Diameter", typeof(double), typeof(PieSlice),
            new PropertyMetadata(DependencyProperty.UnsetValue, (s, e) => { Changed(s as PieSlice); }));

        public PieSlice()
        {
            Loaded += (s, e) =>
            {
                _isLoaded = true;
                Redraw();
            };
        }

        public double StartAngle
        {
            get { return (double)GetValue(StartAngleProperty); }
            set { SetValue(StartAngleProperty, value); }
        }

        public double Angle
        {
            get { return (double)GetValue(AngleProperty); }
            set { SetValue(AngleProperty, value); }
        }

        public double Diameter
        {
            get { return (double)GetValue(DiameterProperty); }
            set { SetValue(DiameterProperty, value); }
        }

        private static void Changed(PieSlice pieSlice)
        {
            if (pieSlice._isLoaded)
            {
                pieSlice.Redraw();
            }
        }

        private void Redraw()
        {
            // Reference:
            // http://blog.jerrynixon.com/2012/06/windows-8-animated-pie-slice.html
            
            Width = Height = 2 * (Diameter / 2 + StrokeThickness);
            var startAngle = StartAngle + 180;
            var endAngle = startAngle + Angle;

            // path container
            var figure = new PathFigure
            {
                StartPoint = new Point(Diameter / 2, Diameter / 2),
                IsClosed = true,
            };

            //  start angle line
            var lineX = Diameter / 2 + Math.Sin(startAngle * Math.PI / 180) * Diameter / 2;
            var lineY = Diameter / 2 - Math.Cos(startAngle * Math.PI / 180) * Diameter / 2;
            var line = new LineSegment { Point = new Point(lineX, lineY) };
            figure.Segments.Add(line);

            // outer arc
            var arcX = Diameter / 2 + Math.Sin(endAngle * Math.PI / 180) * Diameter / 2;
            var arcY = Diameter / 2 - Math.Cos(endAngle * Math.PI / 180) * Diameter / 2;
            var arc = new ArcSegment
            {
                IsLargeArc = Angle >= 180.0,
                Point = new Point(arcX, arcY),
                Size = new Size(Diameter / 2, Diameter / 2),
                SweepDirection = SweepDirection.Clockwise,
            };
            figure.Segments.Add(arc);

            Data = new PathGeometry { Figures = { figure } };
            InvalidateArrange();
        }
    }
}
