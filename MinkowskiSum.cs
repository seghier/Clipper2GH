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
    public class MinkowskiSumComponent : GH_Component
    {
        public MinkowskiSumComponent()
          : base("Clipper2 Minkowski Sum", "C2MinkowskiSum",
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
                Maximum = 15,
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
            pManager.AddCurveParameter("Pattern", "", "", GH_ParamAccess.item);
            pManager.AddCurveParameter("Path", "", "", GH_ParamAccess.item);
            pManager.AddPlaneParameter("Plane", "", "", GH_ParamAccess.item);
            pManager.AddBooleanParameter("IsClosed", "", "", GH_ParamAccess.item, true);
            pManager[0].Optional = true;
            pManager[1].Optional = true;
            pManager[2].Optional = true;
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddGenericParameter("Result", "", "", GH_ParamAccess.list);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            Curve curveA = null;
            Curve curveB = null;
            Plane plane = Plane.Unset;
            bool isclosed = true;

            if (!DA.GetData(0, ref curveA)) return;
            if (!DA.GetData(1, ref curveB)) return;
            if (!DA.GetData(2, ref plane))
            {
                curveA.TryGetPlane(out plane);
            }
            if (!DA.GetData(3, ref isclosed)) return;

            ClipperMinkowskiSum(curveA, curveB, isclosed, plane);

            DA.SetDataList(0, resultCurve);
        }

        List<Curve> resultCurve = new List<Curve>();

        void ClipperMinkowskiSum(Curve curveA, Curve curveB, bool isclosed, Plane plane)
        {
            resultCurve.Clear();
            PathD pathA = Converter.ConvertPolyline(curveA);
            PathD pathB = Converter.ConvertPolyline(curveB);

            PathsD solution = Minkowski.Sum(pathA, pathB, isclosed, precision);

            foreach (PathD path in solution)
            {
                Polyline polyline = new Polyline(path.Select(p => new Point3d(p.x, p.y, 0)));
                polyline.Add(polyline[0]);
                Curve curve = OrientPattern(polyline.ToNurbsCurve(), plane);
                resultCurve.Add(curve);
            }
        }

        Curve OrientPattern(Curve curve, Plane plane)
        {
            Vector3d vector = Point3d.Origin - plane.Origin;
            curve.Translate(vector);
            return curve;
        }

        protected override System.Drawing.Bitmap Icon => Properties.Resources.minsum;

        public override Guid ComponentGuid => new Guid("C2EAFCD1-3AE5-463A-A445-42BF8CE6D7E3");

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