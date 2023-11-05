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
        if (DEBUG_MESSAGES) CLog.Write("OnPlaceEnter player=" + ((player != null)?player.Name():"=null") + " actor=" + ((actor != null) ? actor.Name() : "=null") + " placeIdx=" + placeIndex.ToString());
    }

    public override void OnPlaceLeave(Player player, AiActor actor, int placeIndex)
    {
        try 
        { 
            base.OnPlaceLeave(player, actor, placeIndex);
            if (DEBUG_MESSAGES) CLog.Write("OnPlaceLeave player=" + ((player != null) ? player.Name() : "=null") + " actor=" + ((actor != null) ? actor.Name() : "=null") + " placeIdx=" + placeIndex.ToString());
            if (CConfig.DISABLE_LEAVE_MOVING_AIRCRAFT)
            {
                if ((player != null) && (actor != null) && player.IsConnected() && (actor is AiAircraft))
                {
                    AiAircraft aircraft = actor as AiAircraft;
                    if (DEBUG_MESSAGES) CLog.Write("Player " + player.Name() + " is trying to leave aircraft " + aircraft.Name());
                    bool isAiControlled = m_KillDisusedPlanes.IsAiControlledPlane(aircraft);
                    if (isAiControlled || (placeIndex == 0)) //pilot cann't leave airborne aircraft even if another seat occupied
                    {
                        if (aircraft.IsAlive() && (aircraft.Person(0) != null) && (aircraft.Person(0).Health > 0) && aircraft.IsValid())
                        {
                            //EAircraftLocation aircraftLocation = GetAircraftLocation(aircraft);
                            double aircraftTAS = aircraft.getParameter(part.ParameterTypes.Z_VelocityTAS, -1);
                            // Do not  allow leave moving aircraft!
                            if (aircraftTAS > 1.0)
                            {
                                if (DEBUG_MESSAGES) CLog.Write("Player " + player.Name() + " is about to enter pilot seat again " + aircraft.Name());
                                Timeout((player.Ping() + 50) * 0.001, () =>
                                {
                                    player.PlaceEnter(actor, 0);
                                });
                                Player[] recepients = { player };
                                GamePlay.gpHUDLogCenter(recepients, "Bailout, crash or land and stop!");
                                return;
                            }
                            if (!isAiControlled)
                            {
                                // Hey! Pilot left but 
                                int primIdx = player.PlacePrimary();
                                int secIdx = player.PlaceSecondary();
                                if (DEBUG_MESSAGES) CLog.Write("Hey! Player still in aircraft! PlacePrimary=" + primIdx.ToString() + " PlaceSecondary=" + secIdx.ToString());
                                if (primIdx >= 0)
                                {
                                    // have to generate new on leave event and do stuff there
                                    Timeout(1, () =>
                                    {
                                        player.PlaceLeave(primIdx);
                                    });
                                    return;
                                }
                                if (secIdx >= 0)
                                {
                                    // have to generate new on leave event and do stuff there
                                    Timeout(1, () =>
                                    {
                                        player.PlaceLeave(secIdx);
                                    });
                                    return;
                                }
                            }
                            if (DEBUG_MESSAGES) CLog.Write("Ok, it is allowed to leave pilot seat.");
                        }
                        else
                        {
                            // (aircraft.IsAlive() && (aircraft.Person(0) != null) && (aircraft.Person(0).Health > 0) && aircraft.IsValid())
                            if (DEBUG_MESSAGES)
                            {
                                string msg = "It seems like aricraft can't be piloted... "
                                    + ((!aircraft.IsAlive()) ? "--- (!aircraft.IsAlive())" : "")
                                    + ((aircraft.Person(0) == null) ? "--- (Person(0) == null)" : "")
                                    + (((aircraft.Person(0) != null) && (aircraft.Person(0).Health == 0)) ? "--- (aircraft.Person(0).Health <= 0)" : "")
                                    + ((!aircraft.IsValid()) ? "--- (!aircraft.IsValid())" : "");
                                CLog.Write(msg);
                            }
                        }
                    }
                    else
                    {
                        //if (isAiControlled || (placeIndex == 0))
                        if (DEBUG_MESSAGES)
                        {
                            CLog.Write("It seems like aricraft still piloted... --- (!isAiControlled && (placeIndex != 0)) no need to call KillDisusedPlanes.OnPlaceLeave() just free place");
                            return;
                        }
                    }
                }
                else
                {
                    //if ((player != null) && (actor != null) && player.IsConnected() && (actor is AiAircraft))
                    if (DEBUG_MESSAGES)
                    {
                        string msg = "It seems like aricraft can't be piloted... "
                            + ((player == null) ? "--- (player == null)" : "")
                            + ((actor == null) ? "--- (actor == null)" : "")
                            + ((!player.IsConnected()) ? "--- (!player.IsConnected())" : "")
                            + (!(actor is AiAircraft) ? "--- (!(actor is AiAircraft))" : "");
                        CLog.Write(msg);
                    }
                }
            }
            else
            if (CConfig.DISABLE_AI_TO_FLY_WITH_PLAYER_IN_SECONDARY_PLACE)
            {
                //
                // When trying to leave pilot place then leave all places in aircraft and then destroy it! Disable AI to fly mission!
                //
                if ((player != null) && (actor != null) && player.IsConnected() && (actor is AiAircraft))
                {
                    AiAircraft aircraft = actor as AiAircraft;
                    if (DEBUG_MESSAGES) CLog.Write("Player " + player.Name() + " is trying to leave aircraft " + aircraft.Name());
                    bool isAiControlled = m_KillDisusedPlanes.IsAiControlledPlane(aircraft);
                    if ((placeIndex == 0) && !isAiControlled)
                    {
                        int primIdx = player.PlacePrimary();
                        int secIdx = player.PlaceSecondary();
                        if (DEBUG_MESSAGES) CLog.Write("Hey! Player still in aircraft! PlacePrimary=" + primIdx.ToString() + " PlaceSecondary=" + secIdx.ToString());
                        if (primIdx >= 0)
                        {
                            // have to generate new on leave event and do stuff there
                            Timeout(1, () =>
                            {
                                player.PlaceLeave(primIdx);
                            });
                            return;
                        }
                        if (secIdx >= 0)
                        {
                            // have to generate new on leave event and do stuff there
                            Timeout(1, () =>
                            {
                                player.PlaceLeave(secIdx);
                            });
                            return;
                        }
                    }
                }
                else
                {
                    //if ((player != null) && (actor != null) && player.IsConnected() && (actor is AiAircraft))
                    if (DEBUG_MESSAGES)
                    {
                        string msg = "It seems like aricraft can't be piloted... "
                            + ((player == null) ? "--- (player == null)" : "")
                            + ((actor == null) ? "--- (actor == null)" : "")
                            + ((!player.IsConnected()) ? "--- (!player.IsConnected())" : "")
                            + (!(actor is AiAircraft) ? "--- (!(actor is AiAircraft))" : "");
                        CLog.Write(msg);
                    }
                }
            }
            if (DEBUG_MESSAGES) CLog.Write("m_KillDisusedPlanes.OnPlaceLeave()");
            m_KillDisusedPlanes.OnPlaceLeave(player, actor, placeIndex);
        }
        catch (Exception e)
        {
            if (DEBUG_MESSAGES) CLog.Write(e.ToString());
        }
    }

    public override void OnActorDead(int missionNumber, string shortName, AiActor actor, List<DamagerScore> damages)
    {
        base.OnActorDead(missionNumber, shortName, actor, damages);
        if (DEBUG_MESSAGES) CLog.Write("OnActorDead " + shortName + " actor=" + ((actor != null)?actor.Name():"=null"));
    }

    public override void OnAircraftTookOff(int missionNumber, string shortName, AiAircraft aircraft)
    {
        base.OnAircraftTookOff(missionNumber, shortName, aircraft);
        if (DEBUG_MESSAGES) CLog.Write("OnAircraftTookOff " + shortName + " aircraft=" + ((aircraft != null)?aircraft.Name():"=null"));
    }

    public override void OnAircraftCrashLanded(int missionNumber, string shortName, AiAircraft aircraft)
    {
        base.OnAircraftCrashLanded(missionNumber, shortName, aircraft);
        if (DEBUG_MESSAGES) CLog.Write("OnAircraftCrashLanded " + shortName + " aircraft=" + ((aircraft != null) ? aircraft.Name() : "=null"));
    }
    public override void OnAircraftLanded(int missionNumber, string shortName, AiAircraft aircraft)
    {
        base.OnAircraftLanded(missionNumber, shortName, aircraft);
        if (DEBUG_MESSAGES) CLog.Write("OnAircraftCrashLanded " + shortName + " aircraft=" + ((aircraft != null) ? aircraft.Name() : "=null"));
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
        AiAirport[] missionAirportsSortedByArmy = new AiAirport[ GamePlay.gpAirports().Length ];
        GamePlay.gpAirports().CopyTo(missionAirportsSortedByArmy, 0);

        AiAirport aiAirport;
        // Sort by army
        for (int i = 0; i < missionAirportsSortedByArmy.Length - 1; i++)
        {
            for (int j = i + 1; j < missionAirportsSortedByArmy.Length; j++)
            {
                if (missionAirportsSortedByArmy[i].Army() > missionAirportsSortedByArmy[j].Army())
                {
                    aiAirport = missionAirportsSortedByArmy[i];
                    missionAirportsSortedByArmy[i] = missionAirportsSortedByArmy[j];
                    missionAirportsSortedByArmy[j] = aiAirport;
                }
            }
        }


        //
        // debug printing
        //
        if (DEBUG_MESSAGES) CLog.Write("Sorted list of airfields:");
        for (int i = 0; i < missionAirportsSortedByArmy.Length; i++)
        {
            if (DEBUG_MESSAGES) CLog.Write(missionAirportsSortedByArmy[i].Name() + " army=" + missionAirportsSortedByArmy[i].Army().ToString());
        }

        if(missionAirportsSortedByArmy[0].Army() != 0)
        {
            if (DEBUG_MESSAGES) CLog.Write("OMG! Neutral airports with Army() == 0 NOT FOUND!!!");
            return;
        }

        List<CAirportIndexes> missionAirportIndexesByArmy = new List<CAirportIndexes>();
        missionAirportIndexesByArmy.Add(new CAirportIndexes());
        missionAirportIndexesByArmy[0].Army = 0;//missionAirportsSortedByArmy[0].Army(); // Actually first airport in list have to be neutral so just 0 can be assigned
        missionAirportIndexesByArmy[0].FirstIdx = 0;
        missionAirportIndexesByArmy[0].LastIdx = 0;
        int idx = 0;
        // Lets prepare indexes for different armies airfields for fast searching.
        for (int i = 1; i < missionAirportsSortedByArmy.Length; i++)
        {
            if (missionAirportsSortedByArmy[i].Army() == missionAirportIndexesByArmy[idx].Army)
            {
                missionAirportIndexesByArmy[idx].LastIdx = i;
            }
            else
            {
                idx++;
                missionAirportIndexesByArmy.Add(new CAirportIndexes());
                missionAirportIndexesByArmy[idx].Army = missionAirportsSortedByArmy[i].Army();
                missionAirportIndexesByArmy[idx].FirstIdx = i;
                missionAirportIndexesByArmy[idx].LastIdx = i;

            }
        }

        //
        // debug printing
        //
        if (DEBUG_MESSAGES) CLog.Write("---Indexes for different armies airfields for fast searching done.");
        for (int i = 0; i < missionAirportIndexesByArmy.Count; i++)
        {
            if (DEBUG_MESSAGES) CLog.Write("Index=" + i.ToString() 
                + " Army="+ missionAirportIndexesByArmy[i].Army.ToString() 
                + " First=" + missionAirportIndexesByArmy[i].FirstIdx.ToString()
                + " Last=" + missionAirportIndexesByArmy[i].LastIdx.ToString());
        }

        // create array of neautral airports lists by armies
        if (missionAirportIndexesByArmy.Count > 0) {
            NeutralAirportsByArmies = new CNeutralAirportsByArmies[missionAirportIndexesByArmy.Count];
            // fill army values
            for (int armyIdx = 0; armyIdx < missionAirportIndexesByArmy.Count; armyIdx++)
            {
                NeutralAirportsByArmies[armyIdx] = new CNeutralAirportsByArmies();
                NeutralAirportsByArmies[armyIdx].Army = missionAirportIndexesByArmy[armyIdx].Army;
            }

            //
            // debug printing
            //
            if (DEBUG_MESSAGES) CLog.Write("---Army values filled.");



            // fill airfields NeutralAirportsByArmies from list of all mission neutral airports
            for (int missionNeutralAirportIdx = 0; missionNeutralAirportIdx <= missionAirportIndexesByArmy[0].LastIdx; missionNeutralAirportIdx++)
            {
                AiAirport missionNeutralAirport = missionAirportsSortedByArmy[missionNeutralAirportIdx];
                Point3d missionNeutralAirportPos = missionNeutralAirport.Pos();
                bool nonNeutralAirportFound = false;
                for (int armyIdx = 1; armyIdx < missionAirportIndexesByArmy.Count; armyIdx++)
                {
                    for (int nonNeutralAirportIdx = missionAirportIndexesByArmy[armyIdx].FirstIdx; nonNeutralAirportIdx <= missionAirportIndexesByArmy[armyIdx].LastIdx; nonNeutralAirportIdx++)
                    {
                        AiAirport nonNeutralAirport = missionAirportsSortedByArmy[nonNeutralAirportIdx];
                        Point3d nonNeutralAirportPos = nonNeutralAirport.Pos();
                        if (missionNeutralAirportPos.distanceLinf(ref nonNeutralAirportPos) < missionNeutralAirport.CoverageR())
                        {
                            NeutralAirportsByArmies[armyIdx].aiAirports.Add(missionNeutralAirport);
                            nonNeutralAirportFound = true;
                            break;
                        }
                    }
                    if (nonNeutralAirportFound)
                        break;
                }
                if(!nonNeutralAirportFound)
                {
                    NeutralAirportsByArmies[0].aiAirports.Add(missionNeutralAirport);
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

    public enum EAircraftLocation
    {
        Unknown = 0,
        Airborne,
        DitchedGround,
        DitchedSea,
        NeutralAirfield,
        EnemyAirfield,
        FriendlyAirfield,
    };

    public EAircraftLocation GetAircraftLocation(AiAircraft aircraft) {
        if (aircraft == null)
        {
            return EAircraftLocation.Unknown;
        }

        bool aircraftIsOnTheGround = false;
        if (!aircraft.IsAirborne()) // Just spawen and never airborne.
        {
            aircraftIsOnTheGround = true;
            if (DEBUG_MESSAGES) CLog.Write(aircraft.Name() + " is NOT airborne.");
        }
        else // Important notice! Aircraft that airborne once stays airborne forever, even after landed.
        {
            double aircraftAGL = aircraft.getParameter(part.ParameterTypes.Z_AltitudeAGL, -1);
            double aircraftTAS = aircraft.getParameter(part.ParameterTypes.Z_VelocityTAS, -1);
            if (DEBUG_MESSAGES) CLog.Write(aircraft.Name() + " is AGL=" + aircraftAGL.ToString() + "m and TAS=" + aircraftTAS.ToString() + "m/s");
            if ((aircraftAGL < 5) && (aircraftTAS < 3.6))
            {
                aircraftIsOnTheGround = true;
            }
        }

        if (aircraftIsOnTheGround)
        {
            Point3d aircraftPos = aircraft.Pos();
            int aircraftArmy = aircraft.Army();
            AiAirport airportFriendly;
            Point3d airportFriendlyPos;

            // lets find list of friendly airports
            int friendlyAirportsListIdx = -1;
            for (int i = 0; i < NeutralAirportsByArmies.Length; i++)
            {
                if (NeutralAirportsByArmies[i].Army == aircraftArmy)
                {
                    friendlyAirportsListIdx = i;
                    break;
                }
            }
            if (friendlyAirportsListIdx >= 0)
            {
                int airportsCount = NeutralAirportsByArmies[friendlyAirportsListIdx].aiAirports.Count;
                for (int i = 0; i < airportsCount; i++)
                {
                    airportFriendly = NeutralAirportsByArmies[friendlyAirportsListIdx].aiAirports[i];
                    airportFriendlyPos = airportFriendly.Pos();
                    // Ok, this neutral airport contain friendly spawn area airport. Check if we are in this neutral airport radius
                    double distToAirportFriendly = airportFriendlyPos.distanceLinf(ref aircraftPos);
                    if (distToAirportFriendly < airportFriendly.CoverageR())
                    {
                        if (DEBUG_MESSAGES) CLog.Write(aircraft.Name() + " is on friendly airfiled " + airportFriendly.Name() + " distance " + distToAirportFriendly.ToString());
                        return EAircraftLocation.FriendlyAirfield;
                    }
                }
            }

            if (GamePlay.gpLandType(aircraftPos.x, aircraftPos.y) == LandTypes.WATER)
            {
                if (DEBUG_MESSAGES) CLog.Write(aircraft.Name() + " is ditched in to the water.");
                return EAircraftLocation.DitchedGround;
            }
            //else
            //{
            //    if (DEBUG_MESSAGES) CLog.Write(aircraft.Name() + " is abbandoned on the ground.");
            //    return EAircraftLocation.DitchedGround;
            //}
        }
        else
        {
            if (DEBUG_MESSAGES) CLog.Write(aircraft.Name() + " is airborne.");
            return EAircraftLocation.Airborne;
        }
        if (DEBUG_MESSAGES) CLog.Write(aircraft.Name() + " is abbandoned on the ground.");
        return EAircraftLocation.DitchedGround;
    }

    /*public bool IsAircraftAtFriendlyAirfield(AiAircraft aircraft)
    {
        if (aircraft == null)
        { 
            return false; 
        }
        bool aircraftIsOnTheGround = false;
        if (!aircraft.IsAirborne()) // Just spawen and never airborne.
        {
            aircraftIsOnTheGround = true;
            if (DEBUG_MESSAGES) CLog.Write(aircraft.Name() + " is NOT airborne.");
        }
        else // Important notice! Aircraft that airborne once stays airborne forever, even after landed.
        {
            double aircraftAGL = aircraft.getParameter(part.ParameterTypes.Z_AltitudeAGL, -1);
            double aircraftTAS = aircraft.getParameter(part.ParameterTypes.Z_VelocityTAS, -1);
            if (DEBUG_MESSAGES) CLog.Write(aircraft.Name() + " is AGL=" + aircraftAGL.ToString() + "m and TAS=" + aircraftTAS.ToString() + "m/s");
            if ((aircraftAGL < 5) && (aircraftTAS < 3.6))
            {
                aircraftIsOnTheGround = true;
            }
        }

        if (aircraftIsOnTheGround)
        {
            Point3d aircraftPos = aircraft.Pos();
            int aircraftArmy = aircraft.Army();
            AiAirport airportFriendly;
            Point3d airportFriendlyPos;

            // lets find list of friendly airports
            int friendlyAirportsListIdx = -1;
            // skip neutral airports index 0
            for (int i = 1; i < NeutralAirportsByArmies.Length; i++)
            {
                if (NeutralAirportsByArmies[i].Army == aircraftArmy)
                {
                    friendlyAirportsListIdx = i;
                    break;
                }
            }
            if (friendlyAirportsListIdx >= 0)
            {
                int airportsCount = NeutralAirportsByArmies[friendlyAirportsListIdx].aiAirports.Count;
                for (int i = 0; i < airportsCount; i++)
                {
                    airportFriendly = NeutralAirportsByArmies[friendlyAirportsListIdx].aiAirports[i];
                    airportFriendlyPos = airportFriendly.Pos();
                    // Ok, this neutral airport contain friendly spawn area airport. Check if we are in this neutral airport radius
                    double distToAirportFriendly = airportFriendlyPos.distanceLinf(ref aircraftPos);
                    if (distToAirportFriendly < airportFriendly.CoverageR())
                    {
                        if (DEBUG_MESSAGES) CLog.Write(aircraft.Name() + " is on friendly airfiled " + airportFriendly.Name() + " distance " + distToAirportFriendly.ToString());
                        return true;
                    }
                }
            }
            if (DEBUG_MESSAGES) CLog.Write(aircraft.Name() + " is abbandoned on the ground.");
        }
        else
        {
            if (DEBUG_MESSAGES) CLog.Write(aircraft.Name() + " is airborne.");
        }
        return false;
    }*/
}
