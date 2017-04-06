using UnityEngine;
using System.Collections;
using UnityEditor;
using BLINDED_AM_ME;

namespace BLINDED_AM_ME.Inspector{
	
	[CustomEditor(typeof(Trail_Mesh))]
	[CanEditMultipleObjects]
	public class Trail_MeshEditor : Editor {

		public override void OnInspectorGUI()
		{

			DrawDefaultInspector();

			Object[] myScripts = targets;
			if(GUILayout.Button("Shape It"))
			{
				Trail_Mesh maker;
				for(int i=0; i<myScripts.Length; i++){
					maker = (Trail_Mesh) myScripts[i];
					maker.ShapeIt();
				}
			}
		}

	}

}