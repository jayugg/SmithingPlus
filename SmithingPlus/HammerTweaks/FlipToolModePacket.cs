using ProtoBuf;

namespace SmithingPlus.HammerTweaks;

[ProtoContract]
public class FlipToolModePacket
{
    [ProtoMember(1)]
    public int ToolMode { get; set; } = -1;
}