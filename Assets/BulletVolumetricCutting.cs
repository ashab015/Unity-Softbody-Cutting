using UnityEngine;
using System.Collections;
using BulletSharp.SoftBody;
using System;
using BulletSharp;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Text;
using System.IO;
using System.ComponentModel;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Diagnostics;
using System.IO.Compression;

namespace BulletUnity
{

    public class PairInt
    {
        public PairInt(int ii1, int ii2)
        {
            i1 = ii1;
            i2 = ii2;
        }
        int i1;
        int i2;
    };

    public class TriInt
    {
        public TriInt(int ii1, int ii2, int ii3)
        {
            i1 = ii1;
            i2 = ii2;
            i3 = ii3;
        }
        int i1;
        int i2;
        int i3;
    };

    public class ContactResult : ContactResultCallback
    {
        public bool m_connected;
        public float margin;
        public ContactResult()
        {
            m_connected = false;
            margin = 0.05f;
        }
        public override float AddSingleResult(ManifoldPoint cp, CollisionObjectWrapper colObj0Wrap, int partId0, int index0, CollisionObjectWrapper colObj1Wrap, int partId1, int index1)
        {
            if (cp.Distance <= margin)
                m_connected = true;
            return 1.0f;
        }
    };

    /// <summary>
    /// Used base for any(most) softbodies needing a mesh and meshrenderer.
    /// </summary>
    [RequireComponent(typeof(MeshFilter))]
    [RequireComponent(typeof(MeshRenderer))]
    public class BulletVolumetricCutting : BSoftBody
    {
        //public BUserMeshSettings meshSettings = new BUserMeshSettings();

        private MeshFilter _meshFilter;
        protected MeshFilter meshFilter
        {
            get { return _meshFilter = _meshFilter ?? GetComponent<MeshFilter>(); }
        }

        private List<int> pairs = new List<int>(); // pairs for highres vertices
        private List<Vector3> highres = new List<Vector3>(); // highres vertices
        private List<BulletSharp.Math.Vector3> highresbullet = new List<BulletSharp.Math.Vector3>(); // highres verts
        private List<Vector3> highoffsets = new List<Vector3>();
        private List<int> highindices = new List<int>();
        private List<int> highindices2 = new List<int>();
        private List<int> highres_tetra = new List<int>();
        private List<bool> highres_valid_tetra = new List<bool>();
        List<Vector3> normals = new List<Vector3>();
        List<Vector2> textures = new List<Vector2>();
        List<TriInt> facedata = new List<TriInt>();
        List<Vector2> uvs = new List<Vector2>();
        private bool loaded;
        //private MeshFilter ExtraMeshLoader;
        private int extracount = 0;

        private List<List<int>> instances = new List<List<int>>();
        private List<Vector3> lowres_connect = new List<Vector3>();
        private List<Vector3> lowres = new List<Vector3>();
        private List<int> lowresids = new List<int>();
        private List<Vector3> actuallyverts = new List<Vector3>();
        public bool DisplayHighRes = true;
        public GameObject CutterObject;
        private List<List<int>> linkindexs = new List<List<int>>();
        private SoftBody psb;
        private List<bool> linkdata = new List<bool>();
        private List<bool> brokenlinks = new List<bool>();
        private bool testdone = false;
        private List<bool> ValidTetras = new List<bool>();
        private List<int> lowres_tetra = new List<int>();

     
        internal override void Start()
        {
            
           

            actuallyverts = GetVectors();
            GetVectors2(false);

            base.Start();

            if (DisplayHighRes == true)
            {
                Mesh mesh = GetHighResMesh2(meshFilter.mesh, highres);
                meshFilter.mesh = mesh;
                //meshSettings.UserMesh = mesh;
            }
            if (DisplayHighRes == false)
            {
                Mesh mesh = GetMesh3(meshFilter.mesh, actuallyverts.ToArray());
                meshFilter.mesh = mesh;
                //meshSettings.UserMesh = mesh;
            }

            //ExtraMeshLoader = GameObject.Find("ExtraMeshLoader").GetComponent<MeshFilter>();
            //MeshAllocate();
            World.SolverInfo.NumIterations = 1;
            
        }

        

