using System;
using System.Collections.ObjectModel;
using System.Windows.Input;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using HonorsProject_0._1.Models;
using System.Windows.Threading;
using System.Windows;
using System.Windows.Media.Imaging;

namespace HonorsProject_0._1.ViewModels
{
    internal class PainterViewModel : ParentViewModel
    {
        private static readonly int CanvasSize = 392;
        private static readonly int Step = 8;
        private static readonly int Bound = CanvasSize / Step;
        private static readonly int BoundFlat = Bound * Bound;
        private static readonly int InitialColor = 127 * 6;
        private static readonly int InitialDim = 127;
        private static readonly int Moore = 8;
        private static readonly byte Dead = 8;
        private static readonly int Stroke = 2;
        private static readonly int Center = Bound / 2;
        private static readonly double RectDimension = Step + 0.3;
        private static readonly Random rgen = new();

        private readonly SolidColorBrush[] brushes;
        private readonly byte[,] map;
        private readonly byte[,] replacement;
        private readonly int[] idx;
        private readonly int[] counts;
        private readonly int[] colorBacking;
        private readonly int[] dim;
        private readonly int[] maxx;
        private readonly int[] cc;
        private readonly ObservableCollection<Rectangle> rectangles;
        private int iteration;

        public ObservableCollection<Rectangle> Rectangles
        { set { } get { return rectangles; } }

        public ICommand PaintCmd { get; }
        public ICommand PrintCmd { get; }

        public int[] Max { get { return maxx; } }
        public Brush[] Brush { get { return brushes; } }
        public Adaptor Colors { get; set; }
        public Adaptor Dim { get; set; }

        /**
         * Initialize the PainterViewModel.
         */
        public PainterViewModel()
        {
            rectangles   = new ObservableCollection<Rectangle>();
            brushes      = new SolidColorBrush[Moore];
            map          = new byte[Bound, Bound];
            replacement  = new byte[Bound, Bound];
            idx          = new int[Moore];
            counts       = new int[Moore];
            colorBacking = new int[Moore];
            dim          = new int[Moore];
            maxx         = new int[Moore];
            cc           = new int[Moore];
            iteration    = 0;
            Colors = new Adaptor(colorBacking, SetColor);
            Dim = new Adaptor(dim, SetDim);
            for (int i = 0; i < Moore; ++i)
            {
                idx[i] = i;
                counts[i] = i + 1;
                maxx[i] = InitialColor;
                dim[i] = InitialDim;
                colorBacking[i] = InitialColor;
                brushes[i] = new SolidColorBrush();
            }
            for (int x = 0, i = 0; i < CanvasSize; i += Step)
            {
                for (int j = 0; j < CanvasSize; j += Step, ++x)
                {
                    Rectangle r = new();
                    SolidColorBrush scb = brushes[x & (Moore - 1)];
                    r.Width = RectDimension;
                    r.Height = RectDimension;
                    r.Stroke = scb;
                    r.StrokeThickness = Stroke;
                    r.Fill = scb;
                    Canvas.SetLeft(r, i);
                    Canvas.SetTop(r, j);
                    rectangles.Add(r);
                }
            }
            PaintCmd = new Command(Reset);
            PrintCmd = new Command(Print);
            Reset();
        }

        /**
         * Set the ith dimness value.
         */
        private void SetDim(int i, int value)
        {
            if (SetProperty(ref maxx[i], value * 6, nameof(Max)))
            {
                double ratio = (double)colorBacking[i] / (double)dim[i];
                SetProperty(ref dim[i], value, "Dim");
                SetColor(i, (int)Math.Round(ratio * value));
            }
        }

