"""Publish this game's reviewed source/export and start its public macOS IPA build.

Uses the already authenticated Git Credential Manager login through the helper.
No credentials, personal profiles, build caches or other projects are uploaded.
"""
import argparse
import hashlib
import json
import os
from pathlib import Path
import re
import shutil
import subprocess
import sys

ROOT=Path(__file__).resolve().parents[1]
REPO='yjmjeminq/bleach-spirit-tide-ios'
VERSION='0.10.0'
TAG='v0.10.0-ios.2'
STAGE=ROOT/'Builds/PublicSource-v0.10.0-r2'
STATE=ROOT/'.runtime/ios-build-state.json'
ARCHIVE=ROOT/'Artifacts/SpiritTide-ios-export-v0.10.0-r2.zip'
ENV=dict(os.environ,GIT_TERMINAL_PROMPT='0',GCM_INTERACTIVE='never')


def sha(path):
    with Path(path).open('rb') as stream:return hashlib.file_digest(stream,'sha256').hexdigest()


def run(args,cwd=ROOT):
    result=subprocess.run(args,cwd=cwd,env=ENV,capture_output=True,text=True,encoding='utf-8',errors='replace',timeout=1800)
    if result.returncode:raise RuntimeError(result.stderr[-2000:] or result.stdout[-2000:])
    return result.stdout.strip()


def gh(*args):return run([sys.executable,'-X','utf8',str(ROOT/'Tools/Cloud-GitCredential.py'),*map(str,args)])
def git(*args):return run(['git','-c','credential.https://github.com.helper=','-c','credential.https://github.com.helper=manager-core','-c','credential.username=yjmjeminq',*args],STAGE)


def write(value):
    STATE.parent.mkdir(exist_ok=True)
    STATE.write_text(json.dumps(value,indent=2)+'\n',encoding='utf-8')


def prepare():
    if STAGE.exists():
        previous=json.loads(STATE.read_text())
        assert previous['repository']==REPO and previous['status']=='prepared' and (STAGE/'.git').is_dir()
        assert not git('status','--porcelain'),'Preserve uncommitted source stage changes.'
    metadata=json.loads(Path(str(ARCHIVE)+'.json').read_text())
    assert sha(ARCHIVE)==metadata['sha256'] and metadata['export']['version']==VERSION
    if not STAGE.exists():
        run(['git','clone','--depth','1','https://github.com/'+REPO+'.git',str(STAGE)])
    assert (STAGE/'.git').is_dir()
    for folder in ['Assets','Packages','ProjectSettings','Tools','Docs','.github']:
        shutil.copytree(ROOT/folder,STAGE/folder,dirs_exist_ok=True,ignore=shutil.ignore_patterns('__pycache__','*.pyc'))
    for name in ['README.md','.gitignore']:shutil.copy2(ROOT/name,STAGE/name)
    forbidden={'Library','Temp','Obj','Logs','UserSettings','Builds','BuildLogs','Artifacts','.runtime','ReferenceArchive'}
    patterns=[rb'\b(?:gh[pousr]_[A-Za-z0-9]{30,}|github_pat_[A-Za-z0-9_]{30,})\b',rb'-----BEGIN (?:RSA |EC |OPENSSH )?PRIVATE KEY-----',rb'\b(?:AKIA|ASIA)[A-Z0-9]{16}\b',rb'\bsk-(?:proj-|svcacct-)?[A-Za-z0-9_-]{30,}\b',rb'https://[^\s/@]{1,100}:[^\s/@]+@']
    rows=[]
    for path in sorted(STAGE.rglob('*')):
        if not path.is_file() or ".git" in path.relative_to(STAGE).parts:continue
        name=path.relative_to(STAGE).as_posix()
        assert not any(part in forbidden for part in path.relative_to(STAGE).parts),name
        assert path.suffix.lower() not in {'.p12','.pfx','.pem','.key','.mobileprovision','.ipa','.zip'},name
        assert path.stat().st_size<99*1024**2,name
        if path.suffix.lower() not in {'.png','.jpg','.jpeg','.ttf','.otf','.wav','.mp3'}:
            data=path.read_bytes();assert not any(re.search(p,data) for p in patterns),'Sensitive data in '+name
        rows.append({'path':name,'bytes':path.stat().st_size,'sha256':sha(path)})
    assert all(not p.is_symlink() for p in STAGE.rglob('*'))
    (ROOT/'.runtime').mkdir(exist_ok=True)
    (ROOT/'.runtime/publication-audit.json').write_text(json.dumps({'passed':True,'files':rows,'personalSavesIncluded':False,'credentialsIncluded':False},indent=2),encoding='utf-8')
    write({'repository':REPO,'tag':TAG,'version':VERSION,'sourceSHA256':metadata['sha256'],'stage':str(STAGE),'status':'prepared'})
    print(json.dumps({'passed':True,'sourceFiles':len(rows),'sourceBytes':sum(r['bytes'] for r in rows),'sealedExportBytes':ARCHIVE.stat().st_size,'stage':str(STAGE)}))


