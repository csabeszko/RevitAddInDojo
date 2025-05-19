using System.Collections.Generic;
using Autodesk.Revit.DB;

namespace RevitAddInDojo.Extensions
{
    public static class CurveExtensions
    {
        public static void Visualize(this GeometryObject curve, Document document)
        {
            document.CreateDirectShape(new List<GeometryObject> { curve});
        }
    }
}
