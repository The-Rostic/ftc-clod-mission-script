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

public class CMissionCommon
{
    private const bool DEBUG_MESSAGES = true;

    public CKillDisusedPlanes m_KillDisusedPlanes = null;

    private Mission BaseMission = null; 

    public CMissionCommon(Mission mission)
    {
        BaseMission = mission;
    }

    public void OnBattleInit()
    {
        if (DEBUG_MESSAGES && CLog.IsInitialized) CLog.Write("OnBattleInit()");
    }

    public void OnBattleStarted()
    {
        if (DEBUG_MESSAGES && CLog.IsInitialized) CLog.Write("OnBattleStarted");
        m_KillDisusedPlanes = new CKillDisusedPlanes(BaseMission);
        PrepareAirports();
    }

    public void OnBattleStoped()
    {
        if (DEBUG_MESSAGES && CLog.IsInitialized) CLog.Write("OnBattleStoped");
        CLog.Close();
    }

    private struct GameTick
    {
        public long Min;
        public long Max;
        public long Avg;
        public long AvgCnt;
        public long LogTick;
    }

    private const long TICKS_IN_SECOND = 10000000;
    private const long PERFORMANCE_LOG_INTERVAL = TICKS_IN_SECOND;
    GameTick GameTickData = new GameTick { Max = 0, Min = 1000* PERFORMANCE_LOG_INTERVAL, Avg = 0, AvgCnt = 0, LogTick = 0 };
    private long GameTickLast = -1; // 1 tick is equal to 100 nano secods. 10000000 ticks in second
    private long GameTickLastLog = -1;
    private DateTime GameTickDt;
    public void OnTickGame()
    {
        // NO DEBUG LOG HERE!!! HAVE TO BE ULTRA FAST FUNCTION!!!
        if (CConfig.DEBUG_PERFORMANCE_LOG_ENABLE && CConfig.IsLoggingEnabled() && CLog.IsInitialized)
        {
            GameTickDt = DateTime.Now;
            if (GameTickLast == -1)
            {
                GameTickLast = GameTickDt.Ticks;
                GameTickLastLog = GameTickDt.Ticks;
                return;
            }
            long tickInterval = GameTickDt.Ticks - GameTickLast;
            GameTickLast = GameTickDt.Ticks;
            if (0 == GameTickData.AvgCnt) GameTickData.LogTick = tickInterval;
            GameTickData.Avg += tickInterval;
            GameTickData.AvgCnt++;
            if (tickInterval > GameTickData.Max) GameTickData.Max = tickInterval;
            if (tickInterval < GameTickData.Min) GameTickData.Min = tickInterval;
            if (GameTickDt.Ticks - GameTickLastLog >= PERFORMANCE_LOG_INTERVAL)
            {
                GameTickLastLog = GameTickLast;
                CLog.Write("#PFMNC_GT;MIN;"+GameTickData.Min.ToString()+ ";MAX;"+GameTickData.Max.ToString()+";AVG;"+(GameTickData.Avg/GameTickData.AvgCnt).ToString()+ ";LOG;"+GameTickData.LogTick.ToString());
                GameTickData.Max = 0;
                GameTickData.Min = 1000 * PERFORMANCE_LOG_INTERVAL;
                GameTickData.Avg = 0;
                GameTickData.AvgCnt = 0;
                GameTickData.LogTick = 0;
            }
        }
    }

    GameTick RealTickData = new GameTick { Max = 0, Min = 1000 * PERFORMANCE_LOG_INTERVAL, Avg = 0, AvgCnt = 0, LogTick = 0 };
    private long RealTickLast = -1;
    private long RealTickLastLog = -1;
    private DateTime RealTickDt;

    public void OnTickReal()
    {
        // NO DEBUG LOG HERE!!! HAVE TO BE ULTRA FAST FUNCTION!!!
        if (CConfig.DEBUG_PERFORMANCE_LOG_ENABLE && CConfig.IsLoggingEnabled() && CLog.IsInitialized)
        {
            RealTickDt = DateTime.Now;
            if (RealTickLast == -1)
            {
                RealTickLast = RealTickDt.Ticks;
                RealTickLastLog = RealTickDt.Ticks;
                return;
            }
            long tickInterval = RealTickDt.Ticks - RealTickLast;
            RealTickLast = RealTickDt.Ticks;
            if (0 == RealTickData.AvgCnt) RealTickData.LogTick = tickInterval;
            RealTickData.Avg += tickInterval;
            RealTickData.AvgCnt++;
            if (tickInterval > RealTickData.Max) RealTickData.Max = tickInterval;
            if (tickInterval < RealTickData.Min) RealTickData.Min = tickInterval;
            if (RealTickDt.Ticks - RealTickLastLog >= PERFORMANCE_LOG_INTERVAL)
            {
                RealTickLastLog = RealTickLast;
                CLog.Write("#PFMNC_RT;MIN;" + RealTickData.Min.ToString() + ";MAX;" + RealTickData.Max.ToString() + ";AVG;" + (RealTickData.Avg / RealTickData.AvgCnt).ToString() + ";LOG;" + RealTickData.LogTick.ToString());
                RealTickData.Max = 0;
                RealTickData.Min = 1000 * PERFORMANCE_LOG_INTERVAL;
                RealTickData.Avg = 0;
                RealTickData.AvgCnt = 0;
                RealTickData.LogTick = 0;
            }
        }
    }

    public void OnPlayerDisconnected(Player player, string diagnostic)
    {
        try
        {
            if (DEBUG_MESSAGES && CLog.IsInitialized) CLog.Write("OnPlayerDisconnected player=" + ((player != null) ? player.Name() : "=null") + " diagnostic=" + diagnostic);
            if (player != null)
            {
                int playerIdx = GetPlayerAssignedAiAircraftIdx(player);
                if (playerIdx >= 0)
                {
                    AiAircraft aircraft = PlayersAssignedAircrafts[playerIdx].aircraft;
                    if (DEBUG_MESSAGES && CLog.IsInitialized) CLog.Write("Player " + player.Name() + " was assigned to aircraft! Resign now and destroy aircraft!");
                    DropPlayerFromAssignedAiAircraft(player);
                    BaseMission.Timeout(0.1, () =>
                    {
                        m_KillDisusedPlanes.DestroyPlane(aircraft);
                    });
                }
            }
        }
        catch (Exception e)
        {
            if (DEBUG_MESSAGES && CLog.IsInitialized) CLog.Write(e.ToString() + "\n" + e.Message.ToString());
        }
    }


