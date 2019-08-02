using System;
using System.Collections.Generic;
using UnityEngine;




namespace BurstAtomicThrustModule
{

    public class BurstAtomicThrustModule : PartModule
    {
        #region ConfigValues
        [KSPField]
        public float maxBurstSeconds = 10;      // Max seconds burst can be sustained

        [KSPField]
        public float maxSustainableBurstSeconds = 5;

        [KSPField]
        public float reheatRatio = 5;        // Number of seconds needed to recover for each burst second

        [KSPField]
        public float maxIncreasedThrustPercentage = 1.25f;

        [KSPField]
        public bool controlAllAttached = true;

        [KSPField]
        public bool flashParts = true;

        [KSPField]
        public float flashRate = 2;
        #endregion

        BATMController batmController;

        //[KSPField(isPersistant = true, guiActive = true)]
        [KSPField(isPersistant = false, guiActive = true, guiName = "Seconds Activated", guiFormat = "F2"),
            UI_ProgressBar(minValue = 0f, maxValue = 50f,scene = UI_Scene.Flight)]
        public float SecondsActivated = 0;      // How many seconds a burst has been activated, also used during cooldown


        //[KSPField(isPersistant =true)]

        internal bool primaryBATM = false;  // First one added to controller will be primary


#if false
        [KSPField(isPersistant = true, guiActive = true, guiName = "Engine Group"),
        UI_ChooseOption(affectSymCounterparts = UI_Scene.All,  options = new[] {  "One (1)", "Two (2)", "Three (3)", "Four (4)", "Five (5)" }, scene = UI_Scene.All)]
        public int engineGroup = 0; // "All";


        [KSPField]
        public string engineGroupName = "All";
#endif

        [KSPField(guiActive = true, guiName = "Primary:")]
        public string primary = "";

        // Actions to activate/deactivate burst
        [KSPAction("Toggle burst")]
        public void ActivateBurst(KSPActionParam param)
        {
            ToggleBurst();
        }

        [KSPEvent(guiActive = true, guiName = "Toggle burst", active = true)]
        public void ToggleBurst()
        {
            if (primaryBATM)
            {
                if (batmController.burstActivated)
                {
                    //batmController.burstActivated = !batmController.burstActivated;
                    batmController.burstActivated = false;
                    batmController.burstTriggered = false;
                    FlightInputHandler.state.mainThrottle = batmController.savedOrigThrottle;
                }
                else
                {
                    if (batmController.secondsActivated == 0)
                    {
                        batmController.burstTriggered = true;
                        batmController.savedThrottle = 1f;
                        batmController.savedOrigThrottle = FlightInputHandler.state.mainThrottle;
                        FlightInputHandler.state.mainThrottle = 1f;
                    }
                    else
                    {
                        ScreenMessages.PostScreenMessage("The engines have been overused and need to finish reheating", 3f, ScreenMessageStyle.UPPER_CENTER);
                    }
                }
            }
            else
            {
                batmController.primaryBATM.ToggleBurst();
            }

         
            //if (batmController.burstTriggered && primaryBATM)
            //    batmController.savedThrottle = 1f;

            SetEventTitle();
        }

        public string GetModuleTitle()
        {
            return "Burstable Engine Module";
        }

        public override string GetModuleDisplayName()
        {
            return GetModuleTitle();
        }

        public override string GetInfo()
        {
            string toRet = "";

            toRet +=
                    "This module provides the ability to do a burst of extra thrust on atomic engines by dumping excess fuel into the core.  The drawback is that the core will be cooled down and will need some time to recover to normal operating temperature\n\n" +
                    "Max for burst: " + maxBurstSeconds + " \n" +
                    "Max thrust increase: " + maxIncreasedThrustPercentage.ToString("F2") + "%\n" +
                    "Sustainable Burst Time: " + maxSustainableBurstSeconds + " sec\n" +
                    "Fuel usage during burst: " + Math.Pow(maxIncreasedThrustPercentage, 2).ToString("F2") + "%\n" +
                    "Nuclear core reheat ratio: " + reheatRatio;
            return toRet;
        }

        internal void SetEventTitle()
        {

            if (primaryBATM)
            {
                foreach (var e in BATMController.batmParentParts[this.vessel.persistentId].batmList )
                {
                    if (batmController.burstActivated || batmController.burstTriggered)
                    {
   
                        e.Events["ToggleBurst"].guiName = "Deactivate burst mode";
                        SetThrottle(1, 1);

                    }
                    else
                    {
                        if (batmController.secondsActivated == 0)
                            e.Events["ToggleBurst"].guiName = "Activate burst mode";
                        else
                            e.Events["ToggleBurst"].guiName = "Burst mode disabled";
                    }
                }
            }
            else
                batmController.primaryBATM.SetEventTitle();
        }

