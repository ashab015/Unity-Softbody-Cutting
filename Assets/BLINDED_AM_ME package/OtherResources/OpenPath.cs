using UnityEngine;
using System.Collections;

/*
 * Original Circuit script found in Standard assets
 * 
 */

namespace BLINDED_AM_ME{

	public struct RoutePoint
	{
		public Vector3 position;
		public Vector3 direction;


		public RoutePoint(Vector3 position, Vector3 direction)
		{
			this.position = position;
			this.direction = direction;
		}
	}

	public class OpenPath : MonoBehaviour {

		#region Standard assets variables
		// standard assets
		[SerializeField] private bool smoothRoute = true;

		public Transform[] Waypoints;

		private int       points_num;
		private Vector3[] points_positions;
		private float[]   points_distances;

		[HideInInspector]
		public float      TotalDistance;

		public float editorVisualisationSubsteps = 100;

		//this being here will save GC allocs
		private int p0n;
		private int p1n;
		private int p2n;
		private int p3n;

		private float i;
		private Vector3 P0;
		private Vector3 P1;
		private Vector3 P2;
		private Vector3 P3;

		#endregion		// Use this for initialization
		void Awake () {

			Waypoints = new Transform[transform.childCount];
			for(int i=0; i< transform.childCount; i++){
				Waypoints[i] = transform.GetChild(i);
				Waypoints[i].gameObject.name = "point " + i;
			}
			points_num = Waypoints.Length;

			if (points_num > 1)
			{
				CachePositionsAndDistances();
			}
		}


		#region Standard assets section

		public RoutePoint GetRoutePoint(float dist)
		{
			// position and direction
			Vector3 p1 = GetRoutePosition(dist);
			Vector3 p2 = GetRoutePosition(dist + 0.1f);
			Vector3 delta = p2 - p1;
			return new RoutePoint(p1, delta.normalized);
		}


		public Vector3 GetRoutePosition(float dist)
		{

			dist = Mathf.Clamp(dist, 0.0f, TotalDistance);

			if(dist <= 0)
				return points_positions[0];
			else if(dist >= TotalDistance)
				return points_positions[points_num-1];

			int point = 0;
			while (points_distances[point] < dist)
			{
				point++;
			}

			// get nearest two points, ensuring points wrap-around start & end of circuit
			p1n = point - 1;
			p2n = point;

			// found point numbers, now find interpolation value between the two middle points

			i = Mathf.InverseLerp(points_distances[p1n], points_distances[p2n], dist);

			if (smoothRoute)
			{
				p0n = Mathf.Clamp(point - 2, 0, points_num-1);
				p3n = Mathf.Clamp(point + 1, 0, points_num-1);


				P0 = points_positions[p0n];
				P1 = points_positions[p1n];
				P2 = points_positions[p2n];
				P3 = points_positions[p3n];

				return Math_Functions.CatmullRom(P0, P1, P2, P3, i);
			}
			else
			{

				return Vector3.Lerp(points_positions[p1n], points_positions[p2n], i);
			}
		}


		private void CachePositionsAndDistances()
		{
			// transfer the position of each point and distances between points to arrays for
			// speed of lookup at runtime
			points_positions = new Vector3[points_num];
			points_distances = new float[points_num];

			float accumulateDistance = 0;
			for (int i = 0; i < points_num-1; ++i)
			{
				var t1 = Waypoints[i];
				var t2 = Waypoints[i + 1];
				if (t1 != null && t2 != null)
				{
					Vector3 p1 = t1.localPosition;
					Vector3 p2 = t2.localPosition;
					points_positions[i] = Waypoints[i].localPosition;
					points_distances[i] = accumulateDistance;
					accumulateDistance += (p1 - p2).magnitude;
				}
			}

			points_positions[points_num-1] = Waypoints[points_num-1].localPosition;
			points_distances[points_num-1] = accumulateDistance;

			TotalDistance = accumulateDistance;
		}


		private void OnDrawGizmos()
		{
			DrawGizmos(false);
		}


		private void OnDrawGizmosSelected()
		{
			DrawGizmos(true);
		}


		private void DrawGizmos(bool selected)
		{
			
			Waypoints = new Transform[transform.childCount];
			for(int i=0; i< transform.childCount; i++){
				Waypoints[i] = transform.GetChild(i);
				Waypoints[i].gameObject.name = "point " + i;
			}
			points_num = Waypoints.Length;


			if (points_num > 1)
			{
				CachePositionsAndDistances();
			}

			if(points_num <= 1)
				return;


			Gizmos.color = selected ? Color.yellow : new Color(1, 1, 0, 0.5f);
			Vector3 prev = Waypoints[0].position;
			if (smoothRoute)
			{
				for (float dist = 0; dist < TotalDistance; dist += TotalDistance/editorVisualisationSubsteps)
				{
					Vector3 next = transform.TransformPoint(GetRoutePosition(dist + 1));
					Gizmos.DrawLine(prev, next);
					prev = next;
				}

			}
			else
			{
				for (int n = 0; n < points_num-1; ++n)
				{
					Vector3 next = Waypoints[n + 1].position;
					Gizmos.DrawLine(prev, next);
					prev = next;
				}
			}

		}
			

		#endregion
	}
}