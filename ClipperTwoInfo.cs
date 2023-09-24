using Grasshopper.Kernel;
using System;
using System.Drawing;

namespace Clipper2
{
    public class ClipperTwoInfo : GH_AssemblyInfo
    {
        public override string Name => "Clipper2";

        //Return a 24x24 pixel bitmap to represent this GHA library.
        public override Bitmap Icon => null;

        //Return a short string describing the purpose of this GHA library.
        public override string Description => "";

        public override Guid Id => new Guid("EAA5F7D7-CD27-4D7E-8C81-CB823905250B");

        //Return a string identifying you or your company.
        public override string AuthorName => "";

        //Return a string representing your preferred contact details.
        public override string AuthorContact => "";
    }
}