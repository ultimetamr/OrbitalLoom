"""Verify final APK scene contents and preserve bounded delivery evidence."""
from pathlib import Path
import hashlib
import json
import struct
import zipfile
import xml.etree.ElementTree as ET

root = Path(__file__).resolve().parents[1]
campaign = json.loads((root/'Assets/Resources/EnergyBoxes.json').read_text(encoding='utf-8-sig'))
with zipfile.ZipFile(root/'Builds/Backups/Before-Spatial094.zip') as archive:
    before = json.loads(archive.read('Assets/Resources/EnergyBoxes.json').decode('utf-8-sig'))
assert campaign['levels'][:12] == before['levels'][:12]
assert len(campaign['levels']) == 24
assert [i+1 for i, level in enumerate(campaign['levels']) if level.get('spatial')] == [15, 18, 23]
assert [level['id'] for level in campaign['levels']] == [level['id'] for level in before['levels']]
apk = root/'Builds/OrbitalLoom-MR.apk'
with zipfile.ZipFile(apk) as archive:
    name = next(n for n in archive.namelist() if n.endswith('OrbitalLoom.unity.glb'))
    blob = archive.read(name)
    length, kind = struct.unpack_from('<II', blob, 12)
    assert kind == 0x4E4F534A
    glb = json.loads(blob[20:20+length].decode('utf-8').rstrip('\x00 '))
    names = [node.get('name', '') for node in glb['nodes']]
    required = ['Playable spatial mechanism', 'Committed spatial geometry',
                'Reflecting crystal plane', 'Front depth obstruction',
                'Physical rail', 'Metric preview photon', 'Restored altar']
    present = {name: any(name in node for node in names) for name in required}
    assert all(present.values()), present
    assert not any('Ring HUD surfaces' in node for node in names)
tests = ET.parse(root/'Builds/EditModeTests.xml').getroot()
assert tests.get('failed') == '0' and tests.get('passed') == tests.get('total')
assert int(tests.get('passed')) >= 114
source_folders = ['Assets/Scripts', 'Assets/Editor', 'Assets/Tests', 'Assets/Resources']
hashes = {str(p.relative_to(root)).replace('\\', '/'): hashlib.sha256(p.read_bytes()).hexdigest()
          for folder in source_folders for p in (root/folder).rglob('*')
          if p.is_file() and p.suffix in ['.cs', '.json']}
evidence = dict(version='0.9.4', versionCode=21,
    apkBytes=apk.stat().st_size, apkSHA256=hashlib.sha256(apk.read_bytes()).hexdigest().upper(),
    sceneSHA256=hashlib.sha256((root/'Assets/Scenes/OrbitalLoom.unity').read_bytes()).hexdigest().upper(),
    firstTwelveUnchanged=True, progressIdsUnchanged=True, levels=24, spatialSamples=[15,18,23],
    tests={key:tests.get(key) for key in ['total','passed','failed','skipped']},
    exportedNodes=len(names), exportedMechanisms=present, sourceHashes=hashes,
    sdkChanged=False, limitations=['Headset playtest and sustained performance pending',
        'Single active Unity SpatialCamera: system multi-window criterion not met',
        'Development APK; existing startup SDK warnings still require release work'])
(root/'Builds/Spatial094-ArtifactEvidence.json').write_text(json.dumps(evidence, ensure_ascii=False, indent=2), encoding='utf-8')
print(json.dumps({key: value for key, value in evidence.items() if key != 'sourceHashes'}, ensure_ascii=False, indent=2))
