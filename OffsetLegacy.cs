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
    public class ClipperOffsetLegacy: GH_Component
    {
        public ClipperOffsetLegacy()
          : base("Clipper Offset", "ClipperOffset",
              "",
              "Display", "Wizard")
        {
        }

        public bool preserveCollinear = false;
        public bool reverseSolution = false;
        double scaleFactor = 1000;

        public override void AppendAdditionalMenuItems(ToolStripDropDown menu)
        {
            base.AppendAdditionalMenuItems(menu);
            Menu_AppendSeparator(menu);
            ToolStripMenuItem collinear = Menu_AppendItem(menu, "Preserve Collinear", PresCollinear, true, preserveCollinear);
            ToolStripMenuItem reverse = Menu_AppendItem(menu, "Reverse Solution", RevSolution, true, reverseSolution);

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
            preserveCollinear = !preserveCollinear;
            ExpireSolution(true);
        }

        public void RevSolution(Object sender, EventArgs e)
        {
            reverseSolution = !reverseSolution;
            ExpireSolution(true);
        }

        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddCurveParameter("Polylines", "", "", GH_ParamAccess.list);
            pManager.AddNumberParameter("Distance", "", "", GH_ParamAccess.item, 0.0);
            pManager.AddIntegerParameter("JoinType", "", "", GH_ParamAccess.item, 0);
            pManager.AddIntegerParameter("EndType", "", "", GH_ParamAccess.item, 0);
            pManager.AddNumberParameter("MiterLimit", "", "", GH_ParamAccess.item, 2);
            pManager[0].Optional = true;
            pManager[1].Optional = true;

            if (!(pManager[2] is Param_Integer paramInteger1))
                return;
            paramInteger1.AddNamedValue("Miter", 0);
            paramInteger1.AddNamedValue("Square", 1);
            paramInteger1.AddNamedValue("Round", 2);

            if (!(pManager[3] is Param_Integer paramInteger2))
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
            double distance = 0;
            int jointtype = 0;
            int endtype = 0;
            double miter = 2;

            if (!DA.GetDataList(0, curves)) return;
            foreach (Curve curve in curves)
            {
                if (!curve.IsPolyline() || !curve.IsValid)
                {
                    AddRuntimeMessage(GH_RuntimeMessageLevel.Error, $"Choose a valid polylines");
                    return;
                }
            }

            if (!DA.GetData(1, ref distance)) return;
            if (!DA.GetData(2, ref jointtype)) return;
            if (!DA.GetData(3, ref endtype)) return;
            if (!DA.GetData(4, ref miter)) return;

            endtype %= 5;
            jointtype %= 3;

            ClipperOffsetPoly(curves, distance, jointtype, endtype, miter);

            //DA.SetDataList(0, resultCurve);
            DA.SetDataList(0, holeCurves);
            DA.SetDataList(1, outCurves);
        }

        List<Curve> resultCurve = new List<Curve>();
        List<Curve> holeCurves = new List<Curve>();
        List<Curve> outCurves = new List<Curve>();

        void ClipperOffsetPoly(List<Curve> curves, double distance, int jointype, int endtype, double miterLimit)
        {
            resultCurve.Clear();
            holeCurves.Clear();
            outCurves.Clear();
            double arcTolerance = 0;
            Paths64 paths = ConvertPolyline(curves, scaleFactor);
            ClipperOffset offset = new ClipperOffset(miterLimit, arcTolerance, preserveCollinear, reverseSolution);
            offset.AddPaths(paths, JoinTypes[jointype], EndTypes[endtype]);
            Paths64 solution = new Paths64();
            offset.Execute(distance * scaleFactor, solution);

            foreach (Path64 path in solution)
            {
                Polyline polyline = new Polyline(path.Select(p => new Point3d(p.X / scaleFactor, p.Y / scaleFactor, 0)));
                polyline.Add(polyline[0]);
                if (Clipper.IsPositive(path))
                    outCurves.Add(polyline.ToNurbsCurve());
                else
                    holeCurves.Add(polyline.ToNurbsCurve());
            }
        }

        Paths64 ConvertPolyline(List<Curve> curves, double scaleFactor)
        {
            Paths64 paths = new Paths64();

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

                paths.Add(path);
            }

            return paths;
        }

        List<JoinType> JoinTypes = new List<JoinType>()
        {
            JoinType.Miter,
            JoinType.Square,
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

        protected override System.Drawing.Bitmap Icon => Properties.Resources.clipper;

        public override GH_Exposure Exposure => GH_Exposure.quarternary;

        public override Guid ComponentGuid => new Guid("561F54F5-5851-48E1-9607-66E15609BD4A");

        public override bool Read(GH_IReader reader)
        {
            if (reader.ItemExists("PreservCollinear"))
                preserveCollinear = reader.GetBoolean("PreservCollinear");

            if (reader.ItemExists("ReverseSolution"))
                reverseSolution = reader.GetBoolean("ReverseSolution");

            return base.Read(reader);
        }

        public override bool Write(GH_IWriter writer)
        {
            writer.SetBoolean("PreservCollinear", preserveCollinear);
            writer.SetBoolean("ReverseSolution", reverseSolution);

            return base.Write(writer);
        }
    }
}