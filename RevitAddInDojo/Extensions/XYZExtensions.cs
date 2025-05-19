using System;
using System.Collections.Generic;
using Autodesk.Revit.DB;

namespace RevitAddInDojo.Extensions
{
    public static class XYZExtensions
    {
        public static void VisualizeAsPoint(this XYZ point, Document doc)
        {
            doc.CreateDirectShape(new List<GeometryObject> { Point.Create(point) });
        }

        public static Line VisualizeAsLine(this XYZ vector, Document doc, XYZ origin = null)
        {
            origin ??= XYZ.Zero;
            var endPoint = origin + vector;
            var line = Line.CreateBound(origin, endPoint);
            doc.CreateDirectShape(new List<GeometryObject> { line });

            return line;
        }

        public static Curve AsCurve(this XYZ vector, XYZ origin = null, double? length = null)
        {
            origin ??= XYZ.Zero;
            length ??= vector.GetLength();
            return Line.CreateBound(origin, origin.MoveAlongVector(vector.Normalize(), length.GetValueOrDefault()));
        }

        public static Curve AsCurveUnbound(this XYZ vector, XYZ origin = null, double? length = null)
        {
            origin ??= XYZ.Zero;
            length ??= vector.GetLength();
            return Line.CreateBound(origin.MoveAlongVector(-vector.Normalize(), length.GetValueOrDefault()), origin.MoveAlongVector(vector.Normalize(), length.GetValueOrDefault()));
        }

        public static XYZ MoveAlongVector(this XYZ pointToMove, XYZ vector) => pointToMove.Add(vector);

        public static XYZ MoveAlongVector(this XYZ pointToMove, XYZ vector, double distance) => pointToMove.Add(vector * distance);

        public static XYZ ToNormalizeVector(this Curve curve) => (curve.GetEndPoint(1) - curve.GetEndPoint(0)).Normalize();

        public static XYZ ToVector(this XYZ fromPoint, XYZ toPoint) => toPoint - fromPoint;

        public static XYZ ToNormalizedVector(this XYZ fromPoint, XYZ toPoint) => (toPoint - fromPoint).Normalize();

        public static double DistanceToAlongVector(this XYZ fromPoint, XYZ toPoint, XYZ vectorToMeasureBy) => Math.Abs(fromPoint.ToVector(toPoint).DotProduct(vectorToMeasureBy));

    }
}
