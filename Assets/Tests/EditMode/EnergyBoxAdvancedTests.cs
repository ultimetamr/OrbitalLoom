using System;
using System.Linq;
using NUnit.Framework;
using OrbitalLoom.Gameplay;
using UnityEngine;

namespace OrbitalLoom.Tests
{
    public sealed class EnergyBoxAdvancedTests
    {
        private static EnergyBoxCampaign Campaign()=>EnergyBoxCampaign.Parse(Resources.Load<TextAsset>("EnergyBoxes").text);
        private static EnergyBoxLevel Level(int i)=>Campaign().levels[i];
        [Test] public void TwentyFourLevelsHaveUniqueIdsAndReferences()
        {var c=Campaign();Assert.That(c.levels.Length,Is.EqualTo(24));Assert.That(c.levels.Select(l=>l.id).Distinct().Count(),Is.EqualTo(24));}
        [TestCase(6)][TestCase(7)][TestCase(8)][TestCase(9)][TestCase(10)][TestCase(11)]
        public void NewGraphsAreSolvableAndReferencesCertified(int index)
        {
            var l=Level(index);Assert.That(EnergyBoxSolver.Evaluate(l,l.reference).Complete,Is.True);
            var all=EnergyBoxSolver.Solutions(l);Assert.That(all.Count,Is.EqualTo(1),"No unused choice may inflate apparent solutions");
            CollectionAssert.AreEqual(l.reference,all[0]);Debug.Log("[Advanced certificate] "+l.id+": unique "+string.Join(",",all[0]));
        }
        [Test] public void GateRejectsWrongOrExtraRuneAtGateBeforeGoal()
        {
            var l=Level(6);var r=EnergyBoxSolver.Evaluate(l,new[]{1});Assert.That(r.FirstFailure,Is.EqualTo("door"));Assert.That(r.incoming["door"],Is.EqualTo("星"));
            Assert.That(r.received.ContainsKey("g"),Is.False);Assert.That(r.Message,Does.Contain("需 月").And.Contain("实收 星"));
            l.nodes.First(n=>n.id=="s").outputs=new[]{"extra"};l.nodes=l.nodes.Concat(new[]{new BoxNode{id="extra",kind="relay",title="额外印",rune="日",outputs=new[]{"a"}}}).ToArray();l.Validate();
            Assert.That(EnergyBoxSolver.Evaluate(l,l.reference).failures.ContainsKey("door"),Is.True);
        }
        [Test] public void ConverterReplacesLastRuneAndPreservesPrefixAndEnergy()
        {
            var l=Level(8);var r=EnergyBoxSolver.Evaluate(l,l.reference);Assert.That(r.signatures["g"],Is.EqualTo("星"));Assert.That(r.received["g"],Is.EqualTo(1));
            l.nodes.First(n=>n.id=="s").outputs=new[]{"prefix"};l.nodes=l.nodes.Concat(new[]{new BoxNode{id="prefix",kind="relay",title="前印",rune="日",outputs=new[]{"rm"}}}).ToArray();l.nodes.First(n=>n.id=="g").code="日星";l.Validate();
            r=EnergyBoxSolver.Evaluate(l,l.reference);Assert.That(r.Complete,Is.True);Assert.That(r.signatures["cv"],Is.EqualTo("日星"));
        }
        [Test] public void ConverterRequiresMatchingLastRune()
        {var l=Level(8);l.nodes.First(n=>n.id=="rm").rune="日";var r=EnergyBoxSolver.Evaluate(l,l.reference);Assert.That(r.FirstFailure,Is.EqualTo("cv"));Assert.That(r.Message,Does.Contain("末印需 月"));}
        [Test] public void LinkedEditsAreBidirectionalAtomicUndoTransactions()
        {
            var s=new EnergyBoxSession(Level(10));Assert.That(s.Set(1,2),Is.True);CollectionAssert.AreEqual(new[]{0,2,1},s.Snapshot());Assert.That(s.Moves,Is.EqualTo(1));
            s.Undo();CollectionAssert.AreEqual(new[]{0,0,0},s.Snapshot());Assert.That(s.Set(2,2),Is.True);CollectionAssert.AreEqual(new[]{0,3,2},s.Snapshot());
            s.Select(1);s.Remove();CollectionAssert.AreEqual(new[]{0,0,0},s.Snapshot());s.Undo();CollectionAssert.AreEqual(new[]{0,3,2},s.Snapshot());
        }
        [Test] public void LinkedInventoryFailureLeavesBothSocketsUntouched()
        {var l=Level(10);l.turns=1;var s=new EnergyBoxSession(l);Assert.That(s.Set(1,1),Is.False);Assert.That(s.Used("turn"),Is.Zero);Assert.That(s.UndoAvailable,Is.Zero);}
        [Test] public void InvalidLinkedStateAndMalformedMechanicMetadataRejected()
        {
            var l=Level(10);Assert.That(EnergyBoxSolver.Evaluate(l,new[]{1,2,2}).Complete,Is.False);
            l.links[0].map=new[]{0,1,1,2};Assert.Throws<FormatException>(()=>l.Validate());
            l=Level(8);l.nodes.First(n=>n.id=="cv").swapTo="";Assert.Throws<FormatException>(()=>l.Validate());
        }
        [Test] public void HintsUseNearestValidLayoutAndNeverEditIt()
        {
            var s=new EnergyBoxSession(Level(10));s.Set(0,1);s.Set(1,1);var original=s.Snapshot();
            s.Hint();Assert.That(s.Message,Does.Contain("规则"));s.Hint();Assert.That(s.HintTarget,Is.EqualTo("b"));s.Hint();Assert.That(s.Message,Does.Contain("B：月印"));CollectionAssert.AreEqual(original,s.Snapshot());
            s.Hint();Assert.That(s.Hints,Is.EqualTo(3));
        }
        [TestCase(6)][TestCase(8)][TestCase(10)]
        public void NewTutorialRequiresReadingThenEditingThenSuccessfulTest(int index)
        {
            var l=Level(index);var s=new EnergyBoxSession(l,true);s.BeginTutorial();Assert.That(s.Tutorial.Stage,Is.EqualTo(11));
            if(l.tutorial=="link")s.Select(Array.FindIndex(s.Sockets,n=>n.id==l.tutorialNode));else s.Examine(l.tutorialNode);
            Assert.That(s.Tutorial.Stage,Is.Not.EqualTo(11));
            for(int i=0;i<l.reference.Length;i++){s.Select(i);s.Set(i,l.reference[i]);}s.Test();s.Tick(100);Assert.That(s.Solved,Is.True);Assert.That(s.Tutorial.Completed,Is.True);
        }
        [Test] public void GoalReportAndFailureFocusExplainExpectedVersusActual()
        {var s=new EnergyBoxSession(Level(6));s.Set(0,1);s.Test();s.Tick(100);s.FocusFailure();Assert.That(s.HintTarget,Is.EqualTo("door"));Assert.That(s.GoalReport(s.Level.nodes.First(n=>n.kind=="goal")),Does.Contain("需要").And.Contain("未抵达"));}
        [Test] public void VersionOneSixLevelFilesStillParse()
        {var c=Campaign();c.version=1;c.levels=c.levels.Take(6).ToArray();Assert.That(EnergyBoxCampaign.Parse(JsonUtility.ToJson(c)).levels.Length,Is.EqualTo(6));}
        [Test] public void StoppedOrRunningTestDoesNotRevealUnplayedDiagnosis()
        {
            var s=new EnergyBoxSession(Level(6));s.Set(0,1);s.Test();
            var goal=s.Level.nodes.First(n=>n.kind=="goal");
            Assert.That(s.GoalReport(goal),Does.Contain("星光仍在传递"));
            s.Stop();s.FocusFailure();Assert.That(s.HintTarget,Is.Null);
            Assert.That(s.GoalReport(goal),Does.Contain("已停止"));
            s.Test();s.Tick(100);Assert.That(s.TestFinished,Is.True);
            Assert.That(s.GoalReport(goal),Does.Contain("未抵达"));
        }
    }
}
