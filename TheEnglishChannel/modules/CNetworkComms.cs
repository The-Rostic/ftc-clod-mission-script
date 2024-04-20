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
    //  "battle_area":{"x":8000, "y":8000, "w":"320000", h:"300000", "sector_size":10000},
    //  "aircrafts":{
    //   {"id":"1:BoB_LW_JG26_I.200", "army":2, "x":287144, "y":271037, "h":2871,}, 
    //   {"id":"1:BoB_LW_JG26_I.201", "army":2, "x":287044, "y":271130, "h":2861,}, 
    //   {"id":"2:BoB_RAF_F_249Sqn_Late.000", "army":1, "x":257144, "y":291037, "h":2981,}, 
    //   {"id":"2:BoB_RAF_F_249Sqn_Late.001", "army":1, "x":257044, "y":291030, "h":2991,}, 
    //  },
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
                    // Map Name
                    serverState.Append(MakeJsonStringEntry(CJsonIds.MAP_NAME, MissionCommon.missionMapInfo.Name));
                    //MapTime
                    double dMisTime = BaseMission.GamePlay.gpTimeofDay(); // time of day in hours as double value
                    int mt_hours = (int)dMisTime;
                    dMisTime = dMisTime - mt_hours;
                    int mt_minutes = (int)(dMisTime * 60);
                    int mt_seconds = (int)(dMisTime * 3600) - mt_minutes * 60;
                    string missionTime = mt_hours.ToString() + ":" + mt_minutes.ToString().PadLeft(2, '0') + ":" + mt_seconds.ToString().PadLeft(2, '0');
                    serverState.Append(MakeJsonStringEntry(CJsonIds.MISSION_TIME, missionTime));
                    // JSON last close bracket
                    serverState.Append("}");
                }
                if (DEBUG_MESSAGES && CLog.IsInitialized) CLog.Write("RadarDataEnd\n" + serverState.ToString() + "\n\n");
            }
            catch (Exception e)
            {
                if (DEBUG_MESSAGES && CLog.IsInitialized) CLog.Write(e.ToString() + "\n" + e.Message.ToString());
            }
        }
    }

    private string MakeJsonStringEntry(string id, string val)
    {
        return "\"" + id + "\":\"" + val + "\",\n";
    }
    private string MakeJsonIntEntry(string id, int val)
    {
        return "\"" + id + "\":" + val.ToString() + ",\n";
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
        public const string AC_AIRCRAFTS = "aircrafts";
        public const string AC_ID = "id";
        public const string AC_ARMY = "army";
        //public const string AC_MODEL = "model"; <-- useless data for radar
        //public const string AC_PLAYER0 = "player"; <-- useless data for radar
        public const string AC_X = "x";
        public const string AC_Y = "y";
        public const string AC_H = "h";
    }
}

