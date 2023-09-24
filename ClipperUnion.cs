
using Clipper2Lib;
using GH_IO.Serialization;
using Grasshopper.Kernel;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace ClipperTwo
{

    public class ClipperUnionComponent : GH_Component
    {
        public ClipperUnionComponent()
          : base("Clipper2 Union", "C2Union",
              "",
              "Curve", "Clipper2")
        {
        }

        int precision = 4;

        public override void AppendAdditionalMenuItems(ToolStripDropDown menu)
        {
            base.AppendAdditionalMenuItems(menu);
            Menu_AppendSeparator(menu);

            #region precision
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
                Minimum = 2,
                Maximum = 8,
                DecimalPlaces = 0,
                Value = precision,
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
                precision = (int)numericUpDown.Value;
                ExpireSolution(true);
            };

            #endregion
        }

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddCurveParameter("Polylines", "", "", GH_ParamAccess.list);
            pManager[0].Optional = true;
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddGenericParameter("Result", "", "Result", GH_ParamAccess.list);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            List<Curve> curves = new List<Curve>();

            if (!DA.GetDataList(0, curves)) return;

            foreach (Curve curve in curves)
            {
                if (!curve.IsPolyline() && !curve.IsClosed || !curve.IsValid)
                {
                    AddRuntimeMessage(GH_RuntimeMessageLevel.Error, $"Choose a valid polylines");
                    return;
                }
            }

            foreach (Curve curve in curves)
            {
                if (curve.ClosedCurveOrientation() == CurveOrientation.CounterClockwise)
                    curve.Reverse();
            }

            resultCurve.Clear();

            CurvesUnion(curves);

            DA.SetDataList(0, resultCurve);
        }

        List<Curve> resultCurve = new List<Curve>();

        void CurvesUnion(List<Curve> curves)
        {
            PathsD paths = Converter.ConvertPolylinesA(curves);
            PathsD union = Clipper.Union(paths, null, FillRule.NonZero, precision);
            foreach (var path in union)
            {
                Polyline polyline = new Polyline(path.Select(p => new Point3d(p.x, p.y, 0)));
                polyline.Add(polyline[0]);
                resultCurve.Add(polyline.ToNurbsCurve());
            }
        }

        protected override System.Drawing.Bitmap Icon => Properties.Resources.union;

        public override Guid ComponentGuid => new Guid("09D2406C-78FB-478C-B3C1-3066FDB481F4");

        public override bool Read(GH_IReader reader)
        {
            if (reader.ItemExists("Precision"))
                precision = reader.GetInt32("Precision");
            return base.Read(reader);
        }

        public override bool Write(GH_IWriter writer)
        {
            writer.SetInt32("Precision", precision);
            return base.Write(writer);
        }
    }
}