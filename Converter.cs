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

        public static PathsD ConvertPolylinesA(List<Curve> curves)
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
    }
}
