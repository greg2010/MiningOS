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

        static int logSize = 30;

        /*  
         * Gyro PID constants
         */

        static double gyrokP = 2d;
        static double gyrokI = 0.1d;
        static double gyrokD = 0.35d;

        /*
         * Engine constants 
         */

        static double enginekP = 1d;
        static double enginekI = 0.05d;
        static double enginekD = 0.01d;


        double timeStep = 1 / 100d;
        private IMyRemoteControl mainController = null;
        private GyroController gyroController = null;
        private EngineController engineController = null;
        private RouteController routeController = null;

        private Logger logger = null;

        private IMyRemoteControl GetMainController()
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

        private List<IMyGyro> GetGyros()
        {
            List<IMyGyro> gyroList = new List<IMyGyro>();
            GridTerminalSystem.GetBlocksOfType(gyroList);
            return gyroList;
        }

        private List<IMyThrust> GetThrusters()
        {
            List<IMyThrust> thrusterList = new List<IMyThrust>();
            GridTerminalSystem.GetBlocksOfType(thrusterList);
            return thrusterList;
        }

        public Program()
        {
            this.logger = new Logger(Echo, Me, logSize);
            
            this.mainController = GetMainController();
            this.gyroController = new GyroController(GetGyros(), this.Echo, gyrokP, gyrokI, gyrokD, timeStep);
            //gyroController.shipOrientation = new GyroController.ShipOrientation(new Vector3D (-425.34d, -166.88d, 917.13d), this.mainController);

            this.engineController = new EngineController(GetThrusters(), this.Echo, mainController, enginekP, enginekI, enginekD, timeStep);
            //engineController.shipWorldWaypoint = new EngineController.ShipWorldWaypoint(new Vector3D(-425.34d, -166.88d, 917.13d), this.mainController, 10);
            //gyroController.Schedule(new GyroController.Job(new Vector3D(-190.29, -47.17, 1049.49), this.mainController));

            this.routeController = new RouteController(this.logger, engineController, gyroController);

            RouteController.Waypoint waypoint1 = new RouteController.Waypoint(new EngineController.ShipWorldWaypoint(new Vector3D(-425.34d, -166.88d, 917.13d), this.mainController, 1000), new GyroController.ShipOrientation(new Vector3D(-425.34d, -166.88d, 917.13d), this.mainController));
            RouteController.Waypoint waypoint2 = new RouteController.Waypoint(new EngineController.ShipWorldWaypoint(new Vector3D(-190.29, -47.17, 1049.49), this.mainController, 1000), new GyroController.ShipOrientation(new Vector3D(-190.29, -47.17, 1049.49), this.mainController));

            this.routeController.EnqueueWaypoint(waypoint1);
            this.routeController.EnqueueWaypoint(waypoint2);

            Runtime.UpdateFrequency = UpdateFrequency.Update1;
        }





        public void Save()
        {

        }

        public void Main(string argument, UpdateType updateSource)
        {
            this.routeController.Tick();
        }
    }
}