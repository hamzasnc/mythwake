"""Read-only APK verification; does not connect to an emulator or device."""
import argparse, hashlib, json, struct, subprocess, zipfile
from pathlib import Path

parser = argparse.ArgumentParser()
parser.add_argument('apk', type=Path)
parser.add_argument('--report', required=True, type=Path)
parser.add_argument('--aapt', required=True, type=Path)
args = parser.parse_args()
apk = args.apk.resolve()
with zipfile.ZipFile(apk) as archive:
    assert archive.testzip() is None, 'APK member CRC verification failed'
    native = [name for name in archive.namelist() if name.startswith('lib/') and name.endswith('.so')]
    assert native, 'No native libraries'
    assert all(name.startswith('lib/arm64-v8a/') for name in native), 'Unexpected non-ARM64 library'
    assert 'lib/arm64-v8a/libil2cpp.so' in native, 'IL2CPP library absent'
    machine = {}
    for name in native:
        header = archive.read(name)[:20]
        assert header[:4] == b'\x7fELF' and header[4] == 2 and header[5] == 1, name
        machine[name] = struct.unpack_from('<H', header, 18)[0]
        assert machine[name] == 183, 'ELF is not AArch64: ' + name
    assert 'AndroidManifest.xml' in archive.namelist()
badging = subprocess.run([str(args.aapt), 'dump', 'badging', str(apk)],
                        check=True, capture_output=True, text=True).stdout
assert "package: name='com.xmiepsen.mythwake'" in badging
assert "native-code: 'arm64-v8a'" in badging
args.report.parent.mkdir(parents=True, exist_ok=True)
args.report.with_suffix('.badging.txt').write_text(badging, encoding='utf-8')
result = dict(apk=str(apk), bytes=apk.stat().st_size,
              sha256=hashlib.sha256(apk.read_bytes()).hexdigest(),
              il2cpp=True, abi='arm64-v8a', elf_machines=machine,
              archive='valid', device_test='not performed', emulator_actions='none',
              limit='Build and static package checks do not prove Android rendering or gameplay.')
args.report.write_text(json.dumps(result, indent=2)+'\n', encoding='utf-8')
print(json.dumps(result, indent=2))
