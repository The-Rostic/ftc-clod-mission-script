///
/// Remove Planes Abandoned by Players. Prevents Outside Views, too.
///
/// This is used for making sure that the plane a player has left
/// cannot be regained (outside views) and is removed from the game.
///
///
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
using maddox.game;       /// AMission
using maddox.game.world; /// AiActor

public class CKillDisusedPlanes {
    /// Set to true to see debug messages in server messages
    public bool DEBUG_MESSAGES = true;
    
    protected AMission m_Mission = null;
    /// number of seconds to wait between damage and destroy just spawned or landed aircraft
    protected int m_iSecondsUntilRemove = 2;
    /// number of seconds to wait between damage and destroy airborne aircraft
    protected int m_iSecondsUntilRemoveAirborne = 9999;

    /// <summary>
    /// Constructor
    /// </summary> 
    /// <param name="Mission">The Mission (use 'this')</param>
    public CKillDisusedPlanes(AMission Mission)
    {
        m_Mission = Mission;
    }
    /// <summary>
    /// Make sure that the plane a player has left cannot be regained
    /// </summary> 
    /// <param name="CurPlayer"></param>
    /// <param name="ActorMain"></param>
    /// <param name="iPlaceIndex"></param>
    public void OnPlaceLeave(Player CurPlayer, AiActor ActorMain, int iPlaceIndex)
    {
        m_Mission.Timeout(1, () => {
            DamagePlane(ActorMain, CurPlayer); 
        });
    }
    /// <summary>
    /// Destroy aircraft
    /// </summary> 
    protected void DestroyPlane(AiAircraft Aircraft)
    {
        if (Aircraft != null)
        {
            Aircraft.Destroy();
        }
    }
    /// <summary>
    /// Make an AI controlled aircraft unusable
    /// </summary> 
    /// <param name="CurPlayer"></param>
    /// <param name="ActorMain"></param>
    protected void DamagePlane(AiActor ActorMain, Player CurPlayer)
    {
        if (DEBUG_MESSAGES) CLog.Write(ActorMain.Name() + " will damage/destroy aircraft left by player " + CurPlayer.Name());
        foreach (AiActor actor in ActorMain.Group().GetItems())
        {
            if (actor == null || !(actor is AiAircraft))
            {
                return;
            }

            AiAircraft Aircraft = (actor as AiAircraft);

            if (!IsAiControlledPlane(Aircraft))
            {
                if (DEBUG_MESSAGES) CLog.Write("Damage/destroy " + ActorMain.Name() + " cancelled. Player " + CurPlayer.Name() + " still in aircraft.");
                return;
            }

            if (Aircraft == null)
            {
                return;
            }
            
            /// Make Damage
            /// We wrap in try ... catch to make sure at least *some* of them are effected
            /// no matter what happens (e.g. the Wing part throws on Blenheims)
            
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
            catch(Exception e)
            {
                if(DEBUG_MESSAGES) CLog.Write("Exception on damaging named parts: "+e.ToString());
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
            //    if(DEBUG_MESSAGES) CLog.Write("Exception on damaging wings: "+e.ToString());
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
            catch(Exception e)
            {
                if(DEBUG_MESSAGES) CLog.Write("Exception on damageing engines: " +e.ToString());
            }
            
            if(DEBUG_MESSAGES) CLog.Write("Will destroy " + ActorMain.Name());
            
            m_Mission.Timeout(m_iSecondsUntilRemove, () => { 
                DestroyPlane(Aircraft);
            });
        }
    }
    /// <summary>
    /// Check if aircraft is truly AI only controlled.
    /// </summary> 
    /// <param name="Aircraft"></param>
    /// <returns>true if no real player is in any of the plane's positions</returns>
    protected bool IsAiControlledPlane(AiAircraft Aircraft)
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

        return true;
    }
}