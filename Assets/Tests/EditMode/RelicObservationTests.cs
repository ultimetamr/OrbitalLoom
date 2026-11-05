using System;
using System.Linq;
using NUnit.Framework;
using OrbitalLoom.Gameplay;
using UnityEngine;

namespace OrbitalLoom.Tests
{
    public sealed class RelicObservationTests
    {
        private static EnergyBoxLevel Level(int i)=>EnergyBoxCampaign.Parse(Resources.Load<TextAsset>("EnergyBoxes").text).levels[i];
        [TestCase(14,3)][TestCase(17,12)][TestCase(22,-3)]
        public void PhysicalObservationRequiresTheCorrectFaceBeforeRecording(int level,int turns)
        {
            var session=new EnergyBoxSession(Level(level));var observation=session.Observation;
            Assert.That(observation.Record(),Is.False);Assert.That(observation.Found,Is.False);
            for(int i=0;i<Math.Abs(turns);i++)session.RotateRelic(Math.Sign(turns));
            Assert.That(observation.Aligned,Is.True);session.RecordRelic();
            Assert.That(observation.Found,Is.True);Assert.That(session.Message,Does.Contain(observation.Clue.discovery));
            Assert.That(session.Moves,Is.Zero);Assert.That(session.UndoAvailable,Is.Zero);Assert.That(session.Attempts,Is.Zero);
        }
        [TestCase(14)][TestCase(17)][TestCase(22)]
        public void TextAccessGivesTheSameEvidenceWithoutChangingAnswer(int level)
        {
            var session=new EnergyBoxSession(Level(level));session.RecordRelic(true);
            Assert.That(session.Observation.UsedTextAccess,Is.True);Assert.That(session.Hints,Is.Zero);
            foreach(int i in Enumerable.Range(0,session.Sockets.Length))session.Set(i,session.Level.reference[i]);
            session.Test();session.Tick(100);Assert.That(session.Solved,Is.True);
            Assert.That(session.Observation.Found,Is.True);session.Undo();Assert.That(session.Observation.Found,Is.True);
        }
        [Test] public void BackFaceUsesCircularAnglesAndClueCanBeReread()
        {
            var observation=new RelicObservation(Level(17).relic);
            for(int i=0;i<12;i++)observation.Rotate(-1);
            Assert.That(observation.Aligned,Is.True);Assert.That(observation.Record(),Is.True);
            observation.Rotate(1);Assert.That(observation.Aligned,Is.False);Assert.That(observation.Found,Is.True);
        }
        [Test] public void ObservationIsAnAidAndCannotBlockTheEnergySolver()
        {
            var session=new EnergyBoxSession(Level(14));
            for(int i=0;i<session.Sockets.Length;i++)session.Set(i,session.Level.reference[i]);session.Test();session.Tick(100);
            Assert.That(session.Solved,Is.True);Assert.That(session.Observation.Found,Is.False);
        }
        [Test] public void ThreeHintsProgressFromObservationToRelationToConcreteAction()
        {
            var session=new EnergyBoxSession(Level(22));var original=session.Snapshot();
            session.Hint();Assert.That(session.Message,Does.Contain("观察"));
            session.Hint();Assert.That(session.Message,Does.Contain("关系").And.Contain("合计4"));
            session.Hint();Assert.That(session.Message,Does.Contain("下一步"));
            CollectionAssert.AreEqual(original,session.Snapshot());Assert.That(session.Hints,Is.EqualTo(3));
        }
        [Test] public void FailureExplainsActualConditionOnlyAfterCompletedPlayback()
        {
            var session=new EnergyBoxSession(Level(6));session.Set(0,1);session.Test();
            Assert.That(session.FailureDetail,Does.Contain("完整运行"));session.Tick(100);
            Assert.That(session.FailureDetail,Does.Contain("要求 月").And.Contain("实际 星"));
            Assert.That(session.HintTarget,Is.EqualTo("door"));
        }
        [Test] public void InvalidCluesAreRejectedAndFirstTwelveHaveNoNewRequirements()
        {
            for(int i=0;i<12;i++)Assert.That(Level(i).relic,Is.Null);
            var clue=Level(14).relic;clue.angle=float.NaN;Assert.Throws<FormatException>(()=>clue.Validate());
            clue=Level(14).relic;clue.kind="invented";Assert.Throws<FormatException>(()=>clue.Validate());
        }
        [TestCase(0)][TestCase(14)][TestCase(17)][TestCase(22)][TestCase(23)]
        public void UnityJsonRoundTripKeepsLegacyLevelsAndObservationMetadata(int index)
        {
            var original=Level(index);var copy=JsonUtility.FromJson<EnergyBoxLevel>(JsonUtility.ToJson(original));copy.Validate();
            Assert.That(copy.relic?.kind,Is.EqualTo(original.relic?.kind));
            Assert.That(EnergyBoxSolver.Evaluate(copy,copy.reference).Complete,Is.True);
        }
    }
}
