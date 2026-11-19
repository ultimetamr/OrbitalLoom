using System;
using System.Linq;
using NUnit.Framework;
using OrbitalLoom.Gameplay;
using OrbitalLoom.Interaction;
using UnityEngine;

namespace OrbitalLoom.Tests
{
    public sealed class DirectPuzzleTests
    {
        private static EnergyBoxLevel Level(int index) => EnergyBoxCampaign.Parse(Resources.Load<TextAsset>("EnergyBoxes").text).levels[index];
        [Test] public void DirectActionsRespectTeachingAndUseOneUndoEntryPerCommit()
        {
            var session = new EnergyBoxSession(Level(0), true);
            Assert.That(DirectPuzzleActions.Install(session, 0, "turn"), Is.False);
            session.BeginTutorial();session.Select(0);
            Assert.That(session.Tutorial.Piece, Is.EqualTo("turn"));
            Assert.That(DirectPuzzleActions.Install(session, 0, "turn"), Is.True);
            Assert.That(session.UndoAvailable, Is.EqualTo(1));
            int next = DirectPuzzleActions.RotateChoice(session, 0, session.Choice(0), 1);
            Assert.That(DirectPuzzleActions.Rotate(session, 0, next), Is.True);
            Assert.That(session.UndoAvailable, Is.EqualTo(2));
            Assert.That(session.Tutorial.Button, Is.EqualTo("开始"));
            session.Test();session.Tick(100);Assert.That(session.Solved, Is.True);
            Assert.That(DirectPuzzleActions.Install(session, 0, "turn"), Is.False);
        }
        [Test] public void MagneticSnapRejectsDistantAndInvalidPositions()
        {
            var session = new EnergyBoxSession(Level(0));
            Assert.That(DirectPuzzleActions.NearestSocket(session, session.Sockets[0].Position, .13f), Is.EqualTo(0));
            Assert.That(DirectPuzzleActions.NearestSocket(session, Vector3.one*100, .13f), Is.EqualTo(-1));
            Assert.That(DirectPuzzleActions.NearestSocket(session, new Vector3(float.NaN,0,0), .13f), Is.EqualTo(-1));
        }
        [Test] public void DirectPlacementPreservesInventoryAndAtomicLinkedUndo()
        {
            var campaign=EnergyBoxCampaign.Parse(Resources.Load<TextAsset>("EnergyBoxes").text);
            var level=campaign.levels.First(l=>l.links.Length>0);var s=new EnergyBoxSession(level);
            int slot=Array.FindIndex(s.Sockets,n=>n.id==level.links[0].a);
            string kind=s.Sockets[slot].choices[0].kind;
            Assert.That(DirectPuzzleActions.Install(s,slot,kind),Is.True);
            Assert.That(s.UndoAvailable,Is.EqualTo(1));s.Undo();Assert.That(s.Snapshot().All(v=>v==0),Is.True);
        }
        [TestCaseSource(nameof(AllLevels))]
        public void AllAuthoredSolutionsCanBeReachedWithDirectInstallationAndRotation(int index)
        {
            var s=new EnergyBoxSession(Level(index));
            for(int i=0;i<s.Sockets.Length;i++)
            {
                int wanted=s.Level.reference[i];if(wanted==0)continue;
                if(s.Choice(i)==wanted)continue;
                var kind=s.Sockets[i].choices[wanted-1].kind;
                Assert.That(DirectPuzzleActions.Install(s,i,kind)||s.Installed(i)?.kind==kind,Is.True,s.Level.id);
                int guard=0;
                while(s.Choice(i)!=wanted&&guard++<12)
                {int next=DirectPuzzleActions.RotateChoice(s,i,s.Choice(i),1);Assert.That(DirectPuzzleActions.Rotate(s,i,next),Is.True);}
                Assert.That(s.Choice(i),Is.EqualTo(wanted));
            }
            s.Test();s.Tick(100);Assert.That(s.Solved,Is.True,s.Level.id+": "+s.Message);
        }
        private static int[] AllLevels()=>Enumerable.Range(0,24).ToArray();
    }
}
