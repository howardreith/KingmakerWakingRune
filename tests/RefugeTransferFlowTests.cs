using System;
using System.Collections.Generic;
using KingmakerWakingRune.Sanctum;

namespace KingmakerWakingRune.Tests
{
    /// <summary>Drives the production transfer flow and build gate against a scripted host. No game code runs here.</summary>
    public static class RefugeTransferFlowTests
    {
        private sealed class Host : ITransferHost
        {
            public float Clock;
            public string Refusal;
            public CheckpointProgress Progress = CheckpointProgress.Succeeded;
            public bool Idle;
            public bool ThrowOnStart, ThrowOnCommit, ThrowOnPoll;
            public int Commits, Loads, FadeIns, FadeOuts, Saves;
            public readonly List<string> Warnings = new List<string>();
            public readonly List<string> Stranded = new List<string>();
            public readonly List<string> Abandoned = new List<string>();
            public string TimeoutMessage;

            public float Now => Clock;
            public float FadeSeconds => 0.3f;
            public bool LoadIdle => Idle;
            public bool WindowsClosed() => true;

            public string StartCheckpoint()
            {
                if (ThrowOnStart)
                {
                    throw new InvalidOperationException("start");
                }
                Saves++;
                return Refusal;
            }

            public CheckpointProgress PollCheckpoint(out string failure)
            {
                if (ThrowOnPoll)
                {
                    throw new InvalidOperationException("poll");
                }
                failure = Progress == CheckpointProgress.Failed ? "save failed" : null;
                return Progress;
            }

            public string AbandonCheckpoint(string reason)
            {
                Abandoned.Add(reason);
                return TimeoutMessage;
            }

            public void FadeOut() => FadeOuts++;
            public void LoadDestination() => Loads++;

            public void CommitArrival()
            {
                if (ThrowOnCommit)
                {
                    throw new InvalidOperationException("commit");
                }
                Commits++;
            }

            public void FadeIn() => FadeIns++;
            public void Warn(string text) => Warnings.Add(text);
            public void ReportStranded(string text) => Stranded.Add(text);
            public void Info(string text) { }
            public void Error(string text, Exception exception) { }
        }

        private static void Run(RefugeTransferFlow flow, Host host, float seconds, float step = 0.1f)
        {
            for (float t = 0f; t < seconds; t += step)
            {
                host.Clock += step;
                flow.Tick();
            }
        }

        private static RefugeTransferFlow Departed(Host host)
        {
            var flow = new RefugeTransferFlow(host);
            flow.Begin();
            Run(flow, host, 1.5f);
            Assert.Equal(1, host.Loads, "departure happened");
            return flow;
        }

        private static void AssertNothingSpent(RefugeTransferFlow flow, Host host)
        {
            Assert.Equal(0, host.Commits, "token not spent");
            Assert.Equal(false, flow.Committed, "not committed");
            Assert.Equal(false, flow.InProgress, "not stuck in progress");
        }

        public static void TestSuccessfulTransferCommitsOnce()
        {
            var host = new Host();
            RefugeTransferFlow flow = Departed(host);
            flow.OnArrived(true, false, null);
            flow.OnArrived(true, false, null);
            Assert.Equal(1, host.Commits, "one commit");
            host.Idle = true;
            Run(flow, host, 0.5f);
            Assert.Equal(false, flow.InProgress, "finished");
            Assert.Equal(1, host.FadeIns, "screen restored");
            Assert.Equal(1, host.Saves, "one checkpoint");
        }

        public static void TestRefusedCheckpointNeverDeparts()
        {
            var host = new Host { Refusal = "no saving here" };
            var flow = new RefugeTransferFlow(host);
            flow.Begin();
            Run(flow, host, 1f);
            AssertNothingSpent(flow, host);
            Assert.Equal(0, host.Loads, "no area load");
            Assert.Equal(0, host.FadeOuts, "no fade");
            Assert.Equal("no saving here", host.Warnings[0], "says why");
        }

