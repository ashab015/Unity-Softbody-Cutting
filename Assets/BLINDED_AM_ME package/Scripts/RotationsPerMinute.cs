using UnityEngine;
using System.Collections;

namespace BLINDED_AM_ME{

	public class RotationsPerMinute : MonoBehaviour {

		[Range(0.0f, 1.0f)]
		public float rpm_normal = 0.0f;
		public float maxRpm = 400.0f;
		public float blurRpmActivation = 50.0f;

		public Transform[] rotationObjects = new Transform[0];
		public Renderer    blurRenderer;

		// Use this for initialization
		void Start () {
		
		}

		private float rpm = 0.0f;

		// Update is called once per frame
		void Update () {

			rpm = (rpm_normal * maxRpm);

			if(rpm >= blurRpmActivation)
				blurRenderer.enabled = true;
			else
				blurRenderer.enabled = false;

			foreach(Transform spin in rotationObjects){

				spin.Rotate(Vector3.up,
					((rpm * 0.0166666667f) * 360.0f) * Time.deltaTime,
					Space.Self);
			}
		
		}

	}
}