        internal override bool _BuildCollisionObject()
        {
            //Mesh mesh = new Mesh();

            //GetComponent<MeshFilter>().sharedMesh = mesh;

            //convert the mesh data to Bullet data and create DoftBody
            BulletSharp.Math.Vector3[] bVerts = new BulletSharp.Math.Vector3[actuallyverts.Count];
            for (int i = 0; i < actuallyverts.Count; i++)
            {
                bVerts[i] = actuallyverts[i].ToBullet();
            }

            SoftBody m_BSoftBody = CreateVolumetricSoftbody(World.WorldInfo, bVerts);

            m_collisionObject = m_BSoftBody;
            SoftBodySettings.ConfigureSoftBody(m_BSoftBody);         //Set SB settings

            //Set SB position to GO position
            m_BSoftBody.Rotate(transform.rotation.ToBullet());
            m_BSoftBody.Translate(transform.position.ToBullet());
            m_BSoftBody.Scale(transform.localScale.ToBullet());

            

            return true;
        }

        /// <summary>
        /// Create new SoftBody object using a Mesh
        /// </summary>
        /// <param name="position">World position</param>
        /// <param name="rotation">rotation</param>
        /// <param name="mesh">Need to provide a mesh</param>
        /// <param name="buildNow">Build now or configure properties and call BuildSoftBody() after</param>
        /// <param name="sBpresetSelect">Use a particular softBody configuration pre select values</param>
        /// <returns></returns>
        public static GameObject CreateNew(Vector3 position, Quaternion rotation, Mesh mesh, bool buildNow, SBSettingsPresets sBpresetSelect = SBSettingsPresets.ShapeMatching)
        {
            GameObject go = new GameObject("SoftBodyWMesh");
            go.transform.position = position;
            go.transform.rotation = rotation;
            BSoftBodyWMesh BSoft = go.AddComponent<BSoftBodyWMesh>();

            BSoft.meshSettings.UserMesh = mesh;
            MeshRenderer meshRenderer = go.GetComponent<MeshRenderer>();
            UnityEngine.Material material = new UnityEngine.Material(Shader.Find("Standard"));
            meshRenderer.material = material;

            BSoft.SoftBodySettings.ResetToSoftBodyPresets(sBpresetSelect); //Apply SoftBody settings presets

            if (buildNow)
            {
                BSoft._BuildCollisionObject();  //Build the SoftBody
            }
            go.name = "BSoftBodyWMesh";
            return go;
        }

        /// <summary>
        /// Update Mesh (or line renderer) at runtime, call from Update 
        /// </summary>
        public override void UpdateMesh()
        {
            //if (testdone == false)
            //{
                //CutSolve2();
                //testdone = true;
            //}
            CutSolve2();
            Mesh mesh = meshFilter.mesh;
            //mesh.normals = norms;
            //mesh.RecalculateBounds();

            //CutSolve2();

            if (DisplayHighRes == true)
            {
                meshFilter.mesh = GetHighResMesh2(mesh, highres);
                transform.SetTransformationFromBulletMatrix(m_collisionObject.WorldTransform);  //Set SoftBody position, No motionstate  
                return;
            }
            if (DisplayHighRes == false)
            {
                meshFilter.mesh = GetMesh3(mesh, verts);
                transform.SetTransformationFromBulletMatrix(m_collisionObject.WorldTransform);  //Set SoftBody position, No motionstate  
                return;
            }
        }

        List<List<int>> GetInstances(BulletSharp.Math.Vector3[] vec)
        {

            List<int> instances = new List<int>();
            int pcount = vec.Length;
            for (int x = 0; pcount > x; x++)
            {
                instances.Add(0);
            }

            for (int x = 0; pcount > x; x++)
            {
                BulletSharp.Math.Vector3 pos1 = vec[x];
                for (int i = 0; pcount > i; i++)
                {
                    BulletSharp.Math.Vector3 pos2 = vec[i];
                    if (pos1 == pos2)
                    {
                        instances[x] += 1;
                    }
                }
            }

            List<BulletSharp.Math.Vector3> unique = new List<BulletSharp.Math.Vector3>();
            for (int x = 0; instances.Count > x; x++)
            {
                if (instances[x] > 2)
                {
                    unique.Add(vec[x]);
                }
            }

            List<List<int>> pairs = new List<List<int>>();
            for (int x = 0; unique.Count > x; x++)
            {
                List<int> pindex = new List<int>();
                for (int i = 0; vec.Length > i; i++)
                {
                    if (unique[x] == vec[i])
                    {
                        pindex.Add(i);
                    }
                }
                pairs.Add(pindex);
            }

            return pairs;

        }

