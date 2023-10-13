using ManagedBass;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LoudnessMeterUI.Services
{
    public class RecordingDevice : IDisposable
    {
        public string Name { get; }

        public int Index { get; }

        public RecordingDevice(int index, string name)
        {
            Index = index;

            Name = name;
        }

        public static IEnumerable<RecordingDevice> Enumerate()
        {
            for (int i = 0; Bass.RecordGetDeviceInfo(i, out var info); ++i)
                yield return new RecordingDevice(i, info.Name);
        }

        public void Dispose()
        {
            Bass.CurrentRecordingDevice = Index;
            Bass.RecordFree();
        }

        public override string ToString() => Name;
    }
}
