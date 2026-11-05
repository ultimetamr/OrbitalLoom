using System.Linq;
using NUnit.Framework;
using OrbitalLoom.Gameplay;
using UnityEngine;

namespace OrbitalLoom.Tests
{
    public sealed class SequentialTutorialTests
    {
        private static EnergyBoxLevel Level(int index)=>EnergyBoxCampaign.Parse(Resources.Load<TextAsset>("EnergyBoxes").text).levels[index];
        private static void Focus(BoxTutorial guide,string node,string button)
        {
            Assert.That(guide.Target,Is.EqualTo(node));Assert.That(guide.Button,Is.EqualTo(button));
            Assert.That(guide.Target==null||guide.Button==null,Is.True,"Never guide a node and button simultaneously");
        }
        [Test] public void BasicLessonHandsOneFocusToTheNextCommittedAction()
        {
            var s=new EnergyBoxSession(Level(0),true);Focus(s.Tutorial,null,"开始教学");
            s.BeginTutorial();Focus(s.Tutorial,"a",null);s.Select(0);Focus(s.Tutorial,null,"装转向件");
            s.Install("turn");Focus(s.Tutorial,null,"出口 ›");s.Rotate(1);Focus(s.Tutorial,null,"开始");
            s.Test();Focus(s.Tutorial,null,null);s.Tick(100);Focus(s.Tutorial,null,null);
        }
        [Test] public void RuneLessonKeepsAlreadyCorrectExitsAndMovesToNextSocket()
        {
            var s=new EnergyBoxSession(Level(2),true);s.BeginTutorial();Focus(s.Tutorial,"g1",null);
            s.Examine("g1");Focus(s.Tutorial,"rs",null);s.Examine("rs");Focus(s.Tutorial,"a",null);
            s.Select(0);Focus(s.Tutorial,null,"装等分件");s.Install("split");Focus(s.Tutorial,"b",null);
            Assert.That(s.Tutorial.Allows("test"),Is.False);s.Tick(100);Focus(s.Tutorial,"b",null);
            s.Select(1);Focus(s.Tutorial,null,"装等分件");
            s.Install("split");Focus(s.Tutorial,null,"开始");
            Assert.That(s.Snapshot(),Is.EqualTo(s.Level.reference));s.Test();s.Tick(100);Assert.That(s.Tutorial.Completed,Is.True);
        }
        [TestCase(6)][TestCase(8)][TestCase(12)][TestCase(15)][TestCase(18)]
        public void WrongExitGuidesItsActualDestinationBeforeStart(int index)
        {
            var s=new EnergyBoxSession(Level(index),true);s.BeginTutorial();Focus(s.Tutorial,s.Level.tutorialNode,null);
            s.Examine(s.Level.tutorialNode);s.Select(0);
            string kind=s.Sockets[0].choices.First().kind;s.Install(kind);
            Focus(s.Tutorial,null,"出口 ›");Assert.That(s.Tutorial.Allows("test"),Is.False);
            Assert.That(s.Tutorial.Instruction,Does.Contain(s.Sockets[0].choices[s.Level.reference[0]-1].name));
            s.Rotate(1);Focus(s.Tutorial,null,"开始");Assert.That(s.Tutorial.Allows("test"),Is.True);
        }
        [Test] public void RemovalResetAndSkipDoNotLeaveOldGuidance()
        {
            var s=new EnergyBoxSession(Level(2),true);s.BeginTutorial();s.Examine("g1");s.Examine("rs");
            s.Select(0);s.Install("split");s.Rotate(1);Focus(s.Tutorial,null,"出口 ›");s.Remove();Focus(s.Tutorial,null,"装等分件");
            s.Install("split");Focus(s.Tutorial,"b",null);s.Reset();Focus(s.Tutorial,"a",null);
            s.SkipTutorial();Focus(s.Tutorial,null,null);Assert.That(s.Tutorial.Allows("test"),Is.True);
        }
        [Test] public void FailureGuidesOnlyDiagnosisBeforeAdjustingAgain()
        {
            var s=new EnergyBoxSession(Level(0),true);s.BeginTutorial();s.Select(0);s.Install("turn");s.Test();s.Tick(100);
            Focus(s.Tutorial,null,"目标");s.Undo();Focus(s.Tutorial,null,"装转向件");
        }
        [Test] public void SpatialLessonSelectionMustComeFromAnActualClick()
        {
            var s=new EnergyBoxSession(Level(22));Assert.That(s.SpatialControlChosen,Is.False);
            s.SelectSpatial(-1);Assert.That(s.SpatialControlChosen,Is.False);
            s.SelectSpatial(0);Assert.That(s.SpatialControlChosen,Is.True);
            s.Reset();Assert.That(s.SpatialControlChosen,Is.False);
        }
        [TestCase(0)][TestCase(1)][TestCase(2)][TestCase(6)][TestCase(8)]
        [TestCase(10)][TestCase(12)][TestCase(15)][TestCase(18)][TestCase(21)]
        public void FollowingOnlyDisplayedFocusCompletesEveryPracticalTutorial(int index)
        {
            var s=new EnergyBoxSession(Level(index),true);
            for(int step=0;step<48&&s.Tutorial.Active;step++)
            {
                var guide=s.Tutorial;
                Assert.That(guide.Target==null||guide.Button==null,Is.True);
                if(s.Running){s.Tick(100);continue;}
                string target=guide.Target,button=guide.Button;
                if(target!=null)
                {
                    int socket=System.Array.FindIndex(s.Sockets,n=>n.id==target);
                    if(socket>=0)s.Select(socket);else s.Examine(target);
                }
                else if(button=="开始教学")s.BeginTutorial();
                else if(button=="装转向件")s.Install("turn");
                else if(button=="装等分件"||button=="装棱镜")s.Install("split");
                else if(button=="出口 ›")s.Rotate(1);
                else if(button=="开始")s.Test();
                else Assert.Fail("Teaching led to failure or no action: "+s.Level.id+", stage="+guide.Stage+", "+s.Message);
            }
            Assert.That(s.Solved&&s.Tutorial.Completed,Is.True,s.Level.id+": "+s.Message);
            Assert.That(s.Attempts,Is.EqualTo(1),"The taught route must work on its first run");
        }
    }
}
