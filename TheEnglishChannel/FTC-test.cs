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

//$include .\modules\CConfig.cs
//$include .\modules\CKillDisusedPlanes.cs
//$include .\modules\CLog.cs

public class Mission : AMission
{
    public bool DEBUG_MESSAGES = true;
    
    public CKillDisusedPlanes m_KillDisusedPlanes = null;

    public override void OnBattleStarted()
    {
        base.OnBattleStarted();
        m_KillDisusedPlanes = new CKillDisusedPlanes(this);
        CLog.Init(this);
        if (DEBUG_MESSAGES) CLog.Write("OnBattleStarted");
        PrepareAirfields();
        // listen all the mission
        MissionNumberListener = -1;
    }

    public override void OnBattleStoped()
    {
        base.OnBattleStoped();
        if (DEBUG_MESSAGES) CLog.Write("OnBattleStoped");
        CLog.Close();
    }

    public override void OnPlaceEnter(Player player, AiActor actor, int placeIndex)
    {
        base.OnPlaceEnter(player, actor, placeIndex);
        if (DEBUG_MESSAGES) CLog.Write("OnPlaceEnter player=" + player.Name() + " actor=" + actor.Name() + " placeIdx=" + placeIndex.ToString());
    }

    public override void OnPlaceLeave(Player player, AiActor actor, int placeIndex)
    {
        base.OnPlaceLeave(player, actor, placeIndex);
        if (DEBUG_MESSAGES) CLog.Write("OnPlaceLeave player=" + player.Name() + " actor=" + actor.Name() + " placeIdx=" + placeIndex.ToString());
        m_KillDisusedPlanes.OnPlaceLeave(player, actor, placeIndex);
    }

    public override void OnActorDead(int missionNumber, string shortName, AiActor actor, List<DamagerScore> damages)
    {
        base.OnActorDead(missionNumber, shortName, actor, damages);
        if (DEBUG_MESSAGES) CLog.Write("OnActorDead " + shortName + " actor=" + actor.Name());
    }

    public override void OnAircraftTookOff(int missionNumber, string shortName, AiAircraft aircraft)
    {
        base.OnAircraftTookOff(missionNumber, shortName, aircraft);
        if (DEBUG_MESSAGES) CLog.Write("OnAircraftTookOff " + shortName + " aircraft=" + aircraft.Name());
    }

    public override void OnAircraftCrashLanded(int missionNumber, string shortName, AiAircraft aircraft)
    {
        base.OnAircraftCrashLanded(missionNumber, shortName, aircraft);
        if (DEBUG_MESSAGES) CLog.Write("OnAircraftCrashLanded " + shortName + " aircraft=" + aircraft.Name());
    }
    public override void OnAircraftLanded(int missionNumber, string shortName, AiAircraft aircraft)
    {
        base.OnAircraftLanded(missionNumber, shortName, aircraft);
        if (DEBUG_MESSAGES) CLog.Write("OnAircraftCrashLanded " + shortName + " aircraft=" + aircraft.Name());
    }
    public override void OnTrigger(int missionNumber, string shortName, bool active)
    {
        base.OnTrigger(missionNumber, shortName, active);
    }
    public override void OnTickGame()
    {

    }


    //
    //  Custom functions
    //

    AiAirport[] airportsSortedByArmy = null;

