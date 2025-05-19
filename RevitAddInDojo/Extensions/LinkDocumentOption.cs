using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;

namespace RevitAddInDojo.Extensions
{
    public class LinkDocumentOption : IPickElementsOption
    {
        public List<Element> PickElements(UIDocument uiDocument, Func<Element, bool> validateElement)
        {
            var doc = uiDocument.Document;
            var references = uiDocument.Selection.PickObjects(ObjectType.Element,
                SelectionFilterFactory.CreateLinkableSelectionFilter(doc, validateElement));
            var elements = references
                .Select(r => (doc.GetElement(r.ElementId) as RevitLinkInstance)
                .GetLinkDocument().GetElement(r.LinkedElementId))
                .ToList();

            throw new NotImplementedException();
        }
    }
}
