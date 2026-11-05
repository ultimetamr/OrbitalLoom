using NUnit.Framework;
using OrbitalLoom.UI;

namespace OrbitalLoom.Tests
{
    public sealed class SpatialUIReadabilityTests
    {
        [Test] public void EveryButtonStateHasOpaqueBackingAndAtLeastSevenToOneContrast()
        {
            for(int state=0;state<32;state++)
            {
                bool enabled=(state&1)!=0;
                var backing=SpatialUITheme.Fill(enabled,(state&2)!=0,(state&4)!=0,(state&8)!=0,(state&16)!=0);
                Assert.That(backing.a,Is.EqualTo(1),"State "+state);
                Assert.That(SpatialUITheme.Contrast(SpatialUITheme.Foreground(enabled),backing),Is.GreaterThanOrEqualTo(7),"State "+state);
            }
        }
        [Test] public void InstructionsAndSelectionAccentRemainReadableOnPanels()
        {
            Assert.That(SpatialUITheme.Contrast(SpatialUITheme.Secondary,SpatialUITheme.Panel),Is.GreaterThanOrEqualTo(7));
            Assert.That(SpatialUITheme.Contrast(SpatialUITheme.ButtonAccent,SpatialUITheme.Selected),Is.GreaterThanOrEqualTo(4.5f));
        }
    }
}
