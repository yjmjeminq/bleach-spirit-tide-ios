from pathlib import Path
import hashlib,importlib.util,json,shutil,subprocess,sys

root=Path(__file__).resolve().parents[1]
statepath=root/'.runtime/ios-build-state.json'
state=json.loads(statepath.read_text())
repo=state['repository'];tag=state['tag']

def gh(*args):
    p=subprocess.run([sys.executable,'-X','utf8',str(root/'Tools/Cloud-GitCredential.py'),*args],cwd=root,capture_output=True,text=True,encoding='utf-8',check=True)
    return p.stdout

def api(path):return json.loads(gh('api',path))
run=api(f'repos/{repo}/actions/runs/{state["runId"]}')
assert run['status']=='completed' and run['conclusion']=='success', 'Mac build has not completed successfully'
assert run['head_sha']==state['revision']
release=api(f'repos/{repo}/releases/tags/{tag}')
name='SpiritTide-iPhone-unsigned-v0.12.0.ipa'
asset=next(a for a in release['assets'] if a['name']==name)
assert asset['state']=='uploaded' and asset['size']>1024*1024
download=root/'.runtime/ios-download-v0.12.0'
assert not download.exists(), 'Inspect an existing download before retrying'
download.mkdir()
for pattern in [name,name+'.sha256','build-report.json','input-verification.json']:
    gh('release','download',tag,'--repo',repo,'--pattern',pattern,'--dir',str(download))
ipa=download/name
sha=hashlib.file_digest(ipa.open('rb'),'sha256').hexdigest()
assert asset['digest']=='sha256:'+sha
assert (download/(name+'.sha256')).read_text().split()[0]==sha
build=json.loads((download/'build-report.json').read_text())
inputs=json.loads((download/'input-verification.json').read_text())
assert build['passed'] and build['ipaCreated'] and build['runURL']==state['runURL']
assert build['sourceRevision']==state['revision'] and build['ipa']['sha256']==sha
assert inputs['passed'] and inputs['archiveSHA256']==state['sourceSHA256']
spec=importlib.util.spec_from_file_location('rift_ios',root/'Tools/IOSBuild.py')
module=importlib.util.module_from_spec(spec);spec.loader.exec_module(module)
verification=module.verify_ipa(ipa,root/'Builds/iOS-Xcode',download/'SpiritTide-ios-package-verification-v0.12.0.json')
assert verification['passed'] and verification['sha256']==sha
assert verification['app']['gameFilesSHA256']==inputs['gameFilesSHA256']==build['app']['gameFilesSHA256']
artifacts=root/'Artifacts'
files={name:ipa,name+'.sha256':download/(name+'.sha256'),
       'SpiritTide-ios-build-report-v0.12.0.json':download/'build-report.json',
       'SpiritTide-ios-input-verification-v0.12.0.json':download/'input-verification.json',
       'SpiritTide-ios-package-verification-v0.12.0.json':download/'SpiritTide-ios-package-verification-v0.12.0.json'}
for filename,source in files.items():
    with source.open('rb') as reader,(artifacts/filename).open('xb') as writer:shutil.copyfileobj(reader,writer)
state.update(ipaSHA256=sha,ipaBytes=ipa.stat().st_size,ipaPath=str(artifacts/name),verified=True)
statepath.write_text(json.dumps(state,indent=2),encoding='utf-8')
print(json.dumps({'passed':True,'ipa':str(artifacts/name),'bytes':ipa.stat().st_size,'sha256':sha,'app':verification['app'],'runURL':state['runURL'],'physicalIPhoneTested':False},indent=2))
