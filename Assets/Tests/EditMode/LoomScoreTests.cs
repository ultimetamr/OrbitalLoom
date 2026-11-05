using System;
using System.Linq;
using NUnit.Framework;
using OrbitalLoom.Audio;
using OrbitalLoom.Core;
using OrbitalLoom.Gameplay;
using UnityEngine;
namespace OrbitalLoom.Tests
{
    public sealed class LoomScoreTests
    {
        private static EnergyBoxLevel Level(int index)=>EnergyBoxCampaign.Parse(Resources.Load<TextAsset>("EnergyBoxes").text).levels[index];
        [Test] public void TimingSolutionProducesThreeDistinctNotesAtOneExactBeat()
        {
            var l=Level(22);var r=EnergyBoxSolver.Evaluate(l,l.reference);Assert.That(r.Complete,Is.True);
            var cues=LoomScore.Build(l,r).Where(c=>c.clip.StartsWith("goal-")).ToArray();
            Assert.That(cues.Length,Is.EqualTo(3));Assert.That(cues.Select(c=>c.clip).Distinct().Count(),Is.EqualTo(3));
            Assert.That(cues.All(c=>Math.Abs(c.beat-7)<.001f),Is.True);
        }
        [Test] public void WrongTimingSoundsSeparatedNotesAndDoesNotInventSuccess()
        {
            var l=Level(22);var r=EnergyBoxSolver.Evaluate(l,new[]{1,3,2,3,3});
            Assert.That(l.nodes.Where(n=>n.kind=="goal").All(g=>r.received[g.id]==g.units&&r.signatures[g.id]==g.code),Is.True);
            Assert.That(r.Complete,Is.False);
            var notes=LoomScore.Build(l,r).Where(c=>c.clip.StartsWith("goal-")).ToArray();
            Assert.That(notes.Select(c=>c.beat).Distinct().Count(),Is.GreaterThan(1));
            Assert.That(LoomScore.Build(l,r).Any(c=>c.clip=="win"),Is.False);
        }
        [Test] public void WaitCuesStayInsideActualValveWaitIntervals()
        {
            var l=Level(23);var r=EnergyBoxSolver.Evaluate(l,l.reference);var cues=LoomScore.Build(l,r);
            Assert.That(cues.Count(c=>c.clip=="wait"),Is.EqualTo(5));
            foreach(var cue in cues.Where(c=>c.clip=="wait"))
                Assert.That(l.nodes.Where(n=>n.kind=="socket").Any(n=>r.arrivals.ContainsKey(n.id)&&cue.beat>=r.arrivals[n.id]&&r.traces.Any(t=>t.from==n.id&&cue.beat<t.start)),Is.True);
        }
        [Test] public void FinalPuzzleHasRuneCorrectButRouteAndClockWrongCounterexamples()
        {
            var l=Level(23);var unsynced=EnergyBoxSolver.Evaluate(l,new[]{3,2,1,2,2});
            Assert.That(l.nodes.Where(n=>n.kind=="goal").All(g=>unsynced.received[g.id]==g.units&&unsynced.signatures[g.id]==g.code),Is.True);
            Assert.That(unsynced.Complete,Is.False);Assert.That(unsynced.Message,Does.Contain("提前"));
            // Swapping the processing lanes still produces correct keys, but
            // the asymmetric linked waits no longer align those arrivals.
            var swapped=EnergyBoxSolver.Evaluate(l,new[]{3,2,4,5,6});Assert.That(swapped.Complete,Is.False);
            Assert.That(l.nodes.Where(n=>n.kind=="goal").All(g=>swapped.received[g.id]==g.units&&swapped.signatures[g.id]==g.code),Is.True);
            Assert.That(swapped.Message,Does.Contain("提前"));
        }
        [Test] public void PrismChallengeRequiresLeavingSharedLaneToTheForcedBranch()
        {
            var l=Level(14);Assert.That(l.nodes.Count(n=>n.kind=="socket"),Is.EqualTo(7));
            var r=EnergyBoxSolver.Evaluate(l,new[]{2,3,1,1,2,3,1});Assert.That(r.Complete,Is.False);
            Assert.That(r.Message,Does.Contain("合流"));
        }
        [Test] public void SharedMirrorMustBeUsedBeforeSplitting()
        {
            var l=Level(17);var r=EnergyBoxSolver.Evaluate(l,new[]{1,1,1,1,1,2});Assert.That(r.Complete,Is.False);
            Assert.That(r.failures.Count,Is.GreaterThan(0));
            var valid=EnergyBoxSolver.Evaluate(l,l.reference);Assert.That(valid.incoming["a"],Is.EqualTo("星月"));Assert.That(valid.Complete,Is.True);
        }
        [Test] public void MutingRhythmNeverChangesLogicalSolutionAndSanitizesVolume()
        {
            var l=Level(23);var a=EnergyBoxSolver.Evaluate(l,l.reference);var settings=new AppSettings{rhythmVolume=float.NaN};settings.Sanitize();Assert.That(settings.rhythmVolume,Is.EqualTo(.65f));
            settings.rhythmVolume=0;var b=EnergyBoxSolver.Evaluate(l,l.reference);Assert.That(b.Complete,Is.EqualTo(a.Complete));Assert.That(b.arrivals["g1"],Is.EqualTo(a.arrivals["g1"]));
        }
        [Test] public void InverseWaitRelationIsAvailableAsAClueWithoutTesting()
        {var s=new EnergyBoxSession(Level(22));Assert.That(s.LinkDescription(Array.FindIndex(s.Sockets,n=>n.id=="b")),Does.Contain("相加固定 4"));Assert.That(s.Result,Is.Null);}
        [Test] public void OriginalAudioAssetsAreStereoLoadedAndBounded()
        {
            foreach(string name in new[]{"tick","downbeat","wait","flow","goal-low","goal-mid","goal-high","win","ambient"})
            {var clip=Resources.Load<AudioClip>("LoomAudio/"+name);Assert.That(clip,Is.Not.Null,name);Assert.That(clip.channels,Is.EqualTo(2),name);Assert.That(clip.length,Is.LessThan(6),name);}
            Assert.That(LoomScore.SecondsPerBeat,Is.EqualTo(60.0/84));
        }
    }
}
