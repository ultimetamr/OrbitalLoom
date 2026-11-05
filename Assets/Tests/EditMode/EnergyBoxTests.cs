using System;
using System.Linq;
using NUnit.Framework;
using OrbitalLoom.Gameplay;
using UnityEngine;

namespace OrbitalLoom.Tests
{
    public sealed class EnergyBoxTests
    {
        private static EnergyBoxCampaign Campaign()=>EnergyBoxCampaign.Parse(Resources.Load<TextAsset>("EnergyBoxes").text);
        [TestCase(0)][TestCase(1)][TestCase(2)][TestCase(3)][TestCase(4)][TestCase(5)]
        public void AuthoredReferencePreciselyPowersEveryGoal(int index)
        {
            var level=Campaign().levels[index];var result=EnergyBoxSolver.Evaluate(level,level.reference);
            Assert.That(result.Complete,Is.True,result.Message);
            foreach(var goal in level.nodes.Where(n=>n.kind=="goal"))Assert.That(result.received[goal.id],Is.EqualTo(goal.units));
        }
        [TestCase(0)][TestCase(1)][TestCase(2)][TestCase(3)][TestCase(4)][TestCase(5)]
        public void ExhaustiveSearchCertifiesSolvabilityAndRejectsMostLayouts(int index)
        {
            var level=Campaign().levels[index];var solutions=EnergyBoxSolver.Solutions(level);
            Assert.That(solutions.Count,Is.GreaterThan(0));
            int total=level.nodes.Where(n=>n.kind=="socket").Aggregate(1,(v,n)=>v*(n.choices.Length+1));
            Assert.That(solutions.Count,Is.LessThanOrEqualTo(total/2));
            Debug.Log("[EnergyBox certificate] "+level.id+": "+solutions.Count+" / "+total+" layouts accepted: "+string.Join(";",solutions.Select(s=>string.Join(",",s))));
        }
        [Test] public void EmptyLayoutCannotAutoComplete()
        {foreach(var l in Campaign().levels)Assert.That(EnergyBoxSolver.Evaluate(l,new int[l.reference.Length]).Complete,Is.False);}
        [Test] public void WrongSplitPositionOverloadsReceiver()
        {var r=EnergyBoxSolver.Evaluate(Campaign().levels[2],new[]{2,1});Assert.That(r.Complete,Is.False);Assert.That(r.failures.Values.Any(s=>s.Contains("需要")),Is.True);}
        [Test] public void ShortRouteCreatesContention()
        {var r=EnergyBoxSolver.Evaluate(Campaign().levels[3],new[]{1,3,1});Assert.That(r.Complete,Is.False);Assert.That(r.failures.Values.Any(s=>s.Contains("争用")),Is.True);}
        [Test] public void OneUnitCannotSplitAgain()
        {var r=EnergyBoxSolver.Evaluate(Campaign().levels[3],new[]{3,2,1});Assert.That(r.Complete,Is.False);Assert.That(r.failures.Values.Any(s=>s.Contains("1 格")),Is.True);}
        [Test] public void DepthBypassAvoidsSharedJunction()
        {var l=Campaign().levels[4];Assert.That(EnergyBoxSolver.Evaluate(l,new[]{1,1,1}).Complete,Is.False);Assert.That(EnergyBoxSolver.Evaluate(l,l.reference).Complete,Is.True);Assert.That(l.nodes.Max(n=>n.z)-l.nodes.Min(n=>n.z),Is.GreaterThan(.5f));}
        [Test] public void InventoryCannotBeExceededAndRemovalReturnsPiece()
        {
            var s=new EnergyBoxSession(Campaign().levels[4]);Assert.That(s.Set(0,2),Is.True);Assert.That(s.Set(1,1),Is.True);
            Assert.That(s.Set(2,1),Is.False);Assert.That(s.Used("turn"),Is.EqualTo(2));
            Assert.That(s.Set(0,0),Is.True);Assert.That(s.Set(2,1),Is.True);
        }
        [Test] public void EditingDoesNotSolveUntilExplicitTestCompletes()
        {
            var s=new EnergyBoxSession(Campaign().levels[0]);s.Set(0,2);Assert.That(s.Solved,Is.False);
            s.Test();Assert.That(s.Solved,Is.False);Assert.That(s.Set(0,1),Is.False);
            s.Tick(100);Assert.That(s.Solved,Is.True);s.Undo();Assert.That(s.Solved,Is.False);Assert.That(s.Choice(0),Is.Zero);
        }
        [Test] public void PauseStepAndStopRetainLayout()
        {
            var s=new EnergyBoxSession(Campaign().levels[2]);s.Set(0,1);s.Set(1,1);s.Test();s.TogglePause();s.Tick(100);
            Assert.That(s.Elapsed,Is.Zero);s.Step();Assert.That(s.Elapsed,Is.GreaterThan(0));Assert.That(s.Paused,Is.True);
            s.Stop();Assert.That(s.Running,Is.False);Assert.That(s.Solved,Is.False);CollectionAssert.AreEqual(new[]{1,1},s.Snapshot());
            s.Test();s.Tick(100);Assert.That(s.Solved,Is.True);
        }
        [Test] public void HintsAreLimitedAndResetRestoresInventory()
        {
            var s=new EnergyBoxSession(Campaign().levels[0]);for(int i=0;i<8;i++)s.Hint();Assert.That(s.Hints,Is.EqualTo(3));
            s.Set(0,2);s.Test();s.Tick(100);s.Reset();Assert.That(s.Hints,Is.Zero);Assert.That(s.Used("turn"),Is.Zero);Assert.That(s.Result,Is.Null);
        }
        [Test] public void MalformedReferencesAndNonfiniteCoordinatesAreRejected()
        {
            var l=Campaign().levels[0];l.nodes[0].x=float.NaN;Assert.Throws<FormatException>(()=>l.Validate());
            l=Campaign().levels[0];l.nodes[0].outputs=new[]{"missing"};Assert.Throws<FormatException>(()=>l.Validate());
            Assert.Throws<FormatException>(()=>EnergyBoxCampaign.Parse("{broken"));
            l=Campaign().levels[0];l.reference[0]=99;Assert.Throws<FormatException>(()=>l.Validate());
        }
        [Test] public void LoopIsRejectedWithoutHanging()
        {var l=Campaign().levels[0];l.nodes.First(n=>n.id=="a").choices[0].outputs=new[]{"s"};l.Validate();var r=EnergyBoxSolver.Evaluate(l,new[]{1});Assert.That(r.Complete,Is.False);Assert.That(r.failures.Values.Any(s=>s.Contains("循环")),Is.True);}
    }
}
