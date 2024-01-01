using System;
using System.Linq;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Splines;

namespace SplineTools
{
    [ExecuteInEditMode()]
    [RequireComponent(typeof(SplineContainer))]
    [RequireComponent(typeof(MeshFilter))]
    [RequireComponent(typeof(MeshRenderer))]
    public abstract class SplineBase : MonoBehaviour
    {
        [HideInInspector] public SplineContainer splineContainer;
        [HideInInspector] public MeshFilter meshFilter;
        [HideInInspector] public MeshRenderer meshRenderer;
        [HideInInspector] public Mesh mesh;

        public int resolution = 25;

        private void OnEnable()
        {
            mesh = new Mesh();
            splineContainer = GetComponent<SplineContainer>();
            meshRenderer = GetComponent<MeshRenderer>();
            meshFilter = GetComponent<MeshFilter>();
            meshFilter.mesh = mesh;
            Spline.Changed += OnSplineChanged;
            SplineContainer.SplineAdded += OnSplineContainerChanged;
            SplineContainer.SplineRemoved += OnSplineContainerChanged;

            GetPoints();
            BuildMesh();
        }

        private void OnDisable()
        {
            Spline.Changed -= OnSplineChanged;
            SplineContainer.SplineAdded -= OnSplineContainerChanged;
            SplineContainer.SplineRemoved -= OnSplineContainerChanged;
        }

        private void OnSplineChanged(Spline spline, int arg2, SplineModification modification)
        {
            if (spline.Knots.Count() > 1)
            {
                GetPoints();
                BuildMesh();
            }
        }

        private void OnSplineContainerChanged(SplineContainer container, int arg2)
        {
            GetPoints();
            BuildMesh();
        }

        void OnValidate()
        {
            GetPoints();
            BuildMesh();
        }

        public abstract void GetPoints();
        public abstract void BuildMesh();
        public abstract void OnDrawGizmos();

        public void SampleSpline(int splineIndex, float time, out Vector3 p1)
        {
            float3 position = 0, tangent = 0, upVector = 0f;
            splineContainer?.Evaluate(splineIndex, time, out position, out tangent, out upVector);
            p1 = position;
        }

        public void SampleSplineWidth(int splineIndex, float time, float width, out Vector3 p1, out Vector3 p2)
        {
            float3 position = 0, tangent = 0, upVector = 0f;
            splineContainer?.Evaluate(splineIndex, time, out position, out tangent, out upVector);

            //Tanget is the (forward) direction of the spline
            //Find the right vector by crossing the tangent with the up vector
            var right = math.cross(tangent, upVector);
            //normalize the right vector
            right = math.normalize(right);

            //Draw the line
            p1 = position + right * width;
            p2 = position - right * width;
        }
    }
}
