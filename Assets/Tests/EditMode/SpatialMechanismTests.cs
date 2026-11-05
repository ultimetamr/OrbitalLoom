using System;
using System.Linq;
using NUnit.Framework;
using OrbitalLoom.Gameplay;
using UnityEngine;

namespace OrbitalLoom.Tests
{
    public sealed class SpatialMechanismTests
    {
        private static EnergyBoxLevel Level(int i)=>EnergyBoxCampaign.Parse(Resources.Load<TextAsset>("EnergyBoxes").text).levels[i];
        [Test] public void BridgeMustMeetInThreeDimensionsDespiteIdenticalGraphOutputs()
        {
            var l=Level(14);var good=(int[])l.reference.Clone();
            Assert.That(EnergyBoxSolver.Evaluate(l,good).Complete,Is.True);
            var bad=(int[])good.Clone();bad[5]=2;
            var h=l.nodes.First(n=>n.id=="h");CollectionAssert.AreEqual(h.choices[1].outputs,h.choices[2].outputs);
            var result=EnergyBoxSolver.Evaluate(l,bad);
            Assert.That(result.Complete,Is.False);Assert.That(result.FirstFailure,Is.EqualTo("h"));
            Assert.That(result.Message,Does.Contain("cm"));Assert.That(result.received.ContainsKey("g1"),Is.False);
            Assert.That(Vector3.Distance(SpatialMechanism.Tip(0,45),SpatialMechanism.Tip(1,-45)),Is.LessThan(.001f));
        }
        [Test] public void AFrontViewConnectionCanStillBeBlockedInDepth()
        {
            var l=Level(14);var front=(int[])l.reference.Clone();front[5]=front[6]=4;
            Assert.That(Vector3.Distance(SpatialMechanism.Tip(0,135),SpatialMechanism.Tip(1,-135)),Is.LessThan(.001f));
            var result=EnergyBoxSolver.Evaluate(l,front);
            Assert.That(result.Message,Does.Contain("前层暗石"));Assert.That(result.Complete,Is.False);
            Assert.That(EnergyBoxSolver.Evaluate(l,l.reference).Complete,Is.True);
        }
        [Test] public void ReflectionUsesPlaneNormalAndRejectsBackwardRays()
        {
            Assert.That(Vector3.Distance(SpatialMechanism.Reflected(-45),Vector3.forward),Is.LessThan(.001f));
            Assert.That(SpatialMechanism.MirrorMiss(45),Is.GreaterThan(.5f));
            var l=Level(17);var bad=(int[])l.reference.Clone();bad[5]=3;
            Assert.That(EnergyBoxSolver.Evaluate(l,bad).Message,Does.Contain("反射光偏离"));
            Assert.That(EnergyBoxSolver.Evaluate(l,l.reference).Complete,Is.True);
        }
        [Test] public void FoldedPathsChangeActualTravelInsteadOfWaiting()
        {
            var l=Level(22);var r=EnergyBoxSolver.Evaluate(l,l.reference);
            foreach(var id in l.spatial.controls)
            {
                var t=r.traces.Single(t=>t.from==id);Assert.That(t.start,Is.EqualTo(r.arrivals[id]).Within(.001f));
                Assert.That(l.nodes.First(n=>n.id==id).choices.All(c=>c.delay==0),Is.True);
            }
            Assert.That(SpatialMechanism.Length(SpatialMechanism.Path(0,3)),Is.EqualTo(1.2f).Within(.001f));
            Assert.That(r.arrivals.Values.All(v=>!float.IsNaN(v)),Is.True);
            Assert.That(r.arrivals["g1"],Is.EqualTo(7).Within(.001f));
            l.spatial.speed=.6f;Assert.That(EnergyBoxSolver.Evaluate(l,l.reference).Complete,Is.False,"Metric speed, not an authored delay number, controls synchrony");
        }
        [Test] public void SpatialEditsAreAtomicUndoableAndCannotMutateDuringPlayback()
        {
            var s=new EnergyBoxSession(Level(22));s.SelectSpatial(0);s.RotateSpatial(1);
            var installed=s.Snapshot();Assert.That(s.Choice(4),Is.EqualTo(5));
            s.RotateSpatial(1);Assert.That(s.Choice(4),Is.EqualTo(4));s.Undo();CollectionAssert.AreEqual(installed,s.Snapshot());
            s.Test();Assert.That(s.RotateSpatial(1),Is.False);CollectionAssert.AreEqual(installed,s.Snapshot());
            s.Stop();s.Reset();Assert.That(s.SpatialEdits,Is.Zero);Assert.That(s.Snapshot().All(v=>v==0),Is.True);
        }
        [Test] public void CameraInspectionAndReadingNeverSolveOrChangeTheApparatus()
        {
            var s=new EnergyBoxSession(Level(14));var before=s.Snapshot();
            s.RotateRelic(1);s.RecordRelic(true);s.DismissSpatialLesson();
            CollectionAssert.AreEqual(before,s.Snapshot());Assert.That(s.Moves,Is.Zero);Assert.That(s.Solved,Is.False);
            Assert.That(s.SpatialReport,Does.Contain("安装"));
        }
        [Test] public void SpatialTeachingNeedsAnActualEditAndAValidApparatus()
        {
            var s=new EnergyBoxSession(Level(14));Assert.That(s.SpatialLessonCompleted,Is.False);
            s.RecordRelic(true);Assert.That(s.SpatialLessonCompleted,Is.False);
            s.SelectSpatial(0);s.RotateSpatial(1);Assert.That(s.SpatialLessonCompleted,Is.False);
            for(int i=0;i<s.Sockets.Length;i++)s.Set(i,s.Level.reference[i]);Assert.That(s.SpatialLessonCompleted,Is.True);
            s.SelectSpatial(0);s.RotateSpatial(1);Assert.That(s.SpatialLessonCompleted,Is.False);
        }
        [Test] public void PolylinePacketUsesArcLengthAndHasExactEndpoints()
        {
            var p=SpatialMechanism.Path(1,2);Assert.That(SpatialMechanism.Along(p,0),Is.EqualTo(p[0]));
            Assert.That(Vector3.Distance(SpatialMechanism.Along(p,1),p.Last()),Is.LessThan(.0001f));
            Assert.That(Vector3.Distance(SpatialMechanism.Along(p,.5f),new Vector3(0,0,.3f)),Is.LessThan(.0001f));
        }
        [Test] public void MalformedMetricControlsAreRejectedWhileLegacyLevelsStayCompatible()
        {
            var l=Level(14);l.nodes.First(n=>n.id=="h").choices[0].angle=float.NaN;Assert.Throws<FormatException>(()=>l.Validate());
            l=Level(22);l.spatial.controls[0]="missing";Assert.Throws<FormatException>(()=>l.Validate());
            l=Level(22);l.spatial.speed=0;Assert.Throws<FormatException>(()=>l.Validate());
            for(int i=0;i<12;i++)Assert.That(Level(i).spatial,Is.Null);
        }
    }
}