        List<Vector3> GetVectorInstances(Vector3[] vec)
        {

            List<Vector3> instances = new List<Vector3>();
            List<int> instances2 = new List<int>();
            int pcount = vec.Length;
            for (int x = 0; pcount > x; x++)
            {
                instances2.Add(0);
            }

            for (int x = 0; pcount > x; x++)
            {
                Vector3 pos1 = vec[x];
                for (int i = 0; pcount > i; i++)
                {
                    Vector3 pos2 = vec[i];
                    if (pos1 == pos2)
                    {
                        instances2[x] += 1;
                    }
                }
            }

            for (int x = 0; instances2.Count > x; x++)
            {
                if (instances2[x] > 1)
                {
                    instances.Add(vec[instances2[x]]);
                }
            }

            return instances;

        }

        SoftBody CreateVolumetricSoftbody(SoftBodyWorldInfo worldInfo, BulletSharp.Math.Vector3[] vertices)
        {
            int vertexcount = vertices.Length;
            psb = new SoftBody(worldInfo, vertexcount, vertices, null);

            for (int x = 3; vertexcount > x; x += 4)
            {
                int n0 = x - 3;
                int n1 = x - 2;
                int n2 = x - 1;
                int n3 = x - 0;
                psb.AppendTetra(n0, n1, n2, n3);

                psb.AppendLink(n0, n1);
                linkdata.Add(true);
                brokenlinks.Add(false);

                psb.AppendLink(n1, n2);
                linkdata.Add(true);
                brokenlinks.Add(false);

                psb.AppendLink(n2, n0);
                linkdata.Add(true);
                brokenlinks.Add(false);

                psb.AppendLink(n0, n3);
                linkdata.Add(true);
                brokenlinks.Add(false);

                psb.AppendLink(n1, n3);
                linkdata.Add(true);
                brokenlinks.Add(false);

                psb.AppendLink(n2, n3);
                linkdata.Add(true);
                brokenlinks.Add(false);

                ValidTetras.Add(true);

            }

            instances = GetInstances(vertices);
            //List<bool> ValidCuts = CutTest3(vertices);
            for (int x = 0; instances.Count > x; x++)
            {
                //if (ValidCuts[x] == false)
                {
                //    continue;
                }
                for (int i = 1; instances[x].Count > i; i++)
                {
                    int i1 = instances[x][i - 1];
                    int i2 = instances[x][i - 0];
                    psb.AppendLink(i1, i2);
                    linkdata.Add(false);
                    brokenlinks.Add(false);
                }
            }

            return psb;
        }

        Mesh GetMesh(Mesh mesh, Vector3[] vertices)
        {

            mesh.vertices = vertices;

            List<int> indices = new List<int>();
            for (int x = 0; (vertices.Length / 4) > x; x++)
            {
                int tcount = 4 * x;
                indices.Add(0 + tcount);
                indices.Add(1 + tcount);
                indices.Add(3 + tcount);

                indices.Add(0 + tcount);
                indices.Add(2 + tcount);
                indices.Add(1 + tcount);

                indices.Add(1 + tcount);
                indices.Add(2 + tcount);
                indices.Add(3 + tcount);

                indices.Add(0 + tcount);
                indices.Add(3 + tcount);
                indices.Add(2 + tcount);
            }
            mesh.triangles = indices.ToArray();



            return mesh;
        }

