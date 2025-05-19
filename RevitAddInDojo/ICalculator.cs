using Autodesk.Revit.DB;

namespace RevitAddInDojo
{
    public interface ICalculator
    {
        void Calculate(FamilyInstance familyInstance);
    }
}
