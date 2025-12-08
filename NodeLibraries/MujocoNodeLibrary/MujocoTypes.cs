// Copyright Gradientspace Corp. All Rights Reserved.
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

        public bool IsValid => spec != null;

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

        public bool IsValid => body != null;
    }


    public unsafe class MujocoGeom
    {
        public mjsGeom* geom = null;

        public MujocoGeom(mjsGeom* geom) {
            this.geom = geom;
        }

        public bool IsValid => geom != null;
    }



    public unsafe class MujocoJoint
    {
        public mjsJoint* joint = null;

        public MujocoJoint(mjsJoint* joint)
        {
            this.joint = joint;
        }

        public bool IsValid => joint != null;
    }




    public unsafe class MujocoActuator
    {
        public mjsActuator* actuator = null;

        public MujocoActuator(mjsActuator* actuator)
        {
            this.actuator = actuator;
        }

        public bool IsValid => actuator != null;
    }


}
