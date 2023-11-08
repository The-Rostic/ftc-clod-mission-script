using System;

public static class CConfig : Object
{
	public const bool DEBUG_LOCAL_LOG_ENABLE = false;
	public const bool DEBUG_SERVER_LOG_ENABLE = true;

    /// <summary>
    /// When set to 'true' will try to do it's best to keep player in pilot seat.
    /// </summary>
    public const bool DISABLE_LEAVE_MOVING_AIRCRAFT = true;

    /// <summary>
    /// When set to 'true' will try to set longer timeouts for destroying abandoned aircrafts on runways of active airfields simulating airfield crew need some time to move away crashlanded aircraft.
    /// Or not destroying them at all outside of operational airfields. Realism above all!
    /// 
    /// When set to 'false' will despawn instantly. Good for casual gameplay behaivour.
    /// </summary>
    public const bool REALISTIC_AIRCRAFT_DESPAWN_TIMEOUT = true;

    /// <summary>
    /// Has no effect when when DISABLE_LEAVE_MOVING_AIRCRAFT set to 'true'
    /// When set to 'true' will drop player from all places in aircraft when he is about to leave pilot place.
    /// When set to 'false' default game logic used - no script limitations.
    /// </summary>
	public const bool DISABLE_PLAYER_TO_FLY_AS_PASSANGER_WITH_AI_PILOT = true;
}
