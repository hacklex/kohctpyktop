using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;

namespace Kohctpyktop.Controls
{
    public class BeveledSquare : Control
    {
        private static readonly Geometry StrokePath, OutlinePath, FillPath;

        private static readonly Brush FillBrush = new SolidColorBrush(Color.FromRgb(0x94, 0x94, 0x94));
        private static readonly Brush OutlineBrush = new SolidColorBrush(Color.FromRgb(0xb5, 0, 0));
        private static readonly Brush ShadowBrush = new SolidColorBrush(Color.FromRgb(0x67, 0x67, 0x67));
        private static readonly Transform ShadowTransform = new TranslateTransform(1, 1);

        static BeveledSquare()
        {
            // added little offset (.01) - avoiding misplaced line bug
            StrokePath = StreamGeometry.Parse("M 2,5 L 5,2 H 24.01 l 3,3 V 24 l -3,3 H 5 L 2,24 Z");
            OutlinePath = StreamGeometry.Parse("M 0,4 L 4,0 H 25.01 l 4,4 V 25 l -4,4 H 4 L 0,25 Z");
            FillPath = StreamGeometry.Parse("M 3,5 L 5,3 H 24.01 l 2,2 V 24 l -2,2 H 5 L 3,24 Z");

            AffectsRender<BeveledSquare>(SelectedProperty);
        }
        
        protected override Size MeasureOverride(Size availableSize)
        {
            return new Size(29, 29);
        }

        public static readonly AvaloniaProperty SelectedProperty = AvaloniaProperty.Register<BeveledSquare, bool>(nameof(Selected));

        public bool Selected
        {
            get => (bool) GetValue(SelectedProperty);
            set => SetValue(SelectedProperty, value);
        }

        public override void Render(DrawingContext drawingContext)
        {
            if (Selected)
            {
                drawingContext.DrawGeometry(OutlineBrush, null, OutlinePath);
            }
            else
            {
                using (drawingContext.PushPreTransform(ShadowTransform.Value))
                    drawingContext.DrawGeometry(ShadowBrush, null, StrokePath);
            }

            drawingContext.DrawGeometry(Brushes.Black, null, StrokePath);
            drawingContext.DrawGeometry(FillBrush, null, FillPath);
        }
    }
}