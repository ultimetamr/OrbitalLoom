using System;
using System.Linq;
using NUnit.Framework;
using OrbitalLoom.Gameplay;
using UnityEngine;
namespace OrbitalLoom.Tests
{
    public sealed class EnergyBoxVarietyTests
    {
        private static EnergyBoxLevel Level(int index)=>EnergyBoxCampaign.Parse(Resources.Load<TextAsset>("EnergyBoxes").text).levels[index];
        [TestCase(12)][TestCase(13)][TestCase(14)][TestCase(15)][TestCase(16)][TestCase(17)]
        [TestCase(18)][TestCase(19)][TestCase(20)][TestCase(21)][TestCase(22)][TestCase(23)]
        public void EveryNewPuzzleHasOneCertifiedSolution(int index)
        {var l=Level(index);l.Validate();var solutions=EnergyBoxSolver.Solutions(l);Assert.That(solutions.Count,Is.EqualTo(1),l.id);CollectionAssert.AreEqual(l.reference,solutions.Single());}
        [Test] public void PrismPreservesEnergyButRejectsSwappedRatio()
        {
            var l=Level(12);var r=EnergyBoxSolver.Evaluate(l,new[]{2});Assert.That(r.Complete,Is.True);Assert.That(r.received["g1"],Is.EqualTo(3));Assert.That(r.received["g2"],Is.EqualTo(1));
            Assert.That(EnergyBoxSolver.Evaluate(l,new[]{3}).Complete,Is.False);
            l.nodes.First(n=>n.id=="a").choices[1].parts=new[]{2,1};Assert.That(EnergyBoxSolver.Evaluate(l,new[]{2}).Message,Does.Contain("不匹配"));
        }
        [Test] public void OddEnergyCannotBeRoundedAwayByEqualSplit()
        {var l=Level(14);var r=EnergyBoxSolver.Evaluate(l,new[]{2,1,1,2,1,3,1});Assert.That(r.Complete,Is.False);Assert.That(r.Message,Does.Contain("不能均分"));Assert.That(r.received.ContainsKey("g1"),Is.False);}
        [Test] public void ReverseOperatesOnWholeSequenceAtItsActualRoutePosition()
        {var l=Level(16);Assert.That(EnergyBoxSolver.Evaluate(l,new[]{1}).signatures["g"],Is.EqualTo("日星月"));Assert.That(EnergyBoxSolver.Evaluate(l,new[]{2}).signatures["g"],Is.EqualTo("星月日"));}
        [Test] public void ErasingAnEmptySequenceStopsAtTheDevice()
        {var l=Level(18);l.nodes.First(n=>n.id=="m").rune="";l.nodes.First(n=>n.id=="s1").rune="";var r=EnergyBoxSolver.Evaluate(l,l.reference);Assert.That(r.FirstFailure,Is.EqualTo("er"));Assert.That(r.received.ContainsKey("g"),Is.False);}
        [Test] public void ReverseThenEraseCanRemoveOriginalFirstRune()
        {var l=Level(19);var r=EnergyBoxSolver.Evaluate(l,l.reference);Assert.That(r.signatures["rv"],Is.EqualTo("星月"));Assert.That(r.signatures["er2"],Is.EqualTo("星"));Assert.That(r.Complete,Is.True);}
        [Test] public void SynchronyRequiresAllGoalsOnOneBeatAndShowsArrivalReport()
        {
            var l=Level(21);var r=EnergyBoxSolver.Evaluate(l,new[]{1,1});Assert.That(r.received["g1"],Is.EqualTo(1));Assert.That(r.received["g2"],Is.EqualTo(1));Assert.That(r.Complete,Is.False);Assert.That(r.Message,Does.Contain("提前 1 拍"));
            r=EnergyBoxSolver.Evaluate(l,l.reference);Assert.That(r.Complete,Is.True);Assert.That(r.arrivals["g1"],Is.EqualTo(r.arrivals["g2"]));
            var s=new EnergyBoxSession(l);for(int i=0;i<l.reference.Length;i++)s.Set(i,l.reference[i]);s.Test();Assert.That(s.GoalReport(l.nodes.First(n=>n.id=="g1")),Does.Not.Contain("第 "));s.Tick(100);Assert.That(s.GoalReport(l.nodes.First(n=>n.id=="g1")),Does.Contain("拍"));
        }
        [Test] public void BeatTimeIsUnaffectedByCosmeticNodePositions()
        {var l=Level(21);var before=EnergyBoxSolver.Evaluate(l,l.reference);foreach(var n in l.nodes)n.z=-n.z;var after=EnergyBoxSolver.Evaluate(l,l.reference);Assert.That(after.Complete,Is.True);Assert.That(after.arrivals["g1"],Is.EqualTo(before.arrivals["g1"]));}
        [TestCase(12)][TestCase(15)][TestCase(18)][TestCase(21)]
        public void NewRulesHaveReadEditTestTeaching(int index)
        {
            var l=Level(index);var s=new EnergyBoxSession(l,true);s.BeginTutorial();Assert.That(s.Tutorial.Stage,Is.EqualTo(11));
            var socket=Array.FindIndex(s.Sockets,n=>n.id==l.tutorialNode);if(socket>=0)s.Select(socket);else s.Examine(l.tutorialNode);
            Assert.That(s.Tutorial.Stage,Is.Not.EqualTo(11));
            for(int i=0;i<l.reference.Length;i++){s.Select(i);s.Set(i,l.reference[i]);}Assert.That(s.Tutorial.Completed,Is.False);s.Test();s.Tick(100);Assert.That(s.Tutorial.Completed,Is.True);
        }
        [Test] public void NewMechanicMetadataRoundTripsThroughJson()
        {var l=Level(23);var copy=JsonUtility.FromJson<EnergyBoxLevel>(JsonUtility.ToJson(l));copy.Validate();Assert.That(copy.syncWindow,Is.EqualTo(.1f));Assert.That(EnergyBoxSolver.Evaluate(copy,copy.reference).Complete,Is.True);}
    }
}
