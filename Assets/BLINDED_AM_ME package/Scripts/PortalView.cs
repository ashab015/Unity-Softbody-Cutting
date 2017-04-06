/*
 * Thanks to Aras Pranckevicius' MirrorReflection4
 * http://wiki.unity3d.com/index.php/MirrorReflection4 
 * 
 */

using UnityEngine;
using System.Collections;

namespace BLINDED_AM_ME{

	[ExecuteInEditMode]
	public class PortalView : MonoBehaviour {

		public Transform pointB;
		public Camera    scoutCamera;
		public Vector3  faceNormal = Vector3.forward; // relative to self


		public int m_TextureSize = 256;
		public float m_ClipPlaneOffset = 0.07f;

		private RenderTexture m_PortalTexture = null;
		private int m_OldPortalTextureSize = 0;


		private static bool s_InsideRendering = false;

		// Use this for initialization
		void Start () {
		
		}

		public void OnWillRenderObject()
		{

			if(!enabled || !scoutCamera || !pointB)
				return;

			Camera cam = Camera.current;
			if( !cam )
				return;

			// Safeguard from recursive reflections.        
			if( s_InsideRendering )
				return;
			s_InsideRendering = true;


			var rend = GetComponent<Renderer>();
			if (!enabled || !rend || !rend.sharedMaterial || !rend.enabled)
				return;


			CreateNeededObjects();


			Vector3 pos = transform.position;
			Vector3 normal = transform.TransformDirection(faceNormal);

			// this will make it depend on the points' position, rotation, and sorry also their scales
			// best make their scales 1 or equal
			scoutCamera.transform.position = pointB.TransformPoint(transform.InverseTransformPoint(cam.transform.position));
			scoutCamera.transform.rotation = Quaternion.LookRotation(
				pointB.TransformDirection(transform.InverseTransformDirection(cam.transform.forward)),
				pointB.TransformDirection(transform.InverseTransformDirection(cam.transform.up)));

			// I don't know how this works it just does, I got lucky
			Vector4 clipPlane = CameraSpacePlane( cam, pos, normal, -1.0f );
			Matrix4x4 projection = cam.CalculateObliqueMatrix(clipPlane);
			scoutCamera.projectionMatrix = projection;

			if(!scoutCamera.enabled){ // make it manual
				scoutCamera.Render();
			}else
				scoutCamera.enabled = false;


			Material[] materials = rend.sharedMaterials;
			foreach( Material mat in materials ) {
				if( mat.HasProperty("_PortalTex") )
					mat.SetTexture( "_PortalTex", m_PortalTexture );
			}

			s_InsideRendering = false;
		}


		// Aras Pranckevicius' MirrorReflection4
		// http://wiki.unity3d.com/index.php/MirrorReflection4 
		// Cleanup all the objects we possibly have created
		void OnDisable()
		{
			if( m_PortalTexture ) {
				DestroyImmediate( m_PortalTexture );
				m_PortalTexture = null;
			}
		}

		// Aras Pranckevicius' MirrorReflection4
		// http://wiki.unity3d.com/index.php/MirrorReflection4 
		// On-demand create any objects we need
		private void CreateNeededObjects()
		{
			
			// Reflection render texture
			if( !m_PortalTexture || m_OldPortalTextureSize != m_TextureSize )
			{
				if( m_PortalTexture )
					DestroyImmediate( m_PortalTexture );
				m_PortalTexture = new RenderTexture( m_TextureSize, m_TextureSize, 16 );
				m_PortalTexture.name = "__PortalRenderTexture" + GetInstanceID();
				m_PortalTexture.isPowerOfTwo = true;
				m_PortalTexture.hideFlags = HideFlags.DontSave;
				m_OldPortalTextureSize = m_TextureSize;

				scoutCamera.targetTexture = m_PortalTexture;
			}
	       
		}

		// Aras Pranckevicius' MirrorReflection4
		// http://wiki.unity3d.com/index.php/MirrorReflection4 
		// Given position/normal of the plane, calculates plane in camera space.
		private Vector4 CameraSpacePlane (Camera cam, Vector3 pos, Vector3 normal, float sideSign)
		{
			Vector3 offsetPos = pos + normal * -m_ClipPlaneOffset;
			Matrix4x4 m = cam.worldToCameraMatrix;
			Vector3 cpos = m.MultiplyPoint( offsetPos );
			Vector3 cnormal = m.MultiplyVector( normal ).normalized * sideSign;
			return new Vector4( cnormal.x, cnormal.y, cnormal.z, -Vector3.Dot(cpos,cnormal) );
		}
			
	}
}