using Autodesk.Revit.DB;
using RevitAddInDojo.Extensions;


namespace RevitAddInDojo
{
    public class PipeVectorDrawer : IVectorDrawer
    {
        public void Draw(Document document, FamilyInstance familyInstance)
        {
            if(familyInstance.Category.Id == new ElementId(BuiltInCategory.OST_StructuralFraming))
            {
                var pipeTransformX = familyInstance.GetTransform();

                pipeTransformX.BasisX.AsCurve(pipeTransformX.Origin, 100)
                            .AddText(document, "X", pipeTransformX.BasisX)
                            .Visualize(document);

                var pipeTransformY = familyInstance.GetTransform();
                pipeTransformY.BasisY.AsCurve(pipeTransformY.Origin, 100)
                            .AddText(document, "Y", pipeTransformY.BasisY)
                            .Visualize(document);

                var pipeTransformZ = familyInstance.GetTransform();
                pipeTransformZ.BasisZ.AsCurve(pipeTransformZ.Origin, 100)
                            .AddText(document, "Z", pipeTransformZ.BasisZ)
                            .Visualize(document);
            }
        }
    }

    public class AntennaVectorDrawer : IVectorDrawer
    {
        public void Draw(Document document, FamilyInstance familyInstance)
        {
            if (familyInstance.Category.Id == new ElementId(BuiltInCategory.OST_CommunicationDevices))
            {
                var antennaTransformX = familyInstance.GetTransform();

                antennaTransformX.BasisX.AsCurve(antennaTransformX.Origin, 100)
                            .AddText(document, "X", antennaTransformX.BasisX)
                            .Visualize(document);

                var antennaTransformY = familyInstance.GetTransform();
                antennaTransformY.BasisY.AsCurve(antennaTransformY.Origin, 100)
                            .AddText(document, "Y", antennaTransformY.BasisY)
                            .Visualize(document);

                var antennaTransformZ = familyInstance.GetTransform();
                antennaTransformZ.BasisZ.AsCurve(antennaTransformZ.Origin, 100)
                            .AddText(document, "Z", antennaTransformZ.BasisZ)
                            .Visualize(document);
            }
        }
    }
}