    public void OnPlaceEnter(Player player, AiActor actor, int placeIndex)
    {
        try
        {
            if (DEBUG_MESSAGES && CLog.IsInitialized) CLog.Write("OnPlaceEnter player=" + ((player != null) ? player.Name() : "=null") + " actor=" + ((actor != null) ? actor.Name() : "=null") + " placeIdx=" + placeIndex.ToString());
            // When player entering not pilot position do not care managing this aircraft
            if ((actor != null) && (actor is AiAircraft) && (placeIndex == 0))
            {
                // Player spawned aicraft waypoints update
                AiAircraft aircraft = (actor as AiAircraft);
                bool isPlayerJustSpawnedAtSpawnArea = AiAircraftTryUpdatePlayerSpawnDefaultWay(aircraft);

                if (CConfig.DISABLE_PILOT_TO_LEAVE_MOVING_AIRCRAFT)
                {
                    if (GetPlayerAssignedAiAircraftIdx(player) < 0)
                    {
                        if (DEBUG_MESSAGES && CLog.IsInitialized) CLog.Write("Player " + player.Name() + " will be assigned to aricraft " + aircraft.Name());
                        AssignPlayerToAiAircraft(player, aircraft);
                        // check if aircraft defueled and refuel it back
                        IsDefueledAircraft(aircraft, true);
                    }
                    else
                    if (IsPlayerAssignedToAircraft(player, aircraft))
                    {
                        if (DEBUG_MESSAGES && CLog.IsInitialized) CLog.Write("Player " + player.Name() + " already assigned to this aricraft " + aircraft.Name() + " returned by script?");
                    }
                    else
                    {
                        if (DEBUG_MESSAGES && CLog.IsInitialized) CLog.Write("Player " + player.Name() + " alread assigned to another aricraft!");
                        if (CConfig.DISABLE_PILOT_TO_LEAVE_MOVING_AIRCRAFT && isPlayerJustSpawnedAtSpawnArea)
                        {
                            if (DEBUG_MESSAGES && CLog.IsInitialized) CLog.Write("This aricraft " + aircraft.Name() + " is about to be destroyed!");
                            // PLayer will be removed from this newly created aircraft by script! But not destroyed in OnPlaceLeave() due to he is already assigned to another aircraft! Let's destroy it here!
                            BaseMission.Timeout(0.1, () =>
                            {
                                m_KillDisusedPlanes.DestroyPlane(aircraft);
                            });
                        }
                        else
                        {
                            if (DEBUG_MESSAGES && CLog.IsInitialized) CLog.Write("This aricraft " + aircraft.Name() + " will be intact. Let it fly!");
                        }
                    }
                }

            }
        }
        catch (Exception e)
        {
            if (DEBUG_MESSAGES && CLog.IsInitialized) CLog.Write(e.ToString() + "\n" + e.Message.ToString());
        }
    }

