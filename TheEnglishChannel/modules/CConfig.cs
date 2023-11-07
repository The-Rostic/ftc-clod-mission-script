using System;

public static class CConfig : Object
{
	public const bool DEBUG_LOCAL_LOG_ENABLE = true;
	public const bool DEBUG_SERVER_LOG_ENABLE = true;

	public const bool DISABLE_LEAVE_MOVING_AIRCRAFT = true;
	public const bool REALISTIC_DESPAWN = true; // if set to 'false' will despawn instantly
	public const bool DISABLE_PLAYER_TO_FLY_AS_PASSANGER_WITH_AI_PILOT = true; // has no effect when when REALISTIC_DESPAWN set to 'true'
}
