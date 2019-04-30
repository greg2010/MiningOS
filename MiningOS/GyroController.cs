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

        public class ShipOrientation
        {
            public Vector3D coordinates;
            public IMyCubeBlock reference;

            public ShipOrientation(Vector3D coordinates, IMyCubeBlock reference)
            {
                this.coordinates = coordinates;
                this.reference = reference;
            }
        }

        private List<IMyGyro> gyroList;

        readonly private PID PitchPID;
        readonly private PID YawPID;
        readonly private PID RollPID;

        readonly Action<string> Echo;
        public ShipOrientation shipOrientation;

        public GyroController(List<IMyGyro> gyroList, Action<string> Echo, double kP, double kI, double kD, double timeStep)
        {
            this.gyroList = gyroList;
            this.Echo = Echo;

            this.PitchPID = new PID(kP, kI, kD, timeStep);
            this.YawPID = new PID(kP, kI, kD, timeStep);
            this.RollPID = new PID(kP, kI, kD, timeStep);
        }

        public void EnableOverride()
        {
            foreach (var gyro in gyroList)
            {
                gyro.GyroOverride = true;
            }
        }


        public void DisableOverride()
        {
            foreach (var gyro in gyroList)
            {
                gyro.GyroOverride = false;
            }
        }

        private bool ApplyRotation(Vector3D coordinates, IMyCubeBlock reference)
        {
            Vector3D curPosn = reference.GetPosition();
            Vector3D directionVec = coordinates - curPosn;
            double pitch, yaw = 0;
            VectorMath.GetRotationAngles(directionVec, reference.WorldMatrix.Forward, reference.WorldMatrix.Left, reference.WorldMatrix.Up, out yaw, out pitch);

            Vector3D originalRotVec = new Vector3D(PitchPID.Update(-pitch), YawPID.Update(yaw), RollPID.Update(0));
            Echo($"Error: {-pitch} {yaw} {0}");
            Echo($"PID Response: {originalRotVec.X} {originalRotVec.Y} {originalRotVec.Z}");
            Echo($"PID - error: {originalRotVec.X - -pitch} {originalRotVec.Y - yaw} {originalRotVec.Z - 0}");
            Vector3D originalWorldRotVec = VectorMath.GetWorldDirection(originalRotVec, reference);
            int gyrosCompleted = 0;
            foreach (var gyro in gyroList)
            {
                Vector3D shiftedGyroRotVector = VectorMath.GetBodyDirection(originalWorldRotVec, gyro);
                float pitchRounded = (float)Math.Round(shiftedGyroRotVector.X, 2);
                float yawRounded = (float)Math.Round(shiftedGyroRotVector.Y, 2);
                float rollRounded = (float)Math.Round(shiftedGyroRotVector.Z, 2);
                
                if (pitchRounded == 0f && yawRounded == 0f && rollRounded == 0f) ++gyrosCompleted;
                gyro.Pitch = pitchRounded;
                gyro.Yaw = yawRounded;
                gyro.Roll = rollRounded;
            }

            return gyrosCompleted == gyroList.Count;
        }

        public void Tick()
        {
            if (shipOrientation != null)
            {
                this.EnableOverride();
                ApplyRotation(shipOrientation.coordinates, shipOrientation.reference);
            } else
            {
                this.DisableOverride();

            }

        }
    }
}