    public void OnPlaceLeave(Player player, AiActor actor, int placeIndex)
    {
        try
        {
            if (DEBUG_MESSAGES && CLog.IsInitialized) CLog.Write("OnPlaceLeave player=" + ((player != null) ? player.Name() : "=null") + " actor=" + ((actor != null) ? actor.Name() : "=null") + " placeIdx=" + placeIndex.ToString());
            if (CConfig.DISABLE_PILOT_TO_LEAVE_MOVING_AIRCRAFT)
            {
                if ((player != null) && (actor != null) && player.IsConnected() && (actor is AiAircraft))
                {
                    AiAircraft aircraft = actor as AiAircraft;
                    if (IsPlayerAssignedToAircraft(player, aircraft))
                    {
                        if (DEBUG_MESSAGES && CLog.IsInitialized) CLog.Write("Player " + player.Name() + " is trying to leave place in aircraft " + aircraft.Name());
                        bool isNoPlayersInAircraft = m_KillDisusedPlanes.IsNoPLayersInAircraft(aircraft);
                        if (isNoPlayersInAircraft || (placeIndex == 0)) //pilot cann't leave airborne aircraft even if another seat occupied
                        {
                            if (aircraft.IsAlive() && (aircraft.Person(0) != null) && (aircraft.Person(0).Health > 0) && aircraft.IsValid())
                            {
                                //EAircraftLocation aircraftLocation = GetAircraftLocation(aircraft);
                                double aircraftTAS = aircraft.getParameter(part.ParameterTypes.Z_VelocityTAS, -1);
                                // Do not  allow leave moving aircraft!
                                if (aircraftTAS > 1.0)
                                {
                                    if (DEBUG_MESSAGES && CLog.IsInitialized) CLog.Write("Player " + player.Name() + " is in moving aircraft and is about to enter pilot seat again " + aircraft.Name());
                                    BaseMission.Timeout((player.Ping() + 50) * 0.001, () =>
                                    {
                                        player.PlaceEnter(actor, 0);
                                    });
                                    Player[] recepients = { player };
                                    BaseMission.GamePlay.gpHUDLogCenter(recepients, "Bailout, crash or land!");
                                    return;
                                }
                                else
                                {
                                    if (DEBUG_MESSAGES && CLog.IsInitialized) CLog.Write("Player " + player.Name() + " aircraft not moving.");
                                }
                                if (!isNoPlayersInAircraft)
                                {
                                    // Hey! Pilot left but lets check if he is occupying other places
                                    int primIdx = player.PlacePrimary();
                                    int secIdx = player.PlaceSecondary();
                                    if (DEBUG_MESSAGES && CLog.IsInitialized) CLog.Write("Hey! Player still in aircraft! PlacePrimary=" + primIdx.ToString() + " PlaceSecondary=" + secIdx.ToString());
                                    if (primIdx >= 0)
                                    {
                                        // have to generate new on leave event and do stuff there
                                        BaseMission.Timeout(1, () =>
                                        {
                                            player.PlaceLeave(primIdx);
                                        });
                                        return;
                                    }
                                    if (secIdx >= 0)
                                    {
                                        // have to generate new on leave event and do stuff there
                                        BaseMission.Timeout(1, () =>
                                        {
                                            player.PlaceLeave(secIdx);
                                        });
                                        return;
                                    }
                                }
                                if (DEBUG_MESSAGES && CLog.IsInitialized) CLog.Write("Ok, it is allowed to leave pilot seat.");
                            }
                            else
                            {
                                
                                if (DEBUG_MESSAGES && CLog.IsInitialized)
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
                        else //if (!isNoPlayersInAircraft && (placeIndex != 0))
                        {
                            if (DEBUG_MESSAGES && CLog.IsInitialized) CLog.Write("At least some one is still in aircraft and player " + player.Name() + " not a pilot. No need to call KillDisusedPlanes.OnPlaceLeave(). Just free place");
                            return;
                        }
                        // Player is assigned to aircraft... have to be resigned
                        DropPlayerFromAssignedAiAircraft(player);
                        if (DEBUG_MESSAGES && CLog.IsInitialized) CLog.Write("Player resigned from aircraft.");
                    }
                    else
                    {
                        if (DEBUG_MESSAGES && CLog.IsInitialized) CLog.Write("Player " + player.Name() + " not assigned to this aircraft. Do not destroy unassigned aircraft!");
                        // do not destroy unassigned aircraft!
                        return;
                    }
                }
                else
                {
                    //if ((player != null) && (actor != null) && player.IsConnected() && (actor is AiAircraft))
                    if (DEBUG_MESSAGES && CLog.IsInitialized)
                    {
                        string msg = "It seems like aricraft can't be piloted... "
                            + ((player == null) ? "--- (player == null)" : "")
                            + ((actor == null) ? "--- (actor == null)" : "")
                            + ((!player.IsConnected()) ? "--- (!player.IsConnected())" : "")
                            + (((actor != null) && !(actor is AiAircraft)) ? "--- (!(actor is AiAircraft))" : "");
                        CLog.Write(msg);
                    }
                }

                // ... at this point it is decided aircraft to be destroyed...
            }
            else
            if (CConfig.DISABLE_PLAYER_TO_FLY_AS_PASSANGER_WITH_AI_PILOT)
            {
                //
                // When trying to leave pilot place then leave all places in aircraft and then destroy it! Disable AI to fly mission!
                //
                if ((player != null) && (actor != null) && player.IsConnected() && (actor is AiAircraft))
                {
                    AiAircraft aircraft = actor as AiAircraft;
                    if (DEBUG_MESSAGES && CLog.IsInitialized) CLog.Write("Player " + player.Name() + " is trying to leave aircraft " + aircraft.Name());
                    bool isNoPlayersInAircraft = m_KillDisusedPlanes.IsNoPLayersInAircraft(aircraft);
                    if ((placeIndex == 0) && !isNoPlayersInAircraft)
                    {
                        int primIdx = player.PlacePrimary();
                        int secIdx = player.PlaceSecondary();
                        if (DEBUG_MESSAGES && CLog.IsInitialized) CLog.Write("Hey! Player still in aircraft! PlacePrimary=" + primIdx.ToString() + " PlaceSecondary=" + secIdx.ToString());
                        if (primIdx >= 0)
                        {
                            // have to generate new on leave event and do stuff there
                            BaseMission.Timeout(1, () =>
                            {
                                player.PlaceLeave(primIdx);
                            });
                            return;
                        }
                        if (secIdx >= 0)
                        {
                            // have to generate new on leave event and do stuff there
                            BaseMission.Timeout(1, () =>
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
                    if (DEBUG_MESSAGES && CLog.IsInitialized)
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
            if (DEBUG_MESSAGES && CLog.IsInitialized) CLog.Write("m_KillDisusedPlanes.OnPlaceLeave()");
            m_KillDisusedPlanes.OnPlaceLeave(player, actor, placeIndex);
        }
        catch (Exception e)
        {
            if (DEBUG_MESSAGES && CLog.IsInitialized) CLog.Write(e.ToString() + "\n" + e.Message.ToString());
        }
    }

    ////////////////////////
    //                    //
    //  Custom functions  //
    //                    //
    ////////////////////////


    //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    //
    //  Airport search and sort management fucntions
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
        if (BaseMission.GamePlay.gpAirports().Length == 0)
        {
            return;
        }

        // Get list of all airfields on the map
        AiAirport[] missionAirportsSortedByArmy = new AiAirport[BaseMission.GamePlay.gpAirports().Length ];
        BaseMission.GamePlay.gpAirports().CopyTo(missionAirportsSortedByArmy, 0);

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
        if (DEBUG_MESSAGES && CLog.IsInitialized) CLog.Write("Sorted list of airfields:");
        for (int i = 0; i < missionAirportsSortedByArmy.Length; i++)
        {
            if (DEBUG_MESSAGES && CLog.IsInitialized) CLog.Write(missionAirportsSortedByArmy[i].Name() + " army=" + missionAirportsSortedByArmy[i].Army().ToString());
        }

        if (missionAirportsSortedByArmy[0].Army() != 0)
        {
            if (DEBUG_MESSAGES && CLog.IsInitialized) CLog.Write("OMG! Neutral airports with Army() == 0 NOT FOUND!!!");
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
        if (DEBUG_MESSAGES && CLog.IsInitialized) CLog.Write("---Indexes for different armies airfields for fast searching done.");
        for (int i = 0; i < missionAirportIndexesByArmy.Count; i++)
        {
            if (DEBUG_MESSAGES && CLog.IsInitialized) CLog.Write("Index=" + i.ToString()
                + " Army=" + missionAirportIndexesByArmy[i].Army.ToString()
                + " First=" + missionAirportIndexesByArmy[i].FirstIdx.ToString()
                + " Last=" + missionAirportIndexesByArmy[i].LastIdx.ToString());
        }

        // create array of neautral airports lists by armies
        if (missionAirportIndexesByArmy.Count > 0)
        {
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
            if (DEBUG_MESSAGES && CLog.IsInitialized) CLog.Write("---Army values filled.");

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
                if (!nonNeutralAirportFound)
                {
                    NeutralAirportsByArmies[0].aiAirports.Add(missionNeutralAirport);
                }
            }

            //
            // debug printing
            //
            if (DEBUG_MESSAGES && CLog.IsInitialized) CLog.Write("Neutral airfields by army.");

            for (int armyIdx = 0; armyIdx < NeutralAirportsByArmies.Length; armyIdx++)
            {
                if (DEBUG_MESSAGES && CLog.IsInitialized) CLog.Write("---List of airports for Army=" + NeutralAirportsByArmies[armyIdx].Army.ToString());
                for (int airportIdx = 0; airportIdx < NeutralAirportsByArmies[armyIdx].aiAirports.Count; airportIdx++)
                {
                    if (DEBUG_MESSAGES && CLog.IsInitialized) CLog.Write(NeutralAirportsByArmies[armyIdx].aiAirports[airportIdx].Name());
                }
            }
        }
    }

    //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    //
    // Aircraft location detection funcitons
    //

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

    public EAircraftLocation GetAircraftLocation(AiAircraft aircraft)
    {
        if (aircraft == null)
        {
            return EAircraftLocation.Unknown;
        }

        bool aircraftIsOnTheGround = false;
        if (!aircraft.IsAirborne()) // Just spawen and never airborne.
        {
            aircraftIsOnTheGround = true;
            if (DEBUG_MESSAGES && CLog.IsInitialized) CLog.Write(aircraft.Name() + " is NOT airborne.");
        }
        else // Important notice! Aircraft that airborne once stays airborne forever, even after landed.
        {
            double aircraftAGL = aircraft.getParameter(part.ParameterTypes.Z_AltitudeAGL, -1);
            double aircraftTAS = aircraft.getParameter(part.ParameterTypes.Z_VelocityTAS, -1);
            if (DEBUG_MESSAGES && CLog.IsInitialized) CLog.Write(aircraft.Name() + " is AGL=" + aircraftAGL.ToString() + "m and TAS=" + aircraftTAS.ToString() + "m/s");
            if ((aircraftAGL < 5) && (aircraftTAS < 3.6))
            {
                aircraftIsOnTheGround = true;
            }
        }

        if (aircraftIsOnTheGround)
        {
            Point3d aircraftPos = aircraft.Pos();
            int aircraftArmy = aircraft.Army();

            for (int armyIdx = 0; armyIdx < NeutralAirportsByArmies.Length; armyIdx++)
            {
                for (int airportIdx = 0; airportIdx < NeutralAirportsByArmies[armyIdx].aiAirports.Count; airportIdx++)
                {
                    AiAirport airportFromList = NeutralAirportsByArmies[armyIdx].aiAirports[airportIdx];
                    Point3d airportFromListPos = airportFromList.Pos();
                    double distanceToAirportFromList = airportFromListPos.distanceLinf(ref aircraftPos);
                    if (distanceToAirportFromList < airportFromList.CoverageR())
                    {
                        int airportFromListArmy = NeutralAirportsByArmies[armyIdx].Army;
                        if (airportFromListArmy == aircraftArmy)
                        {
                            if (DEBUG_MESSAGES && CLog.IsInitialized) CLog.Write(aircraft.Name() + " is on friendly airfiled " + airportFromList.Name() + " distance " + distanceToAirportFromList.ToString());
                            return EAircraftLocation.FriendlyAirfield;
                        }
                        else if (airportFromListArmy == 0)
                        {
                            if (DEBUG_MESSAGES && CLog.IsInitialized) CLog.Write(aircraft.Name() + " is on neutral airfiled " + airportFromList.Name() + " distance " + distanceToAirportFromList.ToString());
                            return EAircraftLocation.NeutralAirfield;
                        }
                        else
                        {
                            if (DEBUG_MESSAGES && CLog.IsInitialized) CLog.Write(aircraft.Name() + " is on enemy airfiled " + airportFromList.Name() + " distance " + distanceToAirportFromList.ToString());
                            return EAircraftLocation.EnemyAirfield;
                        }
                    }
                }
            }

            if (BaseMission.GamePlay.gpLandType(aircraftPos.x, aircraftPos.y) == LandTypes.WATER)
            {
                if (DEBUG_MESSAGES && CLog.IsInitialized) CLog.Write(aircraft.Name() + " is ditched in to the water.");
                return EAircraftLocation.DitchedSea;
            }
            //// Do not uncomment code below. It may feel like that is nice logic, but EAircraftLocation.DitchedGround will be returned in the end of function.
            //else
            //{
            //    if (DEBUG_MESSAGES && CLog.IsInitialized) CLog.Write(aircraft.Name() + " is abbandoned on the ground.");
            //    return EAircraftLocation.DitchedGround;
            //}
            ////
        }
        else
        {
            if (DEBUG_MESSAGES && CLog.IsInitialized) CLog.Write(aircraft.Name() + " is airborne.");
            return EAircraftLocation.Airborne;
        }
        if (DEBUG_MESSAGES && CLog.IsInitialized) CLog.Write(aircraft.Name() + " is abbandoned on the ground.");
        return EAircraftLocation.DitchedGround;
    }

    //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    //
    // Currently player spawned aircraft at spawn are assigned with 3 waypoints in radius about 1 km
    // If player just entered aircraft with similar waypoint it is decided that this aircraft was spawned by player at spawn area.
    // If this function stopped working properly, then write new one :)
    //
    //

    public bool IsAircraftPlayerSpawnedAtSpawnArea(AiAircraft aircraft)
    {
        AiWayPoint[] aiWayPoints = aircraft.Group().GetWay();
        if (aiWayPoints[0] is AiAirWayPoint)
        {
            if (aiWayPoints.Length == 3)
            {
                Point3d p1 = aiWayPoints[1].P;
                if ((aiWayPoints[0].P.distanceLinf(ref p1) < 2000)
                && (aiWayPoints[2].P.distanceLinf(ref p1) < 2000))
                {
                    return true;
                }
            }
        }
        return false;
    }

    public bool AiAircraftTryUpdatePlayerSpawnDefaultWay(AiAircraft aircraft)
    {
        if (IsAircraftPlayerSpawnedAtSpawnArea(aircraft))
        {
            if (DEBUG_MESSAGES && CLog.IsInitialized) CLog.Write("Looks like player spawned in spawn area! Reset default waypoints to avoid useless GC orders to land right after takeoff...");
            AiWayPoint[] aiWayPoints = aircraft.Group().GetWay();
            aiWayPoints[1].P.y = aiWayPoints[1].P.y + 5000;
            aircraft.Group().SetWay(aiWayPoints);
            return true;
        }
        return false;
    }

    //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    //
    // Player assigning and resigning to/from aircraft logic
    //
    public class PlayerAssignedToAiAircraft
    {
        public Player player = null;
        public AiAircraft aircraft = null;
        public PlayerAssignedToAiAircraft(Player _player, AiAircraft _aircraft)
        {
            player = _player;
            aircraft = _aircraft;
        }
    }

    List<PlayerAssignedToAiAircraft> PlayersAssignedAircrafts = new List<PlayerAssignedToAiAircraft>();

    public void AssignPlayerToAiAircraft(Player player, AiAircraft aircraft)
    {
        if (player == null)
            return;
        int playerIdx = GetPlayerAssignedAiAircraftIdx(player);
        if (playerIdx < 0)
        {
            //add new
            PlayersAssignedAircrafts.Add(new PlayerAssignedToAiAircraft(player, aircraft));
        }
        else
        {
            // replce old by new
            PlayersAssignedAircrafts[playerIdx].aircraft = aircraft;
        }
    }

    public bool IsPlayerAssignedToAircraft(Player player, AiAircraft aircraft)
    {
        int playerIdx = GetPlayerAssignedAiAircraftIdx(player);
        if (playerIdx >= 0)
        {
            if (PlayersAssignedAircrafts[playerIdx].aircraft.Equals(aircraft))
            {
                return true;
            }
        }
        return false;
    }

    public int GetPlayerAssignedAiAircraftIdx(Player player)
    {
        for (int i = 0; i < PlayersAssignedAircrafts.Count; i++)
        {
            if (PlayersAssignedAircrafts[i].player.Equals(player))
            {
                return i;
            }
        }
        return -1;
    }


    public void DropPlayerFromAssignedAiAircraft(Player player)
    {
        for (int i = 0; i < PlayersAssignedAircrafts.Count; i++)
        {
            if (PlayersAssignedAircrafts[i].player.Equals(player))
            {
                PlayersAssignedAircrafts.RemoveAt(i);
            }
        }
    }

    //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    //
    // Set of functions to defuel abandoned aircrafts on the ground and refuel them back when pilot returned.
    //
    public class DefueledAircraft
    {
        public AiAircraft aircraft = null;
        public int fuelPctBeforeDefuel = 0;

        public DefueledAircraft(AiAircraft aircraft, int fuelPctBeforeDefuel)
        {
            this.aircraft = aircraft;
            this.fuelPctBeforeDefuel = fuelPctBeforeDefuel;
        }
    }
    public List<DefueledAircraft> DefueledAcircrafts = new List<DefueledAircraft>();

    public bool IsDefueledAircraft(AiAircraft aiAircraft, bool refuelAndDrop)
    {
        if (aiAircraft != null)
        {
            for (int i = 0; i < DefueledAcircrafts.Count; i++)
            {
                if (DefueledAcircrafts[i].aircraft.Equals(aiAircraft))
                {
                    if (refuelAndDrop)
                    {
                        DefueledAcircrafts[i].aircraft.RefuelPlane(DefueledAcircrafts[i].fuelPctBeforeDefuel);
                        DefueledAcircrafts.RemoveAt(i);
                    }
                    return true;
                }
            }
        }
        return false;
    }

    //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ///////////////////////////////////////////////////
    ///////////////////////////////////////////////////
    ////                                           ////
    ////  NOT USED EVENTS, JUST FOR DEBUG LOGGING. ////
    ////  COMMENT AFTER DEBUG!!!                   ////
    ////                                           ////
    ///////////////////////////////////////////////////
    ///////////////////////////////////////////////////

    public void OnAircraftTookOff(int missionNumber, string shortName, AiAircraft aircraft)
    {
        try
        {
            if (DEBUG_MESSAGES && CLog.IsInitialized) CLog.Write("OnAircraftTookOff missionNumber=" + missionNumber.ToString() + " shortName=" + shortName + " aircraft=" + ((aircraft != null) ? aircraft.Name() : "=null"));
        }
        catch (Exception e)
        {
            if (DEBUG_MESSAGES && CLog.IsInitialized) CLog.Write(e.ToString() + "\n" + e.Message.ToString());
        }
    }

    public void OnAircraftCrashLanded(int missionNumber, string shortName, AiAircraft aircraft)
    {
        try
        {
            if (DEBUG_MESSAGES && CLog.IsInitialized) CLog.Write("OnAircraftCrashLanded missionNumber=" + missionNumber.ToString() + " shortName=" + shortName + " aircraft=" + ((aircraft != null) ? aircraft.Name() : "=null"));
        }
        catch (Exception e)
        {
            if (DEBUG_MESSAGES && CLog.IsInitialized) CLog.Write(e.ToString() + "\n" + e.Message.ToString());
        }
    }
    public void OnAircraftLanded(int missionNumber, string shortName, AiAircraft aircraft)
    {
        try
        {
            if (DEBUG_MESSAGES && CLog.IsInitialized) CLog.Write("OnAircraftCrashLanded missionNumber=" + missionNumber.ToString() + " shortName=" + shortName + " aircraft=" + ((aircraft != null) ? aircraft.Name() : "=null"));
        }
        catch (Exception e)
        {
            if (DEBUG_MESSAGES && CLog.IsInitialized) CLog.Write(e.ToString() + "\n" + e.Message.ToString());
        }
    }
    public void OnTrigger(int missionNumber, string shortName, bool active)
    {
        try
        {
            if (DEBUG_MESSAGES && CLog.IsInitialized) CLog.Write("OnTrigger missionNumber=" + missionNumber.ToString() + " shortName=" + shortName + " active=" + active.ToString());
        }
        catch (Exception e)
        {
            if (DEBUG_MESSAGES && CLog.IsInitialized) CLog.Write(e.ToString() + "\n" + e.Message.ToString());
        }
    }

    public void OnActorCreated(int missionNumber, string shortName, AiActor actor)
    {
        try
        {
            if (DEBUG_MESSAGES && CLog.IsInitialized) CLog.Write("OnActorCreated missionNumber=" + missionNumber.ToString() + " shortName=" + shortName + " actor=" + ((actor != null) ? actor.Name() : "=null"));
        }
        catch(Exception e)
        {
            if (DEBUG_MESSAGES && CLog.IsInitialized) CLog.Write(e.ToString() + "\n" + e.Message.ToString());
        }
    }

    //// COMENTED DUE TO A LOT OF EVENTS GENERATED
    //public void OnActorDamaged(int missionNumber, string shortName, AiActor actor, AiDamageInitiator initiator, NamedDamageTypes damageType)
    //{
    //    try
    //    {
    //        if (DEBUG_MESSAGES && CLog.IsInitialized) CLog.Write("OnActorDamaged missionNumber="
    //            + missionNumber.ToString()
    //            + " shortName=" + shortName
    //            + " actor=" + ((actor != null) ? actor.Name() : "=null")
    //            + " initiator is " + ((initiator != null) ? (" Actor={" + ((initiator.Actor != null) ? initiator.Actor.Name() : "=null")
    //                                                        + " Person=" + ((initiator.Person != null) ? initiator.Person.Name() : "=null")
    //                                                        + " Player=" + ((initiator.Player != null) ? initiator.Player.Name() : "=null")
    //                                                        + " Tool=" + ((initiator.Tool != null) ? initiator.Tool.Type.ToString() + "}" : "=null}"))
    //                                                      : "=null}")
    //            + " damageType=" + damageType.ToString());
    //    }
    //    catch (Exception e)
    //    {
    //        if (DEBUG_MESSAGES && CLog.IsInitialized) CLog.Write(e.ToString() + "\n" + e.Message.ToString());
    //    }
    //}

    public void OnActorDead(int missionNumber, string shortName, AiActor actor, List<DamagerScore> damages)
    {
        try
        {
            if (DEBUG_MESSAGES && CLog.IsInitialized)
            {
                string msg = "OnActorDead missionNumber=" + missionNumber.ToString() + " shortName=" + shortName + " actor=" + ((actor != null) ? actor.Name() : "=null");

                if (damages == null)
                {
                    msg += " damages==null";
                }
                else
                if (damages.Count == 0)
                {
                    msg += " damages=ZERO DAMAGES";
                }
                else
                {
                    msg += " damages:\n";
                    for (int i = 0; i < damages.Count; i++)
                    {
                        msg += " score=" + damages[i].score + "\n";
                    }
                }
                CLog.Write(msg);
            }
        }
        catch (Exception e)
        {
            if (DEBUG_MESSAGES && CLog.IsInitialized) CLog.Write(e.ToString() + "\n" + e.Message.ToString());
        }
    }

    public void OnActorDestroyed(int missionNumber, string shortName, AiActor actor)
    {
        try
        {
            if (DEBUG_MESSAGES && CLog.IsInitialized) CLog.Write("OnActorDestroyed missionNumber=" + missionNumber.ToString() + " shortName=" + shortName + " actor=" + ((actor != null) ? actor.Name() : "=null"));
        }
        catch (Exception e)
        {
            if (DEBUG_MESSAGES && CLog.IsInitialized) CLog.Write(e.ToString() + "\n" + e.Message.ToString());
        }
    }

    public void OnActorTaskCompleted(int missionNumber, string shortName, AiActor actor)
    {
        try
        {
            if (DEBUG_MESSAGES && CLog.IsInitialized) CLog.Write("OnActorTaskCompleted missionNumber=" + missionNumber.ToString() + " shortName=" + shortName + " actor=" + ((actor != null) ? actor.Name() : "=null"));
        }
        catch (Exception e)
        {
            if (DEBUG_MESSAGES && CLog.IsInitialized) CLog.Write(e.ToString() + "\n" + e.Message.ToString());
        }
    }

    //// COMENTED DUE TO A LOT OF EVENTS GENERATED
    //public void OnAircraftCutLimb(int missionNumber, string shortName, AiAircraft aircraft, AiDamageInitiator initiator, LimbNames limbName)
    //{
    //    try
    //    {
    //        if (DEBUG_MESSAGES && CLog.IsInitialized) CLog.Write("OnAircraftCutLimb missionNumber=" + missionNumber.ToString() + " shortName=" + shortName + " aircraft=" + ((aircraft != null) ? aircraft.Name() : "=null")
    //            + " initiator is {" + ((initiator != null) ? (" Actor=" + ((initiator.Actor != null) ? initiator.Actor.Name() : "=null")
    //                                                        + " Person=" + ((initiator.Person != null) ? initiator.Person.Name() : "=null")
    //                                                        + " Player=" + ((initiator.Player != null) ? initiator.Player.Name() : "=null")
    //                                                        + " Tool=" + ((initiator.Tool != null) ? initiator.Tool.Type.ToString() + "}" : "=null}"))
    //                                                      : "=null}")
    //            + " limbName=" + limbName.ToString());
    //    }
    //    catch (Exception e)
    //    {
    //        if (DEBUG_MESSAGES && CLog.IsInitialized) CLog.Write(e.ToString() + "\n" + e.Message.ToString());
    //    }
    //}

    //// COMMENTED DUE TO TO MANY EXCEPTION GENERATED HERE
    //public void OnAircraftDamaged(int missionNumber, string shortName, AiAircraft aircraft, AiDamageInitiator initiator, NamedDamageTypes damageType)
    //{
    //    try
    //    {
    //        if (DEBUG_MESSAGES && CLog.IsInitialized) CLog.Write("OnActorDamaged missionNumber="
    //            + missionNumber.ToString()
    //            + " shortName=" + shortName
    //            + " aircraft=" + ((aircraft != null) ? aircraft.Name() : "=null")
    //            + " initiator is {" + ((initiator != null) ? (" Actor=" + ((initiator.Actor != null) ? initiator.Actor.Name() : "=null")
    //                                                        + " Person=" + ((initiator.Person != null) ? initiator.Person.Name() : "=null")
    //                                                        + " Player=" + ((initiator.Player != null) ? initiator.Player.Name() : "=null")
    //                                                        + " Tool=" + ((initiator.Tool != null) ? initiator.Tool.Type.ToString() + "}" : "=null}"))
    //                                                      : "=null}")
    //            + " damageType=" + damageType.ToString());
    //    }
    //    catch (Exception e)
    //    {
    //        if (DEBUG_MESSAGES && CLog.IsInitialized) CLog.Write(e.ToString() + "\n" + e.Message.ToString());
    //    }
    //}

    public void OnAircraftKilled(int missionNumber, string shortName, AiAircraft aircraft)
    {
        try
        {
            if (DEBUG_MESSAGES && CLog.IsInitialized) CLog.Write("OnAircraftKilled missionNumber=" + missionNumber.ToString() + " shortName=" + shortName + " aircraft=" + ((aircraft != null) ? aircraft.Name() : "=null"));
        }
        catch (Exception e)
        {
            if (DEBUG_MESSAGES && CLog.IsInitialized) CLog.Write(e.ToString() + "\n" + e.Message.ToString());
        }
    }

    //// COMENTED DUE TO A LOT OF EVENTS GENERATED
    //public void OnAircraftLimbDamaged(int missionNumber, string shortName, AiAircraft aircraft, AiLimbDamage limbDamage)
    //{
    //    try
    //    {
    //        if (DEBUG_MESSAGES && CLog.IsInitialized) CLog.Write("OnAircraftLimbDamaged missionNumber=" + missionNumber.ToString()
    //            + " shortName=" + shortName
    //            + " aircraft=" + ((aircraft != null) ? aircraft.Name() : "=null")
    //            + " limbDamage is {" + ((limbDamage == null) ? "=null" : " LimbId=" + limbDamage.LimbId.ToString() + " ... and other parameters }"));
    //    }
    //    catch (Exception e)
    //    {
    //        if (DEBUG_MESSAGES && CLog.IsInitialized) CLog.Write(e.ToString() + "\n" + e.Message.ToString());
    //    }
    //}

    public void OnAutopilotOff(AiActor actor, int placeIndex)
    {
        try
        {
            if (DEBUG_MESSAGES && CLog.IsInitialized) CLog.Write("OnAutopilotOff actor=" + ((actor != null) ? actor.Name() : "=null") + " placeIndex=" + placeIndex.ToString());
        }
        catch (Exception e)
        {
            if (DEBUG_MESSAGES && CLog.IsInitialized) CLog.Write(e.ToString() + "\n" + e.Message.ToString());
        }
    }

    public void OnAutopilotOn(AiActor actor, int placeIndex)
    {
        try
        {
            if (DEBUG_MESSAGES && CLog.IsInitialized) CLog.Write("OnAutopilotOn actor=" + ((actor != null) ? actor.Name() : "=null") + " placeIndex=" + placeIndex.ToString());
        }
        catch (Exception e)
        {
            if (DEBUG_MESSAGES && CLog.IsInitialized) CLog.Write(e.ToString() + "\n" + e.Message.ToString());
        }
    }

    public void OnBombExplosion(string title, double mass, Point3d pos, AiDamageInitiator initiator, int eventArgInt)
    {
        try
        {
            if (DEBUG_MESSAGES && CLog.IsInitialized) CLog.Write("OnBombExplosion title=" + title + " mass=" + mass.ToString()
                + " pos.{X,Y,Z}={" + pos.x.ToString() + ", " + pos.y.ToString() + ", " + pos.z.ToString() + "}"
                + " initiator is {" + ((initiator != null) ? (" Actor=" + ((initiator.Actor != null) ? initiator.Actor.Name() : "=null")
                                                            + " Person=" + ((initiator.Person != null) ? initiator.Person.Name() : "=null")
                                                            + " Player=" + ((initiator.Player != null) ? initiator.Player.Name() : "=null")
                                                            + " Tool=" + ((initiator.Tool != null) ? initiator.Tool.Type.ToString() + "}" : "=null}"))
                                                          : "=null}")
                + " eventArgInt=" + eventArgInt.ToString());
        }
        catch (Exception e)
        {
            if (DEBUG_MESSAGES && CLog.IsInitialized) CLog.Write(e.ToString() + "\n" + e.Message.ToString());
        }
    }

    public void OnBuildingKilled(string title, Point3d pos, AiDamageInitiator initiator, int eventArgInt)
    {
        try
        {
            if (DEBUG_MESSAGES && CLog.IsInitialized) CLog.Write("OnBuildingKilled title=" + title
                + " pos.{X,Y,Z}={" + pos.x.ToString() + ", " + pos.y.ToString() + ", " + pos.z.ToString() + "}"
                + " initiator is {" + ((initiator != null) ? (" Actor=" + ((initiator.Actor != null) ? initiator.Actor.Name() : "=null")
                                                            + " Person=" + ((initiator.Person != null) ? initiator.Person.Name() : "=null")
                                                            + " Player=" + ((initiator.Player != null) ? initiator.Player.Name() : "=null")
                                                            + " Tool=" + ((initiator.Tool != null) ? initiator.Tool.Type.ToString() + "}" : "=null}"))
                                                          : "=null}")
                + " eventArgInt=" + eventArgInt.ToString());
        }
        catch (Exception e)
        {
            if (DEBUG_MESSAGES && CLog.IsInitialized) CLog.Write(e.ToString() + "\n" + e.Message.ToString());
        }
    }

    public void OnCarter(AiActor actor, int placeIndex)
    {
        try
        {
            if (DEBUG_MESSAGES && CLog.IsInitialized) CLog.Write("OnCarter actor=" + ((actor != null) ? actor.Name() : "=null") + " placeIndex=" + placeIndex.ToString());
        }
        catch (Exception e)
        {
            if (DEBUG_MESSAGES && CLog.IsInitialized) CLog.Write(e.ToString() + "\n" + e.Message.ToString());
        }
    }

    public void OnMissionLoaded(int missionNumber)
    {
        try
        {
            if (DEBUG_MESSAGES && CLog.IsInitialized) CLog.Write("OnMissionLoaded missionNumber=" + missionNumber.ToString());
        }
        catch (Exception e)
        {
            if (DEBUG_MESSAGES && CLog.IsInitialized) CLog.Write(e.ToString() + "\n" + e.Message.ToString());
        }
    }

    public void OnOrderMissionMenuSelected(Player player, int ID, int menuItemIndex)
    {
        try
        {
            if (DEBUG_MESSAGES && CLog.IsInitialized) CLog.Write("OnOrderMissionMenuSelected player=" + ((player != null) ? player.Name() : "=null") + " ID=" + ID.ToString() + " menuItemIndex=" + menuItemIndex.ToString());
        }
        catch (Exception e)
        {
            if (DEBUG_MESSAGES && CLog.IsInitialized) CLog.Write(e.ToString() + "\n" + e.Message.ToString());
        }
    }

    //// COMENTED DUE TO A LOT OF EVENTS GENERATED
    //public void OnPersonHealth(AiPerson person, AiDamageInitiator initiator, float deltaHealth)
    //{
    //    try
    //    {
    //        if (DEBUG_MESSAGES && CLog.IsInitialized) CLog.Write("OnPersonHealth person=" + ((person != null) ? person.Name() : "=null")
    //            + " initiator is {" + ((initiator != null) ? (" Actor=" + ((initiator.Actor != null) ? initiator.Actor.Name() : "=null")
    //                                                        + " Person=" + ((initiator.Person != null) ? initiator.Person.Name() : "=null")
    //                                                        + " Player=" + ((initiator.Player != null) ? initiator.Player.Name() : "=null")
    //                                                        + " Tool=" + ((initiator.Tool != null) ? initiator.Tool.Type.ToString() + "}" : "=null}"))
    //                                                      : "=null}")
    //            + " deltaHealth=" + deltaHealth.ToString());
    //    }
    //    catch (Exception e)
    //    {
    //        if (DEBUG_MESSAGES && CLog.IsInitialized) CLog.Write(e.ToString() + "\n" + e.Message.ToString());
    //    }
    //}

    public void OnPersonMoved(AiPerson person, AiActor fromCart, int fromPlaceIndex)
    {
        try
        {
            if (DEBUG_MESSAGES && CLog.IsInitialized) CLog.Write("OnPersonMoved person=" + ((person != null) ? person.Name() + ((person.Player() != null)?" (player=" + person.Player().Name()+")" : " (no player)") : "=null")
                + " fromCart=" + ((fromCart != null) ? fromCart.Name() : "=null")
                + " fromPlaceIndex=" + fromPlaceIndex.ToString()) ;
        }
        catch (Exception e)
        {
            if (DEBUG_MESSAGES && CLog.IsInitialized) CLog.Write(e.ToString() + "\n" + e.Message.ToString());
        }
    }

    public void OnPersonParachuteFailed(AiPerson person)
    {
        try
        {
            if (DEBUG_MESSAGES && CLog.IsInitialized) CLog.Write("OnPersonParachuteFailed person=" + ((person != null) ? person.Name() : "=null"));
        }
        catch (Exception e)
        {
            if (DEBUG_MESSAGES && CLog.IsInitialized) CLog.Write(e.ToString() + "\n" + e.Message.ToString());
        }
    }

    public void OnPersonParachuteLanded(AiPerson person)
    {
        try
        {
            if (DEBUG_MESSAGES && CLog.IsInitialized) CLog.Write("OnPersonParachuteLanded person=" + ((person != null) ? person.Name() : "=null"));
        }
        catch (Exception e)
        {
            if (DEBUG_MESSAGES && CLog.IsInitialized) CLog.Write(e.ToString() + "\n" + e.Message.ToString());
        }
    }
    public void OnPlayerArmy(Player player, int army)
    {
        try
        {
            if (DEBUG_MESSAGES && CLog.IsInitialized) CLog.Write("OnPlayerArmy player=" + ((player != null) ? player.Name() : "=null") + " army=" + army.ToString());
        }
        catch (Exception e)
        {
            if (DEBUG_MESSAGES && CLog.IsInitialized) CLog.Write(e.ToString() + "\n" + e.Message.ToString());
        }
    }

    public void OnPlayerConnected(Player player)
    {
        try
        {
            if (DEBUG_MESSAGES && CLog.IsInitialized) CLog.Write("OnPlayerConnected player=" + ((player != null) ? player.Name() : "=null"));
        }
        catch (Exception e)
        {
            if (DEBUG_MESSAGES && CLog.IsInitialized) CLog.Write(e.ToString() + "\n" + e.Message.ToString());
        }
    }

    public void OnStationaryKilled(int missionNumber, GroundStationary _stationary, AiDamageInitiator initiator, int eventArgInt)
    {
        try
        {
            if (DEBUG_MESSAGES && CLog.IsInitialized) CLog.Write("OnStationaryKilled missionNumber=" + missionNumber.ToString()
                + " _stationary=" + ((_stationary != null) ? _stationary.Name : "=null")
                + " initiator is {" + ((initiator != null) ? (" Actor=" + ((initiator.Actor != null) ? initiator.Actor.Name() : "=null")
                                                            + " Person=" + ((initiator.Person != null) ? initiator.Person.Name() : "=null")
                                                            + " Player=" + ((initiator.Player != null) ? initiator.Player.Name() : "=null")
                                                            + " Tool=" + ((initiator.Tool != null) ? initiator.Tool.Type.ToString() + "}" : "=null}"))
                                                          : "null}")
                + " eventArgInt=" + eventArgInt.ToString()
                );
        }
        catch (Exception e)
        {
            if (DEBUG_MESSAGES && CLog.IsInitialized) CLog.Write(e.ToString() + "\n" + e.Message.ToString());
        }
    }

    //// SINGLEPLAYER SPECIFIC
    //public void OnUserCreateUserLabel(GPUserLabel ul)
    //{
    //    try
    //    {
    //        if (DEBUG_MESSAGES && CLog.IsInitialized) CLog.Write("OnUserCreateUserLabel ul is {" + ((ul != null) ? (" Player=" + ((ul.Player != null) ? ul.Player.Name() : "=null")
    //                                                                                        + " Type=" + ul.type.ToString() + "}")
    //                                                                                       : "null}"));
    //    }
    //    catch (Exception e)
    //    {
    //        if (DEBUG_MESSAGES && CLog.IsInitialized) CLog.Write(e.ToString() + "\n" + e.Message.ToString());
    //    }
    //}

    // SINGLEPLAYER SPECIFIC
    //public void OnUserDeleteUserLabel(GPUserLabel ul)
    //{
    //    try
    //    {
    //        if (DEBUG_MESSAGES && CLog.IsInitialized) CLog.Write("OnUserCreateUserLabel ul is {" + ((ul != null) ? (" Player=" + ((ul.Player != null) ? ul.Player.Name() : "=null")
    //                                                                                        + " Type=" + ul.type.ToString() + "}")
    //                                                                                       : "null}"));
    //    }
    //    catch (Exception e)
    //    {
    //        if (DEBUG_MESSAGES && CLog.IsInitialized) CLog.Write(e.ToString() + "\n" + e.Message.ToString());
    //    }
    //}
}
