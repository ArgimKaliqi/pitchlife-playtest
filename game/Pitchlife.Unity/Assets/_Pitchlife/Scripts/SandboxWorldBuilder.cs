using UnityEngine;

namespace Pitchlife.P01
{
    /// <summary>Builds a placeholder pitch, a real rigidbody ball, posts and camera at runtime.</summary>
    public static class SandboxWorldBuilder
    {
        public static Rigidbody Build(Transform parent, Shader litShader = null)
        {
            Material green = MaterialFor(new Color(.11f,.42f,.23f), litShader);
            Material green2 = MaterialFor(new Color(.15f,.48f,.27f), litShader);
            Material white = MaterialFor(Color.white, litShader);
            Material blue = MaterialFor(new Color(.15f,.32f,.85f), litShader);

            MakeBlock("Pitch", new Vector3(0,-.15f,0), new Vector3(34,.3f,50), green, parent);

            for (int i=0; i<8; i++)
                MakeDecoration("Grass stripe", new Vector3(0,.002f,-18+i*6),
                    new Vector3(33,.003f,3), i%2==0 ? green2 : green,parent);

            MakeDecoration("Halfway",new Vector3(0,.012f,0),
                new Vector3(25,.02f,.08f),white,parent);
            MakeDecoration("Penalty box rear",new Vector3(0,.012f,3),
                new Vector3(17,.02f,.08f),white,parent);
            MakeDecoration("Left penalty line",new Vector3(-8.5f,.012f,7.5f),
                new Vector3(.08f,.02f,9),white,parent);
            MakeDecoration("Right penalty line",new Vector3(8.5f,.012f,7.5f),
                new Vector3(.08f,.02f,9),white,parent);

            MakePost("Left post",new Vector3(-ShotMath.GoalWidth/2,ShotMath.GoalHeight/2,ShotMath.GoalLineZ),
                ShotMath.GoalHeight,false,white,parent);
            MakePost("Right post",new Vector3(ShotMath.GoalWidth/2,ShotMath.GoalHeight/2,ShotMath.GoalLineZ),
                ShotMath.GoalHeight,false,white,parent);
            MakePost("Crossbar",new Vector3(0,ShotMath.GoalHeight,ShotMath.GoalLineZ),
                ShotMath.GoalWidth,true,white,parent);

            GameObject player = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            player.name = "Placeholder Footballer";
            player.transform.SetParent(parent);
            player.transform.position = new Vector3(-1.2f,.94f,-9.1f);
            player.transform.localScale = new Vector3(.55f,.94f,.55f);
            player.GetComponent<Renderer>().sharedMaterial = blue;

            GameObject ball = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            ball.name = "P01 Physics Ball";
            ball.transform.SetParent(parent);
            ball.transform.position = new Vector3(0,ShotMath.BallRadius+.015f,-7.5f);
            ball.transform.localScale = Vector3.one * ShotMath.BallRadius * 2;
            ball.GetComponent<Renderer>().sharedMaterial = FootballBallVisual.CreateMaterial(
                litShader != null ? litShader : Shader.Find("Universal Render Pipeline/Lit"));

            PhysicsMaterial friction = new PhysicsMaterial("Football");
            friction.bounciness = .45f;
            friction.dynamicFriction = .45f;
            friction.staticFriction = .45f;
            ball.GetComponent<Collider>().sharedMaterial = friction;

            Rigidbody body = ball.AddComponent<Rigidbody>();
            body.maxAngularVelocity = 60;
            body.mass = .43f;
            body.linearDamping = .12f;
            body.angularDamping = .2f;
            body.interpolation = RigidbodyInterpolation.Interpolate;
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

            Camera camera = Camera.main;
            if (camera == null)
            {
                GameObject cameraObject = new GameObject("Main Camera");
                cameraObject.tag = "MainCamera";
                camera = cameraObject.AddComponent<Camera>();
            }
            camera.transform.position = new Vector3(0,7,-17);
            camera.transform.LookAt(new Vector3(0,1.2f,4));
            camera.fieldOfView = 61f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(.57f,.77f,.95f);

            if (Object.FindFirstObjectByType<Light>() == null)
            {
                GameObject lightObject = new GameObject("Sun");
                Light sun = lightObject.AddComponent<Light>();
                sun.type = LightType.Directional;
                sun.intensity = 1.25f;
                lightObject.transform.rotation = Quaternion.Euler(45,-30,0);
            }
            return body;
        }

        private static Material MaterialFor(Color color, Shader litShader)
        {
            Shader shader = litShader != null ? litShader : Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");
            if (shader == null) shader = Shader.Find("Sprites/Default");
            Material material = new Material(shader);
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor",color);
            if (material.HasProperty("_Color")) material.SetColor("_Color",color);
            return material;
        }

        private static GameObject MakeBlock(string name,Vector3 pos,Vector3 scale,
            Material material,Transform parent)
        {
            GameObject obj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            obj.name = name;
            obj.transform.SetParent(parent);
            obj.transform.position = pos;
            obj.transform.localScale = scale;
            obj.GetComponent<Renderer>().sharedMaterial = material;
            return obj;
        }

        private static void MakeDecoration(string name, Vector3 pos, Vector3 scale,
            Material mat, Transform parent)
        {
            GameObject obj = MakeBlock(name,pos,scale,mat,parent);
            Object.Destroy(obj.GetComponent<Collider>());
        }

        private static void MakePost(string name,Vector3 pos,float length,bool horizontal,
            Material material,Transform parent)
        {
            GameObject obj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            obj.name = name;
            obj.transform.SetParent(parent);
            obj.transform.position = pos;
            obj.transform.localScale = new Vector3(.08f,length*.5f,.08f);
            if (horizontal) obj.transform.rotation = Quaternion.Euler(0,0,90);
            obj.GetComponent<Renderer>().sharedMaterial = material;
        }
    }
}