        List<EngineModuleInfo> engineModules;

        // Finds ModuleEnginesFX
        void FindEngineModules(Part part)
        {
            bool multiMode = part.Modules.Contains<MultiModeEngine>();
            bool anyNukes = false;
            int engineModuleCnt = 0;

            foreach (PartModule mod in part.Modules)
            {
                if (mod.moduleName == "ModuleEnginesFX")
                {
                    engineModuleCnt++;
                    var mfx = mod as ModuleEnginesFX;
                    if (mfx.engineType == EngineType.Nuclear)
                        anyNukes = true;
                }
            }

            if (anyNukes && (engineModuleCnt == 1 || multiMode))
            {
                foreach (PartModule mod in part.Modules)
                {
                    if (mod.moduleName == "ModuleEnginesFX")
                    {
                        var mfx = mod as ModuleEnginesFX;
                        

                        //if (mfx.engineType == EngineType.Nuclear)
                        {
                            EngineModuleInfo ei = new EngineModuleInfo(mfx, maxIncreasedThrustPercentage);
                            engineModules.Add(ei);
                        }
                    }
                }
            }
        }
        
        void LoadEngineModules(Part p)
        {
            engineModules = new List<EngineModuleInfo>();
            if (controlAllAttached)
            {

                // This first finds all the engine modules in parts attached to the parent,
                // and then, for the odd case of engines attached to engines, will find
                // all the engine modules in any engine
                foreach (var part in p.vessel.parts)
                {
                    if (part.Modules.Contains<BurstAtomicThrustModule>())
                        FindEngineModules(part);
                }
            }
            else
            {
                FindEngineModules(p);
            }
        }

        void ChangeIspAndThrust(EngineModuleInfo engine, double adjustment, double fuelAdjustment = -1)
        {

            engine.moduleEngineFX.atmosphereCurve = new FloatCurve();
            engine.moduleEngineFX.atmosphereCurve.Add(0f, (float)(engine.GetIsp(0) * adjustment));
            engine.moduleEngineFX.atmosphereCurve.Add(1f, (float)(engine.origAtmosphereCurve.Evaluate(1f) * adjustment));
            engine.moduleEngineFX.atmosphereCurve.Add(4f, (float)(engine.origAtmosphereCurve.Evaluate(4f) * adjustment));
            fuelAdjustment = Math.Max(adjustment, fuelAdjustment);
            engine.moduleEngineFX.maxFuelFlow = engine.origMaxFuelFlow * (float)Math.Pow(Math.Max(1, fuelAdjustment), 2);
        }


