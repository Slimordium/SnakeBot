using System;
// ReSharper disable InconsistentNaming

namespace SnakeBot
{
    [Flags]
    internal enum GpsFixQuality
    {
        NoFix,
        StandardGps,
        DiffGps,
        PPS,
        RTK,
        FloatRTK,
        Estimated,
        Manual,
        Simulation
    }
}