        public static void TestFailedCheckpointNeverDeparts()
        {
            var host = new Host { Progress = CheckpointProgress.Failed };
            var flow = new RefugeTransferFlow(host);
            flow.Begin();
            Run(flow, host, 1f);
            AssertNothingSpent(flow, host);
            Assert.Equal(0, host.Loads, "no area load");
            Assert.Equal(1, host.Warnings.Count, "tells the player");
        }

        public static void TestUnconfirmedCheckpointTimesOutWithoutDeparting()
        {
            var host = new Host { Progress = CheckpointProgress.Running };
            var flow = new RefugeTransferFlow(host);
            flow.Begin();
            Run(flow, host, RefugeTransferFlow.CheckpointSeconds - 5f, 0.5f);
            Assert.Equal(true, flow.InProgress, "still waiting within the limit");
            Assert.Equal(0, host.Loads, "no load while the save runs");
            Run(flow, host, 10f, 0.5f);
            AssertNothingSpent(flow, host);
            Assert.Equal(0, host.Loads, "no load after timing out");
            Assert.Equal(RefugeTransferFlow.CheckpointUnconfirmed, host.Warnings[0], "explains the timeout");
            Assert.Equal(1, host.Abandoned.Count, "the checkpoint is given up, so a late result cannot lead to travel");
        }

        public static void TestTimeoutSaysWhatTheHostReports()
        {
            var host = new Host { Progress = CheckpointProgress.Running, TimeoutMessage = "the save did not finish in time" };
            var flow = new RefugeTransferFlow(host);
            flow.Begin();
            Run(flow, host, RefugeTransferFlow.CheckpointSeconds + 5f, 0.5f);
            Assert.Equal("the save did not finish in time", host.Warnings[0], "the host's own words");
        }

        /// <summary>WR-PLAYTEST-02: a save another mod holds back is pending, however long it takes within the backstop; it departs once, after it is verified.</summary>
        public static void TestDelayedCheckpointDepartsOnceWhenVerified()
        {
            var host = new Host { Progress = CheckpointProgress.Running };
            var flow = new RefugeTransferFlow(host);
            flow.Begin();
            Run(flow, host, 40f, 0.5f);
            Assert.Equal(0, host.FadeOuts, "the lodge stays while the save is pending");
            Assert.Equal(0, host.Warnings.Count, "a pending save is not a failure");
            host.Progress = CheckpointProgress.Succeeded;
            Run(flow, host, 1.5f);
            Assert.Equal(1, host.Loads, "departs once");
            Assert.Equal(1, host.Saves, "one checkpoint");
            Assert.Equal(0, host.Abandoned.Count, "nothing given up");
        }

        public static void TestExceptionDuringCheckpointRestoresControl()
        {
            var host = new Host { ThrowOnStart = true };
            var flow = new RefugeTransferFlow(host);
            flow.Begin();
            Run(flow, host, 1f);
            AssertNothingSpent(flow, host);
            Assert.Equal(1, host.FadeIns, "screen usable");
            var pollHost = new Host { ThrowOnPoll = true };
            var pollFlow = new RefugeTransferFlow(pollHost);
            pollFlow.Begin();
            Run(pollFlow, pollHost, 1f);
            AssertNothingSpent(pollFlow, pollHost);
        }

        public static void TestPartialDestinationIsNotCommitted()
        {
            var host = new Host();
            RefugeTransferFlow flow = Departed(host);
            flow.OnArrived(false, false, "the sanctum did not finish building");
            Run(flow, host, 0.5f);
            AssertNothingSpent(flow, host);
            Assert.Equal(1, host.Stranded.Count, "recovery offered");
            Assert.Equal(1, host.FadeIns, "not left black");
        }

        public static void TestCommitFailureIsReported()
        {
            var host = new Host { ThrowOnCommit = true };
            RefugeTransferFlow flow = Departed(host);
            flow.OnArrived(true, false, null);
            Run(flow, host, 0.5f);
            Assert.Equal(false, flow.Committed, "no false success");
            Assert.Equal(false, flow.InProgress, "not stuck");
            Assert.Equal(1, host.Stranded.Count, "recovery offered");
        }

