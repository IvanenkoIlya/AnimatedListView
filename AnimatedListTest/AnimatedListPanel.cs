using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace AnimatedListTest
{
    public class AnimatedListPanel : Panel
    {
        #region DependencyProperties
        public static readonly DependencyProperty OrientationProperty =
            DependencyProperty.Register("Orientation", typeof(Orientation), typeof(AnimatedListPanel), new PropertyMetadata(Orientation.Vertical));
        public Orientation Orientation
        {
            get { return (Orientation)GetValue(OrientationProperty); }
            set { SetValue(OrientationProperty, value); }
        }
        #endregion
        
        private Size ourSize;
        private double totalChildrenSize;

        public AnimatedListPanel()
        {
            Background = Brushes.Transparent;
        }

        protected override Size MeasureOverride(Size availableSize)
        {
            Size idealSize = new Size(0, 0);
            Size size = new Size(Double.PositiveInfinity, Double.PositiveInfinity);

            foreach (UIElement child in Children)
            {
                child.Measure(size);
                switch (Orientation)
                {
                    case Orientation.Vertical:
                        idealSize.Width = Math.Max(idealSize.Width, child.DesiredSize.Width);
                        idealSize.Height += child.DesiredSize.Height;
                        break;
                    case Orientation.Horizontal:
                        idealSize.Width += child.DesiredSize.Width;
                        idealSize.Height = Math.Max(idealSize.Height, child.DesiredSize.Height);
                        break;
                }
            }

            return idealSize;

            //if (double.IsInfinity(availableSize.Height) || double.IsInfinity(availableSize.Width))
            //    return idealSize;
            //else
            //    return availableSize;
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            if (Children == null || Children.Count == 0)
                return finalSize;

            ourSize = finalSize;
            totalChildrenSize = 0;

            foreach (UIElement child in Children)
            {
                if (child.RenderTransform as TransformGroup == null)
                {
                    child.RenderTransformOrigin = new Point(0.0, 0.0); // Might need to change this based on orientation
                    TransformGroup group = new TransformGroup();
                    group.Children.Add(new TranslateTransform());
                    child.RenderTransform = group;
                }
                Point p = new Point(0, 0);

                child.Arrange(new Rect(p.X, p.Y, child.DesiredSize.Width, child.DesiredSize.Height)); // This starting value needs to be set to childs previous position

                totalChildrenSize += Orientation == Orientation.Vertical ? child.DesiredSize.Height : child.DesiredSize.Width;
            }

            AnimateAll();

            return finalSize;
        }

        private void AnimateAll()
        {
            if (Children == null || Children.Count == 0)
                return;
            
            double duration = 200;
            double Offset = 0;

            foreach (UIElement child in Children)
            {
                double x = 0, y = 0;

                switch (Orientation)
                {
                    case Orientation.Vertical:
                        y = Offset + child.DesiredSize.Height;
                        Offset += child.DesiredSize.Height;
                        break;
                    case Orientation.Horizontal:
                        x = Offset + child.DesiredSize.Width;
                        Offset += child.DesiredSize.Width;
                        break;
                }

                AnimateTo(child, x, y, duration);
            }
        }

        private void AnimateTo(UIElement child, double x, double y, double duration)
        {
            TransformGroup group = (TransformGroup)child.RenderTransform;
            TranslateTransform trans = (TranslateTransform)group.Children[0];

            if (duration == 0)
            {
                trans.BeginAnimation(TranslateTransform.XProperty, null);
                trans.BeginAnimation(TranslateTransform.YProperty, null);

                trans.X = x;
                trans.Y = y;
            }
            else
            {
                trans.BeginAnimation(TranslateTransform.XProperty, CreateAnimation(x, duration));
                trans.BeginAnimation(TranslateTransform.YProperty, CreateAnimation(y, duration));
            }
        }

        private DoubleAnimation CreateAnimation(double to, double duration)
        {
            return CreateAnimation(to, duration, null);
        }

        private DoubleAnimation CreateAnimation(double to, double duration, EventHandler endEvent)
        {
            DoubleAnimation anim = new DoubleAnimation(to, TimeSpan.FromMilliseconds(duration))
            {
                AccelerationRatio = 0.2,
                DecelerationRatio = 0.7
            };
            if (endEvent != null)
                anim.Completed += endEvent;
            return anim;
        }
    }
}