        Mesh GetHighResMesh(Mesh mesh, List<Vector3> vertices)
        {

            //int secondcounter = 0;
            List<Vector3> newvecs = new List<Vector3>(vertices);
            for (int x = 0; pairs.Count > x; x++)
            {
                int id = pairs[x];
                if (verts.Length == 0)
                {
                    newvecs[x] = (highoffsets[x] + new Vector3(0, 0, 0));
                    continue;
                }
                newvecs[x] = (highoffsets[x] + verts[id]);
            }
            mesh.vertices = newvecs.ToArray();


            if (highindices.Count == 0)
            {
                for (int x = 0; (mesh.vertices.Length / 4) > x; x++)
                {
                    int tcount = 4 * x;
                    highindices.Add(0 + tcount);
                    highindices.Add(1 + tcount);
                    highindices.Add(3 + tcount);

                    highindices.Add(0 + tcount);
                    highindices.Add(2 + tcount);
                    highindices.Add(1 + tcount);

                    highindices.Add(1 + tcount);
                    highindices.Add(2 + tcount);
                    highindices.Add(3 + tcount);

                    highindices.Add(0 + tcount);
                    highindices.Add(3 + tcount);
                    highindices.Add(2 + tcount);
                }
                mesh.triangles = highindices.ToArray();
            }

            return mesh;
        }

        Mesh GetMesh2(Mesh mesh)
        {

            List<Vector3> newverts = highres;
            int counter = 0;
            for (int x = 3; newverts.Count > x; x += 4)
            {
                int index = pairs[counter] - 3;
                Vector3 center = (verts[0 + index] + verts[1 + index] + verts[2 + index] + verts[3 + index]) / 4;

                newverts[x - 3] += center;
                newverts[x - 2] += center;
                newverts[x - 1] += center;
                newverts[x - 0] += center;
                counter += 1;
            }

            mesh.vertices = newverts.ToArray();

            List<int> indices = new List<int>();
            for (int x = 0; (mesh.vertices.Length / 4) > x; x++)
            {
                int tcount = 4 * x;
                indices.Add(0 + tcount);
                indices.Add(1 + tcount);
                indices.Add(3 + tcount);

                indices.Add(0 + tcount);
                indices.Add(2 + tcount);
                indices.Add(1 + tcount);

                indices.Add(1 + tcount);
                indices.Add(2 + tcount);
                indices.Add(3 + tcount);

                indices.Add(0 + tcount);
                indices.Add(3 + tcount);
                indices.Add(2 + tcount);
            }
            mesh.triangles = indices.ToArray();



            return mesh;
        }

        Mesh GetMesh3(Mesh mesh, Vector3[] vertices)
        {

            for (int x=3; vertices.Length > x; x+=4)
            {
                int tetraindex = ((x + 1) / 4) - 1;
                if (ValidTetras[tetraindex] == false)
                {
                    vertices[x - 3] = new Vector3(0, 0, 0);
                    vertices[x - 2] = new Vector3(0, 0, 0);
                    vertices[x - 1] = new Vector3(0, 0, 0);
                    vertices[x - 0] = new Vector3(0, 0, 0);
                }
            }
               
            mesh.vertices = vertices;

            List<int> indices = new List<int>();
            for (int x = 0; (vertices.Length / 4) > x; x++)
            {
                int tcount = 4 * x;
                indices.Add(0 + tcount);
                indices.Add(1 + tcount);
                indices.Add(3 + tcount);

                indices.Add(0 + tcount);
                indices.Add(2 + tcount);
                indices.Add(1 + tcount);

                indices.Add(1 + tcount);
                indices.Add(2 + tcount);
                indices.Add(3 + tcount);

                indices.Add(0 + tcount);
                indices.Add(3 + tcount);
                indices.Add(2 + tcount);
            }
            mesh.triangles = indices.ToArray();



            return mesh;
        }

        string getPath()
        {
            string input = Application.dataPath;
            string result = "";
            string pattern = "";
            string replace = "";

            if (input.Contains("Assets"))
            {
                pattern = @"\bBulletVolumetricCutting/Assets\b";
                replace = "BulletVolumetricCutting/api/";
                result = Regex.Replace(input, pattern, replace);
            }
            if (input.Contains("Data"))
            {
                pattern = @"\bBulletVolumetricCutting2_Data\b";
                replace = "api/";
                result = Regex.Replace(input, pattern, replace);
            }
            return result;
        }

