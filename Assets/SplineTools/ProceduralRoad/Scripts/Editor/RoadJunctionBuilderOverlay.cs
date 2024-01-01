using System.Collections.Generic;
using System.Linq;
using SplineTools;
using UnityEditor;
using UnityEditor.Overlays;
using UnityEditor.Splines;
using UnityEngine;
using UnityEngine.Splines;
using UnityEngine.UIElements;

//TODO: Remove Junction, Intersection, Spline, Show Curve Sliders on Selected Intersection, Clear Selection and Curve Sliders
[Overlay(typeof(SceneView), "Road Junction Builder", true)]
public class RoadJunctionBuilderOverlay : Overlay
{
    Label SelectionInfoLabel;
    Button BuildJunctionButton;
    List<Slider> SliderArea = new List<Slider>();
    VisualElement root;

    public override VisualElement CreatePanelContent()
    {
        Debug.Log("CreatePanelContent");
        root = new VisualElement();

        // Create Label
        SelectionInfoLabel = new Label();
        root.Add(SelectionInfoLabel);

        // Create Button
        BuildJunctionButton = new Button(OnBuildJunction)
        {
            text = "Build Junction"
        };
        root.Add(BuildJunctionButton);

        return root;
    }

    public override void OnCreated()
    {
        SplineSelection.changed += OnSplineSelectionChanged;
        Selection.selectionChanged += OnSelectionChanged;
    }

    public override void OnWillBeDestroyed()
    {
        SplineSelection.changed -= OnSplineSelectionChanged;
        Selection.selectionChanged -= OnSelectionChanged;
    }

    private void OnSelectionChanged()
    {
        // //Disable the overlay if the selection is not a spline
        // if (Selection.activeGameObject == null || Selection.activeGameObject.GetComponent<SplineRoad>() == null)
        // {
        //     this.displayed = false;
        // }
        // else
        // {
        //     UpdateSelectionInfo();
        //     //Enable the overlay if the selection is a spline
        //     this.displayed = true;
        // }
    }

    private void OnSplineSelectionChanged()
    {
        UpdateSelectionInfo();
    }

    private void ClearSelectionInfo()
    {
        SelectionInfoLabel.text = "";
    }

    private void UpdateSelectionInfo()
    {
        ClearSelectionInfo();

        BuildJunctionButton.visible = true;
        SliderArea.Clear();

        List<SelectedSplineElementInfo> selectedElements = SplineToolUtilty.GetSelection();

        foreach (SelectedSplineElementInfo element in selectedElements)
        {
            SelectionInfoLabel.text += $"Spline {element.targetIndex}, Knot {element.knotIndex} \n";
        }
    }

    private void OnBuildJunction()
    {
        var selection = SplineToolUtilty.GetSelection();

        if (selection.Count < 2)
        {
            Debug.LogWarning("Select at least two point to build a junction");
            return;
        }

        var road = Selection.activeGameObject.GetComponent<SplineRoad>();
        var intersections = road.GetIntersections();

        var intersection = new Intersection();
        foreach (SelectedSplineElementInfo element in selection)
        {
            var query = intersections.Where(i => i.junctions.Any(j => j.splineIndex == element.targetIndex && j.knotIndex == element.knotIndex));
            if (query.Any())
            {
                intersection = query.First();
            }
        }

        bool isAlreadyPartOfIntersection = false;
        bool newIntersection = true;
        foreach (SelectedSplineElementInfo element in selection)
        {
            var query = intersections.Where(i => i.junctions.Any(j => j.splineIndex == element.targetIndex && j.knotIndex == element.knotIndex));
            if (query.Any())
            {
                isAlreadyPartOfIntersection = true;
                newIntersection = false;
            }
            else
            {
                isAlreadyPartOfIntersection = false;
            }

            if (!isAlreadyPartOfIntersection)
            {
                //Get the spline container
                var container = (SplineContainer)element.target;
                var spline = container.Splines[element.targetIndex];
                intersection.AddJunction(element.targetIndex, element.knotIndex, spline, spline[element.knotIndex]);
            }
        }

        if (newIntersection)
        {
            road.AddIntersection(intersection);
        }

        ShowIntersection(intersection);
        road.BuildMesh();
    }

    public void ShowIntersection(Intersection intersection)
    {
        SelectionInfoLabel.text = "Selected Intersection";
        BuildJunctionButton.visible = false;

        foreach (Slider slider in SliderArea)
        {
            root.Remove(slider);
        }
        SliderArea.Clear();

        for (int i = 0; i < intersection.curves.Count; i++)
        {
            int value = i;
            Slider slider = new Slider($"Curve {i}", 0, 1, SliderDirection.Horizontal);
            slider.labelElement.style.minWidth = 60;
            slider.labelElement.style.maxWidth = 80;
            slider.value = intersection.curves[i];
            slider.RegisterValueChangedCallback((evt) =>
            {
                intersection.curves[value] = evt.newValue;
                Selection.activeGameObject.GetComponent<SplineRoad>().BuildMesh();
                //OnChangeValueEvent.Invoke();
            });
            SliderArea.Add(slider);
        }

        foreach (var slider in SliderArea)
        {
            root.Add(slider);
        }
    }
}
