using System;

namespace KingmakerWakingRune.Sanctum
{
    internal enum BuildStatus
    {
        NotBuilt,
        Building,
        Ready,
        Failed,
    }

    /// <summary>
    /// Publishes the sanctum as built only once all of its setup has succeeded. A build that fails or finds its
    /// scenes unready removes what it made and can be tried again; it never passes for a finished area.
    /// </summary>
    internal sealed class SanctumBuildGate
    {
        public BuildStatus Status { get; private set; }

        public bool IsReady => Status == BuildStatus.Ready;

        public void Ensure(Func<bool> build, Action cleanup, Action<Exception> report)
        {
            if (Status == BuildStatus.Ready || Status == BuildStatus.Building)
            {
                return;
            }
            Status = BuildStatus.Building;
            bool built = false;
            try
            {
                built = build();
            }
            catch (Exception ex)
            {
                report(ex);
            }
            if (built)
            {
                Status = BuildStatus.Ready;
                return;
            }
            try
            {
                cleanup();
            }
            catch (Exception ex)
            {
                report(ex);
            }
            Status = BuildStatus.Failed;
        }

        /// <summary>The scene that held the build has gone.</summary>
        public void Reset()
        {
            Status = BuildStatus.NotBuilt;
        }
    }
}