        public static void TestArrivalTimeoutRestoresControl()
        {
            var host = new Host();
            RefugeTransferFlow flow = Departed(host);
            Run(flow, host, RefugeTransferFlow.ArrivalSeconds + 1f, 1f);
            AssertNothingSpent(flow, host);
            Assert.Equal(1, host.Stranded.Count, "recovery offered");
            Assert.Equal(1, host.FadeIns, "not left black");
        }

        public static void TestDeadLoadIsNoticedBeforeTheLongTimeout()
        {
            var host = new Host();
            RefugeTransferFlow flow = Departed(host);
            host.Idle = true;
            Run(flow, host, RefugeTransferFlow.DeadLoadSeconds + 1f);
            AssertNothingSpent(flow, host);
            Assert.Equal(1, host.Stranded.Count, "recovery offered");
        }

        public static void TestSaveLoadCancelsAnyStage()
        {
            var host = new Host { Progress = CheckpointProgress.Running };
            var flow = new RefugeTransferFlow(host);
            flow.Begin();
            Run(flow, host, 0.5f);
            flow.Cancel("save load");
            AssertNothingSpent(flow, host);
            Assert.Equal(1, host.FadeIns, "screen restored");
            Assert.Equal("save load", host.Abandoned[0], "the pending checkpoint is given up with the journey");
            flow.OnArrived(true, false, null);
            Assert.Equal(0, host.Commits, "a late arrival does not commit");
        }

        public static void TestLoadingASanctumSaveIsNotAnArrival()
        {
            var host = new Host();
            var flow = new RefugeTransferFlow(host);
            flow.OnArrived(true, true, null);
            Assert.Equal(0, host.Commits, "no transfer was running");
        }

        public static void TestFlowCanRunAgainAfterAFailure()
        {
            var host = new Host { Refusal = "busy" };
            var flow = new RefugeTransferFlow(host);
            flow.Begin();
            Run(flow, host, 1f);
            host.Refusal = null;
            flow.Begin();
            Run(flow, host, 1.5f);
            Assert.Equal(1, host.Loads, "second attempt departs");
        }

        public static void TestBuildPublishesOnlyAfterSuccess()
        {
            var gate = new SanctumBuildGate();
            bool seenDuring = true;
            gate.Ensure(() => { seenDuring = gate.IsReady; return true; }, () => throw new Exception("no cleanup expected"), ex => throw ex);
            Assert.Equal(false, seenDuring, "not ready while building");
            Assert.Equal(BuildStatus.Ready, gate.Status, "ready after");
        }

        public static void TestFailedBuildCleansUpAndCanRetry()
        {
            var gate = new SanctumBuildGate();
            int cleanups = 0, reports = 0;
            gate.Ensure(() => throw new InvalidOperationException("after the root"), () => cleanups++, ex => reports++);
            Assert.Equal(false, gate.IsReady, "failed build is not ready");
            Assert.Equal(BuildStatus.Failed, gate.Status, "clearly failed");
            Assert.Equal(1, cleanups, "partial state removed");
            Assert.Equal(1, reports, "reported");
            gate.Ensure(() => true, () => cleanups++, ex => reports++);
            Assert.Equal(true, gate.IsReady, "valid retry");
            Assert.Equal(1, cleanups, "no second cleanup");
        }

        public static void TestUnreadyScenesLeaveTheGateRetryable()
        {
            var gate = new SanctumBuildGate();
            gate.Ensure(() => false, () => { }, ex => { });
            Assert.Equal(BuildStatus.Failed, gate.Status, "not built");
            int builds = 0;
            gate.Ensure(() => { builds++; return true; }, () => { }, ex => { });
            gate.Ensure(() => { builds++; return true; }, () => { }, ex => { });
            Assert.Equal(1, builds, "built once, then ready");
            gate.Reset();
            Assert.Equal(BuildStatus.NotBuilt, gate.Status, "scene unloaded");
        }

        public static void TestFailedCleanupStillLeavesAFailedGate()
        {
            var gate = new SanctumBuildGate();
            int reports = 0;
            gate.Ensure(() => throw new Exception("build"), () => throw new Exception("cleanup"), ex => reports++);
            Assert.Equal(BuildStatus.Failed, gate.Status, "never ready");
            Assert.Equal(2, reports, "both reported");
        }
    }
}
