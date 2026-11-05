using System.IO;
using UnityEditor;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;

namespace OrbitalLoom.Editor
{
    public static class OrbitalLoomTestRunner
    {
        private static TestRunnerApi api;
        [MenuItem("Tools/Orbital Loom/Run EditMode Tests")]
        public static void Run()
        {
            Directory.CreateDirectory("Builds");
            api = ScriptableObject.CreateInstance<TestRunnerApi>();
            api.RegisterCallbacks(new Results());
            api.Execute(new ExecutionSettings(new Filter { testMode = TestMode.EditMode,
                assemblyNames = new[] { "OrbitalLoom.Tests.EditMode" } }) { runSynchronously = true });
        }
        private sealed class Results : ICallbacks
        {
            public void RunStarted(ITestAdaptor tests) { }
            public void TestStarted(ITestAdaptor test) { }
            public void TestFinished(ITestResultAdaptor result) { }
            public void RunFinished(ITestResultAdaptor result)
            {
                File.WriteAllText("Builds/EditModeTests.xml", result.ToXml().OuterXml);
                File.WriteAllText("Library/OrbitalLoom.Task.result", (result.FailCount == 0 && result.PassCount > 0 ? "PASS" : "FAIL") +
                    " tests\nPassed=" + result.PassCount + "\nFailed=" + result.FailCount + "\nSkipped=" + result.SkipCount);
                Debug.Log("[Orbital Loom] Tests: " + result.PassCount + " passed, " + result.FailCount + " failed.");
                api.UnregisterCallbacks(this); Object.DestroyImmediate(api);
            }
        }
    }
}