        public List<Vector3> GetVectors()
        {

            List<Vector3> vec = new List<Vector3>();
            lowresids.Clear();
            string filepath = getPath() + "lowres.obj";
            string[] file = File.ReadAllLines(filepath);

            for (int x = 0; file.Length > x; x++)
            {
                if (file[x].Length == 0)
                {
                    continue;
                }
                if (file[x][0] == 'v')
                {
                    string[] indicies = file[x].Split(' ');
                    vec.Add(new Vector3(Convert.ToSingle(indicies[2]), Convert.ToSingle(indicies[3]), Convert.ToSingle(indicies[4])));
                }
            }

            for (int x = 0; vec.Count > x; x++)
            {
                lowresids.Add(x);
            }

            int tetracounter = 0;
            for (int x = 3; vec.Count > x; x+=4)
            {
                lowres_tetra.Add(tetracounter);
                lowres_tetra.Add(tetracounter);
                lowres_tetra.Add(tetracounter);
                lowres_tetra.Add(tetracounter);
                tetracounter += 1;
            }

            lowres = vec;
            lowres_connect = vec;
            return vec;

        }
        public void GetVectors2(bool addtransform = false)
        {

            pairs = new List<int>();
            List<Vector3> vec = new List<Vector3>();
            List<int> ids = new List<int>();
            List<BulletSharp.Math.Vector3> newvec = new List<BulletSharp.Math.Vector3>();
            string filepath = getPath() + "highres.obj";
            string[] file = File.ReadAllLines(filepath);
            Vector3 centroid = new Vector3(0, 0, 0);

            List<bool> counter = new List<bool>();

            for (int x = 0; file.Length > x; x++)
            {
                if (file[x].Length == 0)
                {
                    continue;
                }
                if (file[x][0] == 'v' && file[x][1] == ' ')
                {
                    string[] indicies = file[x].Split(' ');
                    Vector3 pos = new Vector3(Convert.ToSingle(indicies[2]), Convert.ToSingle(indicies[3]), Convert.ToSingle(indicies[4]));
                    if (addtransform == true)
                    {
                        Vector3 newpos = transform.TransformPoint(pos);
                        vec.Add(newpos);
                        newvec.Add(newpos.ToBullet());
                        //centroid += newpos;
                        continue;
                    }
                    vec.Add(pos);
                    newvec.Add(pos.ToBullet());
                    centroid += pos;
                    uvs.Add(new Vector3(0, 0, 0));
                    counter.Add(false);
                }
                if (file[x][0] == 'v' && file[x][1] == 'n')
                {
                    string[] indicies = file[x].Split(' ');
                    Vector3 normal = new Vector3(Convert.ToSingle(indicies[1]), Convert.ToSingle(indicies[2]), Convert.ToSingle(indicies[3]));                
                    normals.Add(normal);
                }
                if (file[x][0] == 'v' && file[x][1] == 't')
                {
                    string[] indicies = file[x].Split(' ');
                    Vector2 texture = new Vector2(Convert.ToSingle(indicies[1]), Convert.ToSingle(indicies[2]));
                    textures.Add(texture);
                }
                if (file[x][0] == 'f' && file[x][1] == ' ')
                {
                    string[] indicies = file[x].Split(' ');

                    string[] data = indicies[1].Split('/');
                    if (counter[Int32.Parse(data[0]) - 1] == false)
                    {
                        uvs[Int32.Parse(data[0]) - 1] = textures[Int32.Parse(data[1]) - 1];
                        counter[Int32.Parse(data[0]) - 1] = true;
                    }

                    data = indicies[2].Split('/');
                    if (counter[Int32.Parse(data[0]) - 1] == false)
                    {
                        uvs[Int32.Parse(data[0]) - 1] = textures[Int32.Parse(data[1]) - 1];
                        counter[Int32.Parse(data[0]) - 1] = true;
                    }

                    data = indicies[2].Split('/');
                    if (counter[Int32.Parse(data[0]) - 1] == false)
                    {
                        uvs[Int32.Parse(data[0]) - 1] = textures[Int32.Parse(data[1]) - 1];
                        counter[Int32.Parse(data[0]) - 1] = true;
                    }

                }
            }
            centroid /= vec.Count;


            // other data
            for (int x = 0; vec.Count > x; x++)
            {
                vec[x] = vec[x] - centroid;
                newvec[x] = vec[x].ToBullet();
            }

            KdTree kdtree = new KdTree();
            kdtree.build(lowres.ToArray(), lowresids.ToArray());

            for (int x = 0; vec.Count > x; x++)
            {
                int closestpoint = kdtree.nearest(vec[x]);
                Vector3 offset = vec[x] - lowres[closestpoint];
                highoffsets.Add(offset);
                pairs.Add(closestpoint);
            }

            int tetracounter = 0;
            for (int x = 3; vec.Count > x; x += 4)
            {
                highres_tetra.Add(tetracounter);
                highres_tetra.Add(tetracounter);
                highres_tetra.Add(tetracounter);
                highres_tetra.Add(tetracounter);
                highres_valid_tetra.Add(true);
                tetracounter += 1;
            }

            highres = vec;
            highresbullet = newvec;

        }

