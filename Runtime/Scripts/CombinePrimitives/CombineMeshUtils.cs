using System;
using System.Collections.Generic;
using UnityEngine;

namespace GLTFast
{
	public class CombineMeshUtils
	{
		class BlendShapeFrame
		{
			internal Vector3[] Vertices { get; }
			internal Vector3[] Normals { get; }
			internal Vector3[] Tangents { get; }
			int VertexIndex { get; set; }
			internal float Weight { get; set; }
			internal int Index { get; }
			internal string Name { get; }

			internal BlendShapeFrame(string name, int index, int vertexCount)
			{
				Name = name;
				Index = index;
				Vertices = new Vector3[vertexCount];
				Normals = new Vector3[vertexCount];
				Tangents = new Vector3[vertexCount];
			}

			internal void AddVertices(Vector3[] vertices, Vector3[] normals, Vector3[] tangents)
			{
				Array.Copy(vertices, 0, Vertices, VertexIndex, vertices.Length);
				Array.Copy(normals, 0, Normals, VertexIndex, normals.Length);
				Array.Copy(tangents, 0, Tangents, VertexIndex, tangents.Length);
				VertexIndex += vertices.Length;
			}
		}

		internal static void CombineBlendShapes(Mesh combinedMesh, List<PrimitiveData> primitiveList)
		{
			var frames = new List<BlendShapeFrame>();

			BlendShapeFrame GetBlendShapeFrame(string name, int index)
			{
				foreach (BlendShapeFrame frame in frames)
				{
					if (frame.Name == name && frame.Index == index)
						return frame;
				}

				var newFrame = new BlendShapeFrame(name, index, combinedMesh.vertexCount);
				frames.Add(newFrame);
				return newFrame;
			}

			foreach (PrimitiveData data in primitiveList)
			{
				Mesh primitiveMesh = data.Mesh;
				int vertexCount = primitiveMesh.vertexCount;
				var vertices = new Vector3[vertexCount];
				var normals = new Vector3[vertexCount];
				var tangents = new Vector3[vertexCount];

				for (var shapeIndex = 0; shapeIndex < primitiveMesh.blendShapeCount; shapeIndex++)
				{
					string shapeName = primitiveMesh.GetBlendShapeName(shapeIndex);

					for (var frameIndex = 0; frameIndex < primitiveMesh.GetBlendShapeFrameCount(shapeIndex); frameIndex++)
					{
						float weight = primitiveMesh.GetBlendShapeFrameWeight(shapeIndex, frameIndex);

						primitiveMesh.GetBlendShapeFrameVertices(shapeIndex, frameIndex, vertices, normals, tangents);
						BlendShapeFrame frame = GetBlendShapeFrame(shapeName, frameIndex);
						frame.Weight = weight;
						frame.AddVertices(vertices, normals, tangents);
					}
				}
			}

			foreach (BlendShapeFrame frame in frames)
			{
				combinedMesh.AddBlendShapeFrame(frame.Name, frame.Weight, frame.Vertices, frame.Normals, frame.Tangents);
			}
		}
	}
}