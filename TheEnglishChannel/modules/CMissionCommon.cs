using System.Text;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading;
using System.Diagnostics;
using System.IO;
using part;
using maddox.GP;
using maddox.game;
using maddox.game.world;
using maddox.game.play;//--- last line is -- using maddox.game.play;

public class CMissionCommon
{
    private const bool DEBUG_MESSAGES = true;

    private CKillDisusedPlanes killDisusedPlanes = null;
    private CNetworkComms networkComms = null;
    public AMission baseMission = null; 

    public CMissionCommon(AMission mission)
    {
        baseMission = mission;
    }

    public void OnBattleInit()
    {
        if (DEBUG_MESSAGES && CLog.IsInitialized) CLog.Write("OnBattleInit()");
    }

    public void OnBattleStarted()
    {
        if (DEBUG_MESSAGES && CLog.IsInitialized) CLog.Write("OnBattleStarted");
        killDisusedPlanes = new CKillDisusedPlanes(this);
        PrepareMissionMapInfo();
        PrepareAirports();
        if (CConfig.NETWORKING_ENABLE) networkComms = new CNetworkComms(this);
    }

    public void OnBattleStoped()
    {
        if (DEBUG_MESSAGES && CLog.IsInitialized) CLog.Write("OnBattleStoped");
        if (CConfig.NETWORKING_ENABLE) networkComms.OnBattleStoped();
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

    public const long TICKS_IN_SECOND = 10000000;
    public const long TICKS_IN_MILISECOND = 10000;
    private const long PERFORMANCE_LOG_INTERVAL = TICKS_IN_SECOND;
    private GameTick gameTickData = new GameTick { Max = 0, Min = 1000* PERFORMANCE_LOG_INTERVAL, Avg = 0, AvgCnt = 0, LogTick = 0 };
    private long gameTickLast = -1; // 1 tick is equal to 100 nano secods. 10000000 ticks in second
    private long gameTickLastLog = -1;
    private DateTime gameTickDt;
    public void OnTickGame()
    {
        // NO DEBUG LOG HERE!!! HAVE TO BE ULTRA FAST FUNCTION!!!
        //
        // But performance debug logging here when need. DO NOT FORGET TO DISABLE!
        if (CConfig.DEBUG_PERFORMANCE_LOG_ENABLE && CLog.IsInitialized)
        {
            gameTickDt = DateTime.Now;
            if (gameTickLast == -1)
            {
                gameTickLast = gameTickDt.Ticks;
                gameTickLastLog = gameTickDt.Ticks;
                return;
            }
            long tickInterval = gameTickDt.Ticks - gameTickLast;
            gameTickLast = gameTickDt.Ticks;
            if (0 == gameTickData.AvgCnt) gameTickData.LogTick = tickInterval;
            gameTickData.Avg += tickInterval;
            gameTickData.AvgCnt++;
            if (tickInterval > gameTickData.Max) gameTickData.Max = tickInterval;
            if (tickInterval < gameTickData.Min) gameTickData.Min = tickInterval;
            if (gameTickDt.Ticks - gameTickLastLog >= PERFORMANCE_LOG_INTERVAL)
            {
                gameTickLastLog = gameTickLast;
                CLog.Write("#PFMNC_GT;MIN;"+gameTickData.Min.ToString()+ ";MAX;"+gameTickData.Max.ToString()+";AVG;"+(gameTickData.Avg/gameTickData.AvgCnt).ToString()+ ";LOG;"+gameTickData.LogTick.ToString());
                gameTickData.Max = 0;
                gameTickData.Min = 1000 * PERFORMANCE_LOG_INTERVAL;
                gameTickData.Avg = 0;
                gameTickData.AvgCnt = 0;
                gameTickData.LogTick = 0;
            }
        }
        // Radar and other networking stuff poller
        if (CConfig.NETWORKING_ENABLE) networkComms.MissionScriptPoll();
    }

    private GameTick realTickData = new GameTick { Max = 0, Min = 1000 * PERFORMANCE_LOG_INTERVAL, Avg = 0, AvgCnt = 0, LogTick = 0 };
    private long realTickLast = -1;
    private long realTickLastLog = -1;
    private DateTime realTickDt;

    public void OnTickReal()
    {
        // NO DEBUG LOG HERE!!! HAVE TO BE ULTRA FAST FUNCTION!!!
        //
        // But performance debug logging here when need. DO NOT FORGET TO DISABLE!
        if (CConfig.DEBUG_PERFORMANCE_LOG_ENABLE && CLog.IsInitialized)
        {
            realTickDt = DateTime.Now;
            if (realTickLast == -1)
            {
                realTickLast = realTickDt.Ticks;
                realTickLastLog = realTickDt.Ticks;
                return;
            }
            long tickInterval = realTickDt.Ticks - realTickLast;
            realTickLast = realTickDt.Ticks;
            if (0 == realTickData.AvgCnt) realTickData.LogTick = tickInterval;
            realTickData.Avg += tickInterval;
            realTickData.AvgCnt++;
            if (tickInterval > realTickData.Max) realTickData.Max = tickInterval;
            if (tickInterval < realTickData.Min) realTickData.Min = tickInterval;
            if (realTickDt.Ticks - realTickLastLog >= PERFORMANCE_LOG_INTERVAL)
            {
                realTickLastLog = realTickLast;
                CLog.Write("#PFMNC_RT;MIN;" + realTickData.Min.ToString() + ";MAX;" + realTickData.Max.ToString() + ";AVG;" + (realTickData.Avg / realTickData.AvgCnt).ToString() + ";LOG;" + realTickData.LogTick.ToString());
                realTickData.Max = 0;
                realTickData.Min = 1000 * PERFORMANCE_LOG_INTERVAL;
                realTickData.Avg = 0;
                realTickData.AvgCnt = 0;
                realTickData.LogTick = 0;
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
                    AiAircraft aircraft = playersAssignedAircrafts[playerIdx].aircraft;
                    if (DEBUG_MESSAGES && CLog.IsInitialized) CLog.Write("Player " + player.Name() + " was assigned to aircraft! Resign now and destroy aircraft!");
                    DropPlayerFromAssignedAiAircraft(player);
                    baseMission.Timeout(0.1, () =>
                    {
                        killDisusedPlanes.DestroyPlane(aircraft);
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
                bool isPlayerJustSpawnedAtSpawnArea = AiAircraftUpdatePlayerSpawnedGroup(aircraft);

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
                            baseMission.Timeout(0.1, () =>
                            {
                                killDisusedPlanes.DestroyPlane(aircraft);
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
                        bool isNoPlayersInAircraft = killDisusedPlanes.IsNoPLayersInAircraft(aircraft);
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
                                    baseMission.Timeout((player.Ping() + 50) * 0.001, () =>
                                    {
                                        player.PlaceEnter(actor, 0);
                                    });
                                    Player[] recepients = { player };
                                    baseMission.GamePlay.gpHUDLogCenter(recepients, "Bailout, crash or land!");
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
                                        baseMission.Timeout(1, () =>
                                        {
                                            player.PlaceLeave(primIdx);
                                        });
                                        return;
                                    }
                                    if (secIdx >= 0)
                                    {
                                        // have to generate new on leave event and do stuff there
                                        baseMission.Timeout(1, () =>
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
                            if (DEBUG_MESSAGES && CLog.IsInitialized) CLog.Write("At least some one is still in aircraft and player " + player.Name() + " not a pilot. No need to call killDisusedPlanes.OnPlaceLeave(). Just free place");
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
                    bool isNoPlayersInAircraft = killDisusedPlanes.IsNoPLayersInAircraft(aircraft);
                    if ((placeIndex == 0) && !isNoPlayersInAircraft)
                    {
                        int primIdx = player.PlacePrimary();
                        int secIdx = player.PlaceSecondary();
                        if (DEBUG_MESSAGES && CLog.IsInitialized) CLog.Write("Hey! Player still in aircraft! PlacePrimary=" + primIdx.ToString() + " PlaceSecondary=" + secIdx.ToString());
                        if (primIdx >= 0)
                        {
                            // have to generate new on leave event and do stuff there
                            baseMission.Timeout(1, () =>
                            {
                                player.PlaceLeave(primIdx);
                            });
                            return;
                        }
                        if (secIdx >= 0)
                        {
                            // have to generate new on leave event and do stuff there
                            baseMission.Timeout(1, () =>
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
            if (DEBUG_MESSAGES && CLog.IsInitialized) CLog.Write("killDisusedPlanes.OnPlaceLeave()");
            killDisusedPlanes.OnPlaceLeave(player, actor, placeIndex);
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
    // Simple self explanatory funtions
    //
    public AiAirGroup FindAiAirgroupByName(string aiAirgroupName)
    {
        try
        {
            if (DEBUG_MESSAGES && CLog.IsInitialized) CLog.Write("Started search of airgroup with name: " + aiAirgroupName);
            for (int i = 0; i < baseMission.GamePlay.gpArmies().Length; i++)
            {
                AiAirGroup[] grs = baseMission.GamePlay.gpAirGroups(baseMission.GamePlay.gpArmies()[i]);
                if (grs != null)
                {
                    for (int j = 0; j < grs.Length; j++)
                    {
                        try
                        {
                            //if (DEBUG_MESSAGES && CLog.IsInitialized) CLog.Write(grs[j].Name());
                            if (grs[j].Name().Contains(aiAirgroupName))
                            {
                                if (DEBUG_MESSAGES && CLog.IsInitialized) CLog.Write("Airgroup " + aiAirgroupName + " found!");
                                return grs[j];
                            }
                        }
                        catch (Exception ex)
                        {
                            if (DEBUG_MESSAGES && CLog.IsInitialized) CLog.Write("Group idx=" + j.ToString() + " in army=" + baseMission.GamePlay.gpArmies()[i].ToString() + " skipped due to exception.\n" + ex.ToString());
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            if (DEBUG_MESSAGES && CLog.IsInitialized) CLog.Write(ex.ToString());
        }
        if (DEBUG_MESSAGES && CLog.IsInitialized) CLog.Write("Airgroup " + aiAirgroupName + " not found.");
        return null;
    }

    public void StartEnginesForAiAirgroupByName(string aiAirgroupName)
    {
        AiAirGroup airgroup = FindAiAirgroupByName(aiAirgroupName);
        if (airgroup != null)
        {
            try
            {
                airgroup.Idle = false;
                if (DEBUG_MESSAGES && CLog.IsInitialized) CLog.Write("Airgroup \"" + aiAirgroupName + "\" - engines started!");
            }
            catch (Exception ex)
            {
                if (DEBUG_MESSAGES && CLog.IsInitialized) CLog.Write("Airgroup \"" + aiAirgroupName + "\" - failed to start engines!!! \n" + ex.ToString());
            }
        }
    }


    public class CBattleArea
    {
        public int x, y, w, h, sector_size; // all in meters
        
        public void Reset()
        {
            x = y = w = h = sector_size = 0;
        }
    }

    public class CArmy
    {
        public int id;
        public string name;
        public string countries;
    }

    public class CMissionMapInfo
    {
        public string Name = "";
        public CBattleArea BattleArea = new CBattleArea();
        public CArmy[] Armies = null;
    }

    public CMissionMapInfo missionMapInfo = new CMissionMapInfo();
    private void PrepareMissionMapInfo()
    {
        string msMyFolder = Path.GetDirectoryName(baseMission.sPathMyself);
        if (DEBUG_MESSAGES && CLog.IsInitialized) { CLog.Write("Mission file path: " + msMyFolder); }
        if (DEBUG_MESSAGES && CLog.IsInitialized) { CLog.Write("Mission file name: " + baseMission.MissionFileName); }
        ISectionFile sf = baseMission.GamePlay.gpLoadSectionFile(msMyFolder + "\\" + baseMission.MissionFileName);
        
        // Get map name
        missionMapInfo.Name = sf.get("MAIN", "MAP");
        if (DEBUG_MESSAGES && CLog.IsInitialized) { CLog.Write("Map name: " + missionMapInfo.Name); }
        
        // get battle area information (map grids)
        string battleArea = sf.get("MAIN", "BattleArea");
        if (DEBUG_MESSAGES && CLog.IsInitialized) { CLog.Write("Battle area: " + battleArea); }
        string[] nums = battleArea.Split(new char[] {' '});
        bool IsBattleAreaParsed = true;
        if (nums.Length >= 5) 
        {
            IsBattleAreaParsed = Int32.TryParse(nums[0], out missionMapInfo.BattleArea.x);
            if (IsBattleAreaParsed) IsBattleAreaParsed = Int32.TryParse(nums[1], out missionMapInfo.BattleArea.y);
            if (IsBattleAreaParsed) IsBattleAreaParsed = Int32.TryParse(nums[2], out missionMapInfo.BattleArea.w);
            if (IsBattleAreaParsed) IsBattleAreaParsed = Int32.TryParse(nums[3], out missionMapInfo.BattleArea.h);
            if (IsBattleAreaParsed) IsBattleAreaParsed = Int32.TryParse(nums[4], out missionMapInfo.BattleArea.sector_size);
        }
        if (!IsBattleAreaParsed)
        {
            missionMapInfo.BattleArea.Reset();
        }
        if (DEBUG_MESSAGES && CLog.IsInitialized) 
        { 
            CLog.Write("Battle area PARSED: x=" + missionMapInfo.BattleArea.x.ToString() 
            + ", y=" + missionMapInfo.BattleArea.y.ToString()
            + ", w=" + missionMapInfo.BattleArea.w.ToString()
            + ", h=" + missionMapInfo.BattleArea.h.ToString()
            + ", ssz=" + missionMapInfo.BattleArea.sector_size.ToString()); 
        }
        
        // Get armies info
        int[] armies = baseMission.GamePlay.gpArmies();
        if (armies.Length > 0)
        {
            missionMapInfo.Armies = new CArmy[armies.Length];
        }
        for (int i = 0; i < armies.Length; i++)
        {
            missionMapInfo.Armies[i] = new CArmy();
            missionMapInfo.Armies[i].id = armies[i];
            missionMapInfo.Armies[i].name = baseMission.GamePlay.gpArmyName(armies[i]);
            missionMapInfo.Armies[i].countries = "";
            string search_line = "Army " + armies[i];
            string countries = SearchInSectionFile(ref sf,"main",search_line);
            if (countries != null)
            {
                try
                {
                    countries = countries.Substring(search_line.Length + 1, countries.Length - search_line.Length - 1);
                    missionMapInfo.Armies[i].countries = countries;
                }
                catch (Exception e)
                {
                    if (DEBUG_MESSAGES && CLog.IsInitialized) CLog.Write(e.ToString());
                }
            }
            if (DEBUG_MESSAGES && CLog.IsInitialized) CLog.Write("armies[" + i.ToString() + "]=" + missionMapInfo.Armies[i].id.ToString()
                + " name is \"" + missionMapInfo.Armies[i].name + "\""
                + " countries are \"" + missionMapInfo.Armies[i].countries +  "\"");
        }
     }

    private string SearchInSectionFile(ref ISectionFile sf, string section,string search_line)
    {
        int lines = sf.lines(section);
        for (int i = 0; i < lines; i++)
        {
            string key;
            string value;
            sf.get(section,i, out key, out value);
            if ((key != null) && (value != null))
            {
                value = key + " " + value;
                if (value.Contains(search_line))
                {
                    return value;
                }
            }
        }
        return null;
    }

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

    private CNeutralAirportsByArmies[] neutralAirportsByArmies = null;

    public void PrepareAirports()
    {
        if ((baseMission.GamePlay.gpAirports().Length == 0) || (baseMission.GamePlay.gpArmies().Length == 0))
        {
            return;
        }

        // Get list of all airfields on the map
        AiAirport[] missionAirportsSortedByArmy = new AiAirport[baseMission.GamePlay.gpAirports().Length];
        baseMission.GamePlay.gpAirports().CopyTo(missionAirportsSortedByArmy, 0);

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
        if (DEBUG_MESSAGES && CLog.IsInitialized) CLog.Write("---Indexes for different armies airfields for fast searching to be done.");
        for (int i = 0; i < missionAirportIndexesByArmy.Count; i++)
        {
            if (DEBUG_MESSAGES && CLog.IsInitialized) CLog.Write("Index=" + i.ToString()
                + " Army=" + missionAirportIndexesByArmy[i].Army.ToString()
                + " First=" + missionAirportIndexesByArmy[i].FirstIdx.ToString()
                + " Last=" + missionAirportIndexesByArmy[i].LastIdx.ToString());
        }

        //
        // Here let's do same thing for SpawnAreas.
        //
        // Get list of all SpawnAreas on the map

        AiBirthPlace[] missionBirthplacesSortedByArmy = new AiBirthPlace[baseMission.GamePlay.gpBirthPlaces().Length];
        baseMission.GamePlay.gpBirthPlaces().CopyTo(missionBirthplacesSortedByArmy, 0);

        AiBirthPlace aiBirthPlace;
        // Sort by army
        for (int i = 0; i < missionBirthplacesSortedByArmy.Length - 1; i++)
        {
            for (int j = i + 1; j < missionBirthplacesSortedByArmy.Length; j++)
            {
                if (missionBirthplacesSortedByArmy[i].Army() > missionBirthplacesSortedByArmy[j].Army())
                {
                    aiBirthPlace = missionBirthplacesSortedByArmy[i];
                    missionBirthplacesSortedByArmy[i] = missionBirthplacesSortedByArmy[j];
                    missionBirthplacesSortedByArmy[j] = aiBirthPlace;
                }
            }
        }
        //
        // debug printing
        //
        if (DEBUG_MESSAGES && CLog.IsInitialized) CLog.Write("---Sorted list of birthplaces:");
        for (int i = 0; i < missionBirthplacesSortedByArmy.Length; i++)
        {
            if (DEBUG_MESSAGES && CLog.IsInitialized) CLog.Write(missionBirthplacesSortedByArmy[i].Name() + " army=" + missionBirthplacesSortedByArmy[i].Army().ToString());
        }
        // It have to be at least one BirthPlace in this mission
        List<CAirportIndexes> missionBirthPlacesIndexesByArmy = new List<CAirportIndexes>();
        missionBirthPlacesIndexesByArmy.Add(new CAirportIndexes());
        missionBirthPlacesIndexesByArmy[0].Army = missionBirthplacesSortedByArmy[0].Army();
        missionBirthPlacesIndexesByArmy[0].FirstIdx = 0;
        missionBirthPlacesIndexesByArmy[0].LastIdx = 0;
        idx = 0;
        // Lets prepare indexes for different armies BirthPlaces for fast searching.
        for (int i = 1; i < missionBirthplacesSortedByArmy.Length; i++)
        {
            if (missionBirthplacesSortedByArmy[i].Army() == missionBirthPlacesIndexesByArmy[idx].Army)
            {
                missionBirthPlacesIndexesByArmy[idx].LastIdx = i;
            }
            else
            {
                idx++;
                missionBirthPlacesIndexesByArmy.Add(new CAirportIndexes());
                missionBirthPlacesIndexesByArmy[idx].Army = missionBirthplacesSortedByArmy[i].Army();
                missionBirthPlacesIndexesByArmy[idx].FirstIdx = i;
                missionBirthPlacesIndexesByArmy[idx].LastIdx = i;
            }
        }
        //
        // debug printing
        //
        if (DEBUG_MESSAGES && CLog.IsInitialized) CLog.Write("---Indexes for different armies BirthPlaces for fast searching to be done.");
        for (int i = 0; i < missionBirthPlacesIndexesByArmy.Count; i++)
        {
            if (DEBUG_MESSAGES && CLog.IsInitialized) CLog.Write("Index=" + i.ToString()
                + " Army=" + missionBirthPlacesIndexesByArmy[i].Army.ToString()
                + " First=" + missionBirthPlacesIndexesByArmy[i].FirstIdx.ToString()
                + " Last=" + missionBirthPlacesIndexesByArmy[i].LastIdx.ToString());
        }

        //
        // Create array of neautral airports lists by armies
        //
        int missionArmiesCount = baseMission.GamePlay.gpArmies().Length + 1;// +1 for Neutral Army with index 0.
        neutralAirportsByArmies = new CNeutralAirportsByArmies[missionArmiesCount];
        // fill army values
        neutralAirportsByArmies[0] = new CNeutralAirportsByArmies();
        neutralAirportsByArmies[0].Army = 0; // this one is always neutral armie with index 0.
        for (int armyIdx = 1; armyIdx < missionArmiesCount; armyIdx++)
        {
            neutralAirportsByArmies[armyIdx] = new CNeutralAirportsByArmies();
            neutralAirportsByArmies[armyIdx].Army = baseMission.GamePlay.gpArmies()[armyIdx - 1];// remember we added +1 to gpArmies().Length for missionArmiesCount
        }
        //
        // debug printing
        //
        if (DEBUG_MESSAGES && CLog.IsInitialized) CLog.Write("---Army values filled.");

        // fill airfields neutralAirportsByArmies from list of all mission neutral airports
        for (int missionNeutralAirportIdx = 0; missionNeutralAirportIdx <= missionAirportIndexesByArmy[0].LastIdx; missionNeutralAirportIdx++)
        {
            AiAirport missionNeutralAirport = missionAirportsSortedByArmy[missionNeutralAirportIdx];
            Point3d missionNeutralAirportPos = missionNeutralAirport.Pos();
            bool nonNeutralAirportFound = false;
            // search in non neutral airports indexes lists
            for (int armyIdx = 1; armyIdx < missionAirportIndexesByArmy.Count; armyIdx++)
            {
                for (int nonNeutralAirportIdx = missionAirportIndexesByArmy[armyIdx].FirstIdx; nonNeutralAirportIdx <= missionAirportIndexesByArmy[armyIdx].LastIdx; nonNeutralAirportIdx++)
                {
                    AiAirport nonNeutralAirport = missionAirportsSortedByArmy[nonNeutralAirportIdx];
                    Point3d nonNeutralAirportPos = nonNeutralAirport.Pos();
                    if (missionNeutralAirportPos.distanceLinf(ref nonNeutralAirportPos) < missionNeutralAirport.CoverageR())
                    {
                        int nabaIdx;
                        for (nabaIdx = 1; nabaIdx < neutralAirportsByArmies.Length - 1; nabaIdx++)
                        {
                            if (missionAirportIndexesByArmy[armyIdx].Army == neutralAirportsByArmies[nabaIdx].Army)
                                break;
                        }
                        neutralAirportsByArmies[nabaIdx].aiAirports.Add(missionNeutralAirport);
                        nonNeutralAirportFound = true;
                        break;
                    }
                }
                if (nonNeutralAirportFound)
                    break;
            }
            // now search in birth places indexes lists
            if (!nonNeutralAirportFound)
            {
                // remember, birth places can't be neutral
                for (int armyIdx = 0; armyIdx < missionBirthPlacesIndexesByArmy.Count; armyIdx++)
                {
                    for (int nonNeutralBirthPlaceIdx = missionBirthPlacesIndexesByArmy[armyIdx].FirstIdx; nonNeutralBirthPlaceIdx <= missionBirthPlacesIndexesByArmy[armyIdx].LastIdx; nonNeutralBirthPlaceIdx++)
                    {
                        AiBirthPlace nonNeutralBirthPlace = missionBirthplacesSortedByArmy[nonNeutralBirthPlaceIdx];
                        Point3d nonNeutralBirthPlacePos = nonNeutralBirthPlace.Pos();
                        if (missionNeutralAirportPos.distanceLinf(ref nonNeutralBirthPlacePos) < missionNeutralAirport.CoverageR())
                        {
                            int nabaIdx;
                            for (nabaIdx = 1; nabaIdx < neutralAirportsByArmies.Length - 1; nabaIdx++)
                            {
                                if (missionBirthPlacesIndexesByArmy[armyIdx].Army == neutralAirportsByArmies[nabaIdx].Army)
                                    break;
                            }
                            neutralAirportsByArmies[nabaIdx].aiAirports.Add(missionNeutralAirport);
                            nonNeutralAirportFound = true;
                            break;
                        }
                    }
                    if (nonNeutralAirportFound)
                        break;
                }
            }
            // this airport is neutral
            if (!nonNeutralAirportFound)
            {
                neutralAirportsByArmies[0].aiAirports.Add(missionNeutralAirport);
            }
        }
        //
        // debug printing
        //
        if (DEBUG_MESSAGES && CLog.IsInitialized) CLog.Write("Neutral airfields by army.");

        for (int armyIdx = 0; armyIdx < neutralAirportsByArmies.Length; armyIdx++)
        {
            if (DEBUG_MESSAGES && CLog.IsInitialized) CLog.Write("---List of airports for Army=" + neutralAirportsByArmies[armyIdx].Army.ToString());
            for (int airportIdx = 0; airportIdx < neutralAirportsByArmies[armyIdx].aiAirports.Count; airportIdx++)
            {
                if (DEBUG_MESSAGES && CLog.IsInitialized) CLog.Write(neutralAirportsByArmies[armyIdx].aiAirports[airportIdx].Name());
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

            for (int armyIdx = 0; armyIdx < neutralAirportsByArmies.Length; armyIdx++)
            {
                for (int airportIdx = 0; airportIdx < neutralAirportsByArmies[armyIdx].aiAirports.Count; airportIdx++)
                {
                    AiAirport airportFromList = neutralAirportsByArmies[armyIdx].aiAirports[airportIdx];
                    Point3d airportFromListPos = airportFromList.Pos();
                    double distanceToAirportFromList = airportFromListPos.distanceLinf(ref aircraftPos);
                    if (distanceToAirportFromList < (airportFromList.CoverageR() + 1000.0))
                    {
                        int airportFromListArmy = neutralAirportsByArmies[armyIdx].Army;
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

            if (baseMission.GamePlay.gpLandType(aircraftPos.x, aircraftPos.y) == LandTypes.WATER)
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

    public bool AiAircraftUpdatePlayerSpawnedGroup(AiAircraft aircraft)
    {
        if (IsAircraftPlayerSpawnedAtSpawnArea(aircraft))
        {
            if (DEBUG_MESSAGES && CLog.IsInitialized) CLog.Write("Looks like player spawned in spawn area! Reset default waypoints to avoid useless GC orders to land right after takeoff...");
            // APPEARED TO BE USELESS!!! GC is silent on Dedicated server!!!
            // Reset default waypoints to avoid useless GC orders to land right after takeoff...
            //AiWayPoint[] aiWayPoints = aircraft.Group().GetWay();
            //aiWayPoints[1].P.y = aiWayPoints[1].P.y + 5000;
            //aircraft.Group().SetWay(aiWayPoints);

            // also remove all wingmans spawned with him
            AiActor[] actors = aircraft.Group().GetItems();
            CLog.Write("Destroy all AI wingmen in group.");
            for (int i = 0; i < actors.Length; i++)
            {
                if (actors[i] is AiAircraft)
                {
                    AiAircraft aiAircraft = (AiAircraft)actors[i];
                    if (aiAircraft.Player(0) == null)
                    {
                        if (DEBUG_MESSAGES && CLog.IsInitialized) CLog.Write("AI aircraft actor[" + i.ToString() + "].Name=" + actors[i].Name() + " will be destroyed!");
                        aiAircraft.Destroy();
                    }
                }
                else
                {
                    if (DEBUG_MESSAGES && CLog.IsInitialized) CLog.Write("Not aircraft actor[" + i.ToString() + "].Name=" + actors[i].Name());
                }
            }
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

    private List<PlayerAssignedToAiAircraft> playersAssignedAircrafts = new List<PlayerAssignedToAiAircraft>();

    public void AssignPlayerToAiAircraft(Player player, AiAircraft aircraft)
    {
        if (player == null)
            return;
        int playerIdx = GetPlayerAssignedAiAircraftIdx(player);
        if (playerIdx < 0)
        {
            //add new
            playersAssignedAircrafts.Add(new PlayerAssignedToAiAircraft(player, aircraft));
        }
        else
        {
            // replce old by new
            playersAssignedAircrafts[playerIdx].aircraft = aircraft;
        }
    }

    public bool IsPlayerAssignedToAircraft(Player player, AiAircraft aircraft)
    {
        int playerIdx = GetPlayerAssignedAiAircraftIdx(player);
        if (playerIdx >= 0)
        {
            if (playersAssignedAircrafts[playerIdx].aircraft.Equals(aircraft))
            {
                return true;
            }
        }
        return false;
    }

    public int GetPlayerAssignedAiAircraftIdx(Player player)
    {
        for (int i = 0; i < playersAssignedAircrafts.Count; i++)
        {
            if (playersAssignedAircrafts[i].player.Equals(player))
            {
                return i;
            }
        }
        return -1;
    }


    public void DropPlayerFromAssignedAiAircraft(Player player)
    {
        for (int i = 0; i < playersAssignedAircrafts.Count; i++)
        {
            if (playersAssignedAircrafts[i].player.Equals(player))
            {
                playersAssignedAircrafts.RemoveAt(i);
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
    public List<DefueledAircraft> defueledAcircrafts = new List<DefueledAircraft>();

    public bool IsDefueledAircraft(AiAircraft aiAircraft, bool refuelAndDrop)
    {
        if (aiAircraft != null)
        {
            for (int i = 0; i < defueledAcircrafts.Count; i++)
            {
                if (defueledAcircrafts[i].aircraft.Equals(aiAircraft))
                {
                    if (refuelAndDrop)
                    {
                        defueledAcircrafts[i].aircraft.RefuelPlane(defueledAcircrafts[i].fuelPctBeforeDefuel);
                        defueledAcircrafts.RemoveAt(i);
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
            //// COMENTED DUE TO A LOT OF EVENTS GENERATED... TOO MUCH FOR LOGGING
            //if (DEBUG_MESSAGES && CLog.IsInitialized) CLog.Write("OnBombExplosion title=" + title + " mass=" + mass.ToString()
            //    + " pos.{X,Y,Z}={" + pos.x.ToString() + ", " + pos.y.ToString() + ", " + pos.z.ToString() + "}"
            //    + " initiator is {" + ((initiator != null) ? (" Actor=" + ((initiator.Actor != null) ? initiator.Actor.Name() : "=null")
            //                                                + " Person=" + ((initiator.Person != null) ? initiator.Person.Name() : "=null")
            //                                                + " Player=" + ((initiator.Player != null) ? initiator.Player.Name() : "=null")
            //                                                + " Tool=" + ((initiator.Tool != null) ? initiator.Tool.Type.ToString() + "}" : "=null}"))
            //                                              : "=null}")
            //    + " eventArgInt=" + eventArgInt.ToString());
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
            //// COMENTED DUE TO A LOT OF EVENTS GENERATED... TOO MUCH FOR LOGGING
            //if (DEBUG_MESSAGES && CLog.IsInitialized) CLog.Write("OnBuildingKilled title=" + title
            //    + " pos.{X,Y,Z}={" + pos.x.ToString() + ", " + pos.y.ToString() + ", " + pos.z.ToString() + "}"
            //    + " initiator is {" + ((initiator != null) ? (" Actor=" + ((initiator.Actor != null) ? initiator.Actor.Name() : "=null")
            //                                                + " Person=" + ((initiator.Person != null) ? initiator.Person.Name() : "=null")
            //                                                + " Player=" + ((initiator.Player != null) ? initiator.Player.Name() : "=null")
            //                                                + " Tool=" + ((initiator.Tool != null) ? initiator.Tool.Type.ToString() + "}" : "=null}"))
            //                                              : "=null}")
            //    + " eventArgInt=" + eventArgInt.ToString());
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
