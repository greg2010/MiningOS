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
            gyroController.shipOrientation = new GyroController.ShipOrientation(new Vector3D (-425.34d, -166.88d, 917.13d), this.mainController);
            //gyroController.Schedule(new GyroController.Job(new Vector3D(-190.29, -47.17, 1049.49), this.mainController));

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
    }
}