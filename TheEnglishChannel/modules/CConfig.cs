using System;

public static class CConfig : Object
{
    // General logging defines
    public const bool DEBUG_LOCAL_LOG_ENABLE = false;
    public const bool DEBUG_SERVER_LOG_ENABLE = true;
    static public bool IsLoggingEnabled() { return (DEBUG_LOCAL_LOG_ENABLE || DEBUG_SERVER_LOG_ENABLE); }

    // Logging features
    public const bool DEBUG_PERFORMANCE_LOG_ENABLE = false;

    //
    // Network communicatios
    //
    public const string SERVER_IP = "127.0.0.1";
    public const int SERVER_PORT = 37000;

    //////////////////////////////////////////////////////////////////////////
    //
    //                    Spawn / despawn logic.
    //

    // In general script have to disable for AI to control the aircraft that was 
    // spawned by player. But if configuration here and on the server allow, players 
    // are free to take place in aircraft piloted by AI. If player take seat other then pilot seat
    // he is free to leave it anytime. But when player take pilot seat, script will
    // decide what to do when player try to left pilot seat according to config below.
    // There is no restriction for players (not assigned to any aircraft) to take place
    // in aircraft piloted by another player.

    /// <summary>
    /// When set to 'true' will try to set longer timeouts for destroying abandoned aircrafts on runways of active airfields simulating airfield crew need some time to move away crashlanded aircraft.
    /// Or not destroying them at all outside of operational airfields. Realism above all! The noly plroblem ingame logic may destroy it faster than I want to :(
    /// 
    /// When set to 'false' will despawn aircraft instantly. Good for casual gameplay behaivour.
    /// </summary>
    public const bool REALISTIC_AIRCRAFT_DESPAWN_TIMEOUT = true;

    /// <summary>
    /// When set to 'true' will try to do it's best to keep player in pilot seat.
    /// </summary>
    public const bool DISABLE_PILOT_TO_LEAVE_MOVING_AIRCRAFT = true;

    /// <summary>
    /// Has no effect when when DISABLE_LEAVE_MOVING_AIRCRAFT set to 'true'
    /// When set to 'true' will drop player from all places in aircraft when he is about to leave pilot place.
    /// When set to 'false' default game logic used - no script limitations.
    /// </summary>
	public const bool DISABLE_PLAYER_TO_FLY_AS_PASSANGER_WITH_AI_PILOT = true;

    //
    //                    Spawn / despawn logic.
    //
    //////////////////////////////////////////////////////////////////////////
}
