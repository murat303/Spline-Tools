using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Splines;
using System;
using System.Linq;


#if UNITY_EDITOR
using UnityEditor;
#endif

namespace SplineTools
{
    public class SplineRoad : SplineBase
    {
        [SerializeField] private float width = 0.25f;
        [SerializeField] private float yOffset = 0.02f;
        [Range(0.05f, 1f)]
        [SerializeField] private float curveStep = 0.1f;
        [SerializeField] private List<Intersection> intersections = new List<Intersection>();

        [Header("Debug")]
        [SerializeField] private bool showGizmos = true;
        [SerializeField] private float handlesScale = 0.1f;

        private List<Vector3> vertsP1 = new List<Vector3>();
        private List<Vector3> vertsP2 = new List<Vector3>();

        private List<Vector3> junctionGizmoPoints = new List<Vector3>(); //For Gizmos
        private List<Vector3> curveGizmoPoints = new List<Vector3>(); //For Gizmos

        private List<Vector3> verts = new List<Vector3>(); //verts represent the position of the vertices
        private List<int> tris = new List<int>(); //tris represent the index of the vertices, three of them make up a polygon
        private List<Vector3> uvs = new List<Vector3>(); //uv represent the texture coordinates of the vertices
        private float uvOffset = 0f;

        public override void GetPoints()
        {
            vertsP1.Clear();
            vertsP2.Clear();

            if (splineContainer.Splines.Count == 0) return;

            // Determine the step size based on the resolution
            var step = 1f / resolution;
            Vector3 p1, p2;

            // Loop through each spline
            for (int currentSplineIndex = 0; currentSplineIndex < splineContainer.Splines.Count; currentSplineIndex++)
            {
                // Loop through each step in the resolution
                for (int i = 0; i < resolution; i++)
                {
                    // Calculate the parameter t for the current step
                    float t = i * step;

                    // Sample the spline at the parameter t to get two points (p1 and p2)
                    // These points represent the width of the road at this point along the spline
                    SampleSplineWidth(currentSplineIndex, t, width, out p1, out p2);

                    // Add the offset to the y position of the points
                    p1.y += yOffset;
                    p2.y += yOffset;

                    // Add the points to the lists of vertices
                    vertsP1.Add(p1);
                    vertsP2.Add(p2);
                }

                // Sample the spline at the parameter t = 1 to get the last point (p1)
                SampleSplineWidth(currentSplineIndex, 1f, width, out p1, out p2);

                p1.y += yOffset;
                p2.y += yOffset;

                vertsP1.Add(p1);
                vertsP2.Add(p2);
            }
        }

        public override void BuildMesh()
        {
            if (meshFilter == null || mesh == null) return;

            // Clear the lists of vertices, triangles and UVs
            mesh.Clear();
            verts.Clear();
            tris.Clear();
            uvs.Clear();
            uvOffset = 0f;

            var roadMaterial = Resources.Load<Material>("Road");
            var intersectionMaterial = Resources.Load<Material>("Intersection");
            meshRenderer.materials = new Material[] { roadMaterial, intersectionMaterial };

            if (splineContainer.Splines.Count > 0)
            {
                // Loop through each spline
                for (int currentSplineIndex = 0; currentSplineIndex < splineContainer.Splines.Count; currentSplineIndex++)
                {
                    int splineOffset = resolution * currentSplineIndex;
                    splineOffset += currentSplineIndex;

                    // Loop through each vertex
                    for (int currentSplinePoint = 1; currentSplinePoint < resolution + 1; currentSplinePoint++)
                    {
                        // Get the vertices from the first and second lists, converting from world to local coordinates
                        GetVerts(splineOffset, currentSplinePoint, out Vector3 p1, out Vector3 p2, out Vector3 p3, out Vector3 p4);

                        // Calculate the indices for the two triangles that make up the quad
                        GetTris(currentSplineIndex, currentSplinePoint);

                        // Get the UV coordinates for the vertices
                        GetUVs(p1, p3);
                    }
                }

                List<int> intersectionTris = new List<int>();
                SetIntersections(ref intersectionTris);

                mesh.subMeshCount = 2;

                // Set the vertices and triangles of the mesh
                mesh.SetVertices(verts);
                mesh.SetTriangles(tris, 0);
                mesh.SetTriangles(intersectionTris, 1);
                mesh.SetUVs(0, uvs);

                // Recalculate the normals of the mesh for correct lighting
                mesh.RecalculateNormals();
                mesh.RecalculateTangents();
            }
        }

