///
/// === Example integration into Mission ===
///
/// //$include <put-your-path-here>/CKillDisusedPlanes.cs
///
/// class Mission : AMission {
///     ...
///     public CKillDisusedPlanes m_KillDisusedPlanes = null;
///     ...
///     public override void OnBattleStarted()
///     {
///         ...
///         m_KillDisusedPlanes = new CKillDisusedPlanes(this);
///         ...
///     }
///     ...
///     public override void OnPlaceLeave(Player player, AiActor actor, int placeIndex)
///     {
///        base.OnPlaceLeave(player, actor, placeIndex);
///         ...
///        m_KillDisusedPlanes.OnPlaceLeave(player, actor, placeIndex);
///         ...
///     }
///     ...

using System;
using System.Threading;
using maddox.game;       /// AMission
using maddox.game.world; /// AiActor
using maddox.GP;//-----------------------------

public class CKillDisusedPlanes {
    /// Set to true to see debug messages in server messages
    private const bool DEBUG_MESSAGES = true;

    // number of seconds to wait between damage and destroy of aircraft on friendly airfield
    private const int TIMEOUT_NOTAIRBORNE = 0; // NEVER CHANGE! IF NOT EQUAL TO ZERO AIRCRAFT WITH BOMBS WILL EXPLODE EVERYTHING AROUND!

    // number of seconds to wait between damage and destroy of aircraft on friendly airfield
    private const int TIMEOUT_ATOPERATIONALAIRBASE = 600;

    // number of seconds to wait between damage and destroy airborne aircraft
    private const int TIMEOUT_ABANDONED = -1; // < 0 means do not destroy aircraft

    private AMission baseMission = null;
    private CMissionCommon missionCommon = null;

