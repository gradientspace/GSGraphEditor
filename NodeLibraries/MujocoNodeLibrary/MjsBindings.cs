using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using static Mujoco.MujocoLib;


namespace Mujoco
{
    public static class MujocoSpecLib
    {
        [StructLayout(LayoutKind.Sequential)]
        public unsafe struct mjSpec
        {
            public mjsElement* element;             // element type
            public mjString* childclass;            // childclass name

            // compiler data
            public mjsCompiler compiler;            // compiler options
            public byte strippath;               // automatically strip paths from mesh files
            public mjString* meshdir;               // mesh and hfield directory
            public mjString* texturedir;            // texture directory

            // engine data
            public mjOption option;                 // physics options
            //public mjVisual visual;                 // visual options
            //public mjStatistic stat;                // statistics override (if defined)
        }

        [StructLayout(LayoutKind.Sequential)]
        public unsafe struct mjOption
        {
            // timing parameters
            public double timestep;                // timestep
            public double apirate;                 // update rate for remote API (Hz)

            // solver parameters
            public double impratio;                // ratio of friction-to-normal contact impedance
            public double tolerance;               // main solver tolerance
            public double ls_tolerance;            // CG/Newton linesearch tolerance
            public double noslip_tolerance;        // noslip solver tolerance
            public double ccd_tolerance;           // convex collision solver tolerance

            // physical constants
            public fixed double gravity[3];              // gravitational acceleration
            public fixed double wind[3];                 // wind (for lift, drag and viscosity)
            public fixed double magnetic[3];             // global magnetic flux
            public double density;                 // density of medium
            public double viscosity;               // viscosity of medium

            // override contact solver parameters (if enabled)
            public double o_margin;                // margin
            public fixed double o_solref[2];        // solref
            public fixed double o_solimp[5];        // solimp
            public fixed double o_friction[5];           // friction

            // discrete settings
            public int integrator;                 // integration mode (mjtIntegrator)
            public int cone;                       // type of friction cone (mjtCone)
            public int jacobian;                   // type of Jacobian (mjtJacobian)
            public int solver;                     // solver algorithm (mjtSolver)
            public int iterations;                 // maximum number of main solver iterations
            public int ls_iterations;              // maximum number of CG/Newton linesearch iterations
            public int noslip_iterations;          // maximum number of noslip solver iterations
            public int ccd_iterations;             // maximum number of convex collision solver iterations
            public int disableflags;               // bit flags for disabling standard features
            public int enableflags;                // bit flags for enabling optional features
            public int disableactuator;            // bit flags for disabling actuators by group id

            // sdf collision settings
            public int sdf_initpoints;             // number of starting points for gradient descent
            public int sdf_iterations;             // max number of iterations for gradient descent
        };


        [StructLayout(LayoutKind.Sequential)]
        public unsafe struct mjLROpt
        { 
            // flags
            public int mode;                       // which actuators to process (mjtLRMode)
            public int useexisting;                // use existing length range if available
            public int uselimit;                   // use joint and tendon limits if available

            // algorithm parameters
            public double accel;                   // target acceleration used to compute force
            public double maxforce;                // maximum force; 0: no limit
            public double timeconst;               // time constant for velocity reduction; min 0.01
            public double timestep;                // simulation timestep; 0: use mjOption.timestep
            public double inttotal;                // total simulation time interval
            public double interval;                // evaluation time interval (at the end)
            public double tolrange;                // convergence tolerance (relative to range)
        };

