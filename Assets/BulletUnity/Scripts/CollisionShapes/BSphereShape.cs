﻿using System;
using UnityEngine;
using System.Collections;
using BulletSharp;

namespace BulletUnity {
	[AddComponentMenu("Physics Bullet/Shapes/Sphere")]
    public class BSphereShape : BCollisionShape {
        [SerializeField]
        protected float radius = 1f;
        public float Radius
        {
            get { return radius; }
            set
            {
                if (collisionShapePtr != null && value != radius)
                {
                    Debug.LogError("Cannot change the radius after the bullet shape has been created. Radius is only the initial value " +
                                    "Use LocalScaling to change the shape of a bullet shape.");
                }
                else {
                    radius = value;
                }
            }
        }

        [SerializeField]
        protected Vector3 m_localScaling = Vector3.one;
        public Vector3 LocalScaling
        {
            get { return m_localScaling; }
            set
            {
                m_localScaling = value;
                if (collisionShapePtr != null)
                {
                    ((SphereShape)collisionShapePtr).LocalScaling = value.ToBullet();
                }
            }
        }

        public override void OnDrawGizmosSelected() {
            UnityEngine.Vector3 position = transform.position;
            UnityEngine.Quaternion rotation = transform.rotation;
            UnityEngine.Vector3 scale = m_localScaling;
            BUtility.DebugDrawSphere(position, rotation, scale, Vector3.one * radius, Color.yellow);
        }

        public override CollisionShape GetCollisionShape() {
            if (collisionShapePtr == null) {
                collisionShapePtr = new SphereShape(radius);
                ((SphereShape)collisionShapePtr).LocalScaling = m_localScaling.ToBullet();
            }
            return collisionShapePtr;
        }
    }
}
