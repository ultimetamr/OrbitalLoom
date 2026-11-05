using System;
using System.Linq;
using NUnit.Framework;
using OrbitalLoom.Gameplay;
using UnityEngine;

namespace OrbitalLoom.Tests
{
    public sealed class ReasoningExperienceTests
    {
        private static EnergyBoxLevel Level(int index)=>EnergyBoxCampaign.Parse(Resources.Load<TextAsset>("EnergyBoxes").text).levels[index];
        [Test] public void ReachedCausePrecedesUnpoweredReceiver()
        {
            var l=Level(6);var s=new EnergyBoxSession(l);s.Set(0,1);s.Test();
            Assert.That(s.Diagnosis,Is.Null,"Do not reveal unseen failures during first playback");s.Tick(100);
            Assert.That(s.Diagnosis.Node,Is.EqualTo("door"));Assert.That(s.Diagnosis.Unreached,Is.False);
            Assert.That(s.Diagnosis.Evidence,Does.Contain("星"));Assert.That(s.Diagnosis.Category,Is.EqualTo("符文条件不符"));
            Assert.That(s.Result.failures.Keys.Any(k=>k!=s.Diagnosis.Node),Is.True);
        }
        [Test] public void ReviewIsPausedAtObservedFailureWithoutChangingAttemptOrLayout()
        {
            var s=new EnergyBoxSession(Level(6));s.Set(0,1);s.Test();s.Tick(100);
            int attempts=s.Attempts,moves=s.Moves;var state=s.Snapshot();float time=s.Diagnosis.Time;
            s.ReviewFailure();Assert.That(s.ReviewingFailure&&s.Running&&s.Paused,Is.True);
            Assert.That(s.Elapsed,Is.EqualTo(time));s.Tick(100);Assert.That(s.Elapsed,Is.EqualTo(time));
            Assert.That(s.Set(0,2),Is.False,"Review retains normal playback edit guard");
            Assert.That(s.Attempts,Is.EqualTo(attempts));Assert.That(s.Moves,Is.EqualTo(moves));CollectionAssert.AreEqual(state,s.Snapshot());
            s.Stop();s.Set(0,2);Assert.That(s.Result,Is.Null);Assert.That(s.ReviewingFailure,Is.False);
        }
        [Test] public void ReviewCannotLeakBeforeFinishedOrRunAfterSuccess()
        {
            var s=new EnergyBoxSession(Level(0));s.Set(0,1);s.Test();s.ReviewFailure();Assert.That(s.ReviewingFailure,Is.False);
            s.Tick(100);s.Set(0,2);s.Test();s.Tick(100);s.ReviewFailure();Assert.That(s.ReviewingFailure,Is.False);Assert.That(s.Solved,Is.True);
        }
        [Test] public void DiagnosisUsesObservedTraceAndNeverReferenceAnswer()
        {
            var l=Level(6);var state=new[]{1};var a=EnergyBoxSolver.Evaluate(l,state).Diagnosis;
            l.reference=new[]{1};var b=EnergyBoxSolver.Evaluate(l,state).Diagnosis;
            Assert.That(b.Evidence,Is.EqualTo(a.Evidence));Assert.That(b.Time,Is.EqualTo(a.Time));Assert.That(b.Question,Is.EqualTo(a.Question));
        }
        [Test] public void SynchronyIsDiagnosedOnlyWhenLatestReceiverHasArrived()
        {
            var l=Level(21);var r=EnergyBoxSolver.Evaluate(l,new[]{1,1});
            Assert.That(r.Diagnosis.Category,Is.EqualTo("抵达不同拍"));
            Assert.That(r.Diagnosis.Time,Is.EqualTo(l.nodes.Where(n=>n.kind=="goal").Max(n=>r.arrivals[n.id])));
        }
        [Test] public void MiddleRuneChallengeHasThreeOperationsAndVisibleIntermediateConstraint()
        {
            var l=Level(19);Assert.That(l.reference.Length,Is.EqualTo(3));
            var r=EnergyBoxSolver.Evaluate(l,l.reference);Assert.That(r.Complete,Is.True);
            Assert.That(r.signatures["door"],Is.EqualTo("月星"));Assert.That(r.signatures["rv"],Is.EqualTo("星月"));Assert.That(r.signatures["g"],Is.EqualTo("星"));
            var wrong=(int[])l.reference.Clone();wrong[0]=2;
            r=EnergyBoxSolver.Evaluate(l,wrong);Assert.That(r.FirstFailure,Is.EqualTo("door"));Assert.That(r.received.ContainsKey("g"),Is.False);
            CollectionAssert.AreEqual(l.reference,EnergyBoxSolver.Solutions(l).Single());
        }
        [Test] public void SharedJunctionFailureUsesSecondArrivalTime()
        {
            var l=Level(3);var r=EnergyBoxSolver.Evaluate(l,new[]{1,3,1});
            string junction=r.failures.Keys.First(id=>r.failures[id].Contains("争用"));
            Assert.That(r.failureTimes[junction],Is.GreaterThan(r.arrivals[junction]));
            Assert.That(r.Diagnosis.Node,Is.EqualTo(junction));Assert.That(r.Diagnosis.Time,Is.EqualTo(r.failureTimes[junction]));
        }
        [Test] public void CurriculumCoversAllStableProgressIdsWithoutAnswerChoices()
        {
            var levels=EnergyBoxCampaign.Parse(Resources.Load<TextAsset>("EnergyBoxes").text).levels;
            for(int i=0;i<24;i++)
            {
                Assert.That(levels[i].id,Is.EqualTo("box_"+(i+1).ToString("00")));
                Assert.That(levels[i].objective,Is.Not.Null.And.Not.Empty);Assert.That(levels[i].question,Does.EndWith("？"));
                Assert.That(levels[i].curriculum,Is.Not.Null.And.Not.Empty);
            }
        }
        [Test] public void ReadingAndFailedExperimentDoNotCompleteRuneLesson()
        {
            var s=new EnergyBoxSession(Level(2),true);s.BeginTutorial();s.Examine("g1");s.Examine("rs");
            s.Select(0);s.Install("split");s.Test();s.Tick(100);
            Assert.That(s.Tutorial.Completed,Is.False);Assert.That(s.Tutorial.Active,Is.True);
            Assert.That(s.Tutorial.Allows("rotate"),Is.True);Assert.That(s.Tutorial.Stage,Is.EqualTo(6));
        }
        [Test] public void RuneTeachingHighlightsTheActualSelectedSocketsAvailablePiece()
        {
            var l=Level(2);var b=l.nodes.First(n=>n.id=="b");b.choices=b.choices.Where(c=>c.kind=="turn").ToArray();l.reference[1]=1;
            var s=new EnergyBoxSession(l,true);s.BeginTutorial();s.Examine("g1");s.Examine("rs");
            s.Select(0);Assert.That(s.Tutorial.Button,Is.EqualTo("装等分件"));s.Install("split");s.Select(1);
            Assert.That(s.Tutorial.Target,Is.Null,"Installing focuses only its button");Assert.That(s.Tutorial.Button,Is.EqualTo("装转向件"));
        }
    }
}
