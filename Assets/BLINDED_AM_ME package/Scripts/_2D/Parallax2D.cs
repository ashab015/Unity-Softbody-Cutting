using UnityEngine;
using System.Collections;

namespace BLINDED_AM_ME._2D{

	[ExecuteInEditMode]
	public class Parallax2D : MonoBehaviour {

		[System.Serializable]
		public struct TwoDeeLayer{

			[Range(0.0f, 1.0f)]
			public float move_multipler;
			public Transform layer;

			public TwoDeeLayer(float multiplier){
				move_multipler = multiplier;
				layer = null;
			}

		}

		public Transform     _targetCam;
		public Vector2       _offset = Vector2.zero;
		public TwoDeeLayer[] _layers;

		// Use this for initialization
		void Start () {

			if( !_targetCam){
				_targetCam = Camera.main.transform;
			}

		}
			
		void LateUpdate(){

			if( !_targetCam){
				_targetCam = Camera.main.transform;
				return;
			}

			AdjustLayers(_targetCam.position);

		}

//		public void OnWillRenderObject()
//		{
//			if(!enabled)
//				return;
//
//			Camera cam = Camera.current;
//			if( !cam )
//				return;
//
//			AdjustLayers(cam.transform.position);
//
//		}

		void AdjustLayers(Vector3 viewPoint){

			viewPoint += (Vector3) _offset;

			Vector3 displacement = viewPoint - transform.position;
			Vector3 layerSpot = Vector3.zero;

			for(int i=0; i<_layers.Length; i++){

				layerSpot = displacement * _layers[i].move_multipler;
				layerSpot.z = (i+1) * 0.001f;

				_layers[i].layer.localPosition = layerSpot;

			}

		}
	}

}