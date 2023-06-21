using System;
using System.Collections.Generic;
using System.Linq;
using GLTFast;
using GLTFast.Logging;
using Unity.Collections;
using UnityEngine;

namespace GLTFast
{
	internal struct PrimitiveData
	{
		internal uint NodeIndex { get; set; }
		internal string MeshName { get; set; }
		internal Mesh Mesh { get; set; }
		internal int[] MaterialIndices { get; set; }
		internal uint[] Joints { get; set; }
		internal uint? RootJoint { get; set; }
		internal float[] MorphTargetWeights { get; set; }
	}

	public class CombineMeshInstantiator : GameObjectInstantiator
	{
		private readonly Dictionary<uint, List<PrimitiveData>> _primitives = new Dictionary<uint, List<PrimitiveData>>();

		public CombineMeshInstantiator(IGltfReadable gltf, Transform parent, ICodeLogger logger = null, InstantiationSettings settings = null) : base(gltf, parent, logger,
			settings)
		{
		}

		public bool TryGetNodeObject(uint nodeId, out GameObject node)
		{
			return m_Nodes.TryGetValue(nodeId, out node);
		}

		public override void AddPrimitive(uint nodeIndex, string meshName, Mesh mesh, int[] materialIndices, uint[] joints = null, uint? rootJoint = null,
			float[] morphTargetWeights = null,
			int primitiveNumeration = 0)
		{
			if ((m_Settings.Mask & ComponentType.Mesh) == 0)
				return;

			if (!_primitives.TryGetValue(nodeIndex, out List<PrimitiveData> primitives))
			{
				primitives = new List<PrimitiveData>();
				_primitives.Add(nodeIndex, primitives);
			}

			primitives.Add(new PrimitiveData
			{
				NodeIndex = nodeIndex,
				MeshName = meshName,
				Mesh = mesh,
				MaterialIndices = materialIndices,
				Joints = joints,
				RootJoint = rootJoint,
				MorphTargetWeights = morphTargetWeights
			});
		}

		//TODO: check how to handle instanced meshes
		public override void AddPrimitiveInstanced(uint nodeIndex, string meshName, Mesh mesh, int[] materialIndices, uint instanceCount, NativeArray<Vector3>? positions,
			NativeArray<Quaternion>? rotations,
			NativeArray<Vector3>? scales, int primitiveNumeration = 0)
		{
			base.AddPrimitiveInstanced(nodeIndex, meshName, mesh, materialIndices, instanceCount, positions, rotations, scales, primitiveNumeration);
		}

		public override void EndScene(uint[] rootNodeIndices)
		{
			foreach (uint nodeIndex in _primitives.Keys)
			{
				List<PrimitiveData> primitiveList = _primitives[nodeIndex];
				PrimitiveData first = primitiveList[0];

				if (primitiveList.Count > 1)
				{
					var combine = new CombineInstance[primitiveList.Count];
					var materialIndices = new int[primitiveList.Count];
					uint[] joints = null;
					bool hasMorphTargets = first.MorphTargetWeights != null;

					if (first.Joints != null)
						joints = primitiveList.SelectMany(p => p.Joints).ToArray();

					for (var i = 0; i < primitiveList.Count; i++)
					{
						PrimitiveData data = primitiveList[i];
						Mesh primitiveMesh = data.Mesh;
						combine[i] = new CombineInstance { mesh = primitiveMesh };
						materialIndices[i] = data.MaterialIndices[0];
					}

					var mesh = new Mesh { name = first.MeshName };

					mesh.CombineMeshes(combine, false, false);

					if (hasMorphTargets)
						CombineMeshUtils.CombineBlendShapes(mesh, primitiveList);

					first.Mesh = mesh;
					first.MaterialIndices = materialIndices;
					first.Joints = joints;
				}

				base.AddPrimitive(nodeIndex, first.MeshName, first.Mesh, first.MaterialIndices, first.Joints, first.RootJoint, first.MorphTargetWeights, 0);
			}

			base.EndScene(rootNodeIndices);
		}
	}
}