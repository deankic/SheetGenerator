using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using DRTRebar.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using static DRT.ViewGenerator;

namespace DRT
{
    public class FoundationTemplate : ISheetTemplate, IExternalCommand
    {
        private UIApplication app;
        private Document _doc;
        private Autodesk.Revit.UI.UIDocument _uidoc;
        private ViewSheet ViewSheet;
        private BoundingBoxXYZ elementBoundingBox;
        string m_errorInformation;
        List<ElementId> CurrentComponents;


        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            app = commandData.Application;
            _uidoc = app.ActiveUIDocument;
            _doc = _uidoc.Document;

            if (!GetElements())
            {
                message = "Could not get Elements";
            }

            if (!GetBoundingBox())
            {
                message = "Could Not create boundingbox around elements";
            }

            // Create agenerator for views
            ViewGenerator generator = new ViewGenerator();

            //create sheet
            CreateSheet("A101", "PURE DRDLR REBAR DOUBLE 20-06-2019");

            //create views 
            ViewSection plan = generator.CreateView(_doc, elementBoundingBox, ViewTypeGenerate.PLAN);
            ViewSection elevation = generator.CreateView(_doc, elementBoundingBox, ViewTypeGenerate.ELEVATION);

            //CreateView(doc, m_currentComponent, ViewType.PLANSECTION);
            //CreateView(doc, m_currentComponent, ViewType.ELEVATIONSECTION);
            //CreateView(doc, m_currentComponent, ViewType.RIGHTSECTION);
            //CreateView(doc, m_currentComponent, ViewType.LEFTSECTION);
            //CreateView(doc, m_currentComponent, ViewType.PLAN);
            //CreateView(doc, m_currentComponent, ViewType.ELEVATION);
            //CreateView(doc, m_currentComponent, ViewType.RIGHT);
            //CreateView(doc, m_currentComponent, ViewType.LEFT);

            //add views to sheet
            AddViewToSheet(plan, SheetLocation.MiddleLeft);
            AddViewToSheet(elevation, SheetLocation.MiddleCenter);

            //add schedules

            // 
            return Result.Succeeded;
        }

        private bool GetElements()
        {
            foreach (ElementId elementId in _uidoc.Selection.GetElementIds())
            {
                CurrentComponents.Add(elementId);
            }

            if (CurrentComponents.Count > 0)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        private bool GetBoundingBox()
        {
            if (CurrentComponents.Count > 1)
            {
                elementBoundingBox = CurrentComponents.GetBoundingBoxForElements(_doc);

            }
            else
            {
                var m_currentComponent = _doc.GetElement(CurrentComponents.First());
                elementBoundingBox = m_currentComponent.get_BoundingBox(null);

            }

            if (elementBoundingBox !=null)
            {
                return true;

            }
            else
            {
                return false;
            }
        }

        public void AddScheduleToSheet(View view, SheetLocation location, XYZ customPosition = null)
        {
            throw new NotImplementedException();
        }

        public void AddViewToSheet(View view, SheetLocation position, XYZ customPosition = null)
        {
            if (view is ViewSheet)
            {
                throw new InvalidOperationException("Cannot add a sheet view to another sheet.");
            }

            if (_doc == null)
            {
                throw new InvalidOperationException("Document is not set.");
            }

            // Calculate position based on the specified SheetLocation

            XYZ viewPosition = SheetUtils.CalculatePosition(view, ViewSheet,SheetLocation.MiddleCenter);

            Transaction transaction = new Transaction(_doc, "Add View to Sheet");
            transaction.Start();
            try
            {
                Viewport.Create(_doc, ViewSheet.Id, view.Id,viewPosition);
                transaction.Commit();
            }
            catch (Exception ex)
            {
                transaction.RollBack();
                throw new InvalidOperationException("Failed to add view to sheet.", ex);
            }
        }

        public void CreateSheet(string sheetNumber, string titleBlockName)
        {
            // Find the title block family symbol
            FamilySymbol titleBlockSymbol = new FilteredElementCollector(_doc)
                .OfClass(typeof(FamilySymbol))
                .OfCategory(BuiltInCategory.OST_TitleBlocks)
                .FirstOrDefault(e => e.Name.Equals(titleBlockName)) as FamilySymbol;

            SheetUtils.GetTitleBlock(_doc, titleBlockName);

            // Start a transaction to create the sheet
            using (Transaction trans = new Transaction(_doc, "Create Sheet"))
            {
                trans.Start();
                if (titleBlockSymbol != null)
                {
                    if (!titleBlockSymbol.IsActive)
                        titleBlockSymbol.Activate();
                    ViewSheet = ViewSheet.Create(_doc, titleBlockSymbol.Id);
                    ViewSheet.SheetNumber = sheetNumber;
                    // Set additional properties on the sheet here
                }
                trans.Commit();
            }
        }

        public void Load()
        {
            throw new NotImplementedException();
        }

        public void Save()
        {
            throw new NotImplementedException();
        }

    }

