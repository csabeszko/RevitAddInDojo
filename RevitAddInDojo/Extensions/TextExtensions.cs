using System.Linq;
using Autodesk.Revit.DB;

namespace RevitAddInDojo.Extensions
{
    public static class TextExtensions
    {
        public static GeometryObject AddText(this GeometryObject geometryObject, Document doc, string text, XYZ position)
        {
            TextNoteType textType = new FilteredElementCollector(doc)
                .OfClass(typeof(TextNoteType))
                .Cast<TextNoteType>()
                .FirstOrDefault();

            TextNoteOptions textNoteOptions = new TextNoteOptions(textType.Id)
            {
                HorizontalAlignment = HorizontalTextAlignment.Center
            };

            TextNote note = TextNote.Create(doc, doc.ActiveView.Id, position.Multiply(50), text, textNoteOptions);

            return geometryObject;
        }
    }
}
