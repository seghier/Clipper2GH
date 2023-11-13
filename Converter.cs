using Clipper2Lib;
using Rhino.Geometry;
using System.Collections.Generic;
using System.Linq;

namespace ClipperTwo
{
    public static class Converter
    {
        public static PathD ConvertPolyline(Curve curve)
        {

            if (curve.TryGetPolyline(out Polyline polyline))
            {
                return new PathD(polyline.Select(point => new PointD(point.X, point.Y)));
            }
            return new PathD();
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

        public static PathsD ConvertPolylinesA1(List<Curve> curves)
        {
            PathsD pathsD = new PathsD();

            foreach (Curve curve in curves)
            {
                if (curve.TryGetPolyline(out Polyline polyline))
                {
                    PathD path = new PathD(polyline.Select(point => new PointD(point.X, point.Y)));
                    pathsD.Add(path);
                }
            }

            return pathsD;
        }

        public static (PathsD closedPathsD, PathsD openedPathsD) ConvertPolylinesA2(List<Curve> curves)
        {
            PathsD closedPathsD = new PathsD();
            PathsD openedPathsD = new PathsD();

            foreach (Curve curve in curves)
            {
                curve.TryGetPolyline(out Polyline polyline);

                // Clone the polyline if it's closed to keep the original intact
                Polyline modifiedPolyline = curve.IsClosed ? polyline.Duplicate() : polyline;

                if (curve.IsClosed)
                {
                    modifiedPolyline.RemoveAt(modifiedPolyline.Count - 1);
                }

                if (modifiedPolyline.IsValid)
                {
                    PathD path = new PathD(modifiedPolyline.Select(point => new PointD(point.X, point.Y)));

                    // Separate closed and open paths
                    if (curve.IsClosed)
                    {
                        closedPathsD.Add(path);
                    }
                    else
                    {
                        openedPathsD.Add(path);
                    }
                }
            }

            return (closedPathsD, openedPathsD);
        }


        public static PathsD ConvertPolylinesB(Curve curve)
        {
            Polyline polyline;
            if (!curve.TryGetPolyline(out polyline))
            {
                return new PathsD(); // Return an empty PathsD if the conversion fails.
            }

            PathD path = new PathD(polyline.Select(point => new PointD(point.X, point.Y)));
            PathsD pathsD = new PathsD { path }; // Initialize PathsD with the path.

            return pathsD;
        }

        public static List<PointD> ConvertPointD(List<Point3d> points)
        {
            List<PointD> result = new List<PointD>();
            foreach (Point3d point in points)
            {
                PointD pt = new PointD(point.X, point.Y);
                result.Add(pt);
            }
            return result;
        }
    }
}
