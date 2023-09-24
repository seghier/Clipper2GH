using Clipper2Lib;
using Rhino.Geometry;
using System.Collections.Generic;

namespace ClipperTwo
{
    public static class Converter
    {
        public static PathD ConvertPolyline(Curve curve)
        {
            curve.TryGetPolyline(out Polyline polyline);
            PathD path = new PathD();
            foreach (Point3d point in polyline)
            {
                path.Add(new PointD(point.X, point.Y));
            }
            return path;
        }

        public static RectD ConvertRectangle(Rectangle3d rectangle)
        {
            double left = rectangle.Corner(0).X; // xmin
            double top = rectangle.Corner(2).Y; // ymax
            double right = rectangle.Corner(2).X; // xmax
            double bottom = rectangle.Corner(0).Y; // ymin

            RectD rect = new RectD
            {
                left = left,
                top = top,
                right = right,
                bottom = bottom
            };

            return rect;
        }

        public static PathsD ConvertPolylinesA(List<Curve> curves)
        {
            PathsD pathsD = new PathsD();

            foreach (Curve curve in curves)
            {
                Polyline polyline;
                curve.TryGetPolyline(out polyline);

                PathD path = new PathD();

                foreach (Point3d point in polyline)
                {
                    path.Add(new PointD(point.X, point.Y));
                }

                pathsD.Add(path);
            }

            return pathsD;
        }

        public static PathsD ConvertPolylinesB(Curve curve)
        {

            Polyline polyline;
            curve.TryGetPolyline(out polyline);

            PathD path = new PathD();
            PathsD pathsD = new PathsD();

            foreach (Point3d point in polyline)
            {
                path.Add(new PointD(point.X, point.Y));
            }

            pathsD.Add(path);

            return pathsD;
        }

    }
}