        private Color GetColor(int value, int dim) {

            /**
             * Gives the illusion of a color continuum.
             * 
             * This might be inefficient due to mod and
             * branching, but I don't see a better way
             * currently.
             * 
             * the maximum dim/brightness, d, is a number 
             * in the range [0, 255]. The value is a number
             * between zero and d * 6.
             * 
             * The color continuum is created for a 
             * slider in 6 stages:
             * 
             * 1. Red is kept at d while Green is incremented
             *    from 0 to d. Blue is kept at 0.
             *    
             * 2. Red is decremented from d to 0 while Green is
             *    kept at d and Blue is kept at 0.
             *    
             * 3. Red is kept at 0 while Green is kept
             *    at d and Blue is incremented from 0 to d.
             *    
             * 4. Red is kept at 0 while Green is decremented
             *    from d to 0 and Blue is kept at d.
             *    
             * 5. Red is incremented from 0 to d while Green is
             *    kept at 0 and Blue is kept at d.
             * 
             * 6. Red is kept constant at d while Green is kept
             *    at zero and Blue is decremented from d to 0.
             */
            return value < dim              ? Color.FromRgb((byte)dim, (byte)(value % dim), 0) :
                   value < dim << 1         ? Color.FromRgb((byte)(dim - (value % dim)), (byte)dim, 0) :
                   value < (dim << 1) + dim ? Color.FromRgb(0, (byte)dim, (byte)(value % dim)) :
                   value < dim << 2         ? Color.FromRgb(0, (byte)(dim - (value % dim)), (byte)dim) :
                   value < (dim << 2) + dim ? Color.FromRgb((byte)(value % dim), 0, (byte)dim) :
                                              Color.FromRgb((byte)dim, 0, (byte)(dim - (value % dim)));
        }

        /**
         * Set the ith color.
         */
        private void SetColor(int i, int value)
        {
            SetProperty(ref colorBacking[i], value, "Colors");
            brushes[i].Color = GetColor(colorBacking[i], dim[i]);

            // Notify that the brush has been changed.
            OnPropertyChanged(nameof(Brush));
        }

        /**
         * Run a new automaton to create a new pattern.
         */
        public void Reset()
        {
            // Randomize the rules.
            for (int i = 0; i < Moore; ++i)
            {
                int rando = rgen.Next(Moore);
                (idx[rando], idx[i]) = (idx[i], idx[rando]);
                (counts[rando], counts[i]) = (counts[i], counts[rando]);
            }

            // Paint the canvas.
            Dispatcher.CurrentDispatcher.BeginInvoke(() =>
            {
                // Run the automaton.
                Automaton();

                // Update the GUI.
                Paint();
            });
        }

        /**
         * This function clears the map and runs a 
         * cellular automaton with randomized rules
         * to create a new pattern with the existing
         * colors. Lots of room for improvement here.
         */
        private void Automaton() 
        {
            // Clear the map. Kill all pixels.
            for (int i = 0; i < Bound; ++i)
            {
                for (int j = 0; j < Bound; ++j)
                {
                    map[i, j] = Dead;
                }
            }

            // Resurrect one pixel.
            map[Center, Center] = 0;

            // Loop until there are no more dead
            // pixels.
            do
            {
                bool allAlive = true;

                // Look through the entire map.
                for (int _x = 0; _x < Bound; ++_x)
                {
                    for (int _y = 0; _y < Bound; ++_y)
                    {
                        // If the current pixel is alive,
                        // set it in the replacement map
                        // and move straight to the next
                        // pixel.
                        if (map[_x, _y] != Dead)
                        {
                            replacement[_x, _y] = map[_x, _y];
                            continue;
                        }

                        // There is still a dead pixel.
                        allAlive = false;

                        // Clear the counts array.
                        for (int i = 0; i < Moore; ++i) cc[i] = 0;

                        bool stayDead = true;

                        // Count the occurrences of
                        // colors in the moore neighborhood.
                        foreach (Pair pair in Pair.Pairs)
                        {
                            int nx = _x + pair.x, ny = _y + pair.y;
                            if (nx < 0 || nx >= Bound ||
                                ny < 0 || ny >= Bound || 
                                map[nx, ny] >= Dead) continue;
                            stayDead = false;
                            ++cc[idx[map[nx, ny]]];
                        }

                        // If all surrounding pixels
                        // are dead, keep this guy
                        // dead until the next iteration.
                        if (stayDead)
                        {
                            replacement[_x, _y] = Dead;
                            continue;
                        }

                        // Use the rules to set a new color in the
                        // replacement map. There are 64 conditions
                        // To check.
                        for (int k = 0; k < Moore; ++k)
                        {
                            for (int p = 0, m = k; p < Moore; ++p, ++m)
                            {

                                // If this condition isn't met, check
                                // the next one.
                                if (cc[m & (Moore - 1)] != counts[p])
                                    continue;

                                // If this condition is met, set the
                                // color index. Break out of the
                                // double loop and keep looping
                                // through the pixels.
                                replacement[_x, _y] = (byte)k;
                                goto leave;
                            }
                        }

                        // We shouldn't reach here, but just
                        // incase;
                        replacement[_x, _y] = Dead;

                        // Keep looping through the pixels.
                        leave: continue;
                    }
                }

                // If all pixels are alive, we are done.
                if (allAlive) break;

                // Set all pixels in the map to the
                // pixels in the replacement map.
                // (The Goal is to avoid allocating
                // a new array every iteration.)
                for (int i = 0; i < Bound; ++i) 
                {
                    for (int j = 0; j < Bound; ++j) 
                    {
                        map[i, j] = replacement[i, j];
                    }
                }
            } while (true);
        }

