using System.Windows;
using System.Windows.Media;
using Kohctpyktop.Models.Simulation;

namespace Kohctpyktop.Controls
{
    public class PinValuesVisualizer : FrameworkElement
    {
        private const int TickWidth = 1;
        private const int TickHeight = 16;
        
        private static readonly Pen CorrectValuesPen = new Pen(Brushes.Gray, 1);
        private static readonly Pen ActualValuesPen = new Pen(Brushes.Black, 1); 
        
        public static readonly DependencyProperty SimulatedPinProperty = DependencyProperty.Register(
            "SimulatedPin", typeof(SimulatedPin), typeof(PinValuesVisualizer), new FrameworkPropertyMetadata(default(SimulatedPin),
                FrameworkPropertyMetadataOptions.AffectsRender | FrameworkPropertyMetadataOptions.AffectsMeasure));

        public SimulatedPin SimulatedPin
        {
            get => (SimulatedPin) GetValue(SimulatedPinProperty);
            set => SetValue(SimulatedPinProperty, value);
        }

        protected override Size MeasureOverride(Size availableSize)
        {
            return SimulatedPin != null
                ? new Size(SimulatedPin.CorrectValues.Count * (TickWidth + 1), TickHeight)
                : Size.Empty;
        }

        protected override void OnRender(DrawingContext drawingContext)
        {
            bool prevCorrect = false, prevActual = false;
            var correctPath = new PathFigure
            {
                StartPoint = new Point(.5, TickHeight),
                Segments =
                {
                    new LineSegment(new Point(.5, TickHeight - .5), true)
                }
            };
            var actualPath = new PathFigure
            {
                StartPoint = new Point(.5, TickHeight),
                Segments =
                {
                    new LineSegment(new Point(.5, TickHeight - .5), true)
                }
            };
            
            const int realTickWidth = TickWidth + 1;

            void AppendToPath(PathFigure path, int pos, ref bool prev, bool cur)
            {
                if (prev != cur)
                {
                    path.Segments.Add(new LineSegment(new Point(pos * realTickWidth + .5, prev ? .5 : TickHeight - .5), true));
                    path.Segments.Add(new LineSegment(new Point(pos * realTickWidth + .5, cur ? .5 : TickHeight - .5), true));
                }

                prev = cur;
            }

            void ClosePath(PathFigure path, int pos, bool prev)
            {
                path.Segments.Add(new LineSegment(new Point(pos * realTickWidth, prev ? .5 : TickHeight - .5), true));
            }

            var count = SimulatedPin.CorrectValues.Count;
            
            for (var i = 0; i < count; i++)
            {
                AppendToPath(correctPath, i, ref prevCorrect, SimulatedPin.CorrectValues[i]);
                AppendToPath(actualPath, i, ref prevActual, SimulatedPin.ActualValues[i]);
            }

            ClosePath(correctPath, count, prevCorrect);
            ClosePath(actualPath, count, prevActual);
            
            drawingContext.DrawGeometry(null, CorrectValuesPen, new PathGeometry(new[] { correctPath }));
            drawingContext.DrawGeometry(null, ActualValuesPen, new PathGeometry(new[] { actualPath }));
        }
    }
}