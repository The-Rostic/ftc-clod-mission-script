using System.IO;
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
using maddox.GP; //-------------------

//$include .\modules\CMissionCommon.cs
//$include .\modules\CConfig.cs
//$include .\modules\CKillDisusedPlanes.cs
//$include .\modules\CLog.cs
//$include .\modules\CNetworkComms.cs

public class Mission : AMission
{
    public const bool DEBUG_MESSAGES = true;
    private CMissionCommon missionCommon = null;

    /////////////////////////////////////
    // write mission custom code below

    private void StartAIhunters()
    {
        GamePlay.gpPostMissionLoad(Path.GetDirectoryName(this.sPathMyself) + @"\bob-mis-000-sub-bf109.mis");
        GamePlay.gpPostMissionLoad(Path.GetDirectoryName(this.sPathMyself) + @"\bob-mis-000-sub-spits.mis");
        Timeout(60*15, () =>
        {
            StartAIhunters();
        });
    }

    // write mission custom code above
    /////////////////////////////////////

    public override void OnBattleInit()
    {
        base.OnBattleInit();
        // if(CConfig.IsLoggingEnabled()) CLog.Init(this); // <-- !! DO NOT UNCOMMENT !! A lof of OnActorCreated() events genereted in between OnBattleInit() and OnBattleStarted()
        missionCommon = new CMissionCommon(this);
        missionCommon.OnBattleInit();
        /////////////////////////////////////
        // write mission custom code below

        // ...

        // write mission custom code above
        /////////////////////////////////////
    }

    public override void OnBattleStarted()
    {
        base.OnBattleStarted();
        if(CConfig.IsLoggingEnabled()) CLog.Init(this);
        missionCommon.OnBattleStarted();

        /////////////////////////////////////
        // write mission custom code below

        Timeout(15, () =>
        {
            StartAIhunters();
        });

        // write mission custom code above
        /////////////////////////////////////

        // listen all the mission
        MissionNumberListener = -1;
    }

    public override void OnBattleStoped()
    {
        base.OnBattleStoped();
        missionCommon.OnBattleStoped();
        /////////////////////////////////////
        // write mission custom code below

        // ...

        // write mission custom code above
        /////////////////////////////////////
    }

    //left event OnTickGame() commented until to be used
    public override void OnTickGame()
    {
        base.OnTickGame();
        missionCommon.OnTickGame();
        /////////////////////////////////////
        // write mission custom code below

        // ...

        // write mission custom code above
        /////////////////////////////////////
    }

    //left event OnTickReal() commented until to be used
    public override void OnTickReal()
    {
        base.OnTickReal();
        missionCommon.OnTickReal();
        /////////////////////////////////////
        // write mission custom code below

        // ...

        // write mission custom code above
        /////////////////////////////////////
    }

    public override void OnPlayerDisconnected(Player player, string diagnostic)
    {
        base.OnPlayerDisconnected(player, diagnostic);
        missionCommon.OnPlayerDisconnected(player, diagnostic);
        /////////////////////////////////////
        // write mission custom code below

        // ...

        // write mission custom code above
        /////////////////////////////////////
    }

    public override void OnPlaceEnter(Player player, AiActor actor, int placeIndex)
    {
        base.OnPlaceEnter(player, actor, placeIndex);
        missionCommon.OnPlaceEnter(player, actor, placeIndex);
        /////////////////////////////////////
        // write mission custom code below

        // ...

        // write mission custom code above
        /////////////////////////////////////
    }