        public void Start()
        {
            if (HighLogic.LoadedSceneIsFlight)
                batmController = BATMController.AddOrUpdate(this.part.vessel.persistentId, this, controlAllAttached);
            else
                batmController = BATMController.AddOrUpdate(0, this, controlAllAttached);

            SetEventTitle();

            // Following is for debugging/identifying primary/secondary part
            if (primaryBATM)
                primary = "BATMController Primary";
            else
                primary = "BATM secondary";


            if (HighLogic.LoadedSceneIsEditor)
                return;
            LoadEngineModules(this.part);
        }

        
        double CalculatedThrustChange
        {
            get
            {

                //  Set the max thrust to the following formula:
                //      if (secondsActivated < maxBurstSeconds / 2
                //          set the max thrust to maxIncreasedThrustPercent
                //      else                    
                //          hbs = (maxBurstSeconds/2)
                //          s2 = secondsActivated - hbs
                //          m = 1 - (s2 / hbs)
                //          set the max thrust to maxIncreasedThrustPercent * m
                // increase secondsActivated
                // 
                // This currently returns a mostly linear value

                double calculatedMaxThrust = Math.Min(1, (1 - ((batmController.secondsActivated - maxSustainableBurstSeconds) / (maxBurstSeconds - maxSustainableBurstSeconds)))) * (maxIncreasedThrustPercentage - 1) + 1;

                if (!batmController.burstActivated)
                {
                    calculatedMaxThrust = (float)Math.Max(0.01, calculatedMaxThrust);
                }
                return calculatedMaxThrust;

            }
        }

        
        internal void SetThrottle(float throttle, double newThrust)
        {
            //if (f == 1)
            //    FlightInputHandler.state.mainThrottle = f;
            //Log.Info("SetThrottle, throttle: " + throttle + ", newThrust: " + newThrust);
            foreach (var e in engineModules)
            {

                if (throttle == 0)
                {
                    ChangeIspAndThrust(e, newThrust);
                    //ChangeIspAndThrust(e, 0.01f);
                    //e.moduleEngineFX.flameout = true;
                    //e.moduleEngineFX.currentThrottle = e.moduleEngineFX.requestedThrottle = 10f;
                    e.moduleEngineFX.thrustPercentage = 10f;
                    //e.moduleEngineFX.throttleLocked = true;
                }
                else
                {
                    //e.moduleEngineFX.currentThrottle = e.moduleEngineFX.requestedThrottle = f;

                    Log.Info("newThrust: " + newThrust + ", e.origMaxThrust: " + e.origMaxThrust + ", e.maxIncreasedThrustPercentage: " + e.maxIncreasedThrustPercentage);
                    if (newThrust >1)
                    {
                        
#if true
                        e.moduleEngineFX.thrustPercentage = (float)Math.Max(10, Math.Floor(Math.Min((newThrust - 1) / (e.maxIncreasedThrustPercentage /* -1 */)/* *100f */, 100f)));
                        Log.Info("e.moduleEngineFX.thrustPercentage: " + e.moduleEngineFX.thrustPercentage);
                        ChangeIspAndThrust(e, newThrust);
#else
                        float thrustPercentage = (float)Math.Max(10, Math.Floor(Math.Min((newThrust - 1) / (e.maxIncreasedThrustPercentage /* -1 */)/* *100f */, 100f)));
                        Log.Info("thrustPercentage: " + thrustPercentage + ", newThrust: " + newThrust + ", maxIncreasedThrustPercentage: " + e.maxIncreasedThrustPercentage);
                        e.moduleEngineFX.thrustPercentage = thrustPercentage;
                        ChangeIspAndThrust(e, newThrust, Math.Pow(1+e.maxIncreasedThrustPercentage * (100 +(100- thrustPercentage) / 100), 2));
#endif
                    }
                    else
                    {
                        ChangeIspAndThrust(e, newThrust);
                        e.moduleEngineFX.thrustPercentage = 100f;
                    }
                        //e.moduleEngineFX.throttleLocked = false;
                  
                }
            }
  
#if false
            foreach (var e in engineModules)
            {
                Log.Info("throttleUseAlternate: " + e.moduleEngineFX.throttleUseAlternate + ", currentThrottle: " + e.moduleEngineFX.currentThrottle + ", requestedThrottle: " + e.moduleEngineFX.requestedThrottle);
                //e.moduleEngineFX.currentThrottle = f;
                e.moduleEngineFX.requestedThrottle = f;

                //e.moduleEngineFX.throttleUseAlternate = true;

                //e.moduleEngineFX.UpdateThrottle();
            }
#endif
        }


        void DoBurst()
        {
            bool isActive = false;

            foreach (EngineModuleInfo e in engineModules)
            {
                if (e.moduleEngineFX.EngineIgnited) // isActiveAndEnabled)
                {
                    isActive = true;
                }
                ChangeIspAndThrust(e, CalculatedThrustChange);
            }
            if (isActive)
            {
                if (batmController.startTime == 0)
                    batmController.startTime = Planetarium.GetUniversalTime();

                batmController.secondsActivated = Planetarium.GetUniversalTime() - batmController.startTime + (float)Planetarium.fetch.fixedDeltaTime;

                SetThrottle(1, CalculatedThrustChange);
            }
            else
            {
                ToggleBurst();
            }
        }

        void DoBurstTimeExceeded()
        {
            ToggleBurst();

            if (batmController.secondsActivated > 0)
            {
                SetThrottle(0, CalculatedThrustChange);
                FlightInputHandler.state.mainThrottle = batmController.savedOrigThrottle;
                foreach (var e in engineModules)
                {
                    ChangeIspAndThrust(e, CalculatedThrustChange);
                    //ChangeIspAndThrust(e, 0.01f);
                    //e.moduleEngineFX.flameout = true;
                    //e.moduleEngineFX.currentThrottle = e.moduleEngineFX.requestedThrottle = 0.1f;
                    //e.moduleEngineFX.thrustPercentage = 0.1f;
                    //e.moduleEngineFX.throttleLocked = true;
                }
                Fields["SecondsActivated"].guiName = "Reheat cycle";
            }
            else
                Fields["SecondsActivated"].guiName = "Seconds activated";
        }

        void DoActiveBurst()
        {
            if (batmController.secondsActivated < maxBurstSeconds)
            {
                DoBurst();
            }
            else
            {
                DoBurstTimeExceeded();
            }
        }


