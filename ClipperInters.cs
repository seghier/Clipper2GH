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

    public class ClipperIntersComponent : GH_Component
    {
        public ClipperIntersComponent()
          : base("Clipper2 Boolean", "C2Bool",
              "",
              "Curve", "Clipper2")
        {
        }

        public int rule = 3;
        int precision = 4;

        ToolStripMenuItem ruleA;
        ToolStripMenuItem ruleB;
        ToolStripMenuItem ruleC;
        ToolStripMenuItem ruleD;

        public override void AppendAdditionalMenuItems(ToolStripDropDown menu)
        {
            base.AppendAdditionalMenuItems(menu);
            Menu_AppendSeparator(menu);
            ruleA = Menu_AppendItem(menu, "EvenOdd", EvenOddRule, true, false);
            ruleB = Menu_AppendItem(menu, "Positive", PositiveRule, true, false);
            ruleC = Menu_AppendItem(menu, "Negative", NegativeRule, true, false);
            ruleD = Menu_AppendItem(menu, "NonZero", NonZeroRule, true, false);

            ruleA.Checked = (rule == 0);
            ruleB.Checked = (rule == 1);
            ruleC.Checked = (rule == 2);
            ruleD.Checked = (rule == 3);

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
            pManager.AddCurveParameter("PolylineA", "A", "", GH_ParamAccess.item);
            pManager.AddCurveParameter("PolylineB", "B", "", GH_ParamAccess.item);
            pManager.AddIntegerParameter("Operation", "O", "", GH_ParamAccess.item, 0);
            pManager[0].Optional = true;
            pManager[1].Optional = true;

            if (!(pManager[2] is Param_Integer paramInteger1))
                return;
            paramInteger1.AddNamedValue("Union", 0);
            paramInteger1.AddNamedValue("Difference", 1);
            paramInteger1.AddNamedValue("Intersection", 2);
            paramInteger1.AddNamedValue("Xor", 3);
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddGenericParameter("Result", "r", "Result", GH_ParamAccess.list);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            Curve curveA = null;
            Curve curveB = null;
            int choice = 0;
            Message = "";

            if (!DA.GetData(0, ref curveA)) return;
            if (!curveA.IsPolyline() && !curveB.IsPolyline())
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, $"Choose a valid polyline");
                return;
            }
            if (!DA.GetData(1, ref curveB)) return;
            if (!DA.GetData(2, ref choice)) return;

            choice %= 4;

            Message = clipTypes[choice].ToString();

            BooleanOp(curveA, curveB, choice, rule);

            DA.SetDataList(0, resultCurve);
        }

        List<Curve> resultCurve = new List<Curve>();

        void BooleanOp(Curve curveA, Curve curveB, int cliptype, int rule)
        {
            resultCurve.Clear();
            PathsD boolean;

            PathsD subj = Converter.ConvertPolylinesB(curveA);
            PathsD clip = Converter.ConvertPolylinesB(curveB);

            boolean = Clipper.BooleanOp(clipTypes[cliptype], subj, clip, fillRules[rule], precision);
            foreach (var path in boolean)
            {
                Polyline polyline = new Polyline(path.Select(p => new Point3d(p.x, p.y, 0)));
                polyline.Add(polyline[0]);
                resultCurve.Add(polyline.ToNurbsCurve());
            }
        }

        List<ClipType> clipTypes = new List<ClipType>()
        {
            ClipType.Union,
            ClipType.Difference,
            ClipType.Intersection,
            ClipType.Xor,
        };

        List<FillRule> fillRules = new List<FillRule>()
        {
            FillRule.EvenOdd,
            FillRule.Positive,
            FillRule.Negative,
            FillRule.NonZero,
        };

        protected override System.Drawing.Bitmap Icon => Properties.Resources._bool;

        //public override GH_Exposure Exposure => GH_Exposure.senary;

        public override Guid ComponentGuid => new Guid("1D96A8BC-7A39-4752-8418-38771FCD281C");

        public void EvenOddRule(Object sender, EventArgs e)
        {
            rule = 0;
            UpdateMenuCheckedState();
            ExpireSolution(true);
        }
        public void PositiveRule(Object sender, EventArgs e)
        {
            rule = 1;
            UpdateMenuCheckedState();
            ExpireSolution(true);
        }
        public void NegativeRule(Object sender, EventArgs e)
        {
            rule = 2;
            UpdateMenuCheckedState();
            ExpireSolution(true);
        }
        public void NonZeroRule(Object sender, EventArgs e)
        {
            rule = 3;
            UpdateMenuCheckedState();
            ExpireSolution(true);
        }

        private void UpdateMenuCheckedState()
        {
            ruleA.Checked = (rule == 0);
            ruleB.Checked = (rule == 1);
            ruleC.Checked = (rule == 2);
            ruleD.Checked = (rule == 3);
        }

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