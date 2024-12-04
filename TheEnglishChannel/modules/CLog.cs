using System;
using System.IO;
using maddox.game;
using maddox.game.world;
using System.Diagnostics;
using System.Threading;

public static class CLog
{
    private static bool initialized = false;
    public static bool IsInitialized { get { return initialized; } }

    private static AMission baseMission = null;

    private static StreamWriter logFile;
    /// <summary>
    /// this variable set to true only when new data written to log file
    /// </summary>
    private static bool needFlush = false;
    private static TimeSpan lastFlush;
    private static Stopwatch missionStartStopwatch = null;

    /// <summary>
    /// Time mission running in milliseconds
    /// </summary>
    public static TimeSpan MissionRunningTime
    {
        get { TimeSpan time; lock (missionStartStopwatch) { time = missionStartStopwatch.Elapsed; } return time; }
    }


    /// <summary>
    /// Initialization of static class
    /// </summary> 
    /// <param name="Mission">The Mission (use 'this')</param>
    public static void Init(AMission Mission)
    {
        baseMission = Mission;
        missionStartStopwatch = new Stopwatch();
        missionStartStopwatch.Start();

        if (CConfig.DEBUG_LOCAL_LOG_ENABLE)
        {
            lastFlush = MissionRunningTime;
            try
            {
                DateTime dt = DateTime.Now;
                string m_sUserDoc = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + @"\1C SoftClub\il-2 sturmovik cliffs of dover\";
                string m_sMyFolder = Path.GetDirectoryName(baseMission.sPathMyself);
                string dir = m_sUserDoc + m_sMyFolder + @"\Logs";
                // make logs dir if not exists
                if (!Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }

                // keep only last 9 logfiles
                try
                {
                    if (File.Exists(dir + "\\Log_9.log"))
                    {
                        File.Delete(dir + "\\Log_9.log");
                    }
                    for (int i = 8; i > 0; i--)
                    {
                        string efn = dir + "\\Log_" + i.ToString() + ".log";
                        if (File.Exists(efn))
                        {
                            File.Move(efn, dir + "\\Log_" + (i + 1).ToString() + ".log");
                        }
                    }
                }
                catch (Exception ex)
                {
                    Write(ex.ToString() + "\n" + ex.Message.ToString());
                }
                logFile = File.CreateText(dir + "\\Log_1.log");

                //logFile = File.CreateText(dir + "\\" + dt.Year.ToString("0000") + dt.Month.ToString("00") + dt.Day.ToString("00") + dt.Hour.ToString("00") + dt.Minute.ToString("00") + dt.Second.ToString("00") + ".log");
            }
            catch (Exception ex)
            {
                Write(ex.ToString() + "\n" + ex.Message.ToString());
                logFile = null;
            }
        }
        initialized = true;
        Write("Mission initialized...");
    }

    /// <summary>
    /// Close log file
    /// </summary>
    public static void Close()
    {
        if (!initialized) return;
        if (logFile != null)
        {
            try
            {
                Write("Mission stopped.");
                logFile.Close();
            }
            catch
            {
            }
        }
    }
    /// <summary>
    /// Message logging
    /// </summary>
    /// <param name="message">message to log</param>
    public static void Write(string message)
    {
        if (!initialized) return;
        if ((CConfig.DEBUG_SERVER_LOG_ENABLE) || (logFile != null))
        {
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
            // Real time
            DateTime dt = DateTime.Now;
            string logmsg = dt.ToString("H:mm:ss,") + dt.Millisecond.ToString("000") + "(MT+" + missionTime + ") : " + message;

            if (logFile != null)
            {
                try
                {
                    logFile.WriteLine(logmsg);
                    needFlush = true;
                }
                catch
                {
                }
            }

            if (CConfig.DEBUG_SERVER_LOG_ENABLE)
            {
                baseMission.GamePlay.gpLogServer(logmsg);
            }
        }
    }
    /// <summary>
    /// Flush data to disk immidiatly!
    /// </summary>
    public static void FlushBuffers()
    {
        if (!initialized) return;
        if (CConfig.DEBUG_LOCAL_LOG_ENABLE)
        {
            try
            {
                if ((MissionRunningTime.TotalSeconds - lastFlush.TotalSeconds > 30) && needFlush)
                {
                    lastFlush = MissionRunningTime;
                    logFile.Flush();
                    needFlush = false;
                }
            }
            catch
            {
            }
        }
    }
}
