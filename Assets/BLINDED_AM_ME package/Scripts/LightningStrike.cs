using UnityEngine;
using System.Collections;

namespace BLINDED_AM_ME{

	public class LightningStrike{


		private static Vector3 pathPointA = Vector3.zero;
		private static Vector3 pathPointB = Vector3.zero;
		private static float vertexDistance = 0;
		private static Vector3 vertexPoint = Vector3.zero;
		
		private static Vector3 segmentDirection = Vector3.zero;
		private static Vector3 perpendicularDir = Vector3.zero;
		private static Vector3 perpendicularDirAlt = Vector3.zero;
		
		private static Vector2 newOffset = Vector2.zero;
		private static Vector2 previousOffset = Vector2.zero;

		/// <summary>
		/// Strike the specified path of 2 or more points in worldspace
		/// </summary>
		/// <param name="path">Needs 2 or more points in worldspace.</param>
		/// <param name="lineObject">The LineRenderer to set with points.</param>
		/// <param name="zigZagIntensity">How far off the path the zigzag can go in meters.</param>
		/// <param name="zigZagPerMeter">zigZag per meter.</param>
		/// <param name="smoothness">1 makes it straight, 0 makes it crazy.</param>
		public static void Strike(Vector3[] path = null, LineRenderer lineObject = null,
		                                  float zigZagIntensity = 1.0f, 
		                                  float zigZagPerMeter = 2.0f, 
		                                  float smoothness = 0.5f){

			smoothness = Mathf.Clamp(smoothness, 0.01f, 1.0f);
			zigZagIntensity = Mathf.Clamp(zigZagIntensity, 0.01f, 100.0f);
			zigZagPerMeter = Mathf.Clamp(zigZagPerMeter, 0.01f, 1000.0f);

			
			float distance = 0.0f;
			for(int i=0; i<path.Length-1; i++)
				distance += Vector3.Distance(path[i], path[i+1]);

			int vertexCount = Mathf.CeilToInt(distance * zigZagPerMeter);

			lineObject.material.SetTextureScale("_MainTex", new Vector2(distance * zigZagPerMeter, 1.0f));
			lineObject.SetVertexCount(vertexCount);
			
			
			// get the segment groups through distances
			float[] pathGroups = new float[path.Length-1];
			for(int i=0; i<path.Length-1; i++){
				pathGroups[i] = Vector3.Distance(path[i], path[i+1]);
				if(i>0)
					pathGroups[i] += pathGroups[i-1];
			}
			
			// set the points
			lineObject.SetPosition(0, path[0]);
			lineObject.SetPosition(vertexCount-1, path[path.Length-1]);

			previousOffset = Vector2.zero;

			for(int i=1; i<vertexCount-1; i++){

				// distance from first point along path
				vertexDistance = ((float) i / (float) vertexCount) * distance;
				
				// find the segment this one belongs to
				for(int k=0; k<pathGroups.Length; k++)
				if(pathGroups[k] > vertexDistance){
					pathPointA = path[k];
					pathPointB = path[k+1];

					// convert to distance from segment's first point
					if(k > 0)
						vertexDistance -= pathGroups[k-1];
					
					break;
				}

				// dir = targetPosition - currentPosition normalized
				segmentDirection = (pathPointB - pathPointA).normalized;
				// in world space
				vertexPoint = pathPointA + (segmentDirection * vertexDistance);

				// 90 degree turn call it LEFT
				perpendicularDir.x = segmentDirection.y;
				perpendicularDir.y = -segmentDirection.x;
				perpendicularDir.z = segmentDirection.z;

				// 90 degree turn call it UP
				perpendicularDirAlt = Vector3.Cross(perpendicularDir,segmentDirection);


				newOffset.x = Random.Range(-1.0f, 1.0f);
				newOffset.y = Random.Range(-1.0f, 1.0f);

				// smooth it
				if(Vector2.Distance(newOffset, previousOffset) > (1.01f - smoothness)){
					newOffset += (previousOffset - newOffset).normalized * (Vector2.Distance(newOffset, previousOffset) - (1.01f - smoothness));
				}
				previousOffset = newOffset;

				// not normalized to keep its randomness
				newOffset *= zigZagIntensity;
				
				vertexPoint += perpendicularDir * newOffset.x;
				vertexPoint += perpendicularDirAlt * newOffset.y;

				lineObject.SetPosition(i, vertexPoint);
				
			}

		}
	}
}