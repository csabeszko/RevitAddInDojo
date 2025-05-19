using System;
using Autodesk.Revit.DB;

namespace RevitAddInDojo.Extensions.Filtering
{
    public class LinkableSelectionFilter : AbstractSelectionFilter
    {
        private readonly Document _doc;

        public LinkableSelectionFilter(Document doc, Func<Element, bool> validateElement) : base(validateElement)
        {
            _doc = doc;
        }

        public override bool AllowElement(Element elem) => true;

        public override bool AllowReference(Reference reference, XYZ position)
        {
            if(!(_doc.GetElement(reference.ElementId) is RevitLinkInstance linkInstance))
            {
                return ValidateElement(_doc.GetElement(reference.ElementId));
            }

            var element = linkInstance.GetLinkDocument()
                .GetElement(reference.LinkedElementId);

            return ValidateElement(element);
        }
    }
}