        /**
         * This function prints the rectangles to a png
         * image.
         */
        public void Print() {

            // Store the rectangles in a temporary array.
            // Clear the "rectangles" collection.
            // (This is a hack to avoid sharing rectangles
            // between canvases and crashing the program.)
            Rectangle[] rect = new Rectangle[BoundFlat];
            int i = 0;
            foreach (Rectangle r in rectangles) rect[i++] = r;
            rectangles.Clear();

            // Fire up a new canvas and add all the rectangles.
            Canvas canvas = new Canvas();
            foreach (Rectangle r in rect) canvas.Children.Add(r);

            // Save the canvas image to the next available png file.
            Rect bounds = VisualTreeHelper.GetDescendantBounds(canvas);
            RenderTargetBitmap rtb = new((int)bounds.Width, (int)bounds.Height, 96d, 96d, PixelFormats.Default);
            DrawingVisual dv = new();
            using (DrawingContext dc = dv.RenderOpen())
            {
                dc.DrawRectangle(new VisualBrush(canvas), null, new Rect(new Point(), bounds.Size));
            }
            rtb.Render(dv);
            BitmapEncoder pen = new PngBitmapEncoder();
            pen.Frames.Add(BitmapFrame.Create(rtb));
            try
            {
                System.IO.MemoryStream ms = new System.IO.MemoryStream();
                pen.Save(ms);
                ms.Close();
                while (System.IO.File.Exists(String.Format(@"{0}\..\..\..\Art\Save{1}.png", Environment.CurrentDirectory, iteration))) ++iteration;
                System.IO.File.WriteAllBytes(String.Format(@"{0}\..\..\..\Art\Save{1}.png", Environment.CurrentDirectory, iteration++), ms.ToArray());
            }
            catch (Exception) { }

            // Clear the canvas, add the rectangles back to the
            // "rectangles" collection like nothing happened.
            canvas.Children.Clear();
            foreach (Rectangle r in rect) rectangles.Add(r);
        }

        /**
         * This function paints a new pattern to
         * the canvas by updating the brushes in
         * each rectangle.
         */
        private void Paint() 
        {
            for (int x = 0, i = 0; i < Bound; ++i)
            {
                for (int j = 0; j < Bound; ++j, ++x)
                {
                    SolidColorBrush scb = brushes[map[i, j]];
                    Rectangle r = rectangles[x];
                    r.Stroke = scb;
                    r.StrokeThickness = Stroke;
                    r.Fill = scb;
                }
            }
        }
    }
}