    public void PrepareAirfields()
    {
        // Get list of all airfields on the map
        airportsSortedByArmy = GamePlay.gpAirports();
        AiAirport aiAirport = null;
        // Sort by army
        for (int i = 0; i < airportsSortedByArmy.Length - 1; i++)
        {
            for (int j = i + 1; j < airportsSortedByArmy.Length; j++)
            {
                if (airportsSortedByArmy[i].Army() > airportsSortedByArmy[j].Army())
                {
                    aiAirport = airportsSortedByArmy[i];
                    airportsSortedByArmy[i] = airportsSortedByArmy[j];
                    airportsSortedByArmy[j] = aiAirport;
                }
            }
        }
        if (DEBUG_MESSAGES) CLog.Write("Sorted list of airfields:");
        for (int i = 0; i < airportsSortedByArmy.Length; i++)
        {
            if (DEBUG_MESSAGES) CLog.Write(airportsSortedByArmy[i].Name() + " army=" + airportsSortedByArmy[i].Army().ToString());
        }
        ////
        //// Print list of airfields in mission
        ////
        //for (int i = 0; i < GamePlay.gpAirports().Length; i++)
        //{
        //    AiAirport airport = GamePlay.gpAirports()[i];
        //    Point3d airpPos = airport.Pos();
        //    CLog.Write(airport.Name() 
        //        + " at X="+ airpPos.x.ToString() + " Y=" + airpPos.y.ToString() + " Z=" + airpPos.z.ToString()
        //        + " army=" + airport.Army().ToString()
        //        + " CoverageR="+ airport.CoverageR().ToString()
        //        + " FieldR=" + airport.FieldR().ToString()
        //        );
        //}
    }

    public bool IsAircraftAtFriendlyAirfield(AiAircraft Aircraft)
    {
        if (Aircraft == null)
        { 
            return false; 
        }
        bool aircraftIsOnTheGround = false;
        if (!Aircraft.IsAirborne()) // Just spawen and never airborne.
        {
            aircraftIsOnTheGround = true;
            if (DEBUG_MESSAGES) CLog.Write(Aircraft.Name() + " is NOT airborne.");
        }
        else // Important notice! Aircraft that airborne once stays airborne forever, even after landed.
        {
            double aircraftAGL = Aircraft.getParameter(part.ParameterTypes.Z_AltitudeAGL, -1);
            double aircraftTAS = Aircraft.getParameter(part.ParameterTypes.Z_VelocityTAS, -1);
            if (DEBUG_MESSAGES) CLog.Write(Aircraft.Name() + " is AGL=" + aircraftAGL.ToString() + "m and TAS=" + aircraftTAS.ToString() + "m/s");
            if ((aircraftAGL < 5) && (aircraftTAS < 3.6))
            {
                aircraftIsOnTheGround = true;
            }
        }

        if (aircraftIsOnTheGround)
        {
            Point3d aircraftPos = Aircraft.Pos();
            int aircraftArmy = Aircraft.Army();
            AiAirport[] airports = GamePlay.gpAirports();
            AiAirport airportFriendly, airportNeutral;
            Point3d airportNeutralPos;
            for (int i = 0; i < airports.Length; i++)
            {
                airportNeutral = airports[i];
                // get neutral airfileds with friendly spawn area airports nearby...
                if (airportNeutral.Army() == 0)
                {
                    airportNeutralPos = airportNeutral.Pos();
                    for (int j = 0; j < airports.Length; j++)
                    {
                        if (i == j) continue;
                        airportFriendly = airports[j];
                        if (airportFriendly.Army() == aircraftArmy)
                        {
                            Point3d airportFriendlyPos = airportFriendly.Pos();
                            if (airportNeutralPos.distanceLinf(ref airportFriendlyPos) < airportNeutral.CoverageR())
                            {
                                // Ok, this neutral airport contain friendly spawn area airport. Check if we are in this neutral airport radius
                                double distToAirportNeutral = airportNeutralPos.distanceLinf(ref aircraftPos);
                                if (distToAirportNeutral < airportNeutral.CoverageR())
                                {
                                    if (DEBUG_MESSAGES) CLog.Write(Aircraft.Name() + " is on friendly airfiled " + airportNeutral.Name() + " distance " + distToAirportNeutral.ToString());
                                    return true;
                                }
                            }
                        }
                    }
                }
            }
            if (DEBUG_MESSAGES) CLog.Write(Aircraft.Name() + " is abbandoned on the ground.");
        }
        else
        {
            if (DEBUG_MESSAGES) CLog.Write(Aircraft.Name() + " is airborne.");
        }
        return false;
    }
}