BurstAtomicThrustModule

This module allows extra fuel to be dumped into the core of a solid-core atomic engine to gain extra thrust for a short period of time.  This process inevitability cools down the core to such an extent that it loses all power and requires some time to rebuild the core heat in order to be able to function again.

This feature is targeted at space tugs and atomic engines only.  It is not intended to work for non-nuclear engines

Features

Activating the module on one engine will automatically activate it on all other engines that have it on the vessel that are active.
Engines will flash red while in burst mode, and will flash blue while in reheat recovery mode
Can be triggered by an action group if desired.
Upon triggering, throttle will be set to 100%
The extra fuel used is the square of the extra thrust; for example, if the thrust in burst mode is 1.5x the regular thrust, the fuel used will be 2.25x the amount of fuel normally used.
Any movement of the throttle will automatically shut down the burst mode, and also shut down the engines for reheating


Current Limitation