        // Cutting
        public RigidBody LocalCreateRigidBody(float mass, BulletSharp.Math.Vector3 startpos, CollisionShape shape, bool isKinematic = false)
        {
            //rigidbody is dynamic if and only if mass is non zero, otherwise static
            bool isDynamic = (mass != 0.0f);

            BulletSharp.Math.Vector3 localInertia = BulletSharp.Math.Vector3.Zero;
            if (isDynamic)
                shape.CalculateLocalInertia(mass, out localInertia);

            //using motionstate is recommended, it provides interpolation capabilities, and only synchronizes 'active' objects
            BulletSharp.Math.Matrix matrixtrans = BulletSharp.Math.Matrix.Identity;
            matrixtrans.Origin = startpos;
            DefaultMotionState myMotionState = new DefaultMotionState(matrixtrans);

            RigidBodyConstructionInfo rbInfo = new RigidBodyConstructionInfo(mass, myMotionState, shape, localInertia);
            RigidBody body = new RigidBody(rbInfo);
            if (isKinematic)
            {
                body.CollisionFlags = body.CollisionFlags | BulletSharp.CollisionFlags.KinematicObject;
                body.ActivationState = ActivationState.DisableDeactivation;
            }
            rbInfo.Dispose();

            return body;
        }
        public bool NodeEqual(Node n1, Node n2)
        {
            if (n1.Position == n2.Position && n1.Velocity == n2.Velocity && n1.Normal == n2.Normal)
            {
                return true;
            }
            return false;
        }
        public Vector3 BulletToVec(BulletSharp.Math.Vector3 vert)
        {
            return new Vector3(vert.X, vert.Y, vert.Z);
        }
        public List<bool> CutTest3(BulletSharp.Math.Vector3[] vertices)
        {

            float radius = 0.05f;
            List<bool> IValid = new List<bool>();
            for (int x = 0; instances.Count > x; x++)
            {

                Vector3 center = new Vector3(0, 0, 0);
                int countamount = 0;
                for (int i = 0; instances[x].Count > i; i++)
                {
                    int i1 = instances[x][i];
                    center += BulletToVec(vertices[i1]);
                    countamount += 1;
                }
                center /= countamount;

                Collider[] colliding = Physics.OverlapSphere(center, radius);
                if (colliding.Length > 0)
                {
                    IValid.Add(false);
                    continue;
                }
                IValid.Add(true);
            }
            return IValid;

        }
        public void CutTest2()
        {


            float radius = 0.08f;
            for (int x = 0; verts.Length > x; x++)
            {
                Vector3 center = verts[x];
                Collider[] colliding = Physics.OverlapSphere(center, radius);
                if (colliding.Length > 0)
                {
                    //Debug.Log("Collision Made");
                    Node node = psb.Nodes[x];
                    for (int i = 0; psb.Links.Count > i; i++)
                    {
                        if (linkdata[i] == false && brokenlinks[i] == false)
                            continue;
                        if (NodeEqual(psb.Links[i].Nodes[0], node))
                        {
                            brokenlinks[i] = true;
                            continue;
                        }
                        if (NodeEqual(psb.Links[i].Nodes[1], node))
                        {
                            brokenlinks[i] = true;
                            continue;
                        }
                    }
                }
            }


            for (int x = 0; brokenlinks.Count > x; x++)
            {
                if (brokenlinks[x] == true)
                {
                    Node n0 = psb.Links[x].Nodes[0];
                    Node n1 = psb.Links[x].Nodes[1];
                    float distance = BulletSharp.Math.Vector3.Distance(n0.Position, n1.Position);
                    psb.Links[x].RestLength = distance;
                    psb.Links[x].C1 = distance * distance;
                }
            }



        }
        public void CutSolve()
        {


            for (int x = 0; brokenlinks.Count > x; x++)
            {
                if (brokenlinks[x] == true)
                {
                    Node n0 = psb.Links[x].Nodes[0];
                    Node n1 = psb.Links[x].Nodes[1];
                    float distance = BulletSharp.Math.Vector3.Distance(n0.Position, n1.Position);
                    psb.Links[x].RestLength = distance;
                    psb.Links[x].C1 = distance * distance;
                }
            }


        }
        public void CutSolve2()
        {


            float radius = 0.2f;
            for (int x = 3; verts.Length > x; x+=4)
            {
                int i1 = x - 3;
                int i2 = x - 2;
                int i3 = x - 1;
                int i4 = x - 0;
                Vector3 center = BulletToVec((psb.Nodes[i1].Position + psb.Nodes[i2].Position + psb.Nodes[i3].Position + psb.Nodes[i4].Position) / 4);
                Collider[] colliding = Physics.OverlapSphere(center, radius);
                if (colliding.Length > 0)
                {
                    Node node = psb.Nodes[x];
                    int tetraindex = ((x + 1) / 4) - 1;
                    ValidTetras[tetraindex] = false;
                }
            }



        }
        public void CutSolve3()
        {


            float radius = 0.2f;
            for (int x = 3; verts.Length > x; x += 4)
            {
                int i1 = x - 3;
                int i2 = x - 2;
                int i3 = x - 1;
                int i4 = x - 0;
                Vector3 center = BulletToVec((psb.Nodes[i1].Position + psb.Nodes[i2].Position + psb.Nodes[i3].Position + psb.Nodes[i4].Position) / 4);
                Collider[] colliding = Physics.OverlapSphere(center, radius);
                if (colliding.Length > 0)
                {
                    Node node = psb.Nodes[x];
                    int tetraindex = ((x + 1) / 4) - 1;
                    ValidTetras[tetraindex] = false;
                }
            }



        }
        Mesh GetHighResMesh2(Mesh mesh, List<Vector3> vertices)
        {

            //int secondcounter = 0;
            List<Vector3> newvecs = new List<Vector3>(vertices);
            for (int x = 0; pairs.Count > x; x++)
            {
                int id = pairs[x];
                if (verts.Length == 0)
                {
                    newvecs[x] = (highoffsets[x] + new Vector3(0, 0, 0));
                    continue;
                }
                newvecs[x] = (highoffsets[x] + verts[id]);
            }


            if (verts.Length > 0)
            {
                for (int x = 0; pairs.Count > x; x++)
                {
                    int id = pairs[x];
                    int tetraindex = lowres_tetra[id];
                    int hightetraindex = highres_tetra[x];
                    newvecs[x] = (highoffsets[x] + verts[id]);
                    if (ValidTetras[tetraindex] == false)
                    { 
                        newvecs[x] = new Vector3(0, 0, 0);
                    }
                }

                int tetracounter = 0;
                Vector3 empty = Vector3.zero;
                for (int x = 3; pairs.Count > x; x += 4)
                {
                    int x1 = x - 3;
                    int x2 = x - 2;
                    int x3 = x - 1;
                    int x4 = x - 0;

                    if (newvecs[x1] == empty || newvecs[x2] == empty || newvecs[x3] == empty || newvecs[x4] == empty)
                    {
                        newvecs[x1] = empty;
                        newvecs[x2] = empty;
                        newvecs[x3] = empty;
                        newvecs[x4] = empty;
                    }
                    tetracounter += 1;
                }
            }
            mesh.vertices = newvecs.ToArray();


            if (mesh.triangles.Length == 0)
            {
                List<int> indices = new List<int>();
                for (int x = 0; (mesh.vertices.Length / 4) > x; x++)
                {
                    int tcount = 4 * x;
                    indices.Add(0 + tcount);
                    indices.Add(1 + tcount);
                    indices.Add(3 + tcount);

                    indices.Add(0 + tcount);
                    indices.Add(2 + tcount);
                    indices.Add(1 + tcount);

                    indices.Add(1 + tcount);
                    indices.Add(2 + tcount);
                    indices.Add(3 + tcount);

                    indices.Add(0 + tcount);
                    indices.Add(3 + tcount);
                    indices.Add(2 + tcount);
                }
                mesh.triangles = indices.ToArray();
                mesh.uv = uvs.ToArray();
            }
            

            return mesh;
        }




    }
}