using System.Collections.Generic;
using System.Diagnostics;
using System.Windows;
using System.Windows.Media;
using System.Windows.Shapes;
using PlexShareWhiteboard.BoardComponents;

namespace PlexShareWhiteboard
{
    public class SelectObject
    {
        public bool ifSelected;
        public ShapeItem selectedObject;
        public ShapeItem initialSelectionObject;
        public Point initialSelectionPoint;
        public int selectBox;
        public List<Point> finalPointList;

        public SelectObject()
        {
            ifSelected = false;
            selectedObject = null;
            selectBox = -1;
            finalPointList = new List<Point>();
        }
    }

    public partial class WhiteBoardViewModel
    {
        public static bool PointInsideRect(Rect shape, Point click)
        {
            if (click.X > shape.X && click.X < shape.X + shape.Width &&
                click.Y > shape.Y && click.Y < shape.Y + shape.Height)
                return true;
            
            return false;
        }

        public static int PointInsideHighlightBox(Rect shape, Point click, double halfSize)
        {
            double left = shape.X;
            double top = shape.Y;
            double width = shape.Width;
            double height = shape.Height;

            Rect topLeft = new Rect(left - halfSize, top - halfSize, 2 * halfSize, 2 * halfSize);
            Rect topRight = new Rect(left - halfSize + width, top - halfSize, 2 * halfSize, 2 * halfSize);
            Rect bottomLeft = new Rect(left - halfSize, top - halfSize + height, 2 * halfSize, 2 * halfSize);
            Rect bottomRight = new Rect(left - halfSize + width, top - halfSize + height, 2 * halfSize, 2 * halfSize);

            Rect topCentre = new Rect(left - halfSize + width / 2, top - halfSize, 2 * halfSize, 2 * halfSize);
            Rect leftCentre = new Rect(left - halfSize, top - halfSize + height / 2, 2 * halfSize, 2 * halfSize);
            Rect rightCentre = new Rect(left - halfSize + width, top - halfSize + height / 2, 2 * halfSize, 2 * halfSize);
            Rect bottomCentre = new Rect(left - halfSize + width / 2, top - halfSize + height, 2 * halfSize, 2 * halfSize);

            if (PointInsideRect(topLeft, click))
                return 1;
            else if(PointInsideRect(topRight, click))
                return 2;
            else if(PointInsideRect(bottomLeft, click)) 
                return 3;
            else if(PointInsideRect(bottomRight, click))
                return 4;
            else if(PointInsideRect(topCentre, click))
                return 5;
            else if(PointInsideRect(leftCentre, click))
                return 6;
            else if(PointInsideRect(rightCentre, click))
                return 7;
            else if(PointInsideRect(bottomCentre, click))
                return 8;
         
            return -1;
        }
        //deon

        public int PointInsideHighlightBox(LineGeometry shape, Point click, double halfsize)
        {
            Rect top = new(shape.StartPoint.X - halfsize, shape.StartPoint.Y - halfsize, 2 * halfsize, 2 * halfsize);
            Rect bottom = new(shape.EndPoint.X - halfsize, shape.EndPoint.Y - halfsize, 2 * halfsize, 2 * halfsize);

            if (click.X > top.X && click.X < top.X + top.Width &&
                click.Y > top.Y && click.Y < top.Y + top.Height)
                return 0;

            if (click.X > bottom.X && click.X < bottom.X + bottom.Width &&
                click.Y > bottom.Y && click.Y < bottom.Y + bottom.Height)
                return 1;
            return -1;
        }
        //deon
        public static int ClickInsideHighlightBox(Point point, Point click, double halfsize)
        {

            if (click.X > point.X - halfsize && click.X < point.X + halfsize &&
                click.Y > point.Y - halfsize && click.Y < point.Y + halfsize)
                return 1;

            return 0;
        }
        public static bool HelperSelect(Rect boundingBox, Point click, double halfSize)
        {
            if (PointInsideRect(boundingBox, click) || PointInsideHighlightBox(boundingBox, click, halfSize) > 0)
                return true;

            return false;
        }

        public void ObjectSelection(Point a)
        {
            int tempZIndex = -10000;
            Rect boundingBox = new(1, 1, 1, 1);

            for (int i = ShapeItems.Count - 1; i >= 0; i--)
            {
                if (ShapeItems[i].ZIndex < tempZIndex)
                    continue;

                Geometry Child = ShapeItems[i].Geometry;
                boundingBox = Child.Bounds;

                if (HelperSelect(boundingBox, a, blobSize / 2))
                {
                   
                    select.ifSelected = true;
                    select.selectedObject = ShapeItems[i];
                    select.initialSelectionPoint = a;
                    tempZIndex = ShapeItems[i].ZIndex;
                    select.selectBox = 0;
                    Debug.WriteLine("entering select ", select.selectedObject.Id);
                }
                else if (Child.FillContains(a) && Child.GetType().Name == "LineGeometry")
                {

                    HelperSelectLine(boundingBox, tempZIndex, i, a);
                }
            }

            if (select.ifSelected == true)
            {
                if (select.selectedObject.Geometry.GetType().Name == "LineGeometry")
                {
                    //Trace.WriteLine("[Whiteboard]  " + "line selected\n");

                    LineGeometry boundingLine = (LineGeometry)GenerateBoundingLine(select.selectedObject);
                    Debug.WriteLine("selected boundingline " + boundingLine.StartPoint + " " + boundingLine.EndPoint);
                    HighLightIt(boundingLine);
                    int boxNumber = PointInsideHighlightBox(boundingLine, a, blobSize / 2);
                    if (boxNumber >= 0)
                    {
                        //Trace.WriteLine("[Whiteboard]  " + "In transform mode ");
                        mode = "transform_mode";
                        select.selectBox = boxNumber;
                    }
                    else
                    {
                        //Debug.Write("In translate_mode ");
                        mode = "translate_mode";
                    }
                    select.initialSelectionObject = select.selectedObject;
                }
                else
                {
                    Trace.WriteLine("[Whiteboard]  " + "object selected\n");
                    HighLightIt(select.selectedObject.Geometry.Bounds);
                    int boxNumber = PointInsideHighlightBox(boundingBox, a, blobSize / 2);

                    ShapeItem newShape = select.selectedObject.DeepClone();
                    select.initialSelectionObject = newShape;

                    if (select.selectedObject.Geometry.GetType().Name != "GeometryGroup")
                    {
                        if (boxNumber > 4)
                        {
                            Trace.WriteLine("[Whiteboard]  " + "Going to enter dimensionChange_mode \n");
                            mode = "dimensionChange_mode";
                            select.selectBox = boxNumber;
                        }
                        else if (boxNumber > 0)
                        {
                            Trace.WriteLine("[Whiteboard]  " + "Going to enter transform mode \n");
                            mode = "transform_mode";
                            select.selectBox = boxNumber;
                        }
                        else if (select.selectBox == 0)
                        {
                            Debug.Write("Going to enter translate_mode \n");
                            mode = "translate_mode";
                        }
                    }
                    else
                    {
                        UnHighLightIt();
                        if (select.selectBox == 0)
                        {
                            Debug.Write("Going to enter translate_mode \n");
                            mode = "translate_mode";
                        }
                        HighLightTextBox(select.selectedObject.Geometry.Bounds);
                    }

                }

            }
        }

    }


}
