using Clipper2Lib;
using GH_IO.Serialization;
using Grasshopper.Kernel;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace ClipperTwo
{
    public class PointInPolygonComponent : GH_Component
    {
        public PointInPolygonComponent()
          : base("Clipper2 Point In Polyline", "PtInPoly",
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
            pManager.AddCurveParameter("Polyline", "P", "", GH_ParamAccess.item);
            pManager.AddPointParameter("Points", "Pt", "", GH_ParamAccess.list);
            pManager[0].Optional = true;
            pManager[1].Optional = true;
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddPointParameter("On", "X", "", GH_ParamAccess.list);
            pManager.AddPointParameter("Inside", "In", "", GH_ParamAccess.list);
            pManager.AddPointParameter("Outside", "Out", "", GH_ParamAccess.list);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            Curve curve = null;
            List<Point3d> points = new List<Point3d>();

            if (!DA.GetData(0, ref curve)) return;
            if (!DA.GetDataList(1, points)) return;

            PointInPoly(curve, points);

            DA.SetDataList(1, pointsOn);
            DA.SetDataList(2, pointsInside);
            DA.SetDataList(3, pointsOustside);
        }

        List<Point3d> pointsOn = new List<Point3d>();
        List<Point3d> pointsInside = new List<Point3d>();
        List<Point3d> pointsOustside = new List<Point3d>();

        void PointInPoly(Curve curve, List<Point3d> points)
        {
            pointsOn.Clear();
            pointsInside.Clear();
            pointsOustside.Clear();

            List<PointD> pts = Converter.ConvertPointD(points); ;
            PathD polygon = Converter.ConvertPolyline(curve);

            foreach (PointD pt in pts)
            {
                var result = Clipper.PointInPolygon(pt, polygon, precision);

                if (result == PointInPolygonResult.IsOn)
                {
                    pointsOn.Add(new Point3d(pt.x, pt.y, 0));
                }
                else if (result == PointInPolygonResult.IsInside)
                {
                    pointsInside.Add(new Point3d(pt.x, pt.y, 0));
                }
                else if (result == PointInPolygonResult.IsOutside)
                {
                    pointsOustside.Add(new Point3d(pt.x, pt.y, 0));
                }
            }
        }

        protected override System.Drawing.Bitmap Icon => Properties.Resources.ptin;

        public override Guid ComponentGuid => new Guid("7A1C1B74-3D55-437E-973C-9DC35FB29826");

        public override bool Read(GH_IReader reader)
        {
            if (reader.ItemExists("PrecisionPt"))
                precision = reader.GetInt32("PrecisionPt");

            return base.Read(reader);
        }

        public override bool Write(GH_IWriter writer)
        {
            writer.SetInt32("PrecisionPt", precision);
            return base.Write(writer);
        }
    }
}