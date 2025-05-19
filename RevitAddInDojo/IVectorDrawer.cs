using Autodesk.Revit.DB;


namespace RevitAddInDojo
{
    public interface IVectorDrawer
    {
        void Draw(Document document, FamilyInstance familyInstance);
    }
}