        private void GetVerts(int splineOffset, int currentSplinePoint, out Vector3 p1, out Vector3 p2, out Vector3 p3, out Vector3 p4)
        {
            int vertOffset = splineOffset + currentSplinePoint;

            // Get the vertices from the first and second lists, converting from world to local coordinates
            p1 = transform.InverseTransformPoint(vertsP1[vertOffset - 1]);
            p2 = transform.InverseTransformPoint(vertsP2[vertOffset - 1]);
            p3 = transform.InverseTransformPoint(vertsP1[vertOffset]);
            p4 = transform.InverseTransformPoint(vertsP2[vertOffset]);

            /* P3(2)-----P4(3)
             * |          |
             * |          |
             * P1(0)-----P2(1)
             */

            verts.AddRange(new List<Vector3> { p1, p2, p3, p4 });
        }

        private void GetTris(int currentSplineIndex, int currentSplinePoint)
        {
            // Calculate the offset for the triangle indices
            var offset = 0;
            offset = 4 * resolution * currentSplineIndex; //4 vertices per quad
            offset += 4 * (currentSplinePoint - 1);

            // Calculate the indices for the two triangles that make up the quad
            // **** Display them in a clockwise order ****
            var t1 = offset + 0; //index of the vertices
            var t2 = offset + 2; //index of the vertices
            var t3 = offset + 3; //index of the vertices

            var t4 = offset + 3; //index of the vertices
            var t5 = offset + 1; //index of the vertices
            var t6 = offset + 0; //index of the vertices

            tris.AddRange(new List<int> { t1, t2, t3, t4, t5, t6 });
        }

        private void GetUVs(Vector3 p1, Vector3 p3)
        {
            // UV coordinates are the position of the vertices in the mesh
            // They are used to map textures to the mesh
            // The UV coordinates are repsresented as a percentage of the texture where the values go from 0 to 1
            // Each uv index contains a texture positon that should be matched to the vertex in that exact same index

            /* (0,1)-----(1,1)
             * |            |
             * |            |
             * (0,0)-----(1,0)
             */

            float distance = Vector3.Distance(p1, p3) / 4f;
            float uvDistance = uvOffset + distance;

            var uv1 = new Vector2(uvOffset, 0);
            var uv2 = new Vector2(uvOffset, 1);
            var uv3 = new Vector2(uvDistance, 0);
            var uv4 = new Vector2(uvDistance, 1);

            uvs.AddRange(new List<Vector3> { uv1, uv2, uv3, uv4 });
            uvOffset += distance;
        }

        private void SetIntersections(ref List<int> intersectionTris)
        {
            //Junctions
            junctionGizmoPoints.Clear();
            curveGizmoPoints.Clear();

            // //Remove junction in intersections that have been deleted
            // for (int i = 0; i < intersections.Count; i++)
            // {
            //     var intersection = intersections[i];
            //     for (int j = 0; j < intersection.junctions.Count; j++)
            //     {
            //         var junction = intersection.junctions[j];
            //         if (!splineContainer.Splines.Any(s => s == junction.spline))
            //         {
            //             intersection.junctions.RemoveAt(j);
            //             intersection.curves.RemoveAt(j);
            //             break;
            //         }
            //     }
            // }

            // //Remove empty intersections
            // intersections.RemoveAll(i => i.junctions.Count <= 1 || i.curves.Count <= 1);

            //TODO: Spline container icinde siralama degisirse intersectionlar bozuluyor

            if (intersections.Count > 0)
            {
                foreach (var intersection in intersections)
                {
                    var junctionEdges = new List<JuntionEdge>();
                    var center = new Vector3();
                    foreach (var junction in intersection.GetJunctions())
                    {
                        int splineIndex = junction.splineIndex;
                        float t = junction.knotIndex == 0 ? 0f : 1f;
                        SampleSplineWidth(splineIndex, t, width, out Vector3 p1, out Vector3 p2);

                        p1.y += yOffset;
                        p2.y += yOffset;

                        //If knot index is 0 we're facing away from the junction
                        //If we're more than zero we're facing towards the junction
                        if (junction.knotIndex == 0)
                        {
                            junctionEdges.Add(new JuntionEdge(p1, p2));
                            //For Gizmos
                            junctionGizmoPoints.Add(p1);
                            junctionGizmoPoints.Add(p2);
                        }
                        else
                        {
                            junctionEdges.Add(new JuntionEdge(p2, p1));
                            //For Gizmos
                            junctionGizmoPoints.Add(p2);
                            junctionGizmoPoints.Add(p1);
                        }

                        center += p1;
                        center += p2;
                    }

                    //Get the center of all the points
                    center = center / (junctionEdges.Count * 2); // *2 because we have two points per edge

                    //Sort the points based on their direction from the center
                    junctionEdges = junctionEdges.OrderBy(e => Vector3.SignedAngle(e.Center - center, Vector3.forward, Vector3.up)).ToList();

                    //Curve Points
                    var curvePoints = new List<Vector3>();
                    //Add aditional points
                    Vector3 mid, c, b, a;
                    BezierCurve curve;
                    for (int j = 1; j <= junctionEdges.Count; j++)
                    {
                        a = junctionEdges[j - 1].left; //left because we're going clockwise
                        curvePoints.Add(a); //Add the first point
                        b = (j < junctionEdges.Count) ? junctionEdges[j].right : junctionEdges[0].right;

                        mid = Vector3.Lerp(a, b, 0.5f); //Get the mid point between the two points
                        Vector3 dir = mid - center; //Get the direction from the center to the mid point
                        mid = mid + dir * 5; //Move the mid point away from the center
                        c = Vector3.Lerp(mid, center, intersection.curves[j - 1]); //Get the curve point between the mid point and the center

                        curve = new BezierCurve(a, c, b); //Create a curve between the three points
                        for (float t = 0f; t < 1f; t += curveStep) //Sample the curve
                        {
                            var pos = CurveUtility.EvaluatePosition(curve, t); //Get the position at the current t
                            curvePoints.Add(pos); //Add the position to the list

                            //For Gizmos
                            if (t > 0) curveGizmoPoints.Add(pos);
                        }

                        curvePoints.Add(b); //Add the last point
                    }
                    curvePoints.Reverse(); //Reverse the list so we can add the points in the correct order

                    int pointsOffset = verts.Count;
                    //Add the junction points to the mesh
                    for (int j = 1; j <= curvePoints.Count; j++)
                    {
                        var p1 = transform.InverseTransformPoint(center);
                        var p2 = transform.InverseTransformPoint(curvePoints[j - 1]);
                        var p3 = (j == curvePoints.Count) ? transform.InverseTransformPoint(curvePoints[0]) : transform.InverseTransformPoint(curvePoints[j]);

                        verts.Add(p1);
                        verts.Add(p2);
                        verts.Add(p3);

                        intersectionTris.Add(pointsOffset + ((j - 1) * 3) + 0);
                        intersectionTris.Add(pointsOffset + ((j - 1) * 3) + 1);
                        intersectionTris.Add(pointsOffset + ((j - 1) * 3) + 2);

                        var uv1 = new Vector2(p1.z, p1.x);
                        var uv2 = new Vector2(p2.z, p2.x);
                        var uv3 = new Vector2(p3.z, p3.x);

                        uvs.AddRange(new List<Vector3> { uv1, uv2, uv3 });
                    }
                }
            }
        }

