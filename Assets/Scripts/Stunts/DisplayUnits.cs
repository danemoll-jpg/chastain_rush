using System;
using System.Globalization;
namespace Racer
{
    // Presentation only. Physics, records and medal comparisons remain metres/seconds.
    public static class DisplayUnits
    {
        public const double MetresPerMile=1609.344, MetresPerFoot=.3048;
        public static double Mph(double metresPerSecond)=>metresPerSecond*3600/MetresPerMile;
        public static double Feet(double metres)=>metres/MetresPerFoot;
        public static string Speed(double metresPerSecond)=>Mph(metresPerSecond).ToString("0.0",CultureInfo.InvariantCulture)+" mph";
        public static string Jump(double metres)=>Feet(metres).ToString("0.0",CultureInfo.InvariantCulture)+" ft";
        public static string Target(double metres)=>(Math.Ceiling(Feet(metres)*10)/10).ToString("0.0",CultureInfo.InvariantCulture)+" ft";
        public static string Distance(double metres)=>metres>=MetresPerMile*.1
            ?(metres/MetresPerMile).ToString("0.00",CultureInfo.InvariantCulture)+" mi"
            :Feet(metres).ToString("0",CultureInfo.InvariantCulture)+" ft";
    }
}
