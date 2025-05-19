using Autodesk.Revit.Attributes;
using Autodesk.Revit.Creation;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using RevitAddInDojo.Extensions;
using RevitAddInDojo.Extensions.Filtering;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Xml.Linq;
using Document = Autodesk.Revit.DB.Document;

namespace RevitAddInDojo
{
    [Transaction(TransactionMode.Manual)]
    public class Dojo : IExternalCommand
    {
        private const int MaxAntennaDistanceFromPipe = 5;

        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            var uiApplication = commandData.Application;
            var uiDocument = uiApplication.ActiveUIDocument;
            var application = uiApplication.Application;
            var document = uiDocument.Document;

            var references = uiDocument.Selection.PickObjects(ObjectType.Element,
                new ElementSelectionFilter(e => 
                e.Category.Id == new ElementId(BuiltInCategory.OST_CommunicationDevices) || 
                e.Category.Id == new ElementId(BuiltInCategory.OST_StructuralFraming)));
            
            List<Element> selectedElements = references
                                                 .Select(r => document.GetElement(r))
                                                 .Where(e => e != null)
                                                 .ToList();

            if (!selectedElements.Any())
            {
                return Result.Cancelled;
            }

            using (var transaction = new Transaction(document, "Visualization of antenna's lines"))
            {
                transaction.Start();

                var antennas = selectedElements
                    .Where(x => x.Category.Id == new ElementId(BuiltInCategory.OST_CommunicationDevices) && x is FamilyInstance)
                    .Cast<FamilyInstance>();

                var centerPoint = GetCenterPointOfBoundingBox(antennas, document);

                SetAntennasFacingSameDirection(antennas, centerPoint, document);

                SetAntennasSide(selectedElements, antennas, document);

                transaction.Commit();
            }

            return Result.Succeeded;
        }

        private void SetAntennasSide(IEnumerable<Element> selectedElements, IEnumerable<FamilyInstance> antennas, Document document)
        {
            var verticalPipes = selectedElements
                    .Where(x => x.Category.Id == new ElementId(BuiltInCategory.OST_StructuralFraming) && x is FamilyInstance)
                    .Cast<FamilyInstance>();

            foreach (var verticalPipe in verticalPipes)
            {
                var closestAntennas = antennas.Where(x => x.GetTransform().Origin.DistanceTo(verticalPipe.GetTransform().Origin) < MaxAntennaDistanceFromPipe);

                var pipeTransform = verticalPipe.GetTransform();

                //This method is using the closest antennas' regression line so it does not matter the antenna facing inwards or outwards
                XYZ regressionLineVector = GetRegressionLineVectorOfAntennas(closestAntennas);
                regressionLineVector.AsCurveUnbound(closestAntennas.FirstOrDefault().GetTransform().Origin, 100).Visualize(document);

                //This logic is based on Antennas' facing vector so whenever an antenna is facing opposite then it could fail
                //var facingVectorOfAntennas = closestAntennas.Select(x => x.FacingOrientation);
                //XYZ sumVectorsOfAntennas = XYZ.Zero;

                //foreach (var facingVectorOfAntenna in facingVectorOfAntennas)
                //{
                //    sumVectorsOfAntennas += facingVectorOfAntenna;
                //}

                //var averageFacingVectorOfAntennas = (sumVectorsOfAntennas / facingVectorOfAntennas.Count()).Normalize();
                //averageFacingVectorOfAntennas.AsCurve(pipeTransform.Origin, 100).Visualize(document);

                //var perpendicularVector = new XYZ(-averageFacingVectorOfAntennas.Y, averageFacingVectorOfAntennas.X, averageFacingVectorOfAntennas.Z);
                //perpendicularVector.AsCurve(pipeTransform.Origin, 100).Visualize(document);

                foreach (var antenna in closestAntennas)
                {
                    var antennaTransform = antenna.GetTransform();
                    var antennaVector = (antennaTransform.Origin - pipeTransform.Origin).Normalize();

                    var dotProduct = regressionLineVector.DotProduct(antennaVector);

                    var esdtPositionParameter = antenna.get_Parameter(new Guid("801dff70-3e3e-4df5-a52e-1c8e28b2fc7b"));

                    var original = esdtPositionParameter.AsString();

                    if (dotProduct > 0)
                    {
                        esdtPositionParameter.Set("RIGHT" + original);
                    }
                    else
                    {
                        esdtPositionParameter.Set("LEFT" + original);
                    }
                }
            }
        }

