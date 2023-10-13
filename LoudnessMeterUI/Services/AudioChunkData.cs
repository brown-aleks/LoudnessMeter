using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LoudnessMeterUI.Services
{
    public record AudioChunkData(
        double Loudness,
        double ShortTermLUFS,
        double IntegratedLUFS,
        double LoudnessRange,
        double RealtimeDynamics,
        double AverageRealtimeDynamics,
        double MomentaryMaxLUFS,
        double ShortTermMaxLUFS,
        double TruePeakMax
        );
}
