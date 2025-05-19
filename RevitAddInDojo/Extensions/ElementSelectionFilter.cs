using System;
using Autodesk.Revit.DB;

namespace RevitAddInDojo.Extensions.Filtering
{
    public class ElementSelectionFilter : AbstractSelectionFilter
    {
        private readonly Func<Reference, bool> _validateReference;

        public ElementSelectionFilter(Func<Element, bool> validateElement) : base(validateElement) { }

        public ElementSelectionFilter(Func<Element, bool> validateElement, Func<Reference, bool> validateReference) : base(validateElement)
        {
            _validateReference = validateReference;
        }

        public override bool AllowElement(Element elem)
        {
            return ValidateElement(elem);
        }

        public override bool AllowReference(Reference reference, XYZ position)
        {
            return _validateReference?.Invoke(reference) ?? true;
        }
    }
}
