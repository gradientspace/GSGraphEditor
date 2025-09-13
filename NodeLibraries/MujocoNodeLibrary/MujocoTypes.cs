using g3;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Mujoco.MujocoSpecLib;

namespace Mujoco.Nodes
{
    public unsafe class MujocoSpec : IDisposable
    {
        public mjSpec* spec = null;

        public void Initialize()
        {
            if (spec == null)
                spec = mj_makeSpec();                                  // make an empty spec
        }
        public void Release()
        {
            if (spec != null) {
                mj_deleteSpec(spec);
                spec = null;
            }
        }

        ~MujocoSpec() {
            Release();
        }
        public void Dispose() {
            Release();
        }

    }



    public unsafe class MujocoBody
    {
        public mjsBody* body = null;

        public MujocoBody(mjsBody* body) {
            this.body = body;
        }
    }


    public unsafe class MujocoGeom
    {
        public mjsGeom* geom = null;

        public MujocoGeom(mjsGeom* geom) {
            this.geom = geom;
        }
    }


}