        [StructLayout(LayoutKind.Sequential)]
        public unsafe struct mjsCompiler
        {      // compiler options
            public byte autolimits;              // infer "limited" attribute based on range
            public double boundmass;                // enforce minimum body mass
            public double boundinertia;             // enforce minimum body diagonal inertia
            public double settotalmass;             // rescale masses and inertias; <=0: ignore
            public byte balanceinertia;          // automatically impose A + B >= C rule
            public byte fitaabb;                 // meshfit to aabb instead of inertia box
            public byte degree;                  // angles in radians or degrees
            public fixed char eulerseq[3];                // sequence for euler rotations
            public byte discardvisual;           // discard visual geoms in parser
            public byte usethread;               // use multiple threads to speed up compiler
            public byte fusestatic;              // fuse static bodies with parent
            public int inertiafromgeom;             // use geom inertias (mjtInertiaFromGeom)
            public fixed int inertiagrouprange[2];        // range of geom groups used to compute inertia
            public byte saveinertial;            // save explicit inertial clause for all bodies to XML
            public int alignfree;                   // align free joints with inertial frame
            mjLROpt LRopt;                   // options for lengthrange computation
        }

        // this is std::string...??
        [StructLayout(LayoutKind.Sequential)]
        public unsafe struct mjString
        {
            public IntPtr val;
        }


        [StructLayout(LayoutKind.Sequential)]
        public unsafe struct mjsBody
        {
            public mjsElement* element;             // element type
            public mjString* childclass;            // childclass name

            // body frame
            public fixed double pos[3];                   // frame position
            public fixed double quat[4];                  // frame orientation
            public mjsOrientation alt;              // frame alternative orientation

            // inertial frame
            public double mass;                     // mass
            public fixed double ipos[3];                  // inertial frame position
            public fixed double iquat[4];                 // inertial frame orientation
            public fixed double inertia[3];               // diagonal inertia (in i-frame)
            public mjsOrientation ialt;             // inertial frame alternative orientation
            public fixed double fullinertia[6];           // non-axis-aligned inertia matrix

            // other
            byte mocap;                   // is this a mocap body
            double gravcomp;                 // gravity compensation
            void* userdata;           // user data
            byte explicitinertial;        // whether to save the body with explicit inertial clause
            mjsPlugin plugin;                // passive force plugin
            mjString* info;                  // message appended to compiler errors
        }

        [StructLayout(LayoutKind.Sequential)]
        public unsafe struct mjsElement
        {
        }


        [StructLayout(LayoutKind.Sequential)]
        public unsafe struct mjsOrientation
        {   // alternative orientation specifiers
            public mjtOrientation type;             // active orientation specifier
            public fixed double axisangle[4];             // axis and angle
            public fixed double xyaxes[6];                // x and y axes
            public fixed double zaxis[3];                 // z axis (minimal rotation)
            public fixed double euler[3];                 // Euler angles
        }

        [StructLayout(LayoutKind.Sequential)]
        public unsafe struct mjsPlugin
        {        // plugin specification
            mjsElement* element;             // element type
            mjString* name;                  // instance name
            mjString* plugin_name;           // plugin name
            byte active;                  // is the plugin active
            mjString* info;                  // message appended to compiler errors
        }


        [StructLayout(LayoutKind.Sequential)]
        public unsafe struct mjsGeom
        {
            public mjsElement* element;             // element type
            public mjtGeom type;                    // geom type

            // frame, size
            public fixed double pos[3];                   // position
            public fixed double quat[4];                  // orientation
            public mjsOrientation alt;              // alternative orientation
            public fixed double fromto[6];                // alternative for capsule, cylinder, box, ellipsoid
            public fixed double size[3];                  // type-specific size

            // contact related
            public int contype;                     // contact type
            public int conaffinity;                 // contact affinity
            public int condim;                      // contact dimensionality
            public int priority;                    // contact priority
            public fixed double friction[3];              // one-sided friction coefficients: slide, roll, spin
            public double solmix;                   // solver mixing for contact pairs
            public fixed double solref[mjNREF];           // solver reference
            public fixed double solimp[mjNIMP];           // solver impedance
            public double margin;                   // margin for contact detection
            public double gap;                      // include in solver if dist < margin-gap

            // inertia inference
            public double mass;                     // used to compute density
            public double density;                  // used to compute mass and inertia from volume or surface
            public mjtGeomInertia typeinertia;      // selects between surface and volume inertia

