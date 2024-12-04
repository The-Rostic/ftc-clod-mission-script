using System.Text;
using System;
using System.IO;
using System.IO.Compression;
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
    private AMission baseMission = null;
    private CMissionCommon missionCommon = null;
    // Example of serverState. All coordinates, distances and heights in meters.
    //
    // {
    //  "world_state_idx":1234567,
    //  "map_mame":"Land$English_Channel_1940",
    //  "mission_time":"12:20:30.012",
    //  "battle_area":{"x":8000, "y":8000, "w":320000, "h":300000, "sector_size":10000},
    //  "armies":[
    //    {"id":1, "name":"Red", "countries":"gb fr us pl rz"},
    //    {"id":2, "name":"Blue", "countries":"de it"}
    //  ],
    //  "aircrafts":[
    //    {"id":"2:BoB_RAF_F_249Sqn_Late.001","army":1,"model":"Spitfire IIa","eng":1,"sta":1,"x":250583,"y":226461,"z":1002},
    //    { "id":"2:BoB_RAF_F_249Sqn_Late.002","army":1,"model":"Spitfire IIa","eng":1,"sta":1,"x":250689,"y":226633,"z":1001},
    //    { "id":"1:BoB_LW_JG26_I.200","army":2,"model":"Bf 109 E-4","eng":1,"sta":1,"x":286226,"y":210783,"z":1000},
    //    { "id":"1:BoB_LW_JG26_I.201","army":2,"model":"Bf 109 E-4","eng":1,"sta":1,"x":286362,"y":210816,"z":1000}
    //  ]
    // }
    private readonly object serverStateLock = new object();
    private StringBuilder serverState = new StringBuilder();
    private long serverStateIdx = 0; // increments every time serverState updated.
    public CNetworkComms(CMissionCommon mission_common)
    {
        baseMission = mission_common.baseMission;
        missionCommon = mission_common;
        serverState.Capacity = 1024 * 1024; // I doubt real data will ever be such big

        // run to fill "serverState" with data
        MissionScriptPoll();
    }
    public void OnBattleStoped() { 
        // Do something like stop comms and close sockets
    }
    private long radarPollTickLast = 0;
    public void MissionScriptPoll() {
        //
        // Radar data poller
        //
        if (DateTime.Now.Ticks - radarPollTickLast >= CConfig.RADAR_DATA_UPDATE_INTERVAL_MS * CMissionCommon.TICKS_IN_MILISECOND)
        {
            radarPollTickLast = DateTime.Now.Ticks;
            try
            {
                if (DEBUG_MESSAGES && CLog.IsInitialized) CLog.Write("RadarDataStart\n");
                lock (serverStateLock)
                {
                    serverState.Clear();
                    // JSON first open bracket
                    serverState.Append("{\n");

                    ///////////////////////
                    // World state incremental index
                    serverStateIdx++;
                    serverState.Append(MakeJsonIntEntry(CJsonIds.WORLD_STATE_IDX, serverStateIdx, JSONending.BOTH));

                    ///////////////////////
                    // Map name
                    serverState.Append(MakeJsonStringEntry(CJsonIds.MAP_NAME, missionCommon.missionMapInfo.Name, JSONending.BOTH));

                    ///////////////////////
                    // Map time
                    double dMisTime = baseMission.GamePlay.gpTimeofDay(); // time of day in hours as double value
                    int mt_hours = (int)dMisTime;
                    dMisTime = dMisTime - mt_hours;
                    int mt_minutes = (int)(dMisTime * 60);
                    int mt_seconds = (int)(dMisTime * 3600) - mt_minutes * 60;
                    int mt_millisec = (int)(dMisTime * 3600000 - mt_minutes * 60000 - mt_seconds * 1000);
                    string missionTime = mt_hours.ToString()
                        + ":" + mt_minutes.ToString().PadLeft(2, '0')
                        + ":" + mt_seconds.ToString().PadLeft(2, '0')
                        + "." + mt_millisec.ToString().PadLeft(3, '0');
                    serverState.Append(MakeJsonStringEntry(CJsonIds.MISSION_TIME, missionTime, JSONending.BOTH));

                    ///////////////////////
                    // Map battle area
                    // open class CJsonIds.BATTLE_AREA
                    serverState.Append("\"" + CJsonIds.BATTLE_AREA + "\":{"); 
                    CMissionCommon.CBattleArea battleArea = missionCommon.missionMapInfo.BattleArea;
                    serverState.Append(MakeJsonIntEntry(CJsonIds.BATTLE_AREA_X, battleArea.x, JSONending.COMA));
                    serverState.Append(MakeJsonIntEntry(CJsonIds.BATTLE_AREA_Y, battleArea.y, JSONending.COMA));
                    serverState.Append(MakeJsonIntEntry(CJsonIds.BATTLE_AREA_W, battleArea.w, JSONending.COMA));
                    serverState.Append(MakeJsonIntEntry(CJsonIds.BATTLE_AREA_H, battleArea.h, JSONending.COMA));
                    serverState.Append(MakeJsonIntEntry(CJsonIds.BATTLE_AREA_SECTSZ, battleArea.sector_size, JSONending.NONE));
                    // close class CJsonIds.BATTLE_AREA
                    serverState.Append("},\n");

                    ///////////////////////
                    // Mission armies
                    // open array CJsonIds.ARMIES
                    serverState.Append("\"" + CJsonIds.ARMIES + "\":[\n");
                    CMissionCommon.CArmy[] armies = missionCommon.missionMapInfo.Armies;
                    int last_i = armies.Length - 1;
                    for (int i = 0; i < armies.Length; i++)
                    {
                        serverState.Append("{");
                        serverState.Append(MakeJsonIntEntry(CJsonIds.ARMIES_ID, armies[i].id, JSONending.COMA));
                        serverState.Append(MakeJsonStringEntry(CJsonIds.ARMIES_NAME, armies[i].name, JSONending.COMA));
                        serverState.Append(MakeJsonStringEntry(CJsonIds.ARMIES_COUNTRIES, armies[i].countries, JSONending.NONE));
                        serverState.Append(((i == last_i)? "}\n" : "},\n"));
                    }
                    // close array CJsonIds.ARMIES
                    serverState.Append("],\n");
                    
                    ///////////////////////
                    //Get aircrafts
                    // open array CJsonIds.AC_AIRCRAFTS
                    serverState.Append("\"" + CJsonIds.AC_AIRCRAFTS + "\":[\n");
                    last_i = armies.Length - 1;
                    for (int i = 0; i < armies.Length; i++)
                    {
                        int army_id = armies[i].id;
                        AiAirGroup[] aiAirGroups = baseMission.GamePlay.gpAirGroups(army_id);
                        if (aiAirGroups != null)
                        {
                            int last_j = aiAirGroups.Length - 1;
                            for (int j = 0; j < aiAirGroups.Length; j++)
                            {
                                if (aiAirGroups[j] != null)
                                {
                                    AiActor[] actors = aiAirGroups[j].GetItems();
                                    if (actors != null)
                                    {
                                        int last_k = actors.Length - 1;
                                        for (int k = 0; k < actors.Length; k++)
                                        {
                                            if (actors[k] != null)
                                            {
                                                string actorName = actors[k].Name();
                                                Point3d pos = actors[k].Pos();
                                                int stations = ((AiAircraft)actors[k]).Places();
                                                int engines = aiAirGroups[j].aircraftEnginesNum();
                                                string ac_model = ((AiAircraft)actors[k]).VariantName();
                                                serverState.Append("{");
                                                serverState.Append(MakeJsonStringEntry(CJsonIds.AC_ID, actorName, JSONending.COMA));
                                                serverState.Append(MakeJsonIntEntry(CJsonIds.AC_ARMY, army_id, JSONending.COMA));
                                                serverState.Append(MakeJsonStringEntry(CJsonIds.AC_MODEL, ac_model, JSONending.COMA));
                                                serverState.Append(MakeJsonIntEntry(CJsonIds.AC_ENGINES_CNT, engines, JSONending.COMA));
                                                serverState.Append(MakeJsonIntEntry(CJsonIds.AC_CREW_STATIONS_CNT, stations, JSONending.COMA));
                                                serverState.Append(MakeJsonIntEntry(CJsonIds.AC_X, (int)pos.x, JSONending.COMA));
                                                serverState.Append(MakeJsonIntEntry(CJsonIds.AC_Y, (int)pos.y, JSONending.COMA));
                                                serverState.Append(MakeJsonIntEntry(CJsonIds.AC_Z, (int)pos.z, JSONending.NONE));
                                                serverState.Append((((i == last_i) && (j == last_j) && (k == last_k)) ? "}\n" : "},\n"));
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                    // close array CJsonIds.AC_AIRCRAFTS
                    serverState.Append("]\n");


                    // JSON last close bracket
                    serverState.Append("}");
                    string serverStateStr = serverState.ToString();
                    if (DEBUG_MESSAGES && CLog.IsInitialized) CLog.Write("RadarDataEnd\n\n"); //CLog.Write("RadarDataEnd\n<JSONin>" + serverStateStr + "<JSONout>\n\n");
                    //
                    // zipped Base64 test
                    //
                    if (DEBUG_MESSAGES && CLog.IsInitialized) CLog.Write("RadarData64Start\n");
                    string base64 = CompressString.StringCompressor.CompressString(serverStateStr);
                    if (DEBUG_MESSAGES && CLog.IsInitialized) CLog.Write("RadarData64End\n<b64in>" + base64 + "<b64out>\n\n");

                    if (DEBUG_MESSAGES && CLog.IsInitialized) CLog.Write("RadarDataDecodeStart\n");
                    string decode = CompressString.StringCompressor.DecompressString(base64);
                    if (DEBUG_MESSAGES && CLog.IsInitialized) CLog.Write("RadarDataDecodeEnd\n<DECODEin>" + decode + "<DECODEout>\n\n");

                }
            }
            catch (Exception e)
            {
                if (DEBUG_MESSAGES && CLog.IsInitialized) CLog.Write(e.ToString() + "\n" + e.Message.ToString());
            }
        }
    }

    private static class JSONending {
        public static string BOTH = ",\n";
        public static string COMA = ",";
        public static string BREAK = "\n";
        public static string NONE = "";
    }
    private string MakeJsonStringEntry(string id, string val, string ending)
    {
        return "\"" + id + "\":\"" + val + "\"" + ending;
    }
    private string MakeJsonIntEntry(string id, long val, string ending)
    {
        return "\"" + id + "\":" + val.ToString() + ending;
    }
    private class CJsonIds
    {
        public const string WORLD_STATE_IDX = "world_state_idx";
        public const string MAP_NAME = "map_name";
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
        public const string AC_MODEL = "model";
        public const string AC_ENGINES_CNT = "eng";
        public const string AC_CREW_STATIONS_CNT = "sta";
        //public const string AC_PLAYER0 = "player"; <-- useless data for radar
        public const string AC_X = "x";
        public const string AC_Y = "y";
        public const string AC_Z = "z";
    }
}

// base64 data contain only gzipped string
namespace CompressString
{
    internal static class StringCompressor
    {
        /// <summary>
        /// Compresses the string.
        /// </summary>
        /// <param name="text">The text.</param>
        /// <returns></returns>
        public static string CompressString(string text)
        {
            byte[] buffer = Encoding.UTF8.GetBytes(text);
            var memoryStream = new MemoryStream();
            using (var gZipStream = new GZipStream(memoryStream, CompressionMode.Compress, true))
            {
                gZipStream.Write(buffer, 0, buffer.Length);
            }
            memoryStream.Position = 0;
            var compressedData = new byte[memoryStream.Length];
            memoryStream.Read(compressedData, 0, compressedData.Length);
            return Convert.ToBase64String(compressedData);
        }

        /// <summary>
        /// Decompresses the string.
        /// </summary>
        /// <param name="compressedText">The compressed text.</param>
        /// <returns></returns>
        public static string DecompressString(string compressedText)
        {
            byte[] gZipBuffer = Convert.FromBase64String(compressedText);
            using (var memoryStream = new MemoryStream())
            {
                int dataLength = gZipBuffer.Length;
                memoryStream.Write(gZipBuffer, 0, gZipBuffer.Length);
                memoryStream.Position = 0;
                using (var gzip = new GZipStream(memoryStream, CompressionMode.Decompress))
                using (var reader = new StreamReader(gzip))
                {
                    return reader.ReadToEnd();
                }
            }
        }
    }
}
