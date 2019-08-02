using System.Collections.Generic;


namespace BurstAtomicThrustModule
{
    public class BATMController
    {
        static public Dictionary<uint, BATMController> batmParentParts = new Dictionary<uint, BATMController>();

        public uint persistentId;
        internal List<BurstAtomicThrustModule> batmList;

        internal double startTime;
        internal double endTime;
        internal double secondsActivated;
        internal bool burstActivated;
        internal bool burstTriggered;
        internal float savedThrottle;
        internal float savedOrigThrottle;
        internal BurstAtomicThrustModule primaryBATM;

        public static BATMController AddOrUpdate(uint persistentId, BurstAtomicThrustModule batm, bool controlAllAttached)
        {

            if (!controlAllAttached || !batmParentParts.ContainsKey(persistentId))
            {
                BATMController b = new BATMController(persistentId, batm);

                b.secondsActivated = 0; // batm.secondsActivated;
                b.burstActivated = false; // batm.burstActivated;
                b.burstTriggered = false;
                b.savedThrottle = 0; // batm.savedThrottle;
                b.savedOrigThrottle = 0;
                batm.primaryBATM = true;
                b.startTime = 0f;
                b.endTime = 0;
                b.primaryBATM = batm;
                if (controlAllAttached)
                    batmParentParts.Add(persistentId, b);
                else
                    batmParentParts.Add(batm.part.persistentId, b);
                return b;
            }
            else
            {
                if (!batmParentParts[persistentId].batmList.Contains(batm))
                    batmParentParts[persistentId].batmList.Add(batm);
                return batmParentParts[persistentId];
            }
        }

        public BATMController(uint pId, BurstAtomicThrustModule batm)
        {
            persistentId = pId;
            batmList = new List<BurstAtomicThrustModule>();
            batmList.Add(batm);
        }

    }
}