    /// <summary>
    /// Constructor
    /// </summary> 
    /// <param name="Mission">The Mission (use 'this')</param>
    public CKillDisusedPlanes(CMissionCommon mission_common)
    {
        baseMission = mission_common.baseMission;
        missionCommon = mission_common;
    }
    /// <summary>
    /// Make sure that the plane a player has left cannot be regained
    /// </summary> 
    /// <param name="CurPlayer"></param>
    /// <param name="ActorMain"></param>
    /// <param name="iPlaceIndex"></param>
    public void OnPlaceLeave(Player CurPlayer, AiActor ActorMain, int iPlaceIndex)
    {
        // NO DELAYS HERE! Otherwise aircraft with bombs on the ground will blow up all around!
        baseMission.Timeout(0, () => {
            TryDamagePlane(ActorMain, CurPlayer); 
        });
    }
    /// <summary>
    /// Make an AI controlled aircraft unusable if have to
    /// </summary> 
    /// <param name="CurPlayer"></param>
    /// <param name="ActorMain"></param>
    private void TryDamagePlane(AiActor ActorMain, Player CurPlayer)
    {
        if (DEBUG_MESSAGES && CLog.IsInitialized) CLog.Write("Try to damage/destroy aircraft" + ActorMain.Name() + " that was controlled by player " + CurPlayer.Name());

        if (ActorMain == null)
        {
            if (DEBUG_MESSAGES && CLog.IsInitialized) CLog.Write("Damage/destroy cancelled due to (ActorMain == null).");
            return;
        }

        if (!(ActorMain is AiAircraft))
        {
            if (DEBUG_MESSAGES && CLog.IsInitialized) CLog.Write("Damage/destroy " + ActorMain.Name() + " cancelled. ActorMain is NOT AiAircraft");
            return;
        }

        AiAircraft Aircraft = (ActorMain as AiAircraft);

        if (!IsNoPLayersInAircraft(Aircraft))
        {
            if (DEBUG_MESSAGES && CLog.IsInitialized) CLog.Write("Damage/destroy " + ActorMain.Name() + " cancelled. Player still in aircraft.");
            if (Aircraft.Player(0) == null)
            {
                if (DEBUG_MESSAGES && CLog.IsInitialized) CLog.Write("But there is no pilot! Remove fuel from tanks for AI pilot!");
                //Without fuel AI can't move to faraway
                if (!missionCommon.IsDefueledAircraft(Aircraft, false))
                {
                    int fuelPct = Aircraft.GetCurrentFuelQuantityInPercent();
                    missionCommon.defueledAcircrafts.Add(new CMissionCommon.DefueledAircraft(Aircraft, fuelPct));
                }
                Aircraft.RefuelPlane(0);
            }
            return;
        }

        if (!Aircraft.IsAlive())
        {
            if (DEBUG_MESSAGES && CLog.IsInitialized) CLog.Write("Damage/destroy " + ActorMain.Name() + " cancelled due to it is NOT ALIVE already.");
            return;
        }

        // If REALISTIC_DESPAWN disabled just destroy plane fast.
        if (!CConfig.REALISTIC_AIRCRAFT_DESPAWN_TIMEOUT)
        {
            if (DEBUG_MESSAGES && CLog.IsInitialized) CLog.Write("Aircraft " + Aircraft.Name() + " will be destroyed immediately.");
            baseMission.Timeout(0, () =>
            {
                DestroyPlane(Aircraft);
            });
            return;
        }

        /// Make Damage
        /// We wrap in try ... catch to make sure at least *some* of them are effected
        /// no matter what happens (e.g. the Wing part throws on Blenheims)
        CMissionCommon.EAircraftLocation aircraftLocation = missionCommon.GetAircraftLocation(Aircraft);
        if (aircraftLocation == CMissionCommon.EAircraftLocation.Airborne)
        {
            if (DEBUG_MESSAGES && CLog.IsInitialized) CLog.Write("Aircraft" + ActorMain.Name() + " airborne. Damage will be done now to prevent AI control");
            /// Damage named parts
            try
            {
                Aircraft.hitNamed(part.NamedDamageTypes.ControlsElevatorDisabled);
                Aircraft.hitNamed(part.NamedDamageTypes.ControlsAileronsDisabled);
                Aircraft.hitNamed(part.NamedDamageTypes.ControlsRudderDisabled);
                Aircraft.hitNamed(part.NamedDamageTypes.FuelPumpFailure);
                Aircraft.hitNamed(part.NamedDamageTypes.Eng0TotalFailure);
                Aircraft.hitNamed(part.NamedDamageTypes.ElecPrimaryFailure);
                Aircraft.hitNamed(part.NamedDamageTypes.ElecBatteryFailure);
            }
            catch (Exception e)
            {
                if (DEBUG_MESSAGES && CLog.IsInitialized) CLog.Write(e.ToString() + "\n" + e.Message.ToString());
            }

            /// Damage wings
            //try
            //{
            //    Aircraft.hitLimb(part.LimbNames.WingL1, -0.5);
            //    Aircraft.hitLimb(part.LimbNames.WingL2, -0.5);
            //    Aircraft.hitLimb(part.LimbNames.WingL3, -0.5);
            //    Aircraft.hitLimb(part.LimbNames.WingL4, -0.5);
            //    Aircraft.hitLimb(part.LimbNames.WingL5, -0.5);
            //    Aircraft.hitLimb(part.LimbNames.WingL6, -0.5);
            //    Aircraft.hitLimb(part.LimbNames.WingL7, -0.5);
            //}
            //catch(Exception e)
            //{
            //    if (DEBUG_MESSAGES && CLog.IsInitialized) CLog.Write(e.ToString() + "\n" + e.Message.ToString());
            //}

            /// Damage engines
            try
            {
                int iNumOfEngines = (Aircraft.Group() as AiAirGroup).aircraftEnginesNum();

                for (int i = 0; i < iNumOfEngines; i++)
                {
                    Aircraft.hitNamed((part.NamedDamageTypes)Enum.Parse(typeof(part.NamedDamageTypes), "Eng" + i.ToString() + "TotalFailure"));
                }
            }
            catch (Exception e)
            {
                if (DEBUG_MESSAGES && CLog.IsInitialized) CLog.Write(e.ToString() + "\n" + e.Message.ToString());
            }
        }
        else
        {
            if (DEBUG_MESSAGES && CLog.IsInitialized) CLog.Write("Aircraft " + ActorMain.Name() + " on the ground, no damage, just remove fuel from tanks for AI");
            // aircraft on the ground, no need to brake it. Without fuel AI cant move to faraway
            if (!missionCommon.IsDefueledAircraft(Aircraft, false))
            {
                int fuelPct = Aircraft.GetCurrentFuelQuantityInPercent();
                missionCommon.defueledAcircrafts.Add(new CMissionCommon.DefueledAircraft(Aircraft, fuelPct));
            }
            Aircraft.RefuelPlane(0);
        }

        int iDestroyTimeout = TIMEOUT_ABANDONED;
        if((aircraftLocation == CMissionCommon.EAircraftLocation.FriendlyAirfield)
        || (aircraftLocation == CMissionCommon.EAircraftLocation.EnemyAirfield))
        {
            iDestroyTimeout = TIMEOUT_ATOPERATIONALAIRBASE;
            if (!Aircraft.IsAirborne())
            {
                iDestroyTimeout = TIMEOUT_NOTAIRBORNE;
            }
        }

        // Destroy time! ... maybe.
        if (iDestroyTimeout > -1)
        {
            if (DEBUG_MESSAGES && CLog.IsInitialized) CLog.Write("Aircraft " + Aircraft.Name() + " will be destroyed in " + iDestroyTimeout.ToString() + " seconds.");
            baseMission.Timeout(iDestroyTimeout, () =>
            {
                DestroyPlane(Aircraft);
            });
        }
        else
        {
            if (DEBUG_MESSAGES && CLog.IsInitialized) CLog.Write("Aircraft " + Aircraft.Name() + " will be left abandoned. No destruction wil be done.");
        }
    }
    /// <summary>
    /// Destroy aircraft
    /// </summary> 
    public void DestroyPlane(AiAircraft Aircraft)
    {
        try
        {
            if (Aircraft != null)
            {
                if (IsNoPLayersInAircraft(Aircraft))
                {
                    if (DEBUG_MESSAGES && CLog.IsInitialized) CLog.Write("DestroyPlane() : aircraft " + Aircraft.Name() + " to be destroyed right now.");
                    Aircraft.Destroy();
                }
                else
                {
                    if (DEBUG_MESSAGES && CLog.IsInitialized) CLog.Write("DestroyPlane() : aircraft " + Aircraft.Name() + " will not to be destroyed beacuase player was found inside this aircraft.");
                }
            }
            else
            {
                if (DEBUG_MESSAGES && CLog.IsInitialized) CLog.Write("DestroyPlane() : no aircraft (Aircraft == null)");
            }
        }
        catch(Exception e)
        {
            if (DEBUG_MESSAGES && CLog.IsInitialized) CLog.Write(e.ToString() + "\n" + e.Message.ToString());
        }
    }
    /// <summary>
    /// Check if no players in aircraft
    /// </summary> 
    /// <param name="Aircraft"></param>
    /// <returns>true if no real player is in any of the plane's positions</returns>
    public bool IsNoPLayersInAircraft(AiAircraft Aircraft)
    {
        try
        {
            if (Aircraft == null)
            {
                return false;
            }

            /// check if a player is in any of the "places"
            for (int i = 0; i < Aircraft.Places(); i++)
            {
                if (Aircraft.Player(i) != null)
                {
                    return false;
                }
            }
        }
        catch (Exception e)
        {
            if (DEBUG_MESSAGES && CLog.IsInitialized) CLog.Write(e.ToString() + "\n" + e.Message.ToString());
        }

        return true;
    }
}