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

    private static AMission m_Mission = null;

    private static StreamWriter m_LogFile;
    /// <summary>
    /// this variable set to true only when new data written to log file
    /// </summary>
    private static bool m_NeedFlush = false;
    private static TimeSpan m_LastFlush;
    private static Stopwatch m_Stopwatch = null;

    /// <summary>
    /// Time mission running in milliseconds
    /// </summary>
    public static TimeSpan MissionRunningTime
    {
        get { TimeSpan time; lock (m_Stopwatch) { time = m_Stopwatch.Elapsed; } return time; }
    }


    /// <summary>
    /// Initialization of static class
    /// </summary> 
    /// <param name="Mission">The Mission (use 'this')</param>
    public static void Init(AMission Mission)
    {
        m_Mission = Mission;
        m_Stopwatch = new Stopwatch();
        m_Stopwatch.Start();

        if (CConfig.DEBUG_LOCAL_LOG_ENABLE)
        {
            m_LastFlush = MissionRunningTime;
            try
            {
                DateTime dt = DateTime.Now;
                string m_sUserDoc = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + @"\1C SoftClub\il-2 sturmovik cliffs of dover\";
                string m_sMyFolder = Path.GetDirectoryName(m_Mission.sPathMyself);
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
                m_LogFile = File.CreateText(dir + "\\Log_1.log");

                //m_LogFile = File.CreateText(dir + "\\" + dt.Year.ToString("0000") + dt.Month.ToString("00") + dt.Day.ToString("00") + dt.Hour.ToString("00") + dt.Minute.ToString("00") + dt.Second.ToString("00") + ".log");
            }
            catch (Exception ex)
            {
                Write(ex.ToString() + "\n" + ex.Message.ToString());
                m_LogFile = null;
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
        if (m_LogFile != null)
        {
            try
            {
                Write("Mission stopped.");
                m_LogFile.Close();
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
        if ((CConfig.DEBUG_SERVER_LOG_ENABLE) || (m_LogFile != null))
        {
            DateTime dt = DateTime.Now;
            string logmsg = dt.ToString("H:mm:ss,") + dt.Millisecond.ToString("000") + " : " + message;

            if (m_LogFile != null)
            {
                try
                {
                    m_LogFile.WriteLine(logmsg);
                    m_NeedFlush = true;
                }
                catch
                {
                }
            }

            if (CConfig.DEBUG_SERVER_LOG_ENABLE)
            {
                m_Mission.GamePlay.gpLogServer(logmsg);
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
                if ((MissionRunningTime.TotalSeconds - m_LastFlush.TotalSeconds > 30) && m_NeedFlush)
                {
                    m_LastFlush = MissionRunningTime;
                    m_LogFile.Flush();
                    m_NeedFlush = false;
                }
            }
            catch
            {
            }
        }
    }
}
