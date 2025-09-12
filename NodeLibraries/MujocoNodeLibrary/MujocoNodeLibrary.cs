using Gradientspace.NodeGraph;
using Mujoco;
using System;
using System.Text;
using System.Runtime.InteropServices;
using static Mujoco.MujocoLib;
using static Mujoco.MujocoSpecLib;

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
    }

}
