namespace FsEvdTools.Gen;

internal class EmedfDark
{
#pragma warning disable IDE1006 // Naming Styles
    public int unknown { get; set; }
    public List<EmClass> main_classes { get; set; }

    public class EmClass
    {
        public string name { get; set; }
        public int index { get; set; }
        public List<EmInstruction> instrs { get; set; }
    }

    public class EmInstruction
    {
        public string name { get; set; }
        public int index { get; set; }
        public List<EmArgument> args { get; set; }
    }

    public class EmArgument
    {
        public string name { get; set; }
        public int type { get; set; }
        public bool vararg { get; set; }
    }
#pragma warning restore IDE1006 // Naming Styles
}
