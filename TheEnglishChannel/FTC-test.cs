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
        PrepareAirports();
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


    private class CAirportIndexes
    {
        public int Army = -1;
        public int FirstIdx = -1;
        public int LastIdx = -1;
    }

    private class CNeutralAirportsByArmies
    {
        public List<AiAirport> aiAirports = new List<AiAirport>();
        public int Army;
    }

    private CNeutralAirportsByArmies[] NeutralAirportsByArmies = null;

    public void PrepareAirports()
    {
        if (GamePlay.gpAirports().Length == 0)
        {
            return;
        }

        // Get list of all airfields on the map
        AiAirport[] airportsSortedByArmy = new AiAirport[ GamePlay.gpAirports().Length ];
        GamePlay.gpAirports().CopyTo(airportsSortedByArmy, 0);

        AiAirport aiAirport;
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


        //
        // debug printing
        //
        if (DEBUG_MESSAGES) CLog.Write("Sorted list of airfields:");
        for (int i = 0; i < airportsSortedByArmy.Length; i++)
        {
            if (DEBUG_MESSAGES) CLog.Write(airportsSortedByArmy[i].Name() + " army=" + airportsSortedByArmy[i].Army().ToString());
        }


        List<CAirportIndexes> airportIndexesByArmy = new List<CAirportIndexes>();
        airportIndexesByArmy.Add(new CAirportIndexes());
        airportIndexesByArmy[0].Army = airportsSortedByArmy[0].Army(); // Actually first airport in list have to be neutral so just 0 can be assigned
        airportIndexesByArmy[0].FirstIdx = 0;
        airportIndexesByArmy[0].LastIdx = 0;
        int idx = 0;
        // Lets prepare indexes for different armies airfields for fast searching.
        for (int i = 1; i < airportsSortedByArmy.Length; i++)
        {
            if (airportsSortedByArmy[i].Army() == airportIndexesByArmy[idx].Army)
            {
                airportIndexesByArmy[idx].LastIdx = i;
            }
            else
            {
                idx++;
                airportIndexesByArmy.Add(new CAirportIndexes());
                airportIndexesByArmy[idx].Army = airportsSortedByArmy[i].Army();
                airportIndexesByArmy[idx].FirstIdx = i;
                airportIndexesByArmy[idx].LastIdx = i;

            }
        }

        //
        // debug printing
        //
        if (DEBUG_MESSAGES) CLog.Write("---Indexes for different armies airfields for fast searching done.");
        for (int i = 0; i < airportIndexesByArmy.Count; i++)
        {
            if (DEBUG_MESSAGES) CLog.Write("Index=" + i.ToString() 
                + " Army="+ airportIndexesByArmy[i].Army.ToString() 
                + " First=" + airportIndexesByArmy[i].FirstIdx.ToString()
                + " Last=" + airportIndexesByArmy[i].LastIdx.ToString());
        }

        // create array of neautral airports lists by armies if more than just one neutral armie
        if (airportIndexesByArmy.Count > 1) {
            int nonNeutralArmiesCount = airportIndexesByArmy.Count - 1;
            NeutralAirportsByArmies = new CNeutralAirportsByArmies[nonNeutralArmiesCount];
            // fill army values
            for (int armyIdx = 1; armyIdx <= nonNeutralArmiesCount; armyIdx++)
            {
                NeutralAirportsByArmies[armyIdx - 1] = new CNeutralAirportsByArmies();
                NeutralAirportsByArmies[armyIdx - 1].Army = airportIndexesByArmy[armyIdx].Army;
            }

            //
            // debug printing
            //
            if (DEBUG_MESSAGES) CLog.Write("---Army values filled.");



            // fill airfields
            for (int neutralAirportIdx = 0; neutralAirportIdx <= airportIndexesByArmy[0].LastIdx; neutralAirportIdx++)
            {
                AiAirport neutralAirport = airportsSortedByArmy[neutralAirportIdx];
                Point3d neutralAirportPos = neutralAirport.Pos();
                for (int armyIdx = 1; armyIdx <= nonNeutralArmiesCount; armyIdx++)
                {
                    for (int nonNeutralAirportIdx = airportIndexesByArmy[armyIdx].FirstIdx; nonNeutralAirportIdx <= airportIndexesByArmy[armyIdx].LastIdx; nonNeutralAirportIdx++)
                    {
                        AiAirport nonNeutralAirport = airportsSortedByArmy[nonNeutralAirportIdx];
                        Point3d nonNeutralAirportPos = nonNeutralAirport.Pos();
                        if (neutralAirportPos.distanceLinf(ref nonNeutralAirportPos) < neutralAirport.CoverageR())
                        {
                            NeutralAirportsByArmies[armyIdx - 1].aiAirports.Add(neutralAirport);
                            break;
                        }
                    }
                }
            }

            //
            // debug printing
            //
            if (DEBUG_MESSAGES) CLog.Write("Neutral airfields by army.");

            for (int armyIdx = 0; armyIdx < NeutralAirportsByArmies.Length; armyIdx++)
            {
                if (DEBUG_MESSAGES) CLog.Write("---List of airports for Army=" + NeutralAirportsByArmies[armyIdx].Army.ToString());
                for (int airportIdx = 0; airportIdx < NeutralAirportsByArmies[armyIdx].aiAirports.Count; airportIdx++)
                {
                    if (DEBUG_MESSAGES) CLog.Write(NeutralAirportsByArmies[armyIdx].aiAirports[airportIdx].Name());
                }
            }
        }
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