        void DoNormalThrust()
        {
            if (batmController.secondsActivated > 0)
            {
                SetThrottle(0, 1);
                if (primaryBATM)
                    batmController.secondsActivated = Math.Max(0, batmController.secondsActivated - (float)Planetarium.fetch.fixedDeltaTime / reheatRatio);
                if (batmController.secondsActivated <= 0)
                {
                    batmController.startTime = 0;
                    batmController.endTime = 0;
                    batmController.secondsActivated = 0;
                    batmController.burstActivated = false;
                    batmController.burstTriggered = false;
                    SetEventTitle();
                }


                foreach (var e in engineModules)
                {
                    //ChangeIspAndThrust(e, 0.01f);
                    ChangeIspAndThrust(e, 1);

                    //e.moduleEngineFX.flameout = true;
                    //e.moduleEngineFX.currentThrottle = e.moduleEngineFX.requestedThrottle = 0.1f;
                    //e.moduleEngineFX.thrustPercentage = 0.1f;
                    //e.moduleEngineFX.throttleLocked = true;

                }
                Fields["SecondsActivated"].guiName = "Reheat cycle";
            }
            else
            {
                SetThrottle(1, 1);
                foreach (var e in engineModules)
                {
                    ChangeIspAndThrust(e, 1);
                    //e.moduleEngineFX.flameout = false;

                    //e.moduleEngineFX.throttleLocked = false;

                }
                Fields["SecondsActivated"].guiName = "Seconds activated";
            }
        }

#if false
        void test(ModuleEnginesFX engineMod, double atmosphere,  float thrust)
        {
            float maxFuelFlow = engineMod.maxFuelFlow;
            float minFuelFlow = engineMod.minFuelFlow;
            float thrustPercentage = engineMod.thrustPercentage;
            List<Transform> thrustTransforms = engineMod.thrustTransforms;
            List<float> thrustTransformMultipliers = engineMod.thrustTransformMultipliers;
            
            FloatCurve atmosphereCurve = engineMod.atmosphereCurve;
            bool atmChangeFlow = engineMod.atmChangeFlow;
            //FloatCurve atmCurve = engineMod.atmCurve;
            float currentThrottle = engineMod.currentThrottle;
            float IspG = engineMod.g;
           
            List<Propellant> propellants = engineMod.propellants;
            bool active = engineMod.isOperational;
            float resultingThrust = engineMod.maxThrust * thrust;
            bool isFlamedOut = engineMod.flameout;
            double flowRate = 0.0;

            double isp = 0;
            isp = atmosphereCurve.Evaluate((float)atmosphere);
            flowRate = GetFlowRate(resultingThrust, isp);
            double consumptionRate = 0;

            float flowMass = 0f;
            for (int i = 0; i < propellants.Count; ++i)
            {
                Propellant propellant = propellants[i];
                if (!propellant.ignoreForIsp)
                    flowMass += propellant.ratio *
                        PartResourceLibrary.Instance.GetDefinition(propellant.name).density;

                consumptionRate += propellant.ratio * flowRate / flowMass;
            }

            Log.Info("isp: " + isp + ", flowRate: " + flowRate + ", flowMass: " + flowMass + ", consumptionRate: " + consumptionRate);
        }
        public static double GetFlowRate(double thrust, double isp)
        {
            return thrust / GetExhaustVelocity(isp);
        }
        public static double GetExhaustVelocity(double isp)
        {
            return isp * UTILS.GRAVITY;
        }
#endif

        public void FixedUpdate()
        {
            if (HighLogic.LoadedSceneIsEditor)
                return;
            SecondsActivated = (float)batmController.secondsActivated;
            if (flashParts)
                DoFlash();
            if (!primaryBATM)
                return;
                
            if (batmController.burstActivated && FlightInputHandler.state.mainThrottle != batmController.savedThrottle)
            {
                ToggleBurst();              // Toggle it off if the throttle is lowered at all
                FlightInputHandler.state.mainThrottle = batmController.savedOrigThrottle;
            }

            if (batmController.burstTriggered)
            {
                batmController.burstTriggered = false;
                batmController.burstActivated = true;
                SetEventTitle();
                DoActiveBurst();
            }
            else
            {
                if (batmController.burstActivated)
                {
                    DoActiveBurst();
                }
                else
                {
                    DoNormalThrust();
                }
            }

        }

        void DoFlash()
        {
            if (batmController.burstActivated)
                Flash(Color.red);
            else
            {
                if (batmController.secondsActivated > 0)
                    Flash(Color.blue);
                else
                    Flash(Color.black, true);
            }
        }

        double lastTime;
        bool flashStatus = false;
        void Flash(Color color, bool off = false)
        {
            if (off)
            {
                flashStatus = false;
                this.part.SetHighlightDefault();
                return;
            }
            if (Planetarium.GetUniversalTime() - lastTime > 1 / flashRate)
            {
                lastTime = Planetarium.GetUniversalTime();
                if (flashStatus)
                {
                    flashStatus = false;
                    this.part.SetHighlightDefault();
                }
                else
                {
                    flashStatus = true;
                    this.part.SetHighlightDefault();

                    this.part.SetHighlightColor(color);
                    this.part.SetHighlightType(Part.HighlightType.AlwaysOn);
                    this.part.highlighter.ConstantOn(color);
                    this.part.highlighter.SeeThroughOn();
                    this.part.SetHighlight(true, false);
                }
            }
        }
    }
}
