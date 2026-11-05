"""Inspect the actual delivery APK and preserve bounded, reproducible evidence."""
from pathlib import Path
import hashlib, json, struct, zipfile
import xml.etree.ElementTree as ET

root = Path(__file__).resolve().parents[1]
campaign = json.loads((root/'Assets/Resources/EnergyBoxes.json').read_text(encoding='utf-8-sig'))
with zipfile.ZipFile(root/'Builds/Backups/Before-Spatial093.zip') as archive:
    name = next(n for n in archive.namelist() if n.replace('\\','/').endswith('Resources/EnergyBoxes.json'))
    before = json.loads(archive.read(name).decode('utf-8-sig'))
assert campaign['levels'][:12] == before['levels'][:12], 'Original twelve changed'
assert len(campaign['levels']) == 24
assert [i+1 for i,l in enumerate(campaign['levels']) if l.get('relic')] == [15,18,23]
apk = root/'Builds/OrbitalLoom-MR.apk'
with zipfile.ZipFile(apk) as archive:
    name = next(n for n in archive.namelist() if n.endswith('OrbitalLoom.unity.glb'))
    blob = archive.read(name)
    length, kind = struct.unpack_from('<II',blob,12)
    assert kind == 0x4E4F534A, 'Missing GLB JSON chunk'
    glb = json.loads(blob[20:20+length].decode('utf-8').rstrip('\x00 '))
    names = [n.get('name','') for n in glb['nodes']]
    required = ['Spatial evidence','Split star strokes','Reverse inscription',
                'Three voice measure','Restored altar','Awakened heart']
    present = {name:any(name in n for n in names) for name in required}
    assert all(present.values()), present
    assert not any('Ring HUD surfaces' in n for n in names)
tests = ET.parse(root/'Builds/EditModeTests.xml').getroot()
assert tests.get('failed') == '0' and tests.get('passed') == tests.get('total')
evidence = dict(version='0.9.3',versionCode=20,
    apkBytes=apk.stat().st_size,apkSHA256=hashlib.sha256(apk.read_bytes()).hexdigest().upper(),
    firstTwelveUnchanged=True,levels=24,samples=[15,18,23],
    tests={k:tests.get(k) for k in ['total','passed','failed','skipped']},
    exportedNodes=len(names),exportedEvidence=present,
    shaderOrSdkUpgrade=False)
(root/'Builds/Spatial093-ArtifactEvidence.json').write_text(json.dumps(evidence,ensure_ascii=False,indent=2),encoding='utf-8')
print(json.dumps(evidence,ensure_ascii=False,indent=2))
