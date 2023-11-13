using Clipper2Lib;
using GH_IO.Serialization;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace ClipperTwo
{
    public class ClipperVariableOffset : GH_Component
    {
        public ClipperVariableOffset()
          : base("Clipper2 Variable Offset", "C2VarOffset",
              "",
              "Curve", "Clipper2")
        {
        }

        double scaleFactor = 1000;
        public bool flip = false;

        public override void AppendAdditionalMenuItems(ToolStripDropDown menu)
        {
            base.AppendAdditionalMenuItems(menu);
            Menu_AppendSeparator(menu);
            ToolStripMenuItem collinear = Menu_AppendItem(menu, "Flip Offsets", PresCollinear, true, flip);

            #region scale factor
            Menu_AppendSeparator(menu);
            TableLayoutPanel tableLayoutPanel = new TableLayoutPanel
            {
                ColumnCount = 2,
                AutoSize = true,
                Height = 30,
                Width = 140,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                BackColor = Color.Transparent
            };

            Label label = new Label
            {
                Text = "Precision ",
                AutoSize = true,
                Anchor = AnchorStyles.Right
            };

            NumericUpDown numericUpDown = new NumericUpDown
            {
                Minimum = 1,
                Maximum = 100000,
                DecimalPlaces = 0,
                Value = (decimal)scaleFactor,
                Increment = 1,
                Width = 60,
                Anchor = AnchorStyles.Left
            };

            tableLayoutPanel.Controls.Add(label, 0, 0);
            tableLayoutPanel.Controls.Add(numericUpDown, 1, 0);
            Menu_AppendCustomItem(menu, tableLayoutPanel);

            numericUpDown.MouseWheel += (sender, e) =>
            {
                ((HandledMouseEventArgs)e).Handled = true;
            };

            numericUpDown.ValueChanged += (sender, e) =>
            {
                scaleFactor = Convert.ToDouble(numericUpDown.Value);
                ExpireSolution(true);
            };
            #endregion
        }

        public void PresCollinear(Object sender, EventArgs e)
        {
            flip = !flip;
            ExpireSolution(true);
        }

        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddCurveParameter("Polylines", "", "", GH_ParamAccess.list);
            pManager.AddNumberParameter("Distance A", "", "Initial offset", GH_ParamAccess.item, 0.0);
            pManager.AddNumberParameter("Distance B", "", "Second offset", GH_ParamAccess.item, 0.0);
            pManager.AddIntegerParameter("JoinType", "", "", GH_ParamAccess.item, 0);
            pManager.AddIntegerParameter("EndType", "", "", GH_ParamAccess.item, 0);
            pManager.AddNumberParameter("MiterLimit", "", "", GH_ParamAccess.item, 2);
            pManager.AddNumberParameter("ArcTolerance", "", "", GH_ParamAccess.item, 0.25);

            pManager[0].Optional = true;
            pManager[1].Optional = true;
            pManager[2].Optional = true;

            if (!(pManager[3] is Param_Integer paramInteger1))
                return;
            paramInteger1.AddNamedValue("Miter", 0);
            paramInteger1.AddNamedValue("Square", 1);
            paramInteger1.AddNamedValue("Bevel", 2);
            paramInteger1.AddNamedValue("Round", 3);

            if (!(pManager[4] is Param_Integer paramInteger2))
                return;
            paramInteger2.AddNamedValue("Butt", 0);
            paramInteger2.AddNamedValue("Joined", 1);
            paramInteger2.AddNamedValue("Square", 2);
            paramInteger2.AddNamedValue("Round", 3);
            paramInteger2.AddNamedValue("Polygon", 4);
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddGenericParameter("Holes", "", "Holes bounds", GH_ParamAccess.list);
            pManager.AddGenericParameter("Outer", "", "Outer bounds", GH_ParamAccess.list);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            List<Curve> curves = new List<Curve>();
            double distance1 = 0;
            double distance2 = 0;
            int jointtype = 0;
            int endtype = 0;
            double miter = 2;
            double arctol = 0.25;

            if (!DA.GetDataList(0, curves)) return;
            foreach (Curve curve in curves)
            {
                if (!curve.IsPolyline() || !curve.IsValid)
                {
                    AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, $"Choose a valid polylines");
                    return;
                }
            }

            if (!DA.GetData(1, ref distance1)) return;
            if (!DA.GetData(2, ref distance2)) return;
            if (!DA.GetData(3, ref jointtype)) return;
            if (!DA.GetData(4, ref endtype)) return;
            if (!DA.GetData(5, ref miter)) return;
            if (!DA.GetData(6, ref arctol)) return;

            endtype %= 5;
            jointtype %= 4;

            List<Curve> flipCurves = new List<Curve>();
            if (flip)
            {
                foreach (Curve curve in curves)
                {
                    curve.Reverse();
                    flipCurves.Add(curve);
                }
            }
            else
            {
                flipCurves = curves;
            }

            ClipperOffsetPoly(flipCurves, distance1, distance2, jointtype, endtype, miter, arctol);

            //DA.SetDataList(0, resultCurve);
            DA.SetDataList(0, holeCurves);
            DA.SetDataList(1, outCurves);
        }

        List<Curve> resultCurve = new List<Curve>();
        List<Curve> holeCurves = new List<Curve>();
        List<Curve> outCurves = new List<Curve>();

        void ClipperOffsetPoly(List<Curve> curves, double distance1, double distance2, int jointype, int endtype, double miterLimit, double arcTolerance)
        {
            resultCurve.Clear();
            holeCurves.Clear();
            outCurves.Clear();

            var result = ConvertPolyline(curves, scaleFactor);
            Paths64 closedPaths = result.closedPaths;
            Paths64 openPaths = result.openPaths;

            // offset closed curves
            ClipperOffset offsetA = new ClipperOffset();
            offsetA.MiterLimit = miterLimit;
            offsetA.ArcTolerance = arcTolerance;
            offsetA.AddPaths(closedPaths, JoinTypes[jointype], EndTypes[endtype]);
            double value_A(Path64 pathA, PathD pathB, int currPt, int prevPt) { return (currPt * distance2 * scaleFactor) + distance1 * scaleFactor; }
            offsetA.Execute(value_A, closedPaths);

            // offset opened curves
            ClipperOffset offsetB = new ClipperOffset();
            offsetB.MiterLimit = miterLimit;
            offsetB.ArcTolerance = arcTolerance;
            offsetB.AddPaths(openPaths, JoinTypes[jointype], EndTypes[endtype]);
            double value_B(Path64 pathA, PathD pathB, int currPt, int prevPt) { return (currPt * distance2 * scaleFactor) + distance1 * scaleFactor; }
            offsetB.Execute(value_B, openPaths);

            Paths64 paths = Clipper.Union(closedPaths, openPaths, FillRule.EvenOdd);

            foreach (Path64 path in paths)
            {
                Polyline polyline = new Polyline(path.Select(p => new Point3d(p.X / scaleFactor, p.Y / scaleFactor, 0)));
                polyline.Add(polyline[0]);
                if (Clipper.IsPositive(path))
                    outCurves.Add(polyline.ToNurbsCurve());
                else
                    holeCurves.Add(polyline.ToNurbsCurve());
            }
        }

        public static (Paths64 closedPaths, Paths64 openPaths) ConvertPolyline(List<Curve> curves, double scaleFactor)
        {
            Paths64 closedPaths = new Paths64();
            Paths64 openPaths = new Paths64();

            foreach (Curve curve in curves)
            {
                Polyline polyline;
                curve.TryGetPolyline(out polyline);

                Path64 path = new Path64();

                foreach (Point3d point in polyline)
                {
                    long scaledX = (long)(point.X * scaleFactor);
                    long scaledY = (long)(point.Y * scaleFactor);
                    path.Add(new Point64(scaledX, scaledY));
                }

                if (curve.IsClosed)
                {
                    closedPaths.Add(path);
                }
                else
                {
                    openPaths.Add(path);
                }
            }

            return (closedPaths, openPaths);
        }


        List<JoinType> JoinTypes = new List<JoinType>()
        {
            JoinType.Miter,
            JoinType.Square,
            JoinType.Bevel,
            JoinType.Round,
        };

        List<EndType> EndTypes = new List<EndType>()
        {
            EndType.Butt,
            EndType.Joined,
            EndType.Square,
            EndType.Round,
            EndType.Polygon,
        };

        /*/
        void DoRabbit()
        {
            PathsD pd = new PathsD(); // LoadPathsFromResource("InflateDemo.rabbit.bin");

            PathsD solution = new PathsD(pd);
            while (pd.Count > 0)
            {
                // and don't forget to scale the delta offset
                pd = Clipper.InflatePaths(pd, -2.5, JoinType.Round, EndType.Polygon);
                // SimplifyPaths - is not essential but it not only 
                // speeds up the loop but it also tidies the result
                pd = Clipper.SimplifyPaths(pd, 0.25);
                solution.AddRange(pd);
            }
        }
        /*/

        protected override System.Drawing.Bitmap Icon => Properties.Resources.offvar;

        public override Guid ComponentGuid => new Guid("F7DF52A7-D911-4BE9-83D9-0154C61E9650");

        public override bool Read(GH_IReader reader)
        {
            if (reader.ItemExists("FlipCurveD"))
                flip = reader.GetBoolean("FlipCurveD");

            if (reader.ItemExists("ScaleFactor"))
                scaleFactor = reader.GetDouble("ScaleFactor");

            return base.Read(reader);
        }

        public override bool Write(GH_IWriter writer)
        {
            writer.SetBoolean("FlipCurveD", flip);
            writer.SetDouble("ScaleFactor", scaleFactor);

            return base.Write(writer);
        }
    }
}