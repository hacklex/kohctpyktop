using System.Windows;
using System.Windows.Media;

namespace Kohctpyktop.Controls
{
    public class BeveledSquare : FrameworkElement
    {
        private static readonly Geometry StrokePath, OutlinePath, FillPath;

        private static readonly Brush FillBrush = new SolidColorBrush(Color.FromRgb(0x94, 0x94, 0x94));
        private static readonly Brush OutlineBrush = new SolidColorBrush(Color.FromRgb(0xb5, 0, 0));
        private static readonly Brush ShadowBrush = new SolidColorBrush(Color.FromRgb(0x67, 0x67, 0x67));
        private static readonly Transform ShadowTransform = new TranslateTransform(1, 1);

        static BeveledSquare()
        {
            // added little offset (.01) - avoiding misplaced line bug
            StrokePath = Geometry.Parse("M 2,5 5,2 H 24.01 l 3,3 V 24 l -3,3 H 5 L 2,24 Z");
            OutlinePath = Geometry.Parse("M 0,4 4,0 H 25.01 l 4,4 V 25 l -4,4 H 4 L 0,25 Z");
            FillPath = Geometry.Parse("M 3,5 5,3 H 24.01 l 2,2 V 24 l -2,2 H 5 L 3,24 Z");
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