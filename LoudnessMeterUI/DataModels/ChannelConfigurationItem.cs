using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LoudnessMeterUI.DataModels
{
    /// <summary>
    /// Information about a channel configuration
    /// </summary>
    public record ChannelConfigurationItem(string Group, string Text, string ShortText);
}