    public override void OnPlaceLeave(Player player, AiActor actor, int placeIndex)
    {
        base.OnPlaceLeave(player, actor, placeIndex);
        missionCommon.OnPlaceLeave(player, actor, placeIndex);
        /////////////////////////////////////
        // write mission custom code below

        // ...

        // write mission custom code above
        /////////////////////////////////////
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////
    ////                                                                                            ////
    //// Not used events below in CMissionCommon logic... can be commented for better performance.  ////
    ////                                                                                            ////
    ////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////

    public override void OnTrigger(int missionNumber, string shortName, bool active)
    {
        base.OnTrigger(missionNumber, shortName, active);
        missionCommon.OnTrigger(missionNumber, shortName, active);
        /////////////////////////////////////
        // write mission custom code below

        // ...

        // write mission custom code above
        /////////////////////////////////////
    }

    public override void OnActorCreated(int missionNumber, string shortName, AiActor actor)
    {
        base.OnActorCreated(missionNumber, shortName, actor);
        missionCommon.OnActorCreated(missionNumber, shortName, actor);
        /////////////////////////////////////
        // write mission custom code below

        // ...

        // write mission custom code above
        /////////////////////////////////////
    }

    //// COMENTED DUE TO A LOT OF EVENTS GENERATED
    //public override void OnActorDamaged(int missionNumber, string shortName, AiActor actor, AiDamageInitiator initiator, NamedDamageTypes damageType)
    //{
    //    base.OnActorDamaged(missionNumber, shortName, actor, initiator, damageType);
    //    missionCommon.OnActorDamaged(missionNumber, shortName, actor, initiator, damageType);
    //    /////////////////////////////////////
    //    // write mission custom code below
    //
    //    // ...
    //
    //    // write mission custom code above
    //    /////////////////////////////////////
    //}


    public override void OnActorDead(int missionNumber, string shortName, AiActor actor, List<DamagerScore> damages)
    {
        base.OnActorDead(missionNumber, shortName, actor, damages);
        missionCommon.OnActorDead(missionNumber, shortName, actor, damages);
        /////////////////////////////////////
        // write mission custom code below

        // ...

        // write mission custom code above
        /////////////////////////////////////
    }

    public override void OnActorDestroyed(int missionNumber, string shortName, AiActor actor)
    {
        base.OnActorDestroyed(missionNumber, shortName, actor);
        missionCommon.OnActorDestroyed(missionNumber, shortName, actor);
        /////////////////////////////////////
        // write mission custom code below

        // ...

        // write mission custom code above
        /////////////////////////////////////
    }

    public override void OnAircraftTookOff(int missionNumber, string shortName, AiAircraft aircraft)
    {
        base.OnAircraftTookOff(missionNumber, shortName, aircraft);
        missionCommon.OnAircraftTookOff(missionNumber, shortName, aircraft);
        /////////////////////////////////////
        // write mission custom code below

        // ...

        // write mission custom code above
        /////////////////////////////////////
    }

    public override void OnAircraftCrashLanded(int missionNumber, string shortName, AiAircraft aircraft)
    {
        base.OnAircraftCrashLanded(missionNumber, shortName, aircraft);
        missionCommon.OnAircraftCrashLanded(missionNumber, shortName, aircraft);
        /////////////////////////////////////
        // write mission custom code below

        // ...

        // write mission custom code above
        /////////////////////////////////////
    }
    public override void OnAircraftLanded(int missionNumber, string shortName, AiAircraft aircraft)
    {
        base.OnAircraftLanded(missionNumber, shortName, aircraft);
        missionCommon.OnAircraftLanded(missionNumber, shortName, aircraft);
        /////////////////////////////////////
        // write mission custom code below

        // ...

        // write mission custom code above
        /////////////////////////////////////
    }

    public override void OnActorTaskCompleted(int missionNumber, string shortName, AiActor actor)
    {
        base.OnActorTaskCompleted(missionNumber, shortName, actor);
        missionCommon.OnActorTaskCompleted(missionNumber, shortName, actor);
        /////////////////////////////////////
        // write mission custom code below

        // ...

        // write mission custom code above
        /////////////////////////////////////
    }

    //// COMENTED DUE TO A LOT OF EVENTS GENERATED
    //public override void OnAircraftCutLimb(int missionNumber, string shortName, AiAircraft aircraft, AiDamageInitiator initiator, LimbNames limbName)
    //{
    //    base.OnAircraftCutLimb(missionNumber, shortName, aircraft, initiator, limbName);
    //    missionCommon.OnAircraftCutLimb(missionNumber, shortName, aircraft, initiator, limbName);
    //    /////////////////////////////////////
    //    // write mission custom code below
    //
    //    // ...
    //
    //    // write mission custom code above
    //    /////////////////////////////////////
    //}

    //// COMMENTED DUE TO TO MANY EXCEPTION GENERATED HERE
    //public override void OnAircraftDamaged(int missionNumber, string shortName, AiAircraft aircraft, AiDamageInitiator initiator, NamedDamageTypes damageType)
    //{
    //    base.OnAircraftDamaged(missionNumber, shortName, aircraft, initiator, damageType);
    //    missionCommon.OnAircraftDamaged(missionNumber, shortName, aircraft, initiator, damageType);
    //    /////////////////////////////////////
    //    // write mission custom code below
    //
    //    // ...
    //
    //    // write mission custom code above
    //    /////////////////////////////////////
    //}

    public override void OnAircraftKilled(int missionNumber, string shortName, AiAircraft aircraft)
    {
        base.OnAircraftKilled(missionNumber, shortName, aircraft);
        missionCommon.OnAircraftKilled(missionNumber, shortName, aircraft);
        /////////////////////////////////////
        // write mission custom code below

        // ...

        // write mission custom code above
        /////////////////////////////////////
    }

    //// COMENTED DUE TO A LOT OF EVENTS GENERATED
    //public override void OnAircraftLimbDamaged(int missionNumber, string shortName, AiAircraft aircraft, AiLimbDamage limbDamage)
    //{
    //    base.OnAircraftLimbDamaged(missionNumber, shortName, aircraft, limbDamage);
    //    missionCommon.OnAircraftLimbDamaged(missionNumber, shortName, aircraft, limbDamage);
    //    /////////////////////////////////////
    //    // write mission custom code below
    //
    //    // ...
    //
    //    // write mission custom code above
    //    /////////////////////////////////////
    //}

    public override void OnAutopilotOff(AiActor actor, int placeIndex)
    {
        base.OnAutopilotOff(actor, placeIndex);
        missionCommon.OnAutopilotOff(actor, placeIndex);
        /////////////////////////////////////
        // write mission custom code below

        // ...

        // write mission custom code above
        /////////////////////////////////////
    }

    public override void OnAutopilotOn(AiActor actor, int placeIndex)
    {
        base.OnAutopilotOn(actor, placeIndex);
        missionCommon.OnAutopilotOn(actor, placeIndex);
        /////////////////////////////////////
        // write mission custom code below

        // ...

        // write mission custom code above
        /////////////////////////////////////
    }

    public override void OnBombExplosion(string title, double mass, Point3d pos, AiDamageInitiator initiator, int eventArgInt)
    {
        base.OnBombExplosion(title, mass, pos, initiator, eventArgInt);
        missionCommon.OnBombExplosion(title, mass, pos, initiator, eventArgInt);
        /////////////////////////////////////
        // write mission custom code below

        // ...

        // write mission custom code above
        /////////////////////////////////////
    }

    public override void OnBuildingKilled(string title, Point3d pos, AiDamageInitiator initiator, int eventArgInt)
    {
        base.OnBuildingKilled(title, pos, initiator, eventArgInt);
        missionCommon.OnBuildingKilled(title, pos, initiator, eventArgInt);
        /////////////////////////////////////
        // write mission custom code below

        // ...

        // write mission custom code above
        /////////////////////////////////////
    }

    public override void OnCarter(AiActor actor, int placeIndex)
    {
        base.OnCarter(actor, placeIndex);
        missionCommon.OnCarter(actor, placeIndex);
        /////////////////////////////////////
        // write mission custom code below

        // ...

        // write mission custom code above
        /////////////////////////////////////
    }

    public override void OnMissionLoaded(int missionNumber)
    {
        base.OnMissionLoaded(missionNumber);
        missionCommon.OnMissionLoaded(missionNumber);
        /////////////////////////////////////
        // write mission custom code below

        // ...

        // write mission custom code above
        /////////////////////////////////////
    }

    public override void OnOrderMissionMenuSelected(Player player, int ID, int menuItemIndex)
    {
        base.OnOrderMissionMenuSelected(player, ID, menuItemIndex);
        missionCommon.OnOrderMissionMenuSelected(player, ID, menuItemIndex);
        /////////////////////////////////////
        // write mission custom code below

        // ...

        // write mission custom code above
        /////////////////////////////////////
    }

    //// COMENTED DUE TO A LOT OF EVENTS GENERATED
    //public override void OnPersonHealth(AiPerson person, AiDamageInitiator initiator, float deltaHealth)
    //{
    //    base.OnPersonHealth(person, initiator, deltaHealth);
    //    missionCommon.OnPersonHealth(person, initiator, deltaHealth);
    //    /////////////////////////////////////
    //    // write mission custom code below
    //
    //    // ...
    //
    //    // write mission custom code above
    //    /////////////////////////////////////
    //}

    public override void OnPersonMoved(AiPerson person, AiActor fromCart, int fromPlaceIndex)
    {
        base.OnPersonMoved(person, fromCart, fromPlaceIndex);
        missionCommon.OnPersonMoved(person, fromCart, fromPlaceIndex);
        /////////////////////////////////////
        // write mission custom code below

        // ...

        // write mission custom code above
        /////////////////////////////////////
    }

    public override void OnPersonParachuteFailed(AiPerson person)
    {
        base.OnPersonParachuteFailed(person);
        missionCommon.OnPersonParachuteFailed(person);
        /////////////////////////////////////
        // write mission custom code below

        // ...

        // write mission custom code above
        /////////////////////////////////////
    }

    public override void OnPersonParachuteLanded(AiPerson person)
    {
        base.OnPersonParachuteLanded(person);
        missionCommon.OnPersonParachuteLanded(person);
        /////////////////////////////////////
        // write mission custom code below

        // ...

        // write mission custom code above
        /////////////////////////////////////
    }

    public override void OnPlayerArmy(Player player, int army)
    {
        base.OnPlayerArmy(player, army);
        missionCommon.OnPlayerArmy(player, army);
        /////////////////////////////////////
        // write mission custom code below

        // ...

        // write mission custom code above
        /////////////////////////////////////
    }

    public override void OnPlayerConnected(Player player)
    {
        base.OnPlayerConnected(player);
        missionCommon.OnPlayerConnected(player);
        /////////////////////////////////////
        // write mission custom code below

        // ...

        // write mission custom code above
        /////////////////////////////////////
    }

    public override void OnStationaryKilled(int missionNumber, GroundStationary _stationary, AiDamageInitiator initiator, int eventArgInt)
    {
        base.OnStationaryKilled(missionNumber, _stationary, initiator, eventArgInt);
        missionCommon.OnStationaryKilled(missionNumber, _stationary, initiator, eventArgInt);
        /////////////////////////////////////
        // write mission custom code below

        // ...

        // write mission custom code above
        /////////////////////////////////////
    }

    //// SINGLE PLAYER SPECIFIC
    //public override void OnUserCreateUserLabel(GPUserLabel ul)
    //{
    //    base.OnUserCreateUserLabel(ul);
    //    missionCommon.OnUserCreateUserLabel(ul);
    //    /////////////////////////////////////
    //    // write mission custom code below
    //
    //    // ...
    //
    //    // write mission custom code above
    //    /////////////////////////////////////
    //}

    //// SINGLE PLAYER SPECIFIC
    //public override void OnUserDeleteUserLabel(GPUserLabel ul)
    //{
    //    base.OnUserDeleteUserLabel(ul);
    //    missionCommon.OnUserDeleteUserLabel(ul);
    //    /////////////////////////////////////
    //    // write mission custom code below
    //
    //    // ...
    //
    //    // write mission custom code above
    //    /////////////////////////////////////
    //}
}
