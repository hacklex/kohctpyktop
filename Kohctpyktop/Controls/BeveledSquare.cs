using System.Windows;
using System.Windows.Media;

namespace Kohctpyktop.Controls
{
    public class BeveledSquare : FrameworkElement
    {
        private static readonly PathGeometry StrokePath, OutlinePath, FillPath;

        private static readonly Brush FillBrush = new SolidColorBrush(Color.FromRgb(0x94, 0x94, 0x94));
        private static readonly Brush OutlineBrush = new SolidColorBrush(Color.FromRgb(0xb5, 0, 0));
        private static readonly Brush ShadowBrush = new SolidColorBrush(Color.FromRgb(0x67, 0x67, 0x67));
        private static readonly Transform ShadowTransform = new TranslateTransform(1, 1);

        static BeveledSquare()
        {
            // this is not actually beveled square, but there are tweaks for make wpf renderer suck less
            // PLS REPLACE ME!!!!!!!!
            StrokePath = new PathGeometry(new[]
            {
                new PathFigure(new Point(5, 2),
                    new PathSegment[]
                    {
                        new LineSegment(new Point(24, 2), false),
                        new LineSegment(new Point(27, 4), false),
                        new LineSegment(new Point(27, 25), false),
                        new LineSegment(new Point(25, 27), false),
                        new LineSegment(new Point(4, 27), false),
                        new LineSegment(new Point(2, 24), false),
                        new LineSegment(new Point(2, 4), false)
                    }, true)
            });
            OutlinePath = new PathGeometry(new[]
            {
                new PathFigure(new Point(4, 0),
                    new PathSegment[]
                    {
                        new LineSegment(new Point(26, 0), false),
                        new LineSegment(new Point(29, 3), false),
                        new LineSegment(new Point(29, 26), false),
                        new LineSegment(new Point(26, 29), false),
                        new LineSegment(new Point(4, 29), false),
                        new LineSegment(new Point(0, 26), false),
                        new LineSegment(new Point(0, 3), false)
                    }, true)
            });
            FillPath = new PathGeometry(new[]
            {
                new PathFigure(new Point(5, 3),
                    new PathSegment[]
                    {
                        new LineSegment(new Point(24.5, 3), false),
                        new LineSegment(new Point(26, 4.5), false),
                        new LineSegment(new Point(26.5, 24), false),
                        new LineSegment(new Point(24, 26.5), false),
                        new LineSegment(new Point(4.5, 26), false),
                        new LineSegment(new Point(3, 24.5), false),
                        new LineSegment(new Point(3, 4.5), false)
                    }, true)
            });
        }

        public BeveledSquare()
        {
            RenderOptions.SetEdgeMode(this, EdgeMode.Aliased);
        }

        protected override Size MeasureOverride(Size availableSize)
        {
            return new Size(29, 29);
        }

        public static readonly DependencyProperty SelectedProperty = DependencyProperty.Register(
            "Selected", typeof(bool), typeof(BeveledSquare),
            new FrameworkPropertyMetadata(default(bool), FrameworkPropertyMetadataOptions.AffectsRender));

        public bool Selected
        {
            get => (bool) GetValue(SelectedProperty);
            set => SetValue(SelectedProperty, value);
        }
        
        protected override void OnRender(DrawingContext drawingContext)
        {
            if (Selected)
            {
                drawingContext.DrawGeometry(OutlineBrush, null, OutlinePath);
            }
            else
            {
                drawingContext.PushTransform(ShadowTransform);
                drawingContext.DrawGeometry(ShadowBrush, null, StrokePath);
                drawingContext.Pop();
            }

            drawingContext.DrawGeometry(Brushes.Black, null, StrokePath);
            drawingContext.DrawGeometry(FillBrush, null, FillPath);
        }
    }
}