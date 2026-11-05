using System;
using System.Linq;
using NUnit.Framework;
using OrbitalLoom.Gameplay;
using UnityEngine;
namespace OrbitalLoom.Tests
{
 public sealed class EnergyBoxTutorialTests
 {
  static EnergyBoxLevel Level(int i)=>EnergyBoxCampaign.Parse(Resources.Load<TextAsset>("EnergyBoxes").text).levels[i];
  [Test] public void TeachingRequiresCommittedActionsAndSuccessfulTest()
  {
   var s=new EnergyBoxSession(Level(0),true);s.Select(0);Assert.That(s.Selected,Is.EqualTo(-1));
   s.Tick(100);Assert.That(s.Tutorial.Stage,Is.Zero);s.BeginTutorial();Assert.That(s.Tutorial.Stage,Is.EqualTo(1));
   s.Select(0);Assert.That(s.Tutorial.Stage,Is.EqualTo(2));s.Install("turn");Assert.That(s.Tutorial.Stage,Is.EqualTo(3));
   s.Rotate(1);Assert.That(s.Tutorial.Stage,Is.EqualTo(4));s.Test();Assert.That(s.Tutorial.Stage,Is.EqualTo(5));
   s.TogglePause();s.Tick(100);Assert.That(s.Tutorial.Completed,Is.False);s.Stop();Assert.That(s.Tutorial.Stage,Is.EqualTo(4));
   s.Test();s.Tick(100);Assert.That(s.Tutorial.Completed,Is.True);
  }
  [Test] public void FailedTestCanRecoverThroughUndoAndRestart()
  {
   var s=new EnergyBoxSession(Level(0),true);s.BeginTutorial();s.Select(0);s.Install("turn");s.Test();s.Tick(100);
   Assert.That(s.Tutorial.Stage,Is.EqualTo(6));s.Undo();Assert.That(s.Tutorial.Stage,Is.EqualTo(2));s.Reset();Assert.That(s.Tutorial.Stage,Is.EqualTo(1));
   s.SkipTutorial();Assert.That(s.Tutorial.Dismissed,Is.True);Assert.That(s.Tutorial.Allows("test"),Is.True);
   Assert.That(new EnergyBoxSession(Level(0),true).Tutorial.Active,Is.True);
  }
  [Test] public void SplitTeachingDoesNotDemandAnUnavailableRotation()
  {var s=new EnergyBoxSession(Level(1),true);s.BeginTutorial();s.Select(0);s.Install("split");Assert.That(s.Tutorial.Stage,Is.EqualTo(4));s.Test();s.Tick(100);Assert.That(s.Tutorial.Completed,Is.True);}
  [Test] public void RuneTeachingRequiresReadingThenActuallySolving()
  {
   var s=new EnergyBoxSession(Level(2),true);s.BeginTutorial();s.Examine("rs");Assert.That(s.Tutorial.Stage,Is.EqualTo(8));
   s.Examine("g1");Assert.That(s.Tutorial.Stage,Is.EqualTo(9));s.Examine("rs");Assert.That(s.Tutorial.Completed,Is.False);
   Assert.That(s.Tutorial.Stage,Is.EqualTo(12));Assert.That(s.Tutorial.Allows("split"),Is.True);
   for(int i=0;i<s.Sockets.Length;i++){s.Select(i);s.Set(i,s.Level.reference[i]);}
   s.Test();s.Tick(100);Assert.That(s.Tutorial.Completed,Is.True);
  }
  [TestCase(2,1,2,0,0,0)][TestCase(3,1,4,1,0,0)][TestCase(4,1,3,1,0,0)][TestCase(5,1,3,2,1,1)][TestCase(5,1,1,2,3,1)]
  public void EnergyCorrectDecoyFailsOnlyBecauseOfRuneOrder(int index,int a,int b,int c,int d,int e)
  {
   var l=Level(index);int[] state=new[]{a,b,c,d,e}.Take(l.reference.Length).ToArray();
   var r=EnergyBoxSolver.Evaluate(l,state);Assert.That(r.Complete,Is.False);Assert.That(r.failures.Values.Any(x=>x.Contains("锁印")),Is.True,r.Message);
   foreach(var g in l.nodes.Where(n=>n.kind=="goal"))Assert.That(r.received[g.id],Is.EqualTo(g.units));
   foreach(var n in l.nodes)n.code="";Assert.That(EnergyBoxSolver.Evaluate(l,state).Complete,Is.True,"Decoy must satisfy all energy rules");
  }
  [Test] public void SplitCopiesEarlierRunesAndExtraRuneCannotOpenLock()
  {
   var l=Level(1);var s=l.nodes.First(n=>n.id=="s");s.outputs=new[]{"r"};
   l.nodes=l.nodes.Concat(new[]{new BoxNode{id="r",kind="relay",title="月印",rune="月",outputs=new[]{"a"}}}).ToArray();
   foreach(var g in l.nodes.Where(n=>n.kind=="goal"))g.code="月";l.Validate();
   Assert.That(EnergyBoxSolver.Evaluate(l,l.reference).Complete,Is.True);l.nodes.First(n=>n.kind=="goal").code="月星";
   Assert.That(EnergyBoxSolver.Evaluate(l,l.reference).Complete,Is.False);
  }
  [Test] public void MalformedRuneMetadataIsRejected()
  {var l=Level(2);l.nodes.First(n=>n.id=="rm").rune="火";Assert.Throws<FormatException>(()=>l.Validate());l=Level(2);l.nodes.First(n=>n.kind=="source").code="月";Assert.Throws<FormatException>(()=>l.Validate());}
 }
}
