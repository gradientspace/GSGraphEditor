using Gradientspace.NodeGraph;
using Mujoco;
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using static Mujoco.MujocoLib;
using static Mujoco.MujocoSpecLib;
using g3;

namespace Mujoco.Nodes
{
    public static class MujocoNodeLibrary
    {
        public static void Initialize()
        {
        }
    }

    [NodeFunctionLibrary("MjSpecNodes")]
    public unsafe static class MjSpecLibrary
    {
        [NodeFunction(ReturnName = "Spec")]
        public static MujocoSpec mjCreateSpec()
        {
            MujocoSpec spec = new MujocoSpec();
            spec.Initialize();
            return spec;
        }


        [NodeFunction(ReturnName="WorldBody")]
        public static MujocoBody mjFindWorld(MujocoSpec Spec)
        {
            mjsBody* world = mjs_findBody(Spec.spec, "world");
            return new MujocoBody(world);
        }




        [NodeFunction(ReturnName = "New Body")]
        public static MujocoBody mjAddBody(MujocoBody ParentBody,
            string name = "Body",
            Vector3d Position = default,
            Vector3d Rotation = default,
            bool bAddFreeJoint = false)
        {
            mjsBody* body = mjs_addBody(ParentBody.body, null);
            mjs_setName(body->element, name);
            body->pos[0] = Position.x; body->pos[1] = Position.y; body->pos[2] = Position.z;

            if (Rotation.LengthSquared > 0) {
                // dumb random order...!
                Matrix3d rotate = Matrix3d.RotateX(Rotation[0], true) * Matrix3d.RotateY(Rotation[1], true) * Matrix3d.RotateZ(Rotation[2], true);
                Quaterniond quat = new Quaterniond(rotate);
                body->quat[0] = quat.w; body->quat[1] = quat.x; body->quat[2] = quat.y; body->quat[3] = quat.z;
            }

            if (bAddFreeJoint)
                mjs_addFreeJoint(body);
            return new MujocoBody(body);
        }



        [NodeFunction(ReturnName="BoxGeom")]
        public static MujocoGeom mjAddBox(MujocoBody Body,
            string name = "Box",
            double DimensionX = 0.5,
            double DimensionY = 0.5,
            double DimensionZ = 0.5,
            Vector3d Position = default,
            Vector3d Color = default)
        {
            mjsGeom* boxGeom = mjs_addGeom(Body.body, null);
            mjs_setName(boxGeom->element, name);

            boxGeom->type = mjtGeom.mjGEOM_BOX;
            boxGeom->size[0] = DimensionX;
            boxGeom->size[1] = DimensionY;
            boxGeom->size[2] = DimensionZ;
            boxGeom->pos[0] = Position.x; boxGeom->pos[1] = Position.y; boxGeom->pos[2] = Position.z;
            boxGeom->mass = DimensionX*DimensionY*DimensionZ;

            boxGeom->rgba[0] = (float)Math.Clamp(Color.x, 0, 1);
            boxGeom->rgba[1] = (float)Math.Clamp(Color.y, 0, 1);
            boxGeom->rgba[2] = (float)Math.Clamp(Color.z, 0, 1);
            boxGeom->rgba[3] = 1.0f;

            return new MujocoGeom(boxGeom);
        }



        [NodeFunction(ReturnName = "PlaneGeom")]
        public static MujocoGeom mjAddPlane(MujocoBody Body,
            string name = "Plane",
            Vector3d Position = default)
        {
            mjsGeom* planeGeom = mjs_addGeom(Body.body, null);
            mjs_setName(planeGeom->element, name);

            planeGeom->type = mjtGeom.mjGEOM_PLANE;
            planeGeom->size[0] = planeGeom->size[1] = 0; planeGeom->size[2] = 1;
            planeGeom->pos[0] = Position.x; planeGeom->pos[1] = Position.y; planeGeom->pos[2] = Position.z;

            return new MujocoGeom(planeGeom);
        }




        [NodeFunction]
        public static bool mjWriteSpecToXML(MujocoSpec Spec, string XMLFile)
        {
            mjModel_* model = mj_compile(Spec.spec, null);

            if (model == null) {
                IntPtr errStringPtr = mjs_getError(Spec.spec);
                string? errString = Marshal.PtrToStringAnsi(errStringPtr);
                GlobalGraphOutput.AppendError("Spec Compile failed - " + errString);
                return false;
            }

            // how does this work exactly??
            StringBuilder errStrings = new StringBuilder();
            int error_sz = 32;
            mj_saveXML(Spec.spec, XMLFile, errStrings, error_sz);

            mj_deleteModel(model);

            return true;
        }



        [NodeFunction]
        public static void mjStep()
        {
            _mjVFS* vfs = null;

            mjSpec* spec = mj_makeSpec();                                  // make an empty spec
            mjsBody* world = mjs_findBody(spec, "world");

            mjsGeom* my_geom = mjs_addGeom(world, null);
            mjs_setName(my_geom->element, "setname1");

            my_geom->type = mjtGeom.mjGEOM_BOX;                                    // set geom type
            my_geom->size[0] = my_geom->size[1] = my_geom->size[2] = 0.5;  // set box size

            mjModel_* model = mj_compile(spec, vfs);                       // compile to mjModel

            IntPtr errStringPtr = mjs_getError(spec);
            string? errString = Marshal.PtrToStringAnsi(errStringPtr);


            StringBuilder errStrings = new StringBuilder();
            int error_sz = 1;

            //mjModel_* model = mj_loadXML("D:\\git\\mujoco\\model\\humanoid\\humanoid.xml", vfs, errStrings, error_sz);
            //mj_saveLastXML("D:\\git\\mujoco\\model\\humanoid\\spec_test.xml", model, errStrings, error_sz);

            //mj_saveXML(spec, "D:\\git\\mujoco\\model\\humanoid\\spec_test2.xml", errStrings, error_sz);
            mj_saveXML(spec, "C:\\scratch\\MUJOCO_SPEC_1.xml", errStrings, error_sz);

            mj_deleteModel(model);
            mj_deleteSpec(spec);

            model = null;
        }


        [NodeFunction]
        public static void mjSimulateXML(string xmlPath)
        {
            string CurrentFolder = AppContext.BaseDirectory;
            string SimulateEXEPath = Path.Combine(CurrentFolder, "simulate.exe");

            ProcessStartInfo psi = new ProcessStartInfo {
                FileName = SimulateEXEPath,
                Arguments = xmlPath,
                UseShellExecute = false, // Crucial for redirecting input/output and hiding the window
                CreateNoWindow = true,   // Hides the process window
                RedirectStandardOutput = true, // Optional: Redirect standard output
                RedirectStandardError = true   // Optional: Redirect standard error
            };
            try {
                Process? process = Process.Start(psi);
            } catch(Exception ex) {
                GlobalGraphOutput.AppendError(ex.Message);
            }
        }

    }

}
