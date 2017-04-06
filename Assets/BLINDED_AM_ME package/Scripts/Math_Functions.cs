using UnityEngine;
using System.Collections;

namespace BLINDED_AM_ME{

	public class Math_Functions{
		
		// one scoped value to another scope
		
		// values need to be floats
		
		// a <= x <= b 
		
		// 	0 <= (x-a)/(b-a) <=1
		
		// 	new_value = ( (old_value - old_min) / (old_max - old_min) ) * (new_max - new_min) + newmin

		public static float Value_from_another_Scope(float value, float oldMin, float oldMax, float newMin, float newMax)
		{
			float returnValue = 0;

			returnValue = ( (value - oldMin) / (oldMax - oldMin) ) * (newMax - newMin) + newMin;

			return returnValue;
		}

		// physics find the angle

		/**
	 * Checks if projectile can hit (x, y) coordinate with initial velocity length under given gravity.
	 * @param x
	 * @param y
	 * @param velocity initial velocity
	 * @param gravity gravity value; should be greater than 0
	 * @return
	 */


		public static bool CanHitPoint(float x, float y, float velocity, float gravity)
		{
			if(FindDelta(x, y, velocity, gravity) >= 0)
				return true;
			else
				return false;
		}
		
		public static Vector2 AngleToVector_Radians(float angle) // in radians
		{
			return new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)).normalized; // Vector2((float)Math.Cos(angle), (float)Math.Sin(angle));
		}
		
		public static float DegreesToRadians(float degrees)
		{
			return degrees * 0.01745329f;
		}

		public static Vector2 AngleToVector_Degrees(float angle)
		{
			float converted_angle = angle * 0.01745329f;

			return new Vector2(Mathf.Cos(converted_angle), Mathf.Sin(converted_angle)).normalized; // Vector2((float)Math.Cos(angle), (float)Math.Sin(angle));
		}
		
		public static float RadiansToDegrees(float rads)
		{
			return rads * 57.2957795f;
		}
		
		public static float PHYSICS_Find_Needed_Angle1_radians(float x, float y, float velocity, float gravity)
		{
			float gravity_for_clearity = gravity * -1.0f;

			return Mathf.Atan((Mathf.Pow(velocity, 2.0f) + Mathf.Sqrt( Mathf.Pow(velocity, 4.0f) - (gravity_for_clearity * ((gravity_for_clearity * Mathf.Pow(x, 2.0f)) + (2.0f * y * Mathf.Pow(velocity, 2.0f))))))/(gravity_for_clearity * x));    
		}
		
		public static float PHYSICS_Find_Needed_Angle2_radians(float x, float y, float velocity, float gravity)
		{
			float gravity_for_clearity = gravity * -1.0f;

			return Mathf.Atan((Mathf.Pow(velocity, 2.0f) - Mathf.Sqrt( Mathf.Pow(velocity, 4.0f) - (gravity_for_clearity * ((gravity_for_clearity * Mathf.Pow(x, 2.0f)) + (2.0f*y*Mathf.Pow(velocity, 2.0f))))))/(gravity_for_clearity * x));    
		}

		public static float PHYSICS_Find_Needed_Angle1_degrees(float x, float y, float velocity, float gravity)
		{
			float gravity_for_clearity = gravity * -1.0f;

			return Mathf.Atan((Mathf.Pow(velocity, 2.0f) + Mathf.Sqrt( Mathf.Pow(velocity, 4.0f) - (gravity_for_clearity * ((gravity_for_clearity * Mathf.Pow(x, 2.0f)) + (2.0f * y * Mathf.Pow(velocity, 2.0f))))))/(gravity_for_clearity * x)) * 57.2957795f;    
		}
		
		public static float PHYSICS_Find_Needed_Angle2_degrees(float x, float y, float velocity, float gravity)
		{
			float gravity_for_clearity = gravity * -1.0f;

			return Mathf.Atan((Mathf.Pow(velocity, 2.0f) - Mathf.Sqrt( Mathf.Pow(velocity, 4.0f) - (gravity_for_clearity * ((gravity_for_clearity * Mathf.Pow(x, 2.0f)) + (2.0f * y * Mathf.Pow(velocity, 2.0f))))))/(gravity_for_clearity * x)) * 57.2957795f;    
		}
		
		private static float FindDelta(float x, float y, float velocity, float gravity)
		{    
			float gravity_for_clearity = gravity * -1.0f;

			return velocity * velocity * velocity * velocity - gravity_for_clearity * (gravity_for_clearity * x * x + 2.0f * y * velocity * velocity);
		}

		public static Vector3 Point_In_Triangle(Vector2 uv, Vector3 point1, Vector3 point2, Vector3 point3){

			//point = (1 - sqrt(u)) * A + (sqrt(u) * (1 - v)) * B + (sqrt(u) * v) * C
			
			Vector3 point =  (1.0f - Mathf.Sqrt(uv.x)) * point1;
			point += (Mathf.Sqrt(uv.x) * (1.0f - uv.y)) * point2;
			point += (Mathf.Sqrt(uv.x) * uv.y) * point3;

			return point;
		
		}

		public static Vector3 Rotate_Direction(Vector3 dir, Vector3 axis, float angle){

			return Quaternion.AngleAxis(angle, axis) * dir;
		}

		public static Vector3 ScaleIndirection(Vector3 origScale, float scaling, Vector3 normal){
			
			return origScale + (scaling - 1.0f) * Vector3.Project(origScale, normal);
			
		}

		public static Vector3 CatmullRom(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float i)
		{
			// from Standard Assets
			// comments are no use here... it's the catmull-rom equation.
			// Un-magic this, lord vector!
			return 0.5f * ((2f*p1) + (-p0 + p2)*i + (2f*p0 - 5f*p1 + 4f*p2 - p3)*i*i + (-p0 + 3f*p1 - 3f*p2 + p3)*i*i*i);
		}
	}
}