using Clipper2Lib;
using GH_IO.Serialization;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;

namespace ClipperTwo
{
    public class ClipperOffsetComponent : GH_Component
    {
        public ClipperOffsetComponent()
          : base("Clipper2 Offset", "C2Offset",
              "",
              "Curve", "Clipper2")
        {
        }

        int precision = 4;
        int svgWidth = 800;
        int svgHeight = 600;
        double thickness = 1;
        Color color = Color.Black;
        public string filename = "";

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
                Width = 60,
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

            #region width
            Menu_AppendSeparator(menu);

            TableLayoutPanel tableLayoutPanelwidth = new TableLayoutPanel
            {
                ColumnCount = 2,
                AutoSize = true,
                Height = 30,
                Width = 140,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                BackColor = Color.Transparent
            };

            Label widthlabel = new Label
            {
                Text = "Width ",
                Width = 60,
                Anchor = AnchorStyles.Right
            };

            NumericUpDown numericUpDownW = new NumericUpDown
            {
                Minimum = 100,
                Maximum = 4000,
                DecimalPlaces = 0,
                Value = svgWidth,
                Increment = 100,
                Width = 60,
                Anchor = AnchorStyles.Left
            };

            tableLayoutPanelwidth.Controls.Add(widthlabel, 0, 0);
            tableLayoutPanelwidth.Controls.Add(numericUpDownW, 1, 0);
            Menu_AppendCustomItem(menu, tableLayoutPanelwidth);

            numericUpDownW.MouseWheel += (sender, e) =>
            {
                ((HandledMouseEventArgs)e).Handled = true;
            };

            numericUpDownW.ValueChanged += (sender, e) =>
            {
                svgWidth = (int)numericUpDownW.Value;
                ExpireSolution(true);
            };
            #endregion

            #region height
            TableLayoutPanel tableLayoutPanelheight = new TableLayoutPanel
            {
                ColumnCount = 2,
                AutoSize = true,
                Height = 30,
                Width = 140,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                BackColor = Color.Transparent
            };

            Label heightlabel = new Label
            {
                Text = "Height ",
                Width = 60,
                Anchor = AnchorStyles.Right
            };

            NumericUpDown numericUpDownH = new NumericUpDown
            {
                Minimum = 100,
                Maximum = 4000,
                DecimalPlaces = 0,
                Value = svgHeight,
                Increment = 100,
                Width = 60,
                Anchor = AnchorStyles.Left
            };

            tableLayoutPanelheight.Controls.Add(heightlabel, 0, 0);
            tableLayoutPanelheight.Controls.Add(numericUpDownH, 1, 0);
            Menu_AppendCustomItem(menu, tableLayoutPanelheight);

            numericUpDownH.MouseWheel += (sender, e) =>
            {
                ((HandledMouseEventArgs)e).Handled = true;
            };

            numericUpDownH.ValueChanged += (sender, e) =>
            {
                svgHeight = (int)numericUpDownW.Value;
                ExpireSolution(true);
            };
            #endregion

            #region SVG Save Button

            ToolStripButton saveAsSvgButton = new ToolStripButton
            {
                Text = "Save as SVG",
                DisplayStyle = ToolStripItemDisplayStyle.ImageAndText,
                TextAlign = ContentAlignment.MiddleCenter,
                BackColor = Color.FromArgb(245, 245, 245),
                AutoSize = false,
                Width = 80,
                Height = 24,
                AutoToolTip = false,
            };

            // Handle the Paint event
            saveAsSvgButton.Paint += (sender, e) =>
            {
                // Draw the rounded rectangle (border)
                using (GraphicsPath path = GetRoundedRectangle(0, 0, saveAsSvgButton.Width - 1, saveAsSvgButton.Height - 1, 8))
                using (Pen borderPen = new Pen(Color.Gray, 1))
                {
                    e.Graphics.DrawPath(borderPen, path);
                }

            };

            saveAsSvgButton.Click += (sender, e) =>
            {
                using (SaveFileDialog saveFileDialog = new SaveFileDialog())
                {
                    saveFileDialog.Filter = "SVG files (*.svg)|*.svg";
                    saveFileDialog.FilterIndex = 1;
                    saveFileDialog.RestoreDirectory = true;

                    if (saveFileDialog.ShowDialog() == DialogResult.OK)
                    {
                        filename = saveFileDialog.FileName;
                        SvgWriter svg = new SvgWriter();
                        SvgUtils.AddSolution(svg, svgpath, color, thickness, false);
                        SvgUtils.SaveToFile(svg, filename, FillRule.EvenOdd, svgWidth, svgHeight, 20);
                    }
                }
            };

            menu.Items.Add(saveAsSvgButton);
            #endregion

            #region BIN Save Button
            Menu_AppendSeparator(menu);

            ToolStripButton saveAsBinButton = new ToolStripButton
            {
                Text = "Save as BIN",
                DisplayStyle = ToolStripItemDisplayStyle.ImageAndText,
                TextAlign = ContentAlignment.MiddleCenter,
                BackColor = Color.FromArgb(245, 245, 245),
                AutoSize = false,
                Width = 80,
                Height = 24,
                AutoToolTip = false,
            };

