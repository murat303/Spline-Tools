using System.Collections.Generic;
using UnityEngine;

namespace UnityEditor.Splines
{
    public static class SplineToolUtilty
    {
        public static bool HasSelection()
        {
            return SplineSelection.HasActiveSplineSelection();
        }

        /// <summary>
        /// Get the selected spline elements
        /// </summary>
        /// <returns></returns>
        public static List<SelectedSplineElementInfo> GetSelection()
        {
            //Get internal struct data
            List<SelectableSplineElement> elements = SplineSelection.selection;

            //Convert to our own struct
            //Make new public struct data
            List<SelectedSplineElementInfo> selectedElements = new List<SelectedSplineElementInfo>();

            //Convert internal struct to public struct data
            for (int i = 0; i < elements.Count; i++)
            {
                SelectableSplineElement element = elements[i];
                selectedElements.Add(new SelectedSplineElementInfo(element.target, element.targetIndex, element.knotIndex));
            }

            //Return public struct data
            return selectedElements;
        }
    }

    public struct SelectedSplineElementInfo
    {
        public Object target;
        public int targetIndex;
        public int knotIndex;

        public SelectedSplineElementInfo(Object target, int targetIndex, int knotIndex)
        {
            this.target = target;
            this.targetIndex = targetIndex;
            this.knotIndex = knotIndex;
        }
    }
}
