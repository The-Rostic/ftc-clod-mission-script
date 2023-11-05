//
// Print list of airfields in mission
//
if (DEBUG_MESSAGES) CLog.Write("Mission list of airfields:");
for (int i = 0; i < GamePlay.gpAirports().Length; i++)
{
    AiAirport airport = GamePlay.gpAirports()[i];
    Point3d airpPos = airport.Pos();
    CLog.Write(airport.Name() 
        + " at X="+ airpPos.x.ToString() + " Y=" + airpPos.y.ToString() + " Z=" + airpPos.z.ToString()
        + " army=" + airport.Army().ToString()
        + " CoverageR="+ airport.CoverageR().ToString()
        + " FieldR=" + airport.FieldR().ToString()
        );
}


//
// Long way to find out if aircraft on friendly airfield
//
Point3d airportNeutralPos;
AiAirport airportNeutral;
AiAirport[] airports = GamePlay.gpAirports();
for (int i = 0; i < airports.Length; i++)
{
    airportNeutral = airports[i];
    // get neutral airfileds with friendly spawn area airports nearby...
    if (airportNeutral.Army() == 0)
    {
        airportNeutralPos = airportNeutral.Pos();
        for (int j = 0; j < airports.Length; j++)
        {
            if (i == j) continue;
            airportFriendly = airports[j];
            if (airportFriendly.Army() == aircraftArmy)
            {
                Point3d airportFriendlyPos = airportFriendly.Pos();
                if (airportNeutralPos.distanceLinf(ref airportFriendlyPos) < airportNeutral.CoverageR())
                {
                    // Ok, this neutral airport contain friendly spawn area airport. Check if we are in this neutral airport radius
                    double distToAirportNeutral = airportNeutralPos.distanceLinf(ref aircraftPos);
                    if (distToAirportNeutral < airportNeutral.CoverageR())
                    {
                        if (DEBUG_MESSAGES) CLog.Write(Aircraft.Name() + " is on friendly airfiled " + airportNeutral.Name() + " distance " + distToAirportNeutral.ToString());
                        return true;
                    }
                }
            }
        }
    }
}