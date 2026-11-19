"""Patch the exported application icon, then build with Unity's toolchain."""
from pathlib import Path
import os
import shutil
import subprocess
import sys
import xml.etree.ElementTree as ET

source = Path(__file__).resolve().parents[1]
export = Path(sys.argv[1]).resolve()
player = Path(sys.argv[2]).resolve()
android = 'http://schemas.android.com/apk/res/android'
tools = 'http://schemas.android.com/tools'
ET.register_namespace('android', android)
ET.register_namespace('tools', tools)
icon = export/'launcher/src/main/res/mipmap-nodpi/orbital_loom_launcher.png'
icon.parent.mkdir(parents=True, exist_ok=True)
shutil.copy2(source/'Assets/Brand/OrbitalLoomIcon.png', icon)
manifest = export/'launcher/src/main/AndroidManifest.xml'
tree = ET.parse(manifest)
app = tree.getroot().find('application')
for name in ['icon', 'roundIcon']:
    app.set(f'{{{android}}}{name}', '@mipmap/orbital_loom_launcher')
existing = app.get(f'{{{tools}}}replace', '').split(',')
app.set(f'{{{tools}}}replace', ','.join(dict.fromkeys(x for x in existing+['android:icon'] if x)))
activity = next((a for a in app.findall('activity') if a.get(f'{{{android}}}name') == 'com.unity3d.player.SpatialAdapterPlayerActivity'), None)
if activity is None:
    activity = ET.SubElement(app, 'activity', {f'{{{android}}}name': 'com.unity3d.player.SpatialAdapterPlayerActivity'})
activity.set(f'{{{android}}}icon', '@mipmap/orbital_loom_launcher')
ET.indent(tree, space='  ')
tree.write(manifest, encoding='utf-8', xml_declaration=True)
env = os.environ.copy()
env['JAVA_HOME'] = str(player/'OpenJDK')
env['ANDROID_HOME'] = env['ANDROID_SDK_ROOT'] = str(player/'SDK')
temp = Path(os.environ.get('TEMP', str(export))) / 'OrbitalLoomJavaTemp'
temp.mkdir(exist_ok=True)
env['JAVA_TOOL_OPTIONS'] = '-Djdk.net.unixdomain.tmpdir=' + str(temp)
command = [str(player/'OpenJDK/bin/java.exe'),
    '-Djava.nio.channels.spi.SelectorProvider=sun.nio.ch.WindowsSelectorProvider',
    '-cp', str(player/'Tools/gradle/lib/gradle-launcher-9.3.1.jar'),
    'org.gradle.launcher.GradleMain', '--no-daemon', ':launcher:assembleRelease']
with (source/'Builds/Direct-Gradle.log').open('w', encoding='utf-8') as log:
    result = subprocess.run(command, cwd=export, env=env, stdout=log, stderr=subprocess.STDOUT)
print('Gradle exit code:', result.returncode, flush=True)
if result.returncode == 0:
    built = export/'launcher/build/outputs/apk/release/launcher-release.apk'
    target = source/'Builds/OrbitalLoom-1.0.0-code28.apk'
    shutil.copy2(built, target)
    print('APK:', target, flush=True)
raise SystemExit(result.returncode)
