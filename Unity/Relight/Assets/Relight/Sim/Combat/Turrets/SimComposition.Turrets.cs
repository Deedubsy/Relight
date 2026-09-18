using System.Collections.Generic;

namespace Relight.Sim
{
    /// <summary>
    /// The Turret sub-slot of the fixed composition (C-04, W-B). One phase, no initialiser work and no commands:
    /// a turret is fed through <see cref="MachineTransferCommand"/> (W-B wave 1) or W-A's flow, and repaired
    /// through W-A's repair command, both of which call into <see cref="TurretRules"/> / <see cref="TurretHopper"/>.
    /// A fresh <see cref="TurretState"/> already means "no structure damaged, no turret aimed", so there is nothing
    /// for an initialiser to fill.
    /// </summary>
    public static partial class SimComposition
    {
        static partial void AddTurretPhases(List<ITickPhase> list)
        {
            list.Add(new TurretPhase());
        }

        static partial void AddTurretInitializers(List<IStateInitializer> list)
        {
        }

        static partial void AddTurretHandlers(List<ICommandHandler> list)
        {
        }
    }
}