            // fluid forces
            double fluid_ellipsoid;          // whether ellipsoid-fluid model is active
            public fixed double fluid_coefs[5];           // ellipsoid-fluid interaction coefs

            // visual
            public mjString* material;              // name of material
            public fixed float rgba[4];                   // rgba when material is omitted
            public int group;                       // group

            // other
            mjString* hfieldname;            // heightfield attached to geom
            mjString* meshname;              // mesh attached to geom
            double fitscale;                 // scale mesh uniformly
            //mjDoubleVec* userdata;           // user data
            void* userdata;
            mjsPlugin plugin;                // sdf plugin
            mjString* info;                  // message appended to compiler errors
        }

        [StructLayout(LayoutKind.Sequential)]
        public unsafe struct mjsDefault
        {
        }

        [StructLayout(LayoutKind.Sequential)]
        public unsafe struct mjsJoint
        {
        }


        // Create empty spec.
        // mjSpec* mj_makeSpec(void);
        [DllImport("mujoco", CallingConvention = CallingConvention.Cdecl)]
        public static unsafe extern mjSpec* mj_makeSpec();

        // Free memory allocation in mjSpec.
        // void mj_deleteSpec(mjSpec* s);
        [DllImport("mujoco", CallingConvention = CallingConvention.Cdecl)]
        public static unsafe extern void mj_deleteSpec(mjSpec* s);

        // Compile spec to model.
        //mjModel* mj_compile(mjSpec* s, const mjVFS* vfs = null);
        [DllImport("mujoco", CallingConvention = CallingConvention.Cdecl)]
        public static unsafe extern mjModel_* mj_compile(mjSpec* s, _mjVFS* vfs);


        //int mj_saveXML(const mjSpec* s, const char* filename, char* error, int error_sz);
        [DllImport("mujoco", CallingConvention = CallingConvention.Cdecl)]
        public static unsafe extern int mj_saveXML(mjSpec* s, [MarshalAs(UnmanagedType.LPStr)] string filename, StringBuilder error, int error_sz);




        // Find body in spec by name.
        // mjsBody* mjs_findBody(mjSpec* s, const char* name);
        [DllImport("mujoco", CallingConvention = CallingConvention.Cdecl)]
        public static unsafe extern mjsBody* mjs_findBody(mjSpec* s, [MarshalAs(UnmanagedType.LPStr)]string name);

        // Add geom to body.
        //mjsGeom* mjs_addGeom(mjsBody* body, const mjsDefault* def);
        [DllImport("mujoco", CallingConvention = CallingConvention.Cdecl)]
        public static unsafe extern mjsBody* mjs_addBody(mjsBody* body, mjsDefault* def = null);


        // Add geom to body.
        //mjsGeom* mjs_addGeom(mjsBody* body, const mjsDefault* def);
        [DllImport("mujoco", CallingConvention = CallingConvention.Cdecl)]
        public static unsafe extern mjsGeom* mjs_addGeom(mjsBody* body, mjsDefault* def = null);


        // Add geom to body.
        //mjsJoint* mjs_addFreeJoint(mjsBody* body);
        [DllImport("mujoco", CallingConvention = CallingConvention.Cdecl)]
        public static unsafe extern mjsJoint* mjs_addFreeJoint(mjsBody* body);
        



        // Set element's name, return 0 on success.
        // int mjs_setName(mjsElement* element, const char* name);
        [DllImport("mujoco", CallingConvention = CallingConvention.Cdecl)]
        public static unsafe extern int mjs_setName(mjsElement* element, [MarshalAs(UnmanagedType.LPStr)] string name);




        // const char* mjs_getError(mjSpec* s);
        [DllImport("mujoco", CallingConvention = CallingConvention.Cdecl)]
        public static unsafe extern IntPtr mjs_getError(mjSpec* s);

        

    }
}
