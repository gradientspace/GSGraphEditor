using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using static Mujoco.MujocoLib;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Mujoco
{
    public static class MujocoSpecLib
    {
        [StructLayout(LayoutKind.Sequential)]
        public unsafe struct mjSpec
        {
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
            mjsElement* element;             // element type
            mjString* childclass;            // childclass name

            // body frame
            public fixed double pos[3];                   // frame position
            public fixed double quat[4];                  // frame orientation
            mjsOrientation alt;              // frame alternative orientation

            // inertial frame
            double mass;                     // mass
            public fixed double ipos[3];                  // inertial frame position
            public fixed double iquat[4];                 // inertial frame orientation
            public fixed double inertia[3];               // diagonal inertia (in i-frame)
            mjsOrientation ialt;             // inertial frame alternative orientation
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
            mjtOrientation type;             // active orientation specifier
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
            int contype;                     // contact type
            int conaffinity;                 // contact affinity
            int condim;                      // contact dimensionality
            int priority;                    // contact priority
            public fixed double friction[3];              // one-sided friction coefficients: slide, roll, spin
            double solmix;                   // solver mixing for contact pairs
            public fixed double solref[mjNREF];           // solver reference
            public fixed double solimp[mjNIMP];           // solver impedance
            double margin;                   // margin for contact detection
            double gap;                      // include in solver if dist < margin-gap

            // inertia inference
            double mass;                     // used to compute density
            double density;                  // used to compute mass and inertia from volume or surface
            mjtGeomInertia typeinertia;      // selects between surface and volume inertia

            // fluid forces
            double fluid_ellipsoid;          // whether ellipsoid-fluid model is active
            public fixed double fluid_coefs[5];           // ellipsoid-fluid interaction coefs

            // visual
            mjString* material;              // name of material
            public fixed float rgba[4];                   // rgba when material is omitted
            int group;                       // group

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
        public static unsafe extern mjsGeom* mjs_addGeom(mjsBody* body, mjsDefault* def = null);




        // Set element's name, return 0 on success.
        // int mjs_setName(mjsElement* element, const char* name);
        [DllImport("mujoco", CallingConvention = CallingConvention.Cdecl)]
        public static unsafe extern int mjs_setName(mjsElement* element, [MarshalAs(UnmanagedType.LPStr)] string name);




        // const char* mjs_getError(mjSpec* s);
        [DllImport("mujoco", CallingConvention = CallingConvention.Cdecl)]
        public static unsafe extern IntPtr mjs_getError(mjSpec* s);

        

    }
}
