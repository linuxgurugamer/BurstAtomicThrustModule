
namespace BurstAtomicThrustModule
{
 

    public class EngineModuleInfo
    {
        public const float GRAVITY = 9.80665f;

        internal ModuleEnginesFX moduleEngineFX;
        internal float origMaxThrust;
        internal float origMaxFuelFlow;
        internal FloatCurve origAtmosphereCurve;
        internal double fuelDensity;
        internal double origISP;

        internal float maxIncreasedThrustPercentage;


        public EngineModuleInfo(ModuleEnginesFX meFX, float mxIncreasedThrustPercentage)
        {
            moduleEngineFX = meFX;
            origMaxThrust = moduleEngineFX.maxThrust;
            origMaxFuelFlow = moduleEngineFX.maxFuelFlow;
            origAtmosphereCurve = moduleEngineFX.atmosphereCurve;

            fuelDensity = PartResourceLibrary.Instance.GetDefinition(meFX.propellants[0].name).density;
            origISP = origMaxThrust / (origMaxFuelFlow * fuelDensity * GRAVITY);

            this.maxIncreasedThrustPercentage = (mxIncreasedThrustPercentage-1) / 100f;


        }


        // Returns Isp given a 0-1 throttle value
        public float GetIsp(float level)
        {
            float isp = origAtmosphereCurve.Evaluate(level);
            return isp;
        }
    }
}