        private XYZ GetRegressionLineVectorOfAntennas(IEnumerable<FamilyInstance> closestAntennas)
        {
            var origins = closestAntennas.Select(x => x.GetTransform().Origin).ToList();

            // 1. Compute the centroid (geometric center) of all origin points
            double cx = origins.Average(p => p.X);
            double cy = origins.Average(p => p.Y);
            double cz = origins.Average(p => p.Z);
            XYZ centroid = new XYZ(cx, cy, cz);

            // 2. Initialize accumulators for the 3×3 covariance (scatter) matrix
            double xx = 0, xy = 0, xz = 0, yy = 0, yz = 0, zz = 0;

            // 3. Accumulate sums of products (d_i * d_j) for each point’s offset from the centroid
            foreach (var p in origins)
            {
                XYZ d = p - centroid;
                xx += d.X * d.X;
                xy += d.X * d.Y;
                xz += d.X * d.Z;
                yy += d.Y * d.Y;
                yz += d.Y * d.Z;
                zz += d.Z * d.Z;
            }

            // 4. Assemble the symmetric covariance matrix C
            double[,] C = {
                                { xx, xy, xz },
                                { xy, yy, yz },
                                { xz, yz, zz }
                              };

            // 5. Initialize an arbitrary unit vector b for power‐iteration
            XYZ b = new XYZ(1, 1, 1).Normalize();

            // 6. Power‐iteration loop to approximate the principal eigenvector of C
            for (int i = 0; i < 50; i++)
            {
                // Multiply C by the current guess b, then normalize to keep it unit‐length
                var nb = new XYZ(
                    C[0, 0] * b.X + C[0, 1] * b.Y + C[0, 2] * b.Z,
                    C[1, 0] * b.X + C[1, 1] * b.Y + C[1, 2] * b.Z,
                    C[2, 0] * b.X + C[2, 1] * b.Y + C[2, 2] * b.Z
                ).Normalize();

                b = nb; // update b for the next iteration
            }

            return b;
        }

        private XYZ GetCenterPointOfBoundingBox(IEnumerable<FamilyInstance> antennas, Document document)
        {
            var origins = antennas
                            .Select(fi => ((LocationPoint)fi.Location).Point)
                            .ToList();

            double minX = origins.Min(p => p.X);
            double minY = origins.Min(p => p.Y);
            double minZ = origins.Min(p => p.Z);

            double maxX = origins.Max(p => p.X);
            double maxY = origins.Max(p => p.Y);
            double maxZ = origins.Max(p => p.Z);

            var centerPoint = new XYZ((minX + maxX) * 0.5, (minY + maxY) * 0.5, (minZ + maxZ) * 0.5);

            centerPoint.VisualizeAsPoint(document);

            return centerPoint;
        }

        private void SetAntennasFacingSameDirection(IEnumerable<FamilyInstance> antennas, XYZ center, Document document)
        {
            foreach (var antenna in antennas) 
            {
                var antennaCenterVector = (antenna.GetTransform().Origin - center);
                var dotProduct = antenna.FacingOrientation.DotProduct(antennaCenterVector);
                var esdtPositionParameter = antenna.get_Parameter(new Guid("801dff70-3e3e-4df5-a52e-1c8e28b2fc7b"));
                esdtPositionParameter.Set("");

                if (dotProduct > 0)
                {
                    esdtPositionParameter.Set("OUT");
                }
                else
                {
                    esdtPositionParameter.Set("IN");
                }
            }
        }


        private void GetSideBasedOnProjectOrigo(IEnumerable<FamilyInstance> antennas, IEnumerable<FamilyInstance> verticalPipes, Document document)
        {
            foreach (var verticalPipe in verticalPipes)
            {
                var closestAntennas = antennas.Where(x => x.GetTransform().Origin.DistanceTo(verticalPipe.GetTransform().Origin) < MaxAntennaDistanceFromPipe);

                var pipeTransform = verticalPipe.GetTransform();
                var pipeVector = (pipeTransform.Origin - XYZ.Zero).Normalize();
                var perpendicularVectorOfPipeVector = new XYZ(-pipeVector.Y, pipeVector.X, pipeVector.Z);

                pipeVector
                    .AsCurve(XYZ.Zero, 100)
                    .Visualize(document);

                perpendicularVectorOfPipeVector
                    .AsCurve(XYZ.Zero, 100)
                    .Visualize(document);


                foreach (var closestAntenna in closestAntennas)
                {
                    var closestAntennaTransform = closestAntenna.GetTransform();
                    var closestAntennaVector = (closestAntennaTransform.Origin - XYZ.Zero).Normalize();
                    var dotProductOfClosestAntennaVector = perpendicularVectorOfPipeVector.DotProduct(closestAntennaVector);

                    var esdtPositionParameter = closestAntenna.get_Parameter(new Guid("801dff70-3e3e-4df5-a52e-1c8e28b2fc7b"));

                    if (dotProductOfClosestAntennaVector > 0)
                    {
                        esdtPositionParameter.Set("RIGHT");
                    }
                    else
                    {
                        esdtPositionParameter.Set("LEFT");
                    }

                    closestAntennaVector
                        .AsCurve(XYZ.Zero, 100)
                        .AddText(document, dotProductOfClosestAntennaVector >= 0 ? "RIGHT" : "LEFT", closestAntennaVector)
                        .Visualize(document);

                }
            }
        }

