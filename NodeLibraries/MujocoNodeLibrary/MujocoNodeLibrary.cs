// Copyright Gradientspace Corp. All Rights Reserved.
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
        public static MujocoBody? mjFindWorld(ref MujocoSpec Spec)
        {
            mjsBody* world = mjs_findBody(Spec.spec, "world");
            if (world == null)
                return null;
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



        static void configure_geom_transform(mjsGeom* geom, Vector3d Position, Quaterniond Rotation)
        {
            geom->pos[0] = Position.x; geom->pos[1] = Position.y; geom->pos[2] = Position.z;
            geom->alt.type = mjtOrientation.mjORIENTATION_QUAT;
            geom->quat[0] = Rotation.w; geom->quat[1] = Rotation.x; geom->quat[2] = Rotation.y; geom->quat[3] = Rotation.z;
        }
        static void configure_geom_transform(mjsGeom* geom, Vector3d Position, Vector3d Angles)
        {
            geom->pos[0] = Position.x; geom->pos[1] = Position.y; geom->pos[2] = Position.z;
            geom->alt.type = mjtOrientation.mjORIENTATION_EULER;
            geom->alt.euler[0] = Angles[0]; geom->alt.euler[1] = Angles[1]; geom->alt.euler[2] = Angles[2];
        }
        static void configure_geom_attribs(mjsGeom* geom, string name, Vector3d Color)
        {
            if (name.Length > 0)
                mjs_setName(geom->element, name);
            geom->rgba[0] = (float)Math.Clamp(Color.x, 0, 1);
            geom->rgba[1] = (float)Math.Clamp(Color.y, 0, 1);
            geom->rgba[2] = (float)Math.Clamp(Color.z, 0, 1);
            geom->rgba[3] = 1.0f;
        }

        [NodeFunction(ReturnName="BoxGeom")]
        public static MujocoGeom mjAddBox(ref MujocoBody Body,
            string name = "Box",
            double DimensionX = 0.5,
            double DimensionY = 0.5,
            double DimensionZ = 0.5,
            Vector3d Position = default,
            Vector3d EulerAngles = default,
            Vector3d Color = default)
        {
            mjsGeom* boxGeom = mjs_addGeom(Body.body, null);
            boxGeom->type = mjtGeom.mjGEOM_BOX;
            boxGeom->size[0] = DimensionX; boxGeom->size[1] = DimensionY; boxGeom->size[2] = DimensionZ;
            configure_geom_attribs(boxGeom, name, Color);
            configure_geom_transform(boxGeom, Position, EulerAngles);
            return new MujocoGeom(boxGeom);
        }


        [NodeFunction(ReturnName = "Geom")]
        public static MujocoGeom mjAddCylinder(ref MujocoBody Body,
            string name = "Cylinder",
            double Radius = 0.1,
            double Height = 0.5,
            Vector3d Position = default,
            Vector3d EulerAngles = default,
            Vector3d Color = default)
        {
            mjsGeom* boxGeom = mjs_addGeom(Body.body, null);
            boxGeom->type = mjtGeom.mjGEOM_CYLINDER;
            boxGeom->size[0] = Radius; boxGeom->size[1] = Height/2; boxGeom->size[2] = 1.0;
            configure_geom_attribs(boxGeom, name, Color);
            configure_geom_transform(boxGeom, Position, EulerAngles);
            return new MujocoGeom(boxGeom);
        }



        [NodeFunction(ReturnName = "PlaneGeom")]
        public static MujocoGeom mjAddPlane(MujocoBody Body,
            string name = "Plane",
            Vector3d Position = default,
            Vector3d EulerAngles = default,
            Vector3d Color = default)
        {
            mjsGeom* planeGeom = mjs_addGeom(Body.body, null);
            planeGeom->type = mjtGeom.mjGEOM_PLANE;
            planeGeom->size[0] = planeGeom->size[1] = 0; planeGeom->size[2] = 1;
            configure_geom_attribs(planeGeom, name, Color);
            configure_geom_transform(planeGeom, Position, EulerAngles);
            return new MujocoGeom(planeGeom);
        }



        [NodeFunction]
        public static void mjDisableCollision(ref MujocoGeom Geom)
        {
            if (Geom == null || Geom.IsValid == false) {
                GlobalGraphOutput.AppendError("[mjDisableCollision] - Geom is invalid");
                throw new Exception("Geom is invalid");
            }
            Geom.geom->conaffinity = 0;
            Geom.geom->contype = 0;
        }

        [NodeFunction]
        public static void mjSetMass(ref MujocoGeom Geom, double mass)
        {
            if (Geom == null || Geom.IsValid == false) {
                GlobalGraphOutput.AppendError("[mjSetMass] - Geom is invalid");
                throw new Exception("Geom is invalid");
            }
            Geom.geom->mass = mass;
        }

        [NodeFunction]
        public static void mjSetGeomTransformQuat(ref MujocoGeom Geom, Vector3d Position, Quaterniond Rotation)
        {
            if (Geom == null || Geom.IsValid == false) {
                GlobalGraphOutput.AppendError("[mjSetGeomTransformQuat] - Geom is invalid");
                throw new Exception("mjSetGeomTransformQuat is invalid");
            }
            configure_geom_transform(Geom.geom, Position, Rotation);
        }





        [NodeFunction(ReturnName = "Joint")]
        public static MujocoJoint mjAddSlideJoint(ref MujocoBody Body,
            string name = "Slide",
            Vector3d Position = default,
            Vector3d Axis = default,
            double RangeMin = -1,
            double RangeMax = 1,
            double FrictionLoss = 0.1)
        {
            mjsJoint* joint = mjs_addJoint(Body.body);
            if (name.Length > 0)
                mjs_setName(joint->element, name);
            joint->type = mjtJoint.mjJNT_SLIDE;
            joint->pos[0] = Position.x; joint->pos[1] = Position.y; joint->pos[2] = Position.z;
            joint->axis[0] = Axis.x; joint->axis[1] = Axis.y; joint->axis[2] = Axis.z;
            joint->range[0] = RangeMin;
            joint->range[1] = RangeMax;
            joint->frictionloss = FrictionLoss;
            return new MujocoJoint(joint);
        }


        [NodeFunction(ReturnName = "Joint")]
        public static MujocoJoint mjAddHingeJoint(ref MujocoBody Body,
            string name = "Hinge",
            Vector3d Position = default,
            Vector3d Axis = default,
            double RangeMinDeg = -360,
            double RangeMaxDeg = 360,
            double FrictionLoss = 0.1)
        {
            mjsJoint* joint = mjs_addJoint(Body.body);
            if (name.Length > 0)
                mjs_setName(joint->element, name);
            joint->type = mjtJoint.mjJNT_HINGE;
            joint->pos[0] = Position.x; joint->pos[1] = Position.y; joint->pos[2] = Position.z;
            joint->axis[0] = Axis.x; joint->axis[1] = Axis.y; joint->axis[2] = Axis.z;
            if (RangeMinDeg == -360 && RangeMaxDeg == 360) {
                joint->limited = 0;
            } else {
                joint->range[0] = RangeMinDeg;
                joint->range[1] = RangeMaxDeg;
            }
            joint->frictionloss = FrictionLoss;
            return new MujocoJoint(joint);
        }



        [NodeFunction(ReturnName = "Actuator")]
        public static MujocoActuator mjAddActuator(
            ref MujocoSpec Spec,
            MujocoJoint Joint,
            string name = "Actuator",
            double CtrlRangeMin = -1,
            double CtrlRangeMax = 1)
        {
            if (Spec.IsValid == false) GlobalGraphOutput.AppendError("[mjAddActuator] - Spec is null/invalid");
            if (Joint.IsValid == false) GlobalGraphOutput.AppendError("[mjAddActuator] - Joint is null/invalid");

            mjsActuator* actuator = mjs_addActuator(Spec.spec);
            if (name.Length > 0)
                mjs_setName(actuator->element, name);
            mjString* joint_name = mjs_getName(Joint.joint->element);
            string? jn = Marshal.PtrToStringAnsi((nint)joint_name);
            //actuator->target = joint_name;
            mjs_setString(actuator->target, jn!);
            actuator->trntype = mjtTrn.mjTRN_JOINT;
            actuator->ctrlrange[0] = CtrlRangeMin;
            actuator->ctrlrange[1] = CtrlRangeMax;
            actuator->ctrllimited = 1;
            return new MujocoActuator(actuator);
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
