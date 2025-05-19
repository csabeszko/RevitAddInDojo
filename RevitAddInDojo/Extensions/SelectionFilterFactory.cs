using System;
using Autodesk.Revit.DB;
using RevitAddInDojo.Extensions.Filtering;

namespace RevitAddInDojo.Extensions
{
    public static class SelectionFilterFactory
    {
        public static ElementSelectionFilter CreateElementSelectionFilter(Func<Element, bool> validateElement)
        {
            return new ElementSelectionFilter(validateElement);
        }

        public static LinkableSelectionFilter CreateLinkableSelectionFilter(Document doc, Func<Element, bool> validateElement)
        {
            return new LinkableSelectionFilter(doc, validateElement);
        }
    }
}
