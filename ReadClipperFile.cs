using Clipper2Lib;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;
using Rhino.Geometry;
using System;
using System.IO;

namespace ClipperTwo
{
    public class ClipperFileComponent : GH_Component
    {
        public ClipperFileComponent()
            : base("Clipper2 Read BIN File", "ReadBin",
                "",
                "Curve", "Clipper2")
        {
        }

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddTextParameter("File", "", "BIN file", GH_ParamAccess.item);
            pManager[0].Optional = true;
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddPointParameter("Points", "", "", GH_ParamAccess.tree);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            string file = null;

            if (!DA.GetData(0, ref file)) return;

            LoadPathsFromResource(file);

            DA.SetDataTree(0, outPointsTree);
        }

        GH_Structure<GH_Point> outPointsTree = new GH_Structure<GH_Point>();

        void LoadPathsFromResource(string filePath)
        {
            GH_Structure<GH_Point> newPointsTree = new GH_Structure<GH_Point>();

            try
            {
                using (FileStream fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
                {
                    using (BinaryReader reader = new BinaryReader(fileStream))
                    {
                        int len = reader.ReadInt32();
                        //PathsD result = new PathsD(len);

                        for (int i = 0; i < len; i++)
                        {
                            int len2 = reader.ReadInt32();
                            PathD p = new PathD(len2);

                            for (int j = 0; j < len2; j++)
                            {
                                double X = reader.ReadDouble();
                                double Y = reader.ReadDouble();
                                p.Add(new PointD(X, Y));
                                newPointsTree.Append(new GH_Point(new Point3d(X, Y, 0)), new GH_Path(i));
                            }

                            // Only add the path if it's not empty
                            //if (p.Count > 0)
                            //{
                            //result.Add(p);
                            //}
                        }
                    }
                }
            }
            catch
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, $"File not found.");
            }

            // Assign the new structure to the original
            outPointsTree = newPointsTree;
        }


        protected override System.Drawing.Bitmap Icon => Properties.Resources.readf;

        public override Guid ComponentGuid => new Guid("5574A803-0A10-40B1-BE49-9C282168163C");
    }
}
