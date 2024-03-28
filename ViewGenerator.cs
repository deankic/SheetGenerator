using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;


namespace DRT
{
    public class ViewGenerator
    {
        Autodesk.Revit.UI.UIDocument uidoc;
        public Autodesk.Revit.DB.Document doc;
        string m_errorInformation;
        BoundingBoxXYZ elementBoundingBox;
        Autodesk.Revit.DB.Element m_currentComponent;

        public enum ViewTypeGenerate
        {
            PLAN = 0,
            ELEVATION = 1,
            LEFT = 2,
            RIGHT = 3,
            PLANSECTION = 6,
            ELEVATIONSECTION = 5,
            LEFTSECTION = 7,
            RIGHTSECTION = 8,
            REFLECTIVECEILINGPLAN = 4
        }

        public ViewSection CreateView(Document doc, BoundingBoxXYZ elementBoundingBox, ViewTypeGenerate viewDirection)
        {
            // Create a section view. 
            Transaction transaction = new Transaction(doc, "CreateSectionView");
            transaction.Start();

            XYZ min = elementBoundingBox.Min;
            XYZ max = elementBoundingBox.Max;

            BoundingBoxXYZ sectionBox = GenerateTransform(viewDirection);


            ElementId DetailViewId = ElementId.InvalidElementId;
            IList<Element> elems = new FilteredElementCollector(doc).OfClass(typeof(ViewFamilyType)).ToElements();
            foreach (Element e in elems)
            {
                ViewFamilyType v = e as ViewFamilyType;

                if (v != null && v.ViewFamily == ViewFamily.Section)
                {
                    DetailViewId = e.Id;
                    break;
                }
            }

            // Create the section view
            ViewSection viewSection = ViewSection.CreateSection(doc, DetailViewId, sectionBox);


            transaction.Commit();

            //sectionBox.Visualize(doc);



            return viewSection;
        }
        public BoundingBoxXYZ GenerateTransform(ViewTypeGenerate viewType)
        {

            XYZ min = elementBoundingBox.Min;
            XYZ max = elementBoundingBox.Max;

            Transform transform = null;
            FamilyInstance instance = m_currentComponent as FamilyInstance;
            transform = Transform.Identity;


            // Calculate the center point and dimensions of the bounding box
            XYZ center = (min + max) / 2;
            double width = elementBoundingBox.Max.X - elementBoundingBox.Min.X;
            double depth = elementBoundingBox.Max.Y - elementBoundingBox.Min.Y;
            double height = elementBoundingBox.Max.Z - elementBoundingBox.Min.Z;

            BoundingBoxXYZ sectionBox = new BoundingBoxXYZ();

            double PlanWidth;
            double PlanHeight;
            double PlanDepth;

            // Half dimensions for calculations
            double halfWidth = width / 2.0;
            double halfDepth = depth / 2.0;
            double halfheight = height / 2.0;

            double padding = 1;


            switch (viewType)
            {
                case ViewTypeGenerate.PLAN:
                    transform.Origin = center;
                    transform = GetRotatedViewTransform(center, 180, XYZ.BasisX);

                    //transform.Visualize(doc)

                    // change name acourding to direction 
                    PlanWidth = halfWidth + padding;
                    PlanHeight = halfDepth + padding;
                    PlanDepth = halfheight;


                    sectionBox.Min = new XYZ(-PlanWidth, -PlanHeight, -PlanDepth);
                    sectionBox.Max = new XYZ(PlanWidth, PlanHeight, PlanDepth);

                    break;
                case ViewTypeGenerate.ELEVATION:
                    transform.Origin = new XYZ(center.X, elementBoundingBox.Min.Y, center.Z);
                    transform = GetRotatedViewTransform(center, 270, XYZ.BasisX, 180, XYZ.BasisZ);
                    //transform.Visualize(doc);
                    //new boundingbox beacasue of rotation
                    // width height depth

                    // change name acourding to direction 
                    PlanWidth = halfWidth + padding;
                    PlanHeight = halfheight + padding;
                    PlanDepth = halfDepth;


                    sectionBox.Min = new XYZ(-PlanWidth, -PlanHeight, -PlanDepth);
                    sectionBox.Max = new XYZ(PlanWidth, PlanHeight, PlanDepth);

                    break;
                case ViewTypeGenerate.LEFT:
                    transform.Origin = new XYZ(elementBoundingBox.Min.X, center.Y, center.Z);
                    transform = GetRotatedViewTransform(center, -90, XYZ.BasisX, 90, XYZ.BasisY, 180, XYZ.BasisZ);

                    // change name acourding to direction 



                    PlanWidth = halfDepth + padding;
                    PlanHeight = halfheight + padding;
                    PlanDepth = halfWidth;

                    sectionBox.Min = new XYZ(-PlanWidth, -PlanHeight, -PlanDepth);
                    sectionBox.Max = new XYZ(PlanWidth, PlanHeight, PlanDepth);

                    break;


                case ViewTypeGenerate.RIGHT:
                    transform.Origin = new XYZ(elementBoundingBox.Max.X, center.Y, center.Z);
                    transform = GetRotatedViewTransform(center, -90, XYZ.BasisX, 270, XYZ.BasisY, 180, XYZ.BasisZ);
                    //transform.Visualize(doc);
                    //new boundingbox beacasue of rotation
                    // change name acourding to direction 

                    PlanWidth = halfDepth + padding;
                    PlanHeight = halfheight + padding;
                    PlanDepth = halfWidth;

                    sectionBox.Min = new XYZ(-PlanWidth, -PlanHeight, -PlanDepth);
                    sectionBox.Max = new XYZ(PlanWidth, PlanHeight, PlanDepth);

                    break;

                case ViewTypeGenerate.PLANSECTION:
                    // Assuming PLANSECTION is a top-down view that also slices through the element horizontally
                    double sectionOffset = height / 2 - 1; // Adjust this value based on where you want the section cut to occur

                    transform = GetRotatedViewTransform(center, 180, XYZ.BasisX);
                    //  width heigth dpeth


                    // Update the transform origin to the new center

                    // change name acourding to direction 
                    PlanWidth = halfWidth + padding;
                    PlanHeight = halfDepth + padding;
                    PlanDepth = halfheight;

                    transform.Origin = center - new XYZ(0, 0, PlanDepth);


                    sectionBox.Min = new XYZ(-PlanWidth, -PlanHeight, -PlanDepth);
                    sectionBox.Max = new XYZ(PlanWidth, PlanHeight, PlanDepth);


                    break;

                case ViewTypeGenerate.ELEVATIONSECTION:
                    // Position the camera in front of the element, looking horizontally with a vertical section cut
                    //double frontOffset = (depth / 2) +1 // Adjust based on desired section depth

                    transform = GetRotatedViewTransform(center, 270, XYZ.BasisX, 180, XYZ.BasisZ);
                    //transform.Visualize(doc);
                    // Create a new center point for the section box based on the desired offset


                    PlanWidth = halfWidth + padding;
                    PlanHeight = halfheight + padding;
                    PlanDepth = halfDepth;

                    // Update the transform origin to the new center
                    transform.Origin = center + new XYZ(0, PlanDepth, 0);



                    sectionBox.Min = new XYZ(-PlanWidth, -PlanHeight, -PlanDepth);
                    sectionBox.Max = new XYZ(PlanWidth, PlanHeight, PlanDepth);
                    break;
                // Assuming continuation from the previous switch statement
                case ViewTypeGenerate.LEFTSECTION:
                    // For LEFTSECTION, position the camera to the left of the element, looking right with a vertical section cut

                    transform = GetRotatedViewTransform(center, -90, XYZ.BasisX, 90, XYZ.BasisY, 180, XYZ.BasisZ);
                    //transform.Visualize(doc);

                    PlanWidth = halfDepth + padding;
                    PlanHeight = halfheight + padding;
                    PlanDepth = halfWidth;


                    transform.Origin = center + new XYZ(PlanDepth, 0, 0);


                    sectionBox.Min = new XYZ(-PlanWidth, -PlanHeight, -PlanDepth);
                    sectionBox.Max = new XYZ(PlanWidth, PlanHeight, PlanDepth);
                    break;

                case ViewTypeGenerate.RIGHTSECTION:
                    // For RIGHTSECTION, position the camera to the right of the element, looking left with a vertical section cut

                    transform = GetRotatedViewTransform(center, -90, XYZ.BasisX, 270, XYZ.BasisY, 180, XYZ.BasisZ);
                    //transform.Visualize(doc);


                    PlanWidth = halfDepth + padding;
                    PlanHeight = halfheight + padding;
                    PlanDepth = halfWidth;


                    transform.Origin = center + new XYZ(-PlanDepth, 0, 0);

                    sectionBox.Min = new XYZ(-PlanWidth, -PlanHeight, -PlanDepth);
                    sectionBox.Max = new XYZ(PlanWidth, PlanHeight, PlanDepth);
                    break;


                case ViewTypeGenerate.REFLECTIVECEILINGPLAN:
                    transform.Origin = new XYZ(center.X, center.Y, elementBoundingBox.Min.Z);

                    // change name acourding to direction 
                    PlanWidth = halfWidth + padding;
                    PlanHeight = halfDepth + padding;
                    PlanDepth = halfheight;


                    sectionBox.Min = new XYZ(-PlanWidth, -PlanHeight, -PlanDepth);
                    sectionBox.Max = new XYZ(PlanWidth, PlanHeight, PlanDepth);
                    break;

                default:
                    m_errorInformation = "Invalid view direction.";
                    return null;


            }


            try
            {

                sectionBox.Transform = transform;
                
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }



            return sectionBox;

        }