    public interface ISheetTemplate
    {
        void CreateSheet(string sheetNumber, string titleBlockName);
        void AddViewToSheet(View view, SheetLocation location, XYZ customPosition = null);
        void AddScheduleToSheet(View view, SheetLocation location, XYZ customPosition = null);
        void Load();
        void Save();
    }
    public enum SheetLocation
    {
        TopLeft,
        TopCenter,
        TopRight,
        MiddleLeft,
        MiddleCenter,
        MiddleRight,
        BottomLeft,
        BottomCenter,
        BottomRight,
        Custom // Use Custom if you need to specify a location not covered by the predefined options
    }

    public class SheetUtils
    {
        public static FamilySymbol GetTitleBlock(Document doc, string Name)
        {

            // Create a filtered element collector for title blocks
            FilteredElementCollector collector = new FilteredElementCollector(doc);
            collector.OfCategory(BuiltInCategory.OST_TitleBlocks);
            collector.OfClass(typeof(FamilySymbol));


            // Iterate through the collected elements and print their names
            foreach (FamilySymbol element in collector)
            {
                if (element.Name == Name)
                {
                    return element;
                }
            }
            return null;
        }
        public static FamilySymbol GetTitleBlock(Document doc, string ParametName, string Value)
        {

            // Create a filtered element collector for title blocks
            FilteredElementCollector collector = new FilteredElementCollector(doc);
            collector.OfCategory(BuiltInCategory.OST_TitleBlocks);
            collector.OfClass(typeof(FamilySymbol));

            // Iterate through the collected elements and print their names
            foreach (FamilySymbol element in collector)
            {
                try
                {
                    Parameter param = element.GetParameters(ParametName).First();

                    if (param.HasValue)
                    {
                        if (param.AsValueString() == Value)
                        {
                            return element;
                        }

                    }


                }
                catch (Exception)
                {

                    throw;
                }


            }
            return null;
        }
        public static XYZ CalculatePosition(View view, ViewSheet sheet, SheetLocation location, XYZ customPosition = null)
        {
            // Ensure the view is not a sheet
            if (view is ViewSheet)
                throw new InvalidOperationException("The view to add cannot be a sheet.");

            // Get sheet dimensions
            BoundingBoxUV sheetBox = sheet.Outline;
            UV min = sheetBox.Min;
            UV max = sheetBox.Max;
            double x = 0, y = 0;

            // Calculate position based on location enum
            switch (location)
            {
                case SheetLocation.TopLeft:
                    x = min.U;
                    y = max.V;
                    break;
                case SheetLocation.TopCenter:
                    x = (min.U + max.U) / 2;
                    y = max.V;
                    break;
                case SheetLocation.TopRight:
                    x = max.U;
                    y = max.V;
                    break;
                case SheetLocation.MiddleLeft:
                    x = min.U;
                    y = (min.V + max.V) / 2;
                    break;
                case SheetLocation.MiddleCenter:
                    x = (min.U + max.U) / 2;
                    y = (min.V + max.V) / 2;
                    break;
                case SheetLocation.MiddleRight:
                    x = max.U;
                    y = (min.V + max.V) / 2;
                    break;
                case SheetLocation.BottomLeft:
                    x = min.U;
                    y = min.V;
                    break;
                case SheetLocation.BottomCenter:
                    x = (min.U + max.U) / 2;
                    y = min.V;
                    break;
                case SheetLocation.BottomRight:
                    x = max.U;
                    y = min.V;
                    break;
                default:
                    throw new NotImplementedException("Custom location handling is not implemented.");
            }

            // Convert sheet coordinates (UV) to 3D space (XYZ), ignoring Z
            XYZ xyz = new XYZ(x, y,0);

            return xyz;

        }
    }
}
