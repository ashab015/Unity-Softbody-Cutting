using UnityEngine;
using System.Collections;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace BLINDED_AM_ME._ScriptHelper{

	public class SelectableSphereGizmo : MonoBehaviour {

		#if UNITY_EDITOR

		public float radius = 0.25f;
		public Color notSelectedColor = Color.green;
		public Color selectedColor = Color.cyan;

		private void OnDrawGizmos(){
			Drawing(false);
		}

		private void OnDrawGizmosSelected(){
			Drawing(true);
		}

		private void Drawing(bool isSelected){

			if(isSelected)
				Gizmos.color = selectedColor;
			else
				Gizmos.color = notSelectedColor;

			Gizmos.DrawSphere(transform.position,radius);
		}

		#endif
	}
}