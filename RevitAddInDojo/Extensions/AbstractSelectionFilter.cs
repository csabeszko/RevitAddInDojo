using System;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI.Selection;

namespace RevitAddInDojo.Extensions.Filtering
{
    public abstract class AbstractSelectionFilter : ISelectionFilter
    {
        protected readonly Func<Element, bool> ValidateElement;

        protected AbstractSelectionFilter(Func<Element, bool> validateElement)
        {
            ValidateElement = validateElement;
        }

        public abstract bool AllowElement(Element elem);

        public abstract bool AllowReference(Reference reference, XYZ position);
    }
}
