"""Check saved delivery against the exact source and prior progress contracts."""
from pathlib import Path
import hashlib
import json
import struct
import zipfile
import xml.etree.ElementTree as ET

root = Path(__file__).resolve().parents[1]
campaign = json.loads((root/'Assets/Resources/EnergyBoxes.json').read_text(encoding='utf-8-sig'))
with zipfile.ZipFile(root/'Builds/Backups/Before-Reasoning095.zip') as archive:
    before = json.loads(archive.read('EnergyBoxes.json').decode('utf-8-sig'))
levels = campaign['levels']
assert len(levels) == 24
assert [l['id'] for l in levels] == [l['id'] for l in before['levels']]
def gameplay(l):
    return {k:v for k,v in l.items() if k not in ('curriculum','objective','question')}
changed = [i+1 for i,(a,b) in enumerate(zip(levels,before['levels'])) if gameplay(a)!=gameplay(b)]
assert changed == [20], changed
assert all(l.get('objective') and l.get('question') and l.get('curriculum') for l in levels)
assert levels[19]['reference'] == [1,2,1]
tests = ET.parse(root/'Builds/EditModeTests.xml').getroot()
assert tests.get('failed') == '0' and int(tests.get('passed')) >= 124
assert tests.get('passed') == tests.get('total')
assert (root/'Builds/EnergyBox-BatchResult.txt').read_text(encoding='utf-8-sig').startswith('PASS')
assert 'Errors=0' in (root/'Builds/BuildReport.txt').read_text(encoding='utf-8-sig')
apk = root/'Builds/OrbitalLoom-MR.apk'
with zipfile.ZipFile(apk) as archive:
    blob = archive.read(next(n for n in archive.namelist() if n.endswith('OrbitalLoom.unity.glb')))
    length,kind = struct.unpack_from('<II',blob,12)
    assert kind == 0x4E4F534A
    scene = json.loads(blob[20:20+length].decode('utf-8').rstrip('\x00 '))
    names = [n.get('name','') for n in scene['nodes']]
    required = ['Engraved pivot bearings','Bronze mirror rim','Carved length ruler','Restored altar']
    assert all(any(q in n for n in names) for q in required)
    assert not any('Ring HUD surfaces' in n for n in names)
hashes = {str(p.relative_to(root)).replace('\\','/'):hashlib.sha256(p.read_bytes()).hexdigest()
          for folder in ('Assets/Scripts','Assets/Editor','Assets/Tests','Assets/Resources')
          for p in (root/folder).rglob('*') if p.suffix in ('.cs','.json')}
evidence = dict(version='0.9.5',versionCode=22,levels=24,gameplayRedesigned=changed,
    firstTwelveGameplayUnchanged=True,progressIdsUnchanged=True,
    tests={key:tests.get(key) for key in ('total','passed','failed','skipped')},
    apkBytes=apk.stat().st_size,apkSHA256=hashlib.sha256(apk.read_bytes()).hexdigest().upper(),
    sceneSHA256=hashlib.sha256((root/'Assets/Scenes/OrbitalLoom.unity').read_bytes()).hexdigest().upper(),
    exportedNodes=len(names),sourceHashes=hashes,sdkChanged=False,
    limitations=['Physical headset test pending','Existing SDK startup warnings not repaired',
                 '72fps/thermal/HRTF not certified','Single active SpatialCamera','Development APK'])
(root/'Builds/Reasoning095-ArtifactEvidence.json').write_text(json.dumps(evidence,ensure_ascii=False,indent=2),encoding='utf-8')
print(json.dumps({k:v for k,v in evidence.items() if k!='sourceHashes'},ensure_ascii=False,indent=2))