        public Transform GetRotatedViewTransform(XYZ center, double rotationAngleDegrees, XYZ rotationAxis)
        {
            // Create a rotation transform about the given axis by the specified angle
            Transform rotation = Transform.CreateRotation(rotationAxis, Math.PI * rotationAngleDegrees / 180.0);

            // Initialize the transform for the view with rotation applied
            Transform viewTransform = Transform.Identity;
            viewTransform.Origin = center;
            viewTransform.BasisX = rotation.OfVector(XYZ.BasisX);
            viewTransform.BasisY = rotation.OfVector(XYZ.BasisY);
            viewTransform.BasisZ = rotation.OfVector(XYZ.BasisZ);

            return viewTransform;
        }
        public Transform GetRotatedViewTransform(XYZ center, double firstRotationAngleDegrees, XYZ firstRotationAxis, double secondRotationAngleDegrees, XYZ secondRotationAxis)
        {
            // First rotation transform about the given axis by the specified angle
            Transform firstRotation = Transform.CreateRotation(firstRotationAxis, Math.PI * firstRotationAngleDegrees / 180.0);

            // Second rotation transform, applied after the first
            Transform secondRotation = Transform.CreateRotation(secondRotationAxis, Math.PI * secondRotationAngleDegrees / 180.0);

            // Combine the two rotations by multiplying their transforms
            Transform combinedRotation = firstRotation.Multiply(secondRotation);

            // Initialize the transform for the view with combined rotations applied
            Transform viewTransform = Transform.Identity;
            viewTransform.Origin = center; // Set the origin to the specified center
            viewTransform.BasisX = combinedRotation.OfVector(XYZ.BasisX);
            viewTransform.BasisY = combinedRotation.OfVector(XYZ.BasisY);
            viewTransform.BasisZ = combinedRotation.OfVector(XYZ.BasisZ);

            return viewTransform;
        }
        public Transform GetRotatedViewTransform(XYZ center, double firstRotationAngleDegrees, XYZ firstRotationAxis, double secondRotationAngleDegrees, XYZ secondRotationAxis, double thirdRotationAngleDegrees, XYZ thirdRotationAxis)
        {
            // First rotation transform about the given axis by the specified angle
            Transform firstRotation = Transform.CreateRotation(firstRotationAxis, Math.PI * firstRotationAngleDegrees / 180.0);

            // Second rotation transform, applied after the first
            Transform secondRotation = Transform.CreateRotation(secondRotationAxis, Math.PI * secondRotationAngleDegrees / 180.0);

            // Third rotation transform, applied after the second
            Transform thirdRotation = Transform.CreateRotation(thirdRotationAxis, Math.PI * thirdRotationAngleDegrees / 180.0);

            // Combine the first two rotations
            Transform combinedRotation = firstRotation.Multiply(secondRotation);

            // Then combine the result with the third rotation
            combinedRotation = combinedRotation.Multiply(thirdRotation);

            // Initialize the transform for the view with combined rotations applied
            Transform viewTransform = Transform.Identity;
            viewTransform.Origin = center; // Set the origin to the specified center
            viewTransform.BasisX = combinedRotation.OfVector(XYZ.BasisX);
            viewTransform.BasisY = combinedRotation.OfVector(XYZ.BasisY);
            viewTransform.BasisZ = combinedRotation.OfVector(XYZ.BasisZ);

            return viewTransform;
        }
        public static BoundingBoxXYZ GetBoundingBoxForElements(Document doc, ICollection<ElementId> elementIds)
        {
            // Initialize null bounding box to start with
            BoundingBoxXYZ combinedBox = null;

            foreach (ElementId eid in elementIds)
            {
                Element elem = doc.GetElement(eid);
                BoundingBoxXYZ elemBox = elem.get_BoundingBox(null); // null argument gets the model's bounding box

                if (elemBox != null)
                {
                    if (combinedBox == null)
                    {
                        // First element, set combinedBox to its bounding box
                        combinedBox = elemBox;
                    }
                    else
                    {
                        // Update the combinedBox to include the current element's bounding box
                        combinedBox.Min = new XYZ(Math.Min(combinedBox.Min.X, elemBox.Min.X),
                                                  Math.Min(combinedBox.Min.Y, elemBox.Min.Y),
                                                  Math.Min(combinedBox.Min.Z, elemBox.Min.Z));

                        combinedBox.Max = new XYZ(Math.Max(combinedBox.Max.X, elemBox.Max.X),
                                                  Math.Max(combinedBox.Max.Y, elemBox.Max.Y),
                                                  Math.Max(combinedBox.Max.Z, elemBox.Max.Z));
                    }
                }
            }

            return combinedBox;
        }

    }
}
