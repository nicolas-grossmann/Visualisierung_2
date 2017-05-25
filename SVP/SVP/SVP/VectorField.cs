﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace SVP
{
    public class VectorField
    {
        private byte[] backgroundImage;

        public int fieldSize = 500;        

        private double transform;

        private Vec2[, ,] field;

        public VectorField(double width)
        {
            transform = (width / (fieldSize * 2));
        }

        public bool import(string path)
        {

            field = new Vec2[500, 500, 48];

            for (int i = 1; i <= 48; i++)
            {
                string filename = i.ToString();
                if (i < 10)
                {
                    filename = "0" + filename;
                }
                using (BinaryReader uReader = new BinaryReader(new FileStream(path + "\\Uf" + filename + ".bin", FileMode.Open)))
                {
                    using (BinaryReader vReader = new BinaryReader(new FileStream(path + "\\Vf" + filename + ".bin", FileMode.Open)))
                    {
                        for (int x = 0; x < 500; x++)
                        {
                            for (int y = 0; y < 500; y++)
                            {
                                float x1 = swapFloat(uReader.ReadBytes(4));
                                float y1 = swapFloat(vReader.ReadBytes(4));

                                if (x1 > 2000)
                                {
                                    x1 = 0;
                                }

                                if (y1 > 2000)
                                {
                                    y1 = 0;
                                }

                                field[y, x, (i - 1)] = new Vec2(y1, x1 * -1);
                            }
                        }
                    }
                }
            }

            return true;
        }

        private float swapFloat(byte[] floatBytes)
        {
            byte temp = floatBytes[0];
            floatBytes[0] = floatBytes[3];
            floatBytes[3] = temp;
            temp = floatBytes[1];
            floatBytes[1] = floatBytes[2];
            floatBytes[2] = temp;
            return BitConverter.ToSingle(floatBytes, 0);
        }

        public BitmapSource createImage(float minValue, float maxValue)
        {
            byte[] pixelData = new byte[500 * 500 * 4];
            int pixelCount = 0;

            for (int x = 0; x < 500; x++)
            {
                for (int y = 0; y < 500; y++)
                {
                    float value = field[x, y, 1].X;

                    byte intensity = (byte)(value >= minValue && value <= maxValue ? ((float)value / (float)(maxValue - minValue) * 255.0f) : 0);
                    pixelData[pixelCount++] = intensity;
                    pixelData[pixelCount++] = intensity;
                    pixelData[pixelCount++] = intensity;
                    pixelData[pixelCount++] = (byte)255;

                    value = field[x, y, 1].Y;

                    intensity = (byte)(value >= minValue && value <= maxValue ? ((float)value / (float)(maxValue - minValue) * 255.0f) : 0);
                    pixelData[pixelCount - 2] = intensity;
                }
            }

            int stride = 500 * PixelFormats.Bgr32.BitsPerPixel / 8;
            return BitmapSource.Create(500, 500, 96, 96, PixelFormats.Bgr32, null, pixelData, stride);
        }

        public List<Polyline> drawLines(List<Line> lines, Color color, double stroke)
        { 
            List<Polyline> polLines = new List<Polyline>();

            foreach (Line line in lines)
            {
                Polyline polLine = new Polyline();
                polLine.Stroke = new SolidColorBrush(color);
                polLine.StrokeThickness = stroke;

                PointCollection pointCol = new PointCollection();

                foreach (Vec2 point in line.Points)
                {
                    double x = (point.X - 1) * 2;
                    double y = (point.Y - 1) * 2;

                    /*if (x >= 1000) x = 999;
                    if (y >= 1000) y = 999;
                    if (x < 0) x = 0;
                    if (y < 0) y = 0;*/

                    if (x < 1000 && y < 1000 && x >= 0 && y >= 0)
                    {
                        pointCol.Add(new Point((int)Math.Round(y * transform, 0), (int)Math.Round(x * transform, 0)));
                    }
                }

                polLine.Points = pointCol;
                polLines.Add(polLine);
            }

            return polLines;
        }

        public Vec2 interpolateTrilinear(float x, float y, float t)
        {
            int topX = (int)Math.Ceiling(x);
            int topY = (int)Math.Ceiling(y);
            int topT = (int)Math.Ceiling(t);
            int bottomX = (int)Math.Floor(x);
            int bottomY = (int)Math.Floor(y);
            int bottomT = (int)Math.Floor(t);

            float deltaX = (float)Math.Abs(topX - x);
            float deltaY = (float)Math.Abs(topY - y);
            float deltaT = (float)Math.Abs(topT - t);


            if (bottomX < 0)
            {
                Console.WriteLine();
            }

            Vec2 v1 = field[bottomX, bottomY, bottomT] * (1.0f - deltaY) + field[bottomX, topY, bottomT] * deltaY;
            Vec2 v2 = field[topX, bottomY, bottomT] * (1.0f - deltaY) + field[topX, topY, bottomT] * deltaY;

            Vec2 v3 = field[bottomX, bottomY, topT] * (1.0f - deltaY) + field[bottomX, topY, topT] * deltaY;
            Vec2 v4 = field[topX, bottomY, topT] * (1.0f - deltaY) + field[topX, topY, topT] * deltaY;

            Vec2 v5 = v1 * (1.0f - deltaT) + v3 * deltaT;
            Vec2 v6 = v2 * (1.0f - deltaT) + v4 * deltaT;

            return v5 * (1.0f - deltaX) + v6 * deltaX;
        }

        public ImageSource createImage()
        {
            backgroundImage = new byte[1000 * 1000 * 4];
            int pixelCount = 0;

            for (int x = 0; x < 1000; x++)
            {
                for (int y = 0; y < 1000; y++)
                {
                    Vec2 v = field[x / 2, y / 2, 0];

                    byte intensityR = (byte)202;
                    byte intensityG = (byte)226;
                    byte intensityB = (byte)197;

                    if (v.X != 0 || v.Y != 0)
                    {
                        intensityR = (byte)163;
                        intensityG = (byte)204;
                        intensityB = (byte)255;
                    }

                    backgroundImage[pixelCount++] = intensityB;
                    backgroundImage[pixelCount++] = intensityG;
                    backgroundImage[pixelCount++] = intensityR;
                    backgroundImage[pixelCount++] = (byte)255;
                }
            }

            int stride = 1000 * PixelFormats.Bgr32.BitsPerPixel / 8;
            return BitmapSource.Create(1000, 1000, 96, 96, PixelFormats.Bgr32, null, backgroundImage, stride);
        }

        public List<Polyline> drawCenterLines(double[,] centerLines, double[,] percentages, byte[,] colors)
        {
            List<Line> lines = new List<Line>();
            List<Polyline> polLines = new List<Polyline>();

            Line line;

            for (int i = 0; i < centerLines.GetLength(0); i++)
            {
                Vec2 point;

                line = new Line();

                for (int j = 0; j < (centerLines.GetLength(1) / 2); j++)
                {
                    point = new Vec2((float)centerLines[i, j] /*- 1*/, (float)centerLines[i, (j + (centerLines.GetLength(1) / 2))] /*- 1*/);  
                    line.add(point);
                }

                lines.Add(line);

                polLines.Add(drawLines(lines, Color.FromRgb((byte)(colors[i, 0] * 0.7), (byte)(colors[i, 1] * 0.7), (byte)(colors[i, 2] * 0.7)), 10 * percentages[i, 0])[0]);

                lines.Clear();
            }

            return polLines;
        }

        public List<Polyline> drawCluster(double[,] clusterLines, Color color, double stroke)
        {
            List<Line> lines = new List<Line>();

            Line line;

            for (int i = 0; i < clusterLines.GetLength(0); i++)
            {
                Vec2 point;

                line = new Line();

                for (int j = 0; j < (clusterLines.GetLength(1) / 2); j++)
                {
                    point = new Vec2((float)clusterLines[i, j] /*- 1*/, (float)clusterLines[i, (j + (clusterLines.GetLength(1) / 2))] /*- 1*/);
                    line.add(point);
                }

                lines.Add(line);
            }

            return drawLines(lines, color, stroke);         
        }
    }
}
