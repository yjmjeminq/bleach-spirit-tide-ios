"""Run gh for this game's builds using the existing Git Credential Manager login.

Does not prompt, change global CLI authentication, print credentials, or store
them in files. The existing credential is passed only through the child env.
"""
import os
from pathlib import Path
import shutil
import subprocess
import sys

ROOT=Path(__file__).resolve().parents[1]
OWNER='yjmjeminq'


def main():
    environment=os.environ.copy()
    environment.update(GIT_TERMINAL_PROMPT='0',GCM_INTERACTIVE='never')
    command=['git','-c','credential.https://github.com.helper=',
             '-c','credential.https://github.com.helper=manager-core','credential','fill']
    result=subprocess.run(command,input='protocol=https\nhost=github.com\nusername='+OWNER+'\n\n',
                          capture_output=True,text=True,encoding='utf-8',env=environment)
    if result.returncode!=0:
        raise RuntimeError('The existing Git Credential Manager login is unavailable')
    credential=dict(line.split('=',1) for line in result.stdout.splitlines() if '=' in line)
    token=credential.get('password')
    if not token:raise RuntimeError('No existing GitHub credential was returned')
    environment.update(GH_TOKEN=token,GH_HOST='github.com',GH_PROMPT_DISABLED='1')
    installed=ROOT.parent/'.runtime/github-cli/app/bin/gh.exe'
    executable=str(installed) if installed.is_file() else shutil.which('gh')
    if not executable:raise RuntimeError('GitHub CLI is not installed')
    owner=subprocess.run([executable,'api','user','--jq','.login'],capture_output=True,text=True,encoding='utf-8',env=environment)
    if owner.returncode!=0 or owner.stdout.strip()!=OWNER:
        raise RuntimeError('The existing Git login does not authenticate as the expected repository owner')
    if sys.argv[1:]==['--credential-check']:
        print('Existing Git Credential Manager login verified for '+OWNER+'; no new authentication performed.')
        return 0
    if not sys.argv[1:]:raise RuntimeError('Supply a gh command')
    return subprocess.run([executable,*sys.argv[1:]],env=environment).returncode


if __name__=='__main__':
    try:sys.exit(main())
    except RuntimeError as error:
        print(str(error),file=sys.stderr)
        sys.exit(1)
