using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace BLINDED_AM_ME{

	[RequireComponent(typeof(MeshFilter))]
	[RequireComponent(typeof(MeshRenderer))]
	[RequireComponent(typeof(Trail_Path))]
	public class Trail_Mesh : MonoBehaviour {

		// Mesh Values
		private List<Vector3>   _vertices  = new List<Vector3>();
		private List<Vector3>   _normals   = new List<Vector3>();
		private List<Vector2>   _uvs       = new List<Vector2>();
		private List<int>       _triangles = new List<int>();
		private List<List<int>> _subIndices = new List<List<int>>();

		public bool   shouldOptimizeMesh = false; // removes doubles
		public Mesh   segment_sourceMesh;
		private float _segment_length;
		private float _segment_MinZ;
		private float _segment_MaxZ;

		private Trail_Path _path;
		private Transform  _helpTransform1;
		private Transform  _helpTransform2;
		private int numDoubles = 0;

		// called by
		public void ShapeIt(){

			if(segment_sourceMesh == null){
				Debug.LogError("missing source mesh");
				return;
			}

			_helpTransform1 = new GameObject("_helpTransform1").transform;
			_helpTransform2 = new GameObject("_helpTransform2").transform;

			// because it messes it up
			Quaternion oldRotation = transform.rotation;
			transform.rotation = Quaternion.identity;

			ResetLists();
			ScanSourceMesh();
			Craft(); // make segments
			Apply(); // apply values

			transform.rotation = oldRotation;

			DestroyImmediate(_helpTransform1.gameObject);
			DestroyImmediate(_helpTransform2.gameObject);
		}

		public void Craft(){

			_path = GetComponent<Trail_Path>();
			numDoubles = 0;
			Trail_Path_Point pointA = _path.GetPathPoint(0.0f);
			Trail_Path_Point pointB = pointA;
		

			for(float dist=0.0f; dist<_path.TotalDistance; dist+=_segment_length){
				
				pointB = _path.GetPathPoint(Mathf.Clamp(dist + _segment_length,0,_path.TotalDistance));

				_helpTransform1.rotation = Quaternion.LookRotation(pointA.forward, pointA.up);
				_helpTransform1.position = transform.TransformPoint(pointA.point);

				_helpTransform2.rotation = Quaternion.LookRotation(pointB.forward, pointB.up);
				_helpTransform2.position = transform.TransformPoint(pointB.point);

				Add_Segment();

				pointA = pointB;
			}

			if(shouldOptimizeMesh)
				Debug.Log("Double verts removed:" + numDoubles);
		}

		public void Add_Segment(){

			int[] indices;

			// go throught the submeshes
			for(int sub=0; sub<segment_sourceMesh.subMeshCount; sub++){
				indices = segment_sourceMesh.GetIndices(sub);
				for(int i=0; i<indices.Length; i+=3){

					AddTriangle(new int[]{
						indices[i],
	                    indices[i+1],
	                    indices[i+2]
					},sub);
				}
			}
		}

		public void AddTriangle( int[] indices, int submesh){

			// vertices
			Vector3[] verts = new Vector3[3]{
				segment_sourceMesh.vertices[indices[0]],
				segment_sourceMesh.vertices[indices[1]],
				segment_sourceMesh.vertices[indices[2]]
			};
			// normals
			Vector3[] norms = new Vector3[3]{

				segment_sourceMesh.normals[indices[0]],
				segment_sourceMesh.normals[indices[1]],
				segment_sourceMesh.normals[indices[2]]
			};

			// apply offset
			float lerpValue = 0.0f;
			Vector3 pointA, pointB;
			Vector3 normA, normB;
			for(int i=0; i<3; i++){

				lerpValue = Math_Functions.Value_from_another_Scope(verts[i].z, _segment_MinZ, _segment_MaxZ, 0.0f, 1.0f);
				verts[i].z = 0.0f;
					
				pointA = _helpTransform1.TransformPoint(verts[i]); // to world
				pointB = _helpTransform2.TransformPoint(verts[i]);

				verts[i] = transform.InverseTransformPoint(Vector3.Lerp(pointA, pointB,lerpValue)); // to local

				normA = _helpTransform1.TransformDirection(norms[i]);
				normB = _helpTransform2.TransformDirection(norms[i]);

				norms[i] = transform.InverseTransformDirection(Vector3.Lerp(normA, normB, lerpValue));


				// is this new?
				int index = -1;

				if(shouldOptimizeMesh)
				for(int iterator=0; iterator<_vertices.Count; iterator++){
					if(_vertices[iterator] == verts[i] &&
						_normals[iterator] == norms[i] &&
						_uvs[iterator] == segment_sourceMesh.uv[indices[i]]){
						index = iterator;
						break;
					}
				}

				// it is
				if(index == -1){

					_subIndices[submesh].Add(_vertices.Count);
					_triangles.Add(_vertices.Count);
					_vertices.Add(verts[i]);
					_normals.Add(norms[i]);
					_uvs.Add(segment_sourceMesh.uv[indices[i]]);


				}else{ // it is not
					numDoubles++;
					_subIndices[submesh].Add(index);
					_triangles.Add(index);
				}

			}

		}

		public void ResetLists(){

			_vertices.Clear();
			_normals.Clear();
			_uvs.Clear();
			_triangles.Clear();
			_subIndices.Clear();
			for(int sub=0; sub<segment_sourceMesh.subMeshCount; sub++){
				_subIndices.Add(new List<int>());
			}

		}

		public void ScanSourceMesh(){

			float min_z = 0.0f, max_z = 0.0f;

			// find length
			for(int i=0; i<segment_sourceMesh.vertexCount;i++){

				Vector3 vert = segment_sourceMesh.vertices[i];
				if(vert.z < min_z)
					min_z = vert.z;

				if(vert.z > max_z)
					max_z = vert.z;
			}

			_segment_MinZ = min_z;
			_segment_MaxZ = max_z;
			_segment_length = max_z - min_z;

		}

		public void Apply(){

			Mesh trail = new Mesh();
			trail.name =  "Trail";
			trail.vertices  = _vertices.ToArray();
			trail.triangles = _triangles.ToArray();
			trail.normals   = _normals.ToArray();
			trail.uv        = _uvs.ToArray();

			trail.subMeshCount = segment_sourceMesh.subMeshCount;
			for(int i=0; i<trail.subMeshCount; i++)
				trail.SetIndices(_subIndices[i].ToArray(), MeshTopology.Triangles, i);	

			#if UNITY_EDITOR
			// for light mapping
			UnityEditor.Unwrapping.GenerateSecondaryUVSet(trail);

			#endif

			GetComponent<MeshFilter>().mesh = trail;

		}

	}
}