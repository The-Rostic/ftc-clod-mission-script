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


public class CNetworkComms
{
    private const bool DEBUG_MESSAGES = true;
    private Mission BaseMission = null;
    public CNetworkComms(Mission mission)
    {
        BaseMission = mission;
    }

    public void OnBattleStoped() { 
        // Do something like stop comms and close sockets
    }
}

