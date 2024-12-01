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
using maddox.GP;
using System.Security.AccessControl; //-------------------


public class CNetworkComms
{
    private const bool DEBUG_MESSAGES = true;
    private Mission BaseMission = null;
    private CMissionCommon MissionCommon = null;
    // Example of serverState. All coordinates, distances and heights in meters.
    //
    // {
    //  "map_mame":"Land$English_Channel_1940",
    //  "mission_time":"12:20:30",
    //  "battle_area":{"x":8000, "y":8000, "w":320000, "h":300000, "sector_size":10000},
    //  "armies":[
    //    {"id":1, "name":"Red", "countries":"gb fr us pl rz"},
    //    {"id":2, "name":"Blue", "countries":"de it"},
    //  ],
    //  "aircrafts":[
    //   {"id":"1:BoB_LW_JG26_I.200", "army":2, "x":287144, "y":271037, "z":2871,}, 
    //   {"id":"1:BoB_LW_JG26_I.201", "army":2, "x":287044, "y":271130, "z":2861,}, 
    //   {"id":"2:BoB_RAF_F_249Sqn_Late.000", "army":1, "x":257144, "y":291037, "z":2981,}, 
    //   {"id":"2:BoB_RAF_F_249Sqn_Late.001", "army":1, "x":257044, "y":291030, "z":2991,}, 
    //  ],
    // }
    private readonly object serverStateLock = new object();
    private StringBuilder serverState = new StringBuilder();
    public CNetworkComms(Mission mission, CMissionCommon mission_common)
    {
        BaseMission = mission;
        MissionCommon = mission_common;
        serverState.Capacity = 1024 * 1024; // I doubt real data will ever be such big

        // run to fill "serverState" with data
        MissionScriptPoll();
    }
    public void OnBattleStoped() { 
        // Do something like stop comms and close sockets
    }
    private long RadarPollTickLast = 0;
    public void MissionScriptPoll() {
        //
        // Radar data poller
        //
        if (DateTime.Now.Ticks - RadarPollTickLast >= CConfig.RADAR_DATA_UPDATE_INTERVAL_MS * CMissionCommon.TICKS_IN_MILISECOND)
        {
            RadarPollTickLast = DateTime.Now.Ticks;
            try
            {
                if (DEBUG_MESSAGES && CLog.IsInitialized) CLog.Write("RadarDataStart\n");
                lock (serverStateLock)
                {
                    serverState.Clear();
                    // JSON first open bracket
                    serverState.Append("{\n");

                    ///////////////////////
                    // Map name
                    serverState.Append(MakeJsonStringEntry(CJsonIds.MAP_NAME, MissionCommon.missionMapInfo.Name));

                    ///////////////////////
                    // Map time
                    double dMisTime = BaseMission.GamePlay.gpTimeofDay(); // time of day in hours as double value
                    int mt_hours = (int)dMisTime;
                    dMisTime = dMisTime - mt_hours;
                    int mt_minutes = (int)(dMisTime * 60);
                    int mt_seconds = (int)(dMisTime * 3600) - mt_minutes * 60;
                    string missionTime = mt_hours.ToString() + ":" + mt_minutes.ToString().PadLeft(2, '0') + ":" + mt_seconds.ToString().PadLeft(2, '0');
                    serverState.Append(MakeJsonStringEntry(CJsonIds.MISSION_TIME, missionTime));

                    ///////////////////////
                    // Map battle area
                    // open class CJsonIds.BATTLE_AREA
                    serverState.Append("\"" + CJsonIds.BATTLE_AREA + "\":{"); 
                    CMissionCommon.CBattleArea battleArea = BaseMission.missionCommon.missionMapInfo.BattleArea;
                    serverState.Append(MakeJsonIntEntry(CJsonIds.BATTLE_AREA_X, battleArea.x, NO_LINE_BREAK));
                    serverState.Append(MakeJsonIntEntry(CJsonIds.BATTLE_AREA_Y, battleArea.y, NO_LINE_BREAK));
                    serverState.Append(MakeJsonIntEntry(CJsonIds.BATTLE_AREA_W, battleArea.w, NO_LINE_BREAK));
                    serverState.Append(MakeJsonIntEntry(CJsonIds.BATTLE_AREA_H, battleArea.h, NO_LINE_BREAK));
                    serverState.Append(MakeJsonIntEntry(CJsonIds.BATTLE_AREA_SECTSZ, battleArea.sector_size, NO_LINE_BREAK));
                    // close class CJsonIds.BATTLE_AREA
                    serverState.Append("},\n");

                    ///////////////////////
                    // Mission armies
                    // open array CJsonIds.ARMIES
                    serverState.Append("\"" + CJsonIds.ARMIES + "\":[\n");
                    CMissionCommon.CArmy[] armies = BaseMission.missionCommon.missionMapInfo.Armies;
                    for (int i = 0; i < armies.Length; i++)
                    {
                        serverState.Append("{");
                        serverState.Append(MakeJsonIntEntry(CJsonIds.ARMIES_ID, armies[i].id, NO_LINE_BREAK));
                        serverState.Append(MakeJsonStringEntry(CJsonIds.ARMIES_NAME, armies[i].name, NO_LINE_BREAK));
                        serverState.Append(MakeJsonStringEntry(CJsonIds.ARMIES_COUNTRIES, armies[i].countries, NO_LINE_BREAK));
                        serverState.Append("},\n");
                    }
                    // close array CJsonIds.ARMIES
                    serverState.Append("],\n");
                    
                    ///////////////////////
                    //Get aircrafts
                    // open array CJsonIds.AC_AIRCRAFTS
                    serverState.Append("\"" + CJsonIds.AC_AIRCRAFTS + "\":[\n");
                    for (int i = 0; i < armies.Length; i++)
                    {
                        int army_id = armies[i].id;
                        AiAirGroup[] aiAirGroups = BaseMission.GamePlay.gpAirGroups(army_id);
                        if (aiAirGroups != null)
                        {
                            for (int j = 0; j < aiAirGroups.Length; j++)
                            {
                                if (aiAirGroups[j] != null)
                                {
                                    //string agName = aiAirGroups[j].Name();
                                    //if (agName == null) agName = "NULL";
                                    //if (DEBUG_MESSAGES && CLog.IsInitialized) CLog.Write("aiAirGroups["+j.ToString()+"].Name() =" + agName);
                                    AiActor[] actors = aiAirGroups[j].GetItems();
                                    if (actors != null)
                                    {
                                        for (int k = 0; k < actors.Length; k++)
                                        {
                                            if (actors[k] != null)
                                            {
                                                string actorName = actors[k].Name();
                                                Point3d pos = actors[k].Pos();
                                                serverState.Append("{");
                                                serverState.Append(MakeJsonStringEntry(CJsonIds.AC_ID, actorName, NO_LINE_BREAK));
                                                serverState.Append(MakeJsonIntEntry(CJsonIds.AC_ARMY, army_id, NO_LINE_BREAK));
                                                serverState.Append(MakeJsonIntEntry(CJsonIds.AC_X, (int)pos.x, NO_LINE_BREAK));
                                                serverState.Append(MakeJsonIntEntry(CJsonIds.AC_Y, (int)pos.y, NO_LINE_BREAK));
                                                serverState.Append(MakeJsonIntEntry(CJsonIds.AC_Z, (int)pos.z, NO_LINE_BREAK));
                                                serverState.Append("},\n");

                                                //if (DEBUG_MESSAGES && CLog.IsInitialized) CLog.Write("actors["+k.ToString()+"].Name() =" + actorName);
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                    // close array CJsonIds.ARMIES
                    serverState.Append("],\n");


                    // JSON last close bracket
                    serverState.Append("}");
                    if (DEBUG_MESSAGES && CLog.IsInitialized) CLog.Write("RadarDataEnd\n<JSONin>" + serverState.ToString() + "<JSONout>\n\n");
                }
            }
            catch (Exception e)
            {
                if (DEBUG_MESSAGES && CLog.IsInitialized) CLog.Write(e.ToString() + "\n" + e.Message.ToString());
            }
        }
    }

    private const bool NO_LINE_BREAK = false;
    private string MakeJsonStringEntry(string id, string val, bool brkline = true)
    {
        return "\"" + id + "\":\"" + val + "\"," + (brkline ? "\n" : " ");
    }
    private string MakeJsonIntEntry(string id, int val, bool brkline = true)
    {
        return "\"" + id + "\":" + val.ToString() + "," + (brkline ? "\n" : " ");
    }
    private class CJsonIds
    {
        public const string MAP_NAME = "map_mame";
        public const string MISSION_TIME = "mission_time";
        public const string BATTLE_AREA = "battle_area";
        public const string BATTLE_AREA_X = "x";
        public const string BATTLE_AREA_Y = "y";
        public const string BATTLE_AREA_W = "w";
        public const string BATTLE_AREA_H = "h";
        public const string BATTLE_AREA_SECTSZ = "sector_size";
        public const string ARMIES = "armies";
        public const string ARMIES_ID = "id";
        public const string ARMIES_NAME = "name";
        public const string ARMIES_COUNTRIES = "countries";
        public const string AC_AIRCRAFTS = "aircrafts";
        public const string AC_ID = "id";
        public const string AC_ARMY = "army";
        //public const string AC_MODEL = "model"; <-- useless data for radar
        //public const string AC_PLAYER0 = "player"; <-- useless data for radar
        public const string AC_X = "x";
        public const string AC_Y = "y";
        public const string AC_Z = "z";
    }
}

