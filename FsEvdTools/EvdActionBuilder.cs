using Bex;

namespace FsEvdTools;

public abstract class EvdActionBuilder<T>
{
    public record Action(int CategoryId, int ActionId, byte[] ArgBuffer);
    public delegate Action ActionImporter(T action);
    public delegate T ActionExporter(Action action);

    public bool BigEndian { get; set; }

    protected ActionImporter ImportAction { get; }
    protected ActionExporter ExportAction { get; }

    protected EvdActionBuilder(ActionImporter importAction, ActionExporter exportAction)
    {
        BigEndian = false;
        ImportAction = importAction;
        ExportAction = exportAction;
    }

    protected bool Is(T action, int categoryId, int actionId)
    {
        var act = ImportAction(action);
        return act.CategoryId == categoryId && act.ActionId == actionId;
    }

    protected bool ParseMaybe<TArgs>(T action, int categoryId, int actionId, Func<BexReader, TArgs> createArgs, out TArgs? args)
    {
        var act = ImportAction(action);
        if (act.CategoryId == categoryId && act.ActionId == actionId)
        {
            var br = new BexReader(act.ArgBuffer, BigEndian);
            args = createArgs(br);
            return true;
        }
        else
        {
            args = default;
            return false;
        }
    }

    protected T CreateAction(int categoryId, int actionId, ReadOnlySpan<object> args)
    {
        var action = new Action(categoryId, actionId, WriteArgBuffer(args));
        return ExportAction(action);
    }

    protected byte[] WriteArgBuffer(ReadOnlySpan<object> args)
    {
        var bw = new BexWriter(BigEndian);
        foreach (object arg in args)
        {
            switch (arg)
            {
                case sbyte or byte: break;
                case short or ushort: bw.Align(2); break;
                case int or uint or float: bw.Align(4); break;
                default: throw new ArgumentException($"Unknown argument type {arg.GetType()}");
            }

            switch (arg)
            {
                case sbyte s8: bw.WriteSByte(s8); break;
                case byte u8: bw.WriteByte(u8); break;
                case short s16: bw.WriteInt16(s16); break;
                case ushort u16: bw.WriteUInt16(u16); break;
                case int s32: bw.WriteInt32(s32); break;
                case uint u32: bw.WriteUInt32(u32); break;
                case float f32: bw.WriteSingle(f32); break;
                default: throw new ArgumentException($"Unknown argument type {arg.GetType()}");
            }
        }
        bw.Align(4);
        return bw.FinishBytes();
    }
}
