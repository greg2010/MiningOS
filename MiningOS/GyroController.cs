using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using SpaceEngineers.Game.ModAPI.Ingame;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text;
using System;
using VRage.Collections;
using VRage.Game.Components;
using VRage.Game.ModAPI.Ingame;
using VRage.Game.ModAPI.Ingame.Utilities;
using VRage.Game.ObjectBuilders.Definitions;
using VRage.Game;
using VRageMath;

namespace IngameScript
{
    class GyroController
    {
        public struct Job
        {
            public Vector3D coordinates;
            public IMyCubeBlock reference;
            public Job(Vector3D coordinates, IMyCubeBlock reference)
            {
                this.coordinates = coordinates;
                this.reference = reference;
            }
        }

        public List<IMyGyro> gyroList;
        private List<Job> jobList = new List<Job>();
        readonly Action<string> Echo;

        public GyroController(List<IMyGyro> gyroList, Action<string> Echo)
        {
            this.gyroList = gyroList;
            this.Echo = Echo;
        }

        public void Schedule(Job job)
        {
            jobList.Add(job);
        }

        public void Tick()
        {

            if (jobList.Count > 0)
            {
                Job curJob = jobList.First();
                Echo($"GyroController: Found job with coords {curJob.coordinates.ToString()}");
                var curPosn = curJob.reference.GetPosition();
                Vector3D directionVec = curJob.coordinates - curPosn;
                double pitch, yaw = 0;
                VectorMath.GetRotationAngles(directionVec, curJob.reference.WorldMatrix.Forward, curJob.reference.WorldMatrix.Left, curJob.reference.WorldMatrix.Up, out yaw, out pitch);
                Echo($"Pitch: {pitch} Yaw: {yaw}");
                var originalRotVec = new Vector3D(-pitch, yaw, 0);
                int gyrosCompleted = 0;
                foreach (var gyro in gyroList)
                {
                    var originalWorldRotVec = VectorMath.GetWorldDirection(originalRotVec, curJob.reference);
                    var shiftedGyroRotVector = VectorMath.GetBodyDirection(originalWorldRotVec, gyro);
                    float pitchRounded = (float)Math.Round(shiftedGyroRotVector.X, 2);
                    float yawRounded = (float)Math.Round(shiftedGyroRotVector.Y, 2);
                    float rollRounded = (float)Math.Round(shiftedGyroRotVector.Z, 2);

                    Echo($"PitchSpeed: {pitchRounded} YawSpeed: {yawRounded} RollSpeed: {rollRounded}");
                    if (pitchRounded == 0f && yawRounded == 0f && rollRounded == 0f)
                    {
                        gyro.GyroOverride = false;
                        gyrosCompleted++;
                    }
                    else
                    {
                        gyro.Pitch = pitchRounded;
                        gyro.Yaw = yawRounded;
                        gyro.Roll = rollRounded;
                        gyro.GyroOverride = true;
                    }
                }
                if (gyrosCompleted == gyroList.Count)
                {
                    Echo($"Job with coords {curJob.coordinates.ToString()} completed, popping");
                    jobList.Remove(curJob);

                }
            }
        }
    }
}