def publish():
    state=json.loads(STATE.read_text());assert state['status']=='prepared'
    audit=json.loads((ROOT/'.runtime/publication-audit.json').read_text());assert audit['passed']
    for row in audit['files']:assert sha(STAGE/row['path'])==row['sha256'],'Source changed after review'
    assert sha(ARCHIVE)==state['sourceSHA256']
    assert gh('api','user','--jq','.login')=='yjmjeminq'
    # A dedicated new repository preserves every existing game repository.
    try:
        remote=json.loads(gh('repo','view',REPO,'--json','nameWithOwner,visibility,description'))
    except RuntimeError:
        gh('repo','create',REPO,'--public','--description','BLEACH Spirit Tide Unity source and iLoader iPhone test builds')
        remote=json.loads(gh('repo','view',REPO,'--json','nameWithOwner,visibility,description'))
    assert remote['nameWithOwner']==REPO and remote['visibility']=='PUBLIC' and remote['description']=='BLEACH Spirit Tide Unity source and iLoader iPhone test builds'
    if not (STAGE/'.git').exists():git('init','--initial-branch=main')
    git('config','user.name','yjmjeminq');git('config','user.email','yjmjeminq@users.noreply.github.com')
    if git('status','--porcelain'):
        git('add','--all');git('commit','-m','Add verified Spirit Tide 0.10.0 iPhone source and touch input checks')
    revision=git('rev-parse','HEAD')
    if not git('remote'):git('remote','add','origin','https://github.com/'+REPO+'.git')
    assert git('remote','get-url','origin')=='https://github.com/'+REPO+'.git'
    git('push','-u','origin','main')
    notes=ROOT/'.runtime/ios-release-notes.md'
    notes.write_text('BLEACH: 영해의 균열 0.10.0 iPhone 시험판\n\niPhone 15 Pro · iOS 17 이상 · 가로 화면 · ARM64 · Metal.\n6개 캐릭터, 5액트, 30던전, 114방.\n0.10.0: 진행에 맞춘 지난 이야기, 출정 준비, 보스 10종 패턴 연습, 실제 피격 안내와 중간보스 뒤 동료 정비.\n\nSpiritTide-iPhone-unsigned-v0.10.0.ipa를 iLoader에서 서명·설치하세요.\n컴파일과 패키지 검증을 수행하며 실제 아이폰 설치·플레이는 별도 확인이 필요합니다.\n',encoding='utf-8')
    gh('release','create',TAG,ARCHIVE,'--repo',REPO,'--target',revision,'--title','Spirit Tide 0.10.0 iPhone test','--notes-file',notes,'--prerelease')
    state.update(revision=revision,status='published');write(state)
    gh('workflow','run','ios-build.yml','--repo',REPO,'--ref','main','-f','source_tag='+TAG,'-f','source_sha256='+state['sourceSHA256'])
    state['status']='dispatched';write(state)
    print(json.dumps({'repository':'https://github.com/'+REPO,'release':'https://github.com/'+REPO+'/releases/tag/'+TAG,'revision':revision,'status':state['status']}))


def status():
    state=json.loads(STATE.read_text());runs=json.loads(gh('api','repos/'+REPO+'/actions/workflows/ios-build.yml/runs?event=workflow_dispatch&per_page=10'))['workflow_runs']
    matching=[r for r in runs if r['head_sha']==state.get('revision')]
    if not matching:print(json.dumps({'status':'waiting_for_run'}));return
    job=matching[0];state.update(runId=job['id'],runURL=job['html_url'],status=job['status'],conclusion=job['conclusion']);write(state)
    jobs=json.loads(gh('api','repos/'+REPO+'/actions/runs/'+str(job['id'])+'/jobs'))['jobs']
    print(json.dumps({'runURL':state['runURL'],'status':state['status'],'conclusion':state['conclusion'],'steps':[{'name':s['name'],'status':s['status'],'conclusion':s['conclusion']} for j in jobs for s in j['steps']]}))


if __name__=='__main__':
    parser=argparse.ArgumentParser();parser.add_argument('action',choices=['prepare','publish','status']);args=parser.parse_args()
    {'prepare':prepare,'publish':publish,'status':status}[args.action]()
