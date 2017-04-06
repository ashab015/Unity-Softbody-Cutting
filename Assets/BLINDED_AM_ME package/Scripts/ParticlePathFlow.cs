using UnityEngine;
using System.Collections;
using System.Collections.Generic;


#if UNITY_EDITOR
using UnityEditor;
#endif

namespace BLINDED_AM_ME{

	[ExecuteInEditMode]
	[RequireComponent(typeof(ParticleSystem))]
	[RequireComponent(typeof(OpenPath))]
	public class ParticlePathFlow : MonoBehaviour {

		public class PathParticleTracker
		{
			public ParticleSystem.Particle particle;
			public float distance;
			public float rotation;

			public PathParticleTracker()
			{

				this.particle = new ParticleSystem.Particle();
				this.particle.lifetime = 0.0f;

			}

			public void Revive(ParticleSystem systemRef){

				this.distance = Random.Range(0.0f, 1.0f);
				this.rotation = Random.Range(0.0f, 360.0f);
				
				this.particle.startLifetime = systemRef.startLifetime;
				this.particle.lifetime = this.particle.startLifetime;
				this.particle.startColor = systemRef.startColor;
				this.particle.startSize = systemRef.startSize;
				this.particle.rotation = systemRef.startRotation;
			}
		}

		public float emissionRate = 25.0f;
		private float emissionRateTracker = 0.0f;


		[Range(0.0f, 5.0f)]
		public float pathWidth = 0.0f;

		private int                       particle_count;
		private PathParticleTracker[]     particle_trackerArray;
		private ParticleSystem.Particle[] particle_array;
		private ParticleSystem            particle_system;


		private double editorTimeDelta = 0.0f;
		private double editorTimetracker = 0.0f;


		private OpenPath path;

		void OnEnable () {

			path = GetComponent<OpenPath>();

			particle_system = GetComponent<ParticleSystem>();
			ParticleSystem.EmissionModule em = particle_system.emission;
			em.enabled = false;

			particle_array        = new ParticleSystem.Particle[particle_system.maxParticles];

			particle_trackerArray = new PathParticleTracker[particle_system.maxParticles];
			for(int i=0; i<particle_trackerArray.Length; i++)
				particle_trackerArray[i] = new PathParticleTracker();

			emissionRateTracker = 1.0f/emissionRate;


	#if UNITY_EDITOR
			if(!Application.isPlaying){
				editorTimetracker = EditorApplication.timeSinceStartup;
			}
	#endif

		}

		private static Vector3 perpendicularDir = Vector3.up;
		void LateUpdate () {

	#if UNITY_EDITOR
			if(!Application.isPlaying){
				editorTimeDelta = EditorApplication.timeSinceStartup - editorTimetracker;
				editorTimetracker = EditorApplication.timeSinceStartup;
			}
	#endif

			if(transform.childCount <= 1)
				return;

			// emision
			if(emissionRateTracker <= 0.0f){
				emissionRateTracker += 1.0f/emissionRate;

				RenewOneDeadParticle();
			}
			emissionRateTracker -= (Application.isPlaying ? Time.deltaTime : (float) editorTimeDelta);

			// age them
			foreach(PathParticleTracker tracker in particle_trackerArray)
			if(tracker.particle.lifetime > 0.0f){
				tracker.particle.lifetime = Mathf.Max(tracker.particle.lifetime - (Application.isPlaying ? Time.deltaTime : (float) editorTimeDelta), 0.0f);
			}


			float normLifetime = 0.0f;
			RoutePoint Rpoint;

			// move them
			foreach(PathParticleTracker tracker in particle_trackerArray)
			if(tracker.particle.lifetime > 0.0f){

				normLifetime = tracker.particle.lifetime/tracker.particle.startLifetime;
				normLifetime = 1.0f - normLifetime;
				
				Rpoint = path.GetRoutePoint(normLifetime * path.TotalDistance);
				
				// 90 degree turn
				perpendicularDir.x = Rpoint.direction.y;
				perpendicularDir.y = -Rpoint.direction.x;
				perpendicularDir.z = Rpoint.direction.z;

				// rotate around Rpoint.direction
				perpendicularDir = Math_Functions.Rotate_Direction(perpendicularDir, Rpoint.direction, tracker.rotation);
				
				// targetPos
				Rpoint.position += (pathWidth * tracker.distance) * perpendicularDir;

				tracker.particle.position = Rpoint.position;
				tracker.particle.velocity = Rpoint.direction;
			
			}

			particle_count = 0;

			// set the given array
			foreach(PathParticleTracker tracker in particle_trackerArray)
			if(tracker.particle.lifetime > 0.0f){ // it's alive
				particle_array[particle_count] = tracker.particle;
				particle_count++;
			}
			
			particle_system.SetParticles(particle_array, particle_count);

		}

		void RenewOneDeadParticle(){

			for(int i=0; i<particle_trackerArray.Length; i++)
			if(particle_trackerArray[i].particle.lifetime <= 0.0f){
				particle_trackerArray[i].Revive(particle_system);
				break;
			}
		}
			
	}
}