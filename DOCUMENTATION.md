## EvdActionBuilder

These classes provide strongly-typed methods to inspect and create EMEVD actions; they are generated based on the EMEDFs provided with DarkScript3, with a separate class for each supported game.

FsEvdTools is designed to be agnostic as to which underlying formats library you use, so the EvdActionBuilders must be constructed with a pair of lambdas to translate to and from your own types. The builder also has a BigEndian property to support parsing PS3 and X360 actions.

```cs
// For instance, if using SoulsFormats or its derivatives
var e = new EvdActionBuilderDarkSouls<EMEVD.Instruction>(
    action => new(action.Bank, action.ID, action.ArgData), 
    action => new(action.CategoryId, action.ActionId, action.ArgBuffer)
    );

// If you're one of the two people on earth with a reason to manipulate PS3/X360 files
e.BigEndian = true;
```

Once constructed, the builder provides three methods for every type of EMEVD action in the corresponding game:

```cs
// Check if an action is this type of action
if (e.SetEventFlag(action))
{
    Console.WriteLine("Yep, it's a SetEventFlag.");
}

// Check if an action is this type of action, and if so parse its arguments
if (e.SetEventFlag(action, out var args))
{
    Console.WriteLine($"Yep, it's a SetEventFlag, and it's setting flag {args.eventFlagId}.");
}

// Create a new action with the specified arguments
var action = e.SetEventFlag(20, 1);
```

### Examples

```cs
// Full example of creating a simple event for testing

var e = new EvdActionBuilderEldenRing<EMEVD.Instruction>(
    a => new(a.Bank, a.ID, a.ArgData), 
    a => new(a.CategoryId, a.ActionId, a.ArgBuffer));

int testEventId = 9900;

var evd = EMEVD.Read("event/common.emevd.dcx");

var constructor = evd.Events.Find(e => e.ID == 0);
constructor.Instructions.Insert(0, e.InitializeEvent(-1, testEventId));

evd.Events.Add(new(testEventId) {
    Instructions = [
        e.WaitFixedTimeSeconds(5),
        e.DisplayBanner(33),
        ],
});

// Please don't actually read and write to the same file like this, it's just an example
evd.Write("event/common.emevd.dcx");
```

```cs
// From the Free Reign mod
// Inspecting and converting all actions that check the variation to check an event flag instead

void transmuteVariations(EMEVD.Event evt)
{
    for (int i = 0; i < evt.Instructions.Count; i++)
    {
        var act = evt.Instructions[i];
        if (e.IfIsMapVariation(act, out var ifArgs))
        {
            evt.Instructions[i] = e.IfEventFlag(ifArgs.resultConditionGroup, ifArgs.isVariation, 0, variationFlagBase + ifArgs.mapVariationId);
        }
        else if (e.SkipIfMapVariationId(act, out var skipArgs))
        {
            evt.Instructions[i] = e.SkipIfEventFlag(skipArgs.numberOfSkippedLines, skipArgs.isVariation, 0, variationFlagBase + skipArgs.mapVariationId);
        }
        else if (e.GotoIfMapVariationId(act, out var gotoArgs))
        {
            evt.Instructions[i] = e.GotoIfEventFlag(gotoArgs.label, gotoArgs.isVariation, 0, variationFlagBase + gotoArgs.mapVariationId);
        }
        else if (e.EndIfMapVariationId(act, out var endArgs))
        {
            evt.Instructions[i] = e.EndIfEventFlag(endArgs.executionEndType, endArgs.isVariation, 0, variationFlagBase + endArgs.mapVariationId);
        }
    }
}
```

```cs
// From the Coral Disc Player mod
// Building events from scratch and emitting appropriate initializations with LINQ

var constructor = evd.Events.Find(e => e.ID == 0);
constructor.Instructions.Insert(0, e.InitializeEvent(-1, evtSetup));

var validTracks = Config.Tracks.Select((track, i) => (track, i)).Where(track => track.track.BgmId != -1);
evd.Events.AddRange([
    new(evtSetup) {
        Instructions = [
            ..validTracks.Select(track => e.InitializeEvent(-1, evtSetMenuFlag, flagMenuSelectedTrackGarageBase + (uint)track.i, track.track.BgmId, flagSelectedTrackGarageValueBase)),
            ..validTracks.Select(track => e.InitializeEvent(-1, evtGetMenuFlag, flagMenuSelectedTrackGarageBase + (uint)track.i, track.track.BgmId, flagSelectedTrackGarageValueBase)),
            ..validTracks.Select(track => e.InitializeEvent(-1, evtSetMenuFlag, flagMenuSelectedTrackAcTestBase + (uint)track.i, track.track.BgmId, flagSelectedTrackAcTestValueBase)),
            ..validTracks.Select(track => e.InitializeEvent(-1, evtGetMenuFlag, flagMenuSelectedTrackAcTestBase + (uint)track.i, track.track.BgmId, flagSelectedTrackAcTestValueBase)),
            ]
    },
    new(evtSetMenuFlag) {
        Parameters = [
            new(0, 4, 8, 4),
            new(0, 12, 4, 4),
            new(1, 4, 0, 4),
            new(2, 4, 8, 4),
            new(2, 12, 4, 4),
            new(3, 4, 0, 4),
            ],
        Instructions = [
            e.IfEventValue(0, ~0u, eventValueBits, 0, ~0u),
            e.SetEventFlag(0, ~0u, 1),
            e.IfEventValue(0, ~0u, eventValueBits, 1, ~0u),
            e.SetEventFlag(0, ~0u, 0),
            e.EndUnconditionally(1),
            ]
    },
    new(evtGetMenuFlag) {
        Parameters = [
            new(0, 4, 0, 4),
            new(1, 0, 8, 4),
            new(1, 8, 4, 4),
            new(2, 4, 0, 4),
            ],
        Instructions = [
            e.IfEventFlag(0, 1, 0, ~0u),
            e.EventValueOperation(~0u, eventValueBits, -1, 0, 0, 5),
            e.IfEventFlag(0, 0, 0, ~0u),
            e.EndUnconditionally(1),
            ]
    },
]);
```
