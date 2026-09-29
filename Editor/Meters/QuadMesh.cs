// QuadMesh.cs
// Writes axis-aligned colored rectangles into a UI Toolkit mesh: every meter draws all its
// rectangles into one MeshWriteData (one draw call, no Painter2D paths, no allocations).
using UnityEngine;
using UnityEngine.UIElements;

namespace AudioReactive.EditorTools
{
    internal struct QuadMesh
    {
        private readonly MeshWriteData _mesh;
        private ushort _next;

        // Allocates room for exactly quadCount rectangles (all must then be written).
        public QuadMesh(MeshGenerationContext context, int quadCount)
        {
            _mesh = context.Allocate(quadCount * 4, quadCount * 6);
            _next = 0;
        }

        public void Rect(float x0, float y0, float x1, float y1, Color32 color)
        {
            _mesh.SetNextVertex(new Vertex { position = new Vector3(x0, y0, Vertex.nearZ), tint = color });
            _mesh.SetNextVertex(new Vertex { position = new Vector3(x1, y0, Vertex.nearZ), tint = color });
            _mesh.SetNextVertex(new Vertex { position = new Vector3(x1, y1, Vertex.nearZ), tint = color });
            _mesh.SetNextVertex(new Vertex { position = new Vector3(x0, y1, Vertex.nearZ), tint = color });
            _mesh.SetNextIndex(_next);
            _mesh.SetNextIndex((ushort)(_next + 1));
            _mesh.SetNextIndex((ushort)(_next + 2));
            _mesh.SetNextIndex((ushort)(_next + 2));
            _mesh.SetNextIndex((ushort)(_next + 3));
            _mesh.SetNextIndex(_next);
            _next += 4;
        }

        public void Rect(Rect r, Color32 color) => Rect(r.xMin, r.yMin, r.xMax, r.yMax, color);
    }

    internal static class MeterColors
    {
        public static readonly Color32 Background = new Color32(24, 24, 24, 255);
        public static readonly Color32 Grid = new Color32(70, 70, 70, 255);
        public static readonly Color32 Fill = new Color32(90, 200, 255, 255);
        public static readonly Color32 FillDim = new Color32(40, 90, 120, 255);
        public static readonly Color32 Marker = new Color32(120, 255, 140, 255);
        public static readonly Color32 Envelope = new Color32(255, 200, 40, 255);
        public static readonly Color32 Octave = new Color32(230, 230, 230, 255);
    }
}