            // Handle the Paint event
            saveAsBinButton.Paint += (sender, e) =>
            {
                // Draw the rounded rectangle (border)
                using (GraphicsPath path = GetRoundedRectangle(0, 0, saveAsBinButton.Width - 1, saveAsBinButton.Height - 1, 8))
                using (Pen borderPen = new Pen(Color.Gray, 1))
                {
                    e.Graphics.DrawPath(borderPen, path);
                }

            };

            saveAsBinButton.Click += SaveFile;
            menu.Items.Add(saveAsBinButton);
            #endregion

        }

        private GraphicsPath GetRoundedRectangle(int x, int y, int width, int height, int radius)
        {
            GraphicsPath path = new GraphicsPath();

            path.AddArc(x, y, radius * 2, radius * 2, 180, 90); // Top-left corner
            path.AddArc(width - 2 * radius, y, radius * 2, radius * 2, 270, 90); // Top-right corner
            path.AddArc(width - 2 * radius, height - 2 * radius, radius * 2, radius * 2, 0, 90); // Bottom-right corner
            path.AddArc(x, height - 2 * radius, radius * 2, radius * 2, 90, 90); // Bottom-left corner
            path.CloseFigure();

            return path;
        }


        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
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
            paramInteger1.AddNamedValue("Bevel", 2);
            paramInteger1.AddNamedValue("Round", 3);

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

        PathsD pp = new PathsD();
        PathsD svgpath = new PathsD();
        protected override void BeforeSolveInstance()
        {
            pp.Clear();
            svgpath = new PathsD();
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
            jointtype %= 4;

            ClipperOffsetPoly(curves, distance * 2, jointtype, endtype, miter);

            DA.SetDataList(0, holeCurves);
            DA.SetDataList(1, outCurves);
        }

        List<Curve> holeCurves = new List<Curve>();
        List<Curve> outCurves = new List<Curve>();

        void ClipperOffsetPoly(List<Curve> curves, double distance, int jointype, int endtype, double miterLimit)
        {
            holeCurves.Clear();
            outCurves.Clear();

            //PathsD pathsD = Converter.ConvertPolylinesA1(curves);

            var result = Converter.ConvertPolylinesA2(curves);
            PathsD closedPaths = result.closedPathsD;
            PathsD openedPaths = result.openedPathsD;

            //offset closed curves
            PathsD solutionDA = new PathsD();
            solutionDA = Clipper.InflatePaths(closedPaths, distance, JoinTypes[jointype], EndTypes[1], miterLimit, precision);

            // offset opened curves
            PathsD solutionDB = new PathsD();
            solutionDB = Clipper.InflatePaths(openedPaths, distance, JoinTypes[jointype], EndTypes[endtype], miterLimit, precision);

            PathsD solutionD = Clipper.Union(solutionDA, solutionDB, FillRule.EvenOdd, precision);

            foreach (PathD path in solutionD)
            {
                PathD newPath = new PathD();

                foreach (PointD point in path)
                {
                    newPath.Add(new PointD(point.x, -point.y));
                }

                Polyline polyline = new Polyline(path.Select(p => new Point3d(p.x, p.y, 0)));
                polyline.Add(polyline[0]);
                if (Clipper.IsPositive(path))
                    outCurves.Add(polyline.ToNurbsCurve());
                else
                    holeCurves.Add(polyline.ToNurbsCurve());

                pp.Add(path);
                svgpath.Add(newPath);
            }
        }

        void SaveFile(object sender, System.EventArgs e)
        {
            using (SaveFileDialog saveFileDialog = new SaveFileDialog())
            {
                saveFileDialog.Filter = "BIN files (*.bin)|*.bin";
                saveFileDialog.FilterIndex = 1;
                saveFileDialog.RestoreDirectory = true;

                if (saveFileDialog.ShowDialog() == DialogResult.OK)
                {
                    filename = saveFileDialog.FileName;
                    Clipper2Lib.ClipperFileIO.SaveToBinFile(filename, pp);
                }
            }
        }

        List<JoinType> JoinTypes = new List<JoinType>()
        {
            JoinType.Miter,
            JoinType.Square,
            JoinType.Bevel,
            JoinType.Round
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

        public override Guid ComponentGuid => new Guid("ECC627E4-F10E-4013-81D9-8278C4D5F8E1");

        public override bool Read(GH_IReader reader)
        {
            if (reader.ItemExists("Precision"))
                precision = reader.GetInt32("Precision");

            if (reader.ItemExists("SVGWidth"))
                svgWidth = reader.GetInt32("SVGWidth");

            if (reader.ItemExists("SVGHeight"))
                svgHeight = reader.GetInt32("SVGHeight");

            return base.Read(reader);
        }

        public override bool Write(GH_IWriter writer)
        {
            writer.SetInt32("Precision", precision);
            writer.SetInt32("SVGWidth", svgWidth);
            writer.SetInt32("SVGHeight", svgHeight);

            return base.Write(writer);
        }
    }
}