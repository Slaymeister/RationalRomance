using System.Collections.Generic;
using System.Diagnostics;
using RimWorld;
using Verse;
using Verse.AI;

namespace RationalRomance_Code
{
    public class JobDriver_LeadHookup : JobDriver
    {
        public bool successfulPass = true;

        public bool wasSuccessfulPass => successfulPass;

        private Pawn actor => GetActor();

        private Pawn TargetPawn => TargetThingA as Pawn;

        private Building_Bed TargetBed => TargetThingB as Building_Bed;

        private TargetIndex TargetPawnIndex => TargetIndex.A;

        private TargetIndex TargetBedIndex => TargetIndex.B;

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return true;
        }

        private bool DoesTargetPawnAcceptAdvance()
        {
            return !PawnUtility.WillSoonHaveBasicNeed(TargetPawn) && !PawnUtility.EnemiesAreNearby(TargetPawn) &&
                   TargetPawn.CurJob.def != JobDefOf.LayDown && TargetPawn.CurJob.def != JobDefOf.BeatFire &&
                   TargetPawn.CurJob.def != JobDefOf.Arrest && TargetPawn.CurJob.def != JobDefOf.Capture &&
                   TargetPawn.CurJob.def != JobDefOf.EscortPrisonerToBed &&
                   TargetPawn.CurJob.def != JobDefOf.ExtinguishSelf && TargetPawn.CurJob.def != JobDefOf.FleeAndCower &&
                   TargetPawn.CurJob.def != JobDefOf.MarryAdjacentPawn &&
                   TargetPawn.CurJob.def != JobDefOf.PrisonerExecution &&
                   TargetPawn.CurJob.def != JobDefOf.ReleasePrisoner && TargetPawn.CurJob.def != JobDefOf.Rescue &&
                   TargetPawn.CurJob.def != JobDefOf.SocialFight &&
                   TargetPawn.CurJob.def != JobDefOf.SpectateCeremony &&
                   TargetPawn.CurJob.def != JobDefOf.TakeToBedToOperate &&
                   TargetPawn.CurJob.def != JobDefOf.TakeWoundedPrisonerToBed &&
                   TargetPawn.CurJob.def != JobDefOf.UseCommsConsole && TargetPawn.CurJob.def != JobDefOf.Vomit &&
                   TargetPawn.CurJob.def != JobDefOf.Wait_Downed && SexualityUtilities.WillPawnTryHookup(TargetPawn) &&
                   SexualityUtilities.IsHookupAppealing(TargetPawn, actor);
        }

        private bool IsTargetPawnOkay()
        {
            return !TargetPawn.Dead && !TargetPawn.Downed;
        }

        private bool IsTargetPawnFreeForHookup()
        {
            return !PawnUtility.WillSoonHaveBasicNeed(TargetPawn) && !PawnUtility.EnemiesAreNearby(TargetPawn) &&
                   TargetPawn.CurJob.def != JobDefOf.LayDown && TargetPawn.CurJob.def != JobDefOf.BeatFire &&
                   TargetPawn.CurJob.def != JobDefOf.Arrest && TargetPawn.CurJob.def != JobDefOf.Capture &&
                   TargetPawn.CurJob.def != JobDefOf.EscortPrisonerToBed &&
                   TargetPawn.CurJob.def != JobDefOf.ExtinguishSelf && TargetPawn.CurJob.def != JobDefOf.FleeAndCower &&
                   TargetPawn.CurJob.def != JobDefOf.MarryAdjacentPawn &&
                   TargetPawn.CurJob.def != JobDefOf.PrisonerExecution &&
                   TargetPawn.CurJob.def != JobDefOf.ReleasePrisoner && TargetPawn.CurJob.def != JobDefOf.Rescue &&
                   TargetPawn.CurJob.def != JobDefOf.SocialFight &&
                   TargetPawn.CurJob.def != JobDefOf.SpectateCeremony &&
                   TargetPawn.CurJob.def != JobDefOf.TakeToBedToOperate &&
                   TargetPawn.CurJob.def != JobDefOf.TakeWoundedPrisonerToBed &&
                   TargetPawn.CurJob.def != JobDefOf.UseCommsConsole && TargetPawn.CurJob.def != JobDefOf.Vomit &&
                   TargetPawn.CurJob.def != JobDefOf.Wait_Downed;
        }

        [DebuggerHidden]
        protected override IEnumerable<Toil> MakeNewToils()
        {
            if (!IsTargetPawnFreeForHookup())
            {
                yield break;
            }

            // walk to target pawn
            yield return Toils_Goto.GotoThing(TargetPawnIndex, PathEndMode.Touch);

            var TryItOn = new Toil();
            // make sure target is feeling ok
            TryItOn.AddFailCondition(() => !IsTargetPawnOkay());
            TryItOn.defaultCompleteMode = ToilCompleteMode.Delay;
            // show heart between pawns
            TryItOn.initAction = delegate
            {
                ticksLeftThisToil = 50;
                MoteMaker.ThrowMetaIcon(actor.Position, actor.Map, ThingDefOf.Mote_Heart);
            };
            yield return TryItOn;

            var AwaitResponse = new Toil
            {
                defaultCompleteMode = ToilCompleteMode.Instant,
                initAction = delegate
                {
                    var list = new List<RulePackDef>();
                    successfulPass = DoesTargetPawnAcceptAdvance();
                    if (successfulPass)
                    {
                        MoteMaker.ThrowMetaIcon(TargetPawn.Position, TargetPawn.Map, ThingDefOf.Mote_Heart);
                        list.Add(RRRMiscDefOf.HookupSucceeded);
                    }
                    else
                    {
                        MoteMaker.ThrowMetaIcon(TargetPawn.Position, TargetPawn.Map, ThingDefOf.Mote_IncapIcon);
                        actor.needs.mood.thoughts.memories.TryGainMemory(RRRThoughtDefOf.RebuffedMyHookupAttempt,
                            TargetPawn);
                        TargetPawn.needs.mood.thoughts.memories.TryGainMemory(RRRThoughtDefOf.FailedHookupAttemptOnMe,
                            actor);
                        list.Add(RRRMiscDefOf.HookupFailed);
                    }

                    // add "tried hookup with" to the log
                    Find.PlayLog.Add(new PlayLogEntry_Interaction(RRRMiscDefOf.TriedHookupWith, pawn, TargetPawn,
                        list));
                }
            };
            AwaitResponse.AddFailCondition(() => !wasSuccessfulPass);
            yield return AwaitResponse;

            if (wasSuccessfulPass)
            {
                yield return new Toil
                {
                    defaultCompleteMode = ToilCompleteMode.Instant,
                    initAction = delegate
                    {
                        if (!wasSuccessfulPass)
                        {
                            return;
                        }

                        actor.jobs.jobQueue.EnqueueFirst(new Job(RRRJobDefOf.DoLovinCasual, TargetPawn,
                            TargetBed, TargetBed.GetSleepingSlotPos(0)));
                        TargetPawn.jobs.jobQueue.EnqueueFirst(new Job(RRRJobDefOf.DoLovinCasual, actor,
                            TargetBed, TargetBed.GetSleepingSlotPos(1)));
                        // important for 1.1 that the hookup leader ends their job last. best guess is that it's related to the new garbage collection
                        TargetPawn.jobs.EndCurrentJob(JobCondition.InterruptOptional);
                        actor.jobs.EndCurrentJob(JobCondition.InterruptOptional);
                    }
                };
            }
        }
    }
}