        public void AddIntersection(Intersection intersection)
        {
            intersections.Add(intersection);
        }

        public List<Intersection> GetIntersections()
        {
            return intersections;
        }

        public override void OnDrawGizmos()
        {
#if UNITY_EDITOR
            if (!showGizmos) return;

            Handles.color = Color.white;
            for (int i = 0; i < vertsP1.Count; i++)
            {
                var p1 = vertsP1[i];
                var p2 = vertsP2[i];
                Handles.SphereHandleCap(0, p1, Quaternion.identity, handlesScale, EventType.Repaint);
                Handles.SphereHandleCap(0, p2, Quaternion.identity, handlesScale, EventType.Repaint);
                Handles.DrawLine(p1, p2);
            }

            for (int i = 0; i < junctionGizmoPoints.Count; i++)
            {
                if (i % 2 == 0) Handles.color = Color.blue;
                else Handles.color = Color.red;
                var p = junctionGizmoPoints[i];
                Handles.SphereHandleCap(0, p, Quaternion.identity, handlesScale, EventType.Repaint);
            }

            Handles.color = Color.white;
            for (int i = 0; i < curveGizmoPoints.Count; i++)
            {
                var p = curveGizmoPoints[i];
                Handles.SphereHandleCap(0, p, Quaternion.identity, handlesScale, EventType.Repaint);
            }
#endif
        }
    }

    [Serializable]
    public struct JunctionInfo
    {
        public int splineIndex;
        public int knotIndex;
        public Spline spline;
        public BezierKnot knot;

        public JunctionInfo(int splineIndex, int knotIndex, Spline spline, BezierKnot knot)
        {
            this.splineIndex = splineIndex;
            this.knotIndex = knotIndex;
            this.spline = spline;
            this.knot = knot;
        }
    }

    [Serializable]
    public struct JuntionEdge
    {
        public Vector3 left;
        public Vector3 right;

        public Vector3 Center => (left + right) / 2;

        public JuntionEdge(Vector3 p1, Vector3 p2)
        {
            this.left = p1;
            this.right = p2;
        }
    }

    [Serializable]
    public class Intersection
    {
        public List<JunctionInfo> junctions = new List<JunctionInfo>();
        public List<float> curves = new List<float>();

        public void AddJunction(int splineIndex, int knotIndex, Spline spline, BezierKnot knot)
        {
            junctions.Add(new JunctionInfo(splineIndex, knotIndex, spline, knot));
            curves.Add(0.9f);
        }

        internal IEnumerable<JunctionInfo> GetJunctions()
        {
            return junctions;
        }
    }
}
