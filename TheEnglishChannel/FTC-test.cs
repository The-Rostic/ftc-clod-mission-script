using System.Text;
using System;
using System.Collections;
using maddox.game;
using maddox.game.world;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading;
using System.Diagnostics;
using part;

//$include .\modules\CConfig.cs
//$include .\modules\CKillDisusedPlanes.cs
//$include .\modules\CLog.cs

public class Mission : AMission
{
    public CKillDisusedPlanes m_KillDisusedPlanes = null;

    public override void OnBattleStarted()
    {
        base.OnBattleStarted();
        m_KillDisusedPlanes = new CKillDisusedPlanes(this);
        CLog.Init(this);
        CLog.Write("OnBattleStarted");
        // listen all the mission
        this.MissionNumberListener = -1;
    }

    public override void OnBattleStoped()
    {
        base.OnBattleStoped();
        CLog.Write("OnBattleStoped");
        CLog.Close();
    }

    public override void OnPlaceEnter(Player player, AiActor actor, int placeIndex)
    {
        base.OnPlaceEnter(player, actor, placeIndex);
        CLog.Write("OnPlaceEnter player=" + player.Name() + " actor=" + actor.Name() + " placeIdx=" + placeIndex.ToString());
    }

    public override void OnPlaceLeave(Player player, AiActor actor, int placeIndex)
    {
        base.OnPlaceLeave(player, actor, placeIndex);
        CLog.Write("OnPlaceEnter player=" + player.Name() + " actor=" + actor.Name() + " placeIdx=" + placeIndex.ToString());
        m_KillDisusedPlanes.OnPlaceLeave(player, actor, placeIndex);
    }

    public override void OnActorDead(int missionNumber, string shortName, AiActor actor, List<DamagerScore> damages)
    {
        base.OnActorDead(missionNumber, shortName, actor, damages);
    }

    /*public override void OnAircraftTookOff(int missionNumber, string shortName, AiAircraft aircraft)
    {
        base.OnAircraftTookOff(missionNumber, shortName, aircraft);
        CLog.Write("OnAircraftTookOff " + shortName + " aircraft=" + aircraft.Name());
    }*/

    public override void OnAircraftCrashLanded(int missionNumber, string shortName, AiAircraft aircraft)
    {
        base.OnAircraftCrashLanded(missionNumber, shortName, aircraft);
        CLog.Write("OnAircraftCrashLanded " + shortName + " aircraft=" + aircraft.Name());
    }
    public override void OnAircraftLanded(int missionNumber, string shortName, AiAircraft aircraft)
    {
        base.OnAircraftLanded(missionNumber, shortName, aircraft);
        CLog.Write("OnAircraftCrashLanded " + shortName + " aircraft=" + aircraft.Name());
    }
    public override void OnTrigger(int missionNumber, string shortName, bool active)
    {
        base.OnTrigger(missionNumber, shortName, active);
    }
    public override void OnTickGame()
    {

    }

}
