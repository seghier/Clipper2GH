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
    public class ClipShapesComponent : GH_Component
    {
        public ClipShapesComponent()
          : base("Clipper2 Rectangle Clip", "C2RectClip",
              "Don't rotate the rectagnle",
              "Curve", "Clipper2")
        {
        }

        int precision = 4;

        public override void AppendAdditionalMenuItems(ToolStripDropDown menu)
        {
            base.AppendAdditionalMenuItems(menu);
            Menu_AppendSeparator(menu);
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

            NumericUpDown numericUpDown2 = new NumericUpDown
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
            tableLayoutPanel.Controls.Add(numericUpDown2, 1, 0);
            Menu_AppendCustomItem(menu, tableLayoutPanel);

            numericUpDown2.MouseWheel += (sender, e) =>
            {
                ((HandledMouseEventArgs)e).Handled = true;
            };

            numericUpDown2.ValueChanged += (sender, e) =>
            {
                precision = (int)numericUpDown2.Value;
                ExpireSolution(true);
            };
            #endregion
        }

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddCurveParameter("Polylines", "", "", GH_ParamAccess.list);
            pManager.AddRectangleParameter("Rectangle", "", "", GH_ParamAccess.item);
            pManager.AddBooleanParameter("Convex", "", "", GH_ParamAccess.item, false);

            pManager[0].Optional = true;
            pManager[1].Optional = true;
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddGenericParameter("Result", "", "Result", GH_ParamAccess.list);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            List<Curve> curves = new List<Curve>();
            Rectangle3d rectangle = Rectangle3d.Unset;
            bool convexOnly = false;

            if (!DA.GetDataList(0, curves)) return;
            foreach (Curve curve in curves)
            {
                if (!curve.IsPolyline() || !curve.IsValid)
                {
                    AddRuntimeMessage(GH_RuntimeMessageLevel.Error, $"Choose a valid polylines");
                    return;
                }
            }
            if (!DA.GetData(1, ref rectangle)) return;
            if (!DA.GetData(2, ref convexOnly)) return;

            List<Curve> newcurves = new List<Curve>();
            var mirror = Transform.Mirror(Plane.WorldZX);
            rectangle.Transform(mirror);

            foreach (Curve curve in curves)
            {
                curve.Transform(mirror);
                newcurves.Add(curve);
            }

            ClipShapesGh(newcurves, rectangle, convexOnly);

            List<Curve> newresultCurve = new List<Curve>();
            foreach (Curve curve in resultCurve)
            {
                curve.Transform(mirror);
                newresultCurve.Add(curve);
            }

            DA.SetDataList(0, newresultCurve);
        }

        List<Curve> resultCurve = new List<Curve>();

        void ClipShapesGh(List<Curve> curves, Rectangle3d rectangle, bool convexOnly)
        {
            resultCurve.Clear();

            PathsD paths = Converter.ConvertPolylinesA(curves);
            RectD rect = Converter.ConvertRectangle(rectangle);
            PathsD cliprect;

            cliprect = Clipper.ExecuteRectClip(rect, paths, precision, convexOnly);

            foreach (var path in cliprect)
            {
                Polyline polyline = new Polyline(path.Select(p => new Point3d(p.x, p.y, 0)));
                polyline.Add(polyline[0]);
                resultCurve.Add(polyline.ToNurbsCurve());
            }
        }

        protected override System.Drawing.Bitmap Icon => Properties.Resources.clipshapes;

        public override Guid ComponentGuid => new Guid("DA58CEAB-42FA-4AF8-9FCC-75792B068131");

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
