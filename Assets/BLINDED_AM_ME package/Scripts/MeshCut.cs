using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace BLINDED_AM_ME{

	public class MeshCut{


		public class MeshCutSide{

			public List<Vector3>  vertices  = new List<Vector3>();
			public List<Vector3>  normals   = new List<Vector3>();
			public List<Vector2>  uvs       = new List<Vector2>();
			public List<int>      triangles = new List<int>();
			public List<List<int>> subIndices = new List<List<int>>();


			public void ClearAll(){

				vertices.Clear();
				normals.Clear();
				uvs.Clear();
				triangles.Clear();
				subIndices.Clear();
			}

			public void AddTriangle( int p1, int p2, int p3, int submesh){

				// triangle index order goes 1,2,3,4....

				int base_index = vertices.Count;

				subIndices[submesh].Add(base_index);
				subIndices[submesh].Add(base_index+1);
				subIndices[submesh].Add(base_index+2);

				triangles.Add(base_index);
				triangles.Add(base_index+1);
				triangles.Add(base_index+2);

				vertices.Add(victim_mesh.vertices[p1]);
				vertices.Add(victim_mesh.vertices[p2]);
				vertices.Add(victim_mesh.vertices[p3]);

				normals.Add(victim_mesh.normals[p1]);
				normals.Add(victim_mesh.normals[p2]);
				normals.Add(victim_mesh.normals[p3]);

				uvs.Add(victim_mesh.uv[p1]);
				uvs.Add(victim_mesh.uv[p2]);
				uvs.Add(victim_mesh.uv[p3]);

			}

			public void AddTriangle(Vector3[] points3, Vector3[] normals3, Vector2[] uvs3, Vector3 faceNormal, int submesh){

				Vector3 calculated_normal = Vector3.Cross((points3[1] - points3[0]).normalized, (points3[2] - points3[0]).normalized);

				int p1 = 0;
				int p2 = 1;
				int p3 = 2;

				if(Vector3.Dot(calculated_normal, faceNormal) < 0){

					p1 = 2;
					p2 = 1;
					p3 = 0;
				}

				int base_index = vertices.Count;

				subIndices[submesh].Add(base_index);
				subIndices[submesh].Add(base_index+1);
				subIndices[submesh].Add(base_index+2);

				triangles.Add(base_index);
				triangles.Add(base_index+1);
				triangles.Add(base_index+2);

				vertices.Add(points3[p1]);
				vertices.Add(points3[p2]);
				vertices.Add(points3[p3]);

				normals.Add(normals3[p1]);
				normals.Add(normals3[p2]);
				normals.Add(normals3[p3]);

				uvs.Add(uvs3[p1]);
				uvs.Add(uvs3[p2]);
				uvs.Add(uvs3[p3]);
			}

		}

		private static MeshCutSide left_side = new MeshCutSide();
		private static MeshCutSide right_side = new MeshCutSide();

		private static Plane blade;
		private static Mesh victim_mesh;

		// capping stuff
		private static List<Vector3> new_vertices = new List<Vector3>();


		/// <summary>
		/// Cut the specified victim, blade_plane and capMaterial.
		/// </summary>
		/// <param name="victim">Victim.</param>
		/// <param name="blade_plane">Blade plane.</param>
		/// <param name="capMaterial">Cap material.</param>
		public static GameObject[] Cut(GameObject victim, Vector3 anchorPoint, Vector3 normalDirection, Material capMaterial){

			// set the blade relative to victim
			blade = new Plane(victim.transform.InverseTransformDirection(-normalDirection),
				victim.transform.InverseTransformPoint(anchorPoint));

			// get the victims mesh
			victim_mesh = victim.GetComponent<MeshFilter>().mesh;

			// reset values
			new_vertices.Clear();
			left_side.ClearAll();
			right_side.ClearAll();


			bool[] sides = new bool[3];
			int[] indices;
			int   p1,p2,p3;

			// go throught the submeshes
			for(int sub=0; sub<victim_mesh.subMeshCount; sub++){

				indices = victim_mesh.GetIndices(sub);

				left_side.subIndices.Add(new List<int>());
				right_side.subIndices.Add(new List<int>());

				for(int i=0; i<indices.Length; i+=3){

					p1 = indices[i];
					p2 = indices[i+1];
					p3 = indices[i+2];

					sides[0] = blade.GetSide(victim_mesh.vertices[p1]);
					sides[1] = blade.GetSide(victim_mesh.vertices[p2]);
					sides[2] = blade.GetSide(victim_mesh.vertices[p3]);


					// whole triangle
					if(sides[0] == sides[1] && sides[0] == sides[2]){

						if(sides[0]){ // left side
							left_side.AddTriangle(p1,p2,p3,sub);
						}else{
							right_side.AddTriangle(p1,p2,p3,sub);
						}

					}else{ // cut the triangle
						
						Cut_this_Face(sub, sides, p1, p2, p3);
					}
				}
			}


			Material[] mats = victim.GetComponent<MeshRenderer>().sharedMaterials;

			if(mats[mats.Length-1].name != capMaterial.name){ // add cap indices

				left_side.subIndices.Add(new List<int>());
				right_side.subIndices.Add(new List<int>());

				Material[] newMats = new Material[mats.Length+1];
				mats.CopyTo(newMats, 0);
				newMats[mats.Length] = capMaterial;
				mats = newMats;
			}




			// cap the opennings
			Capping();


			// Left Mesh

			Mesh left_HalfMesh = new Mesh();
			left_HalfMesh.name =  "Split Mesh Left";
			left_HalfMesh.vertices  = left_side.vertices.ToArray();
			left_HalfMesh.triangles = left_side.triangles.ToArray();
			left_HalfMesh.normals   = left_side.normals.ToArray();
			left_HalfMesh.uv        = left_side.uvs.ToArray();

			left_HalfMesh.subMeshCount = left_side.subIndices.Count;
			for(int i=0; i<left_side.subIndices.Count; i++)
				left_HalfMesh.SetIndices(left_side.subIndices[i].ToArray(), MeshTopology.Triangles, i);	

			// Right Mesh

			Mesh right_HalfMesh = new Mesh();
			right_HalfMesh.name = "Split Mesh Right";
			right_HalfMesh.vertices  = right_side.vertices.ToArray();
			right_HalfMesh.triangles = right_side.triangles.ToArray();
			right_HalfMesh.normals   = right_side.normals.ToArray();
			right_HalfMesh.uv        = right_side.uvs.ToArray();

			right_HalfMesh.subMeshCount = right_side.subIndices.Count;
			for(int i=0; i<right_side.subIndices.Count; i++)
				right_HalfMesh.SetIndices(right_side.subIndices[i].ToArray(), MeshTopology.Triangles, i);

			// assign the game objects

			victim.name = "left side";
			victim.GetComponent<MeshFilter>().mesh = left_HalfMesh;

			GameObject leftSideObj = victim;

			GameObject rightSideObj = new GameObject("right side", typeof(MeshFilter), typeof(MeshRenderer));
			rightSideObj.transform.position = victim.transform.position;
			rightSideObj.transform.rotation = victim.transform.rotation;
			rightSideObj.GetComponent<MeshFilter>().mesh = right_HalfMesh;
		

			// assign mats
			leftSideObj.GetComponent<MeshRenderer>().materials = mats;
			rightSideObj.GetComponent<MeshRenderer>().materials = mats;

			return new GameObject[]{ leftSideObj, rightSideObj };

		}
			

		static void Cut_this_Face(int submesh, bool[] sides, int index1, int index2, int index3){


			Vector3[] leftPoints = new Vector3[2];
			Vector3[] leftNormals = new Vector3[2];
			Vector2[] leftUvs = new Vector2[2];
			Vector3[] rightPoints = new Vector3[2];
			Vector3[] rightNormals = new Vector3[2];
			Vector2[] rightUvs = new Vector2[2];

			bool didset_left = false;
			bool didset_right = false;

			int p = index1;
			for(int side=0; side<3; side++){

				switch(side){
				case 0: p = index1;
					break;
				case 1: p = index2;
					break;
				case 2: p = index3;
					break;

				}

				if(sides[side]){
					if(!didset_left){
						didset_left = true;

						leftPoints[0]   = victim_mesh.vertices[p];
						leftPoints[1]   = leftPoints[0];
						leftUvs[0]     = victim_mesh.uv[p];
						leftUvs[1]     = leftUvs[0];
						leftNormals[0] = victim_mesh.normals[p];
						leftNormals[1] = leftNormals[0];

					}else{

						leftPoints[1]    = victim_mesh.vertices[p];
						leftUvs[1]      = victim_mesh.uv[p];
						leftNormals[1]  = victim_mesh.normals[p];

					}
				}else{
					if(!didset_right){
						didset_right = true;

						rightPoints[0]   = victim_mesh.vertices[p];
						rightPoints[1]   = rightPoints[0];
						rightUvs[0]     = victim_mesh.uv[p];
						rightUvs[1]     = rightUvs[0];
						rightNormals[0] = victim_mesh.normals[p];
						rightNormals[1] = rightNormals[0];

					}else{

						rightPoints[1]   = victim_mesh.vertices[p];
						rightUvs[1]     = victim_mesh.uv[p];
						rightNormals[1] = victim_mesh.normals[p];

					}
				}
			}


			float normalizedDistance = 0.0f;
			float distance = 0;
			blade.Raycast(new Ray(leftPoints[0], (rightPoints[0] - leftPoints[0]).normalized), out distance);

			normalizedDistance =  distance/(rightPoints[0] - leftPoints[0]).magnitude;
			Vector3 newVertex1 = Vector3.Lerp(leftPoints[0], rightPoints[0], normalizedDistance);
			Vector2 newUv1     = Vector2.Lerp(leftUvs[0], rightUvs[0], normalizedDistance);
			Vector3 newNormal1 = Vector3.Lerp(leftNormals[0] , rightNormals[0], normalizedDistance);

			new_vertices.Add(newVertex1);

			blade.Raycast(new Ray(leftPoints[1], (rightPoints[1] - leftPoints[1]).normalized), out distance);

			normalizedDistance =  distance/(rightPoints[1] - leftPoints[1]).magnitude;
			Vector3 newVertex2 = Vector3.Lerp(leftPoints[1], rightPoints[1], normalizedDistance);
			Vector2 newUv2     = Vector2.Lerp(leftUvs[1], rightUvs[1], normalizedDistance);
			Vector3 newNormal2 = Vector3.Lerp(leftNormals[1] , rightNormals[1], normalizedDistance);

			new_vertices.Add(newVertex2);


			left_side.AddTriangle(new Vector3[]{leftPoints[0], newVertex1, newVertex2},
				new Vector3[]{leftNormals[0], newNormal1, newNormal2 },
				new Vector2[]{leftUvs[0], newUv1, newUv2}, newNormal1,
				submesh);

			left_side.AddTriangle(new Vector3[]{leftPoints[0], leftPoints[1], newVertex2},
				new Vector3[]{leftNormals[0], leftNormals[1], newNormal2},
				new Vector2[]{leftUvs[0], leftUvs[1], newUv2}, newNormal2,
				submesh);

			right_side.AddTriangle(new Vector3[]{rightPoints[0], newVertex1, newVertex2},
				new Vector3[]{rightNormals[0], newNormal1, newNormal2},
				new Vector2[]{rightUvs[0], newUv1, newUv2}, newNormal1,
				submesh);

			right_side.AddTriangle(new Vector3[]{rightPoints[0], rightPoints[1], newVertex2},
				new Vector3[]{rightNormals[0], rightNormals[1], newNormal2},
				new Vector2[]{rightUvs[0], rightUvs[1], newUv2}, newNormal2,
				submesh);

		}

		private static List<Vector3> capVertTracker = new List<Vector3>();
		private static List<Vector3> capVertpolygon = new List<Vector3>();

		static void Capping(){

			capVertTracker.Clear();

			for(int i=0; i<new_vertices.Count; i++)
				if(!capVertTracker.Contains(new_vertices[i]))
				{
					capVertpolygon.Clear();
					capVertpolygon.Add(new_vertices[i]);
					capVertpolygon.Add(new_vertices[i+1]);

					capVertTracker.Add(new_vertices[i]);
					capVertTracker.Add(new_vertices[i+1]);


					bool isDone = false;
					while(!isDone){
						isDone = true;

						for(int k=0; k<new_vertices.Count; k+=2){ // go through the pairs

							if(new_vertices[k] == capVertpolygon[capVertpolygon.Count-1] && !capVertTracker.Contains(new_vertices[k+1])){ // if so add the other

								isDone = false;
								capVertpolygon.Add(new_vertices[k+1]);
								capVertTracker.Add(new_vertices[k+1]);

							}else if(new_vertices[k+1] == capVertpolygon[capVertpolygon.Count-1] && !capVertTracker.Contains(new_vertices[k])){// if so add the other

								isDone = false;
								capVertpolygon.Add(new_vertices[k]);
								capVertTracker.Add(new_vertices[k]);
							}
						}
					}

					FillCap(capVertpolygon);

				}
			
		}

		static void FillCap(List<Vector3> vertices){


			// center of the cap
			Vector3 center = Vector3.zero;
			foreach(Vector3 point in vertices)
				center += point;

			center = center/vertices.Count;

			// you need an axis based on the cap
			Vector3 upward = Vector3.zero;
			// 90 degree turn
			upward.x = blade.normal.y;
			upward.y = -blade.normal.x;
			upward.z = blade.normal.z;
			Vector3 left = Vector3.Cross(blade.normal, upward);

			Vector3 displacement = Vector3.zero;
			Vector3 newUV1 = Vector3.zero;
			Vector3 newUV2 = Vector3.zero;

			for(int i=0; i<vertices.Count; i++){

				displacement = vertices[i] - center;
				newUV1 = Vector3.zero;
				newUV1.x = 0.5f + Vector3.Dot(displacement, left);
				newUV1.y = 0.5f + Vector3.Dot(displacement, upward);
				newUV1.z = 0.5f + Vector3.Dot(displacement, blade.normal);

				displacement = vertices[(i+1) % vertices.Count] - center;
				newUV2 = Vector3.zero;
				newUV2.x = 0.5f + Vector3.Dot(displacement, left);
				newUV2.y = 0.5f + Vector3.Dot(displacement, upward);
				newUV2.z = 0.5f + Vector3.Dot(displacement, blade.normal);

			//	uvs.Add(new Vector2(relativePosition.x, relativePosition.y));
			//	normals.Add(blade.normal);

				left_side.AddTriangle( new Vector3[]{
					vertices[i], vertices[(i+1) % vertices.Count], center
				},new Vector3[]{
					-blade.normal, -blade.normal, -blade.normal
				},new Vector2[]{
					newUV1, newUV2, new Vector2(0.5f, 0.5f)
				},-blade.normal, left_side.subIndices.Count-1);

				right_side.AddTriangle( new Vector3[]{
					vertices[i], vertices[(i+1) % vertices.Count], center
				},new Vector3[]{
					blade.normal, blade.normal, blade.normal
				},new Vector2[]{
					newUV1, newUV2, new Vector2(0.5f, 0.5f)
				},blade.normal, right_side.subIndices.Count-1);

			}
				

		}
	
	}
}