        private void GetSideBasedOnAverageVectorOfAntennasBasisY(List<Element> selectedElements)
        {
            var pipe = selectedElements.FirstOrDefault(x => x is FamilyInstance && x.Category.BuiltInCategory == BuiltInCategory.OST_StructuralFraming) as FamilyInstance;
            var antenna1 = selectedElements.FirstOrDefault(x => x is FamilyInstance && x.Category.BuiltInCategory == BuiltInCategory.OST_CommunicationDevices) as FamilyInstance;
            var antenna2 = selectedElements.LastOrDefault(x => x is FamilyInstance && x.Category.BuiltInCategory == BuiltInCategory.OST_CommunicationDevices) as FamilyInstance;

            var pipeTransform = pipe.GetTransform();
            var antenna1Transform = antenna1.GetTransform();
            var antenna2Transform = antenna2.GetTransform();

            XYZ originOfPipe = (pipe.Location as LocationPoint)?.Point ?? pipe.GetTransform()?.Origin;

            XYZ averageVectorOfAntennas = (antenna1Transform.BasisY + antenna2Transform.BasisY).Normalize();

            XYZ perpendicularVectorForAverageVectorOfAntennas = new XYZ(-averageVectorOfAntennas.Y, averageVectorOfAntennas.X, averageVectorOfAntennas.Z);

            var antenna1DotProduct = perpendicularVectorForAverageVectorOfAntennas.DotProduct(antenna1Transform.BasisY) >= 0 ? "RIGHT" : "LEFT";
            var antenna2DotProduct = perpendicularVectorForAverageVectorOfAntennas.DotProduct(antenna2Transform.BasisY) >= 0 ? "RIGHT" : "LEFT";
        }

        private void CreateSomeLines()
        {
            /*
                    var antennaTransform = antenna.GetTransform();

                    //This is the X vector of antenna
                   var antennaBasisX = new XYZ(antennaTransform.BasisX.X, antennaTransform.BasisX.Y, antennaTransform.BasisX.Z);
                    antennaBasisX
                        .AsCurve(antennaBasisX, 100)
                        .AddText(document, "Antenna's Basis X", antennaBasisX)
                        .Visualize(document);
                    //This is the X vector of antenna lying on XY coordinate system(without Z)
                    var antennaBasisXWithoutZ = new XYZ(antennaTransform.BasisX.X, antennaTransform.BasisX.Y, 0);
                    antennaBasisXWithoutZ
                        .AsCurve(antennaBasisXWithoutZ, 100)
                        .AddText(document, "Antenna's Basis X without Z height", antennaBasisXWithoutZ)
                        .Visualize(document)
                        ;

                    var basisZ = XYZ.BasisZ;

                    basisZ.AsCurve(basisZ, 100).Visualize(document);

                    var dotProduct = basisZ.DotProduct(antennaBasisX);

                    double angle = antennaBasisX.AngleTo(antennaBasisXWithoutZ) * (180 / Math.PI);

                    if (dotProduct > 0)
                    {
                        angle *= -1;
                    }

                    TaskDialog.Show("Angle of Xs", $"{angle}°; dotProduct: {dotProduct}");

                   // //This is the Y vector of antenna
                   //var antennaBasisY = new XYZ(antennaTransform.BasisY.X, antennaTransform.BasisY.Y, antennaTransform.BasisY.Z);
                   // antennaBasisY
                   //     .AsCurve(antennaBasisY, 100)
                   //     .AddText(document, "Antenna's Basis Y", antennaBasisY)
                   //     .Visualize(document);
                   // //This is the Y vector of antenna lying on XY coordinate system(without Z)
                   // var antennaBasisYWithoutZ = new XYZ(antennaTransform.BasisY.X, antennaTransform.BasisY.Y, 0);
                   // antennaBasisYWithoutZ
                   //     .AsCurve(antennaBasisYWithoutZ, 100)
                   //     .AddText(document, "Antenna's Basis Y without Z height", antennaBasisYWithoutZ)
                   //     .Visualize(document)
                   //     ;

                   // TaskDialog.Show("Angle of Ys", $"{antennaBasisY.AngleTo(antennaBasisYWithoutZ) * (180 / Math.PI)}°");


                   // //This is the Z vector of antenna
                   //var antennaBasisZ = new XYZ(antennaTransform.BasisZ.X, antennaTransform.BasisZ.Y, antennaTransform.BasisZ.Z);
                   // antennaBasisZ
                   //     .AsCurve(antennaBasisY, 100)
                   //     .AddText(document, "Antenna's Basis Z", antennaBasisZ)
                   //     .Visualize(document);
                   // //This is the Z vector of antenna lying on XY coordinate system(without Z)
                   // var antennaBasisZWithoutZ = new XYZ(antennaTransform.BasisZ.X, antennaTransform.BasisZ.Y, 0);
                   // antennaBasisZWithoutZ
                   //     .AsCurve(antennaBasisZWithoutZ, 100)
                   //     .AddText(document, "Antenna's Basis Z without Z height", antennaBasisZWithoutZ)
                   //     .Visualize(document)
                   //     ;

                   // TaskDialog.Show("Angle of Zs", $"{antennaBasisZ.AngleTo(antennaBasisZWithoutZ) * (180 / Math.PI)}°");

             */
        }
    }
}
