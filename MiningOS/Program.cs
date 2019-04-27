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
    partial class Program : MyGridProgram
    {
        private IMyRemoteControl mainController = null;
        private Dictionary<Base6Directions.Direction, List<IMyThrust>> directionalThrusters = null;
        private GyroController gyroController = null;

        private IMyRemoteControl getMainController()
        {
            var controllers = new List<IMyRemoteControl>();
            GridTerminalSystem.GetBlocksOfType<IMyRemoteControl>(controllers);
            controllers = controllers.Where(controller => controller.CustomName.Contains("Main")).ToList();
            if (controllers.Count != 1)
            {
                Echo($"{controllers.Count} main remote controllers found, exiting...");
                throw new Exception();
            }
            return controllers[0];
        }

        private Dictionary<Base6Directions.Direction, List<IMyThrust>> getDirectionalThrusters(IMyCubeBlock reference)
        {
            Dictionary<Base6Directions.Direction, List<IMyThrust>> thrustersDict = new Dictionary<Base6Directions.Direction, List<IMyThrust>>();
            List<IMyThrust> thrusters = new List<IMyThrust>();
            GridTerminalSystem.GetBlocksOfType<IMyThrust>(thrusters);
            foreach (var thruster in thrusters)
            {
                var direction = reference.Orientation.TransformDirectionInverse(Base6Directions.GetFlippedDirection(thruster.Orientation.Forward));
                if (!thrustersDict.ContainsKey(direction))
                {
                    thrustersDict.Add(direction, new List<IMyThrust>());
                }
                thrustersDict[direction].Add(thruster);
            }
            return thrustersDict;
        }

        private List<IMyGyro> getGyros()
        {
            List<IMyGyro> gyroList = new List<IMyGyro>();
            GridTerminalSystem.GetBlocksOfType<IMyGyro>(gyroList);
            return gyroList;
        }

        public Program()
        {
            mainController = getMainController();
            directionalThrusters = getDirectionalThrusters(mainController);
            gyroController = new GyroController(getGyros(), this.Echo);
            gyroController.Schedule(new GyroController.Job(new Vector3D(-425.34d, -166.88d, 917.13d), this.mainController));
            gyroController.Schedule(new GyroController.Job(new Vector3D(-190.29, -47.17, 1049.49), this.mainController));
            

            foreach (var tg in directionalThrusters)
            {
                Echo($"Direction: {tg.Key} count: {tg.Value.Count} ");
            }
            Runtime.UpdateFrequency = UpdateFrequency.Update10;
        }





        public void Save()
        {

        }

        public void Main(string argument, UpdateType updateSource)
        {
            gyroController.Tick();
        }


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
            Action<string> Echo;

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

        class PID
        {
            private double kP;
            private double kI;
            private double kD;

            private double timeStep;

            private double prevError;

            private double cP;
            private double cI;
            private double cD;


            public PID(double kP, double kI, double kD, double timeStep)
            {
                this.kP = kP;
                this.kI = kI;
                this.kD = kD;
                this.timeStep = timeStep;
                this.Reset();
            }

            public void Reset()
            {
                this.prevError = 0d;
                this.cP = 0d;
                this.cI = 0d;
                this.cD = 0d;
            }

            public double Update(double error)
            {
                var deltaError = error - this.prevError;
                this.cP = error;
                this.cI = error * timeStep;
                this.cD = timeStep != 0 ? deltaError / timeStep : 0;
                this.prevError = error;
                return (this.kP * this.cP) + (this.kI * this.cI) + (this.kD * this.cD);
            }
        }
    }
}