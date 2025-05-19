using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System;


namespace RevitAddInDojo
{
    public class AntennaPlumbCalculator : ICalculator
    {
        public void Calculate(FamilyInstance familyInstance)
        {
            var antennaTransform = familyInstance.GetTransform();
            var antennaBasisX = new XYZ(antennaTransform.BasisX.X, antennaTransform.BasisX.Y, antennaTransform.BasisX.Z);
            var antennaBasisXWithoutZ = new XYZ(antennaTransform.BasisX.X, antennaTransform.BasisX.Y, 0);
            var basisZ = XYZ.BasisZ;
            var dotProduct = basisZ.DotProduct(antennaBasisX);
            double angle = antennaBasisX.AngleTo(antennaBasisXWithoutZ) * (180 / Math.PI);
            if (dotProduct > 0)
            {
                angle *= -1;
            }

            TaskDialog.Show("Angle", $"Angle: {angle}°; dotProduct: {dotProduct}");
        }
    }
}
