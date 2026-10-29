"""Assemble a complete starter ZIP (launcher + game + soundtrack) from already-published signed manifests, verify its extracted
inventory, and stage it as a publisher draft for Publish-LauncherRelease.py. Never signs, uploads or reads keys.
Layout as the 0.21 Full ZIP: ChastainRush/ launcher, cover, README, state.json, versions/<build>, shared/soundtracks/<build>;
plus the icon and poster beside the launcher for a Steam shortcut (plain copies)."""
import argparse,importlib.util,json,pathlib,shutil,subprocess,zipfile
ROOT=pathlib.Path(__file__).resolve().parents[1]
spec=importlib.util.spec_from_file_location('release',ROOT/'Tools/Prepare-LauncherRelease.py');r=importlib.util.module_from_spec(spec);spec.loader.exec_module(r)
def verify(checks,work,envelope):
    # The launcher's own signature check (pinned key); returns the signed payload.
    p=subprocess.run([str(checks),str(work/'verify-root'),str(work/'verify-saves'),'verify',str(envelope)],capture_output=True,text=True)
    if p.returncode:raise ValueError(f'Signature check failed for {envelope}: {p.stderr.strip()}')
    return json.loads(p.stdout)
def copy_exact(source,target,files):
    for f in files:
        s=source/f['path'];t=target/f['path']
        if s.is_symlink() or s.stat().st_size!=f['bytes'] or r.digest(s)!=f['sha256']:raise ValueError(f'{s} differs from its signed manifest')
        t.parent.mkdir(parents=True,exist_ok=True);shutil.copy2(s,t)
def main():
    p=argparse.ArgumentParser(description=__doc__)
    p.add_argument('--game',type=pathlib.Path,required=True,help='installed runtime folder matching --game-manifest')
    p.add_argument('--game-manifest',type=pathlib.Path,required=True,help='published signed game-manifest.json')
    p.add_argument('--music',type=pathlib.Path,required=True,help='installed soundtrack folder matching --music-manifest')
    p.add_argument('--music-manifest',type=pathlib.Path,required=True,help='published signed soundtrack-manifest.json')
    p.add_argument('--launcher',type=pathlib.Path,default=ROOT/'Builds/Launcher/ChastainRushLauncher.exe')
    p.add_argument('--checks',type=pathlib.Path,default=ROOT/'Builds/Launcher/LauncherChecks.exe')
    p.add_argument('--cover',type=pathlib.Path,default=ROOT/'SourceArt/Title/ChastainRushCover.png')
    p.add_argument('--icon',type=pathlib.Path,default=ROOT/'SourceArt/Title/GameIcon.png')
    p.add_argument('--poster',type=pathlib.Path,default=ROOT/'SourceArt/Title/ChastainRushPoster.png')
    p.add_argument('--readme',type=pathlib.Path,default=ROOT/'Docs/Starter/README-LAUNCHER.md')
    p.add_argument('--out',type=pathlib.Path,required=True,help='new draft folder')
    p.add_argument('--zip-name',required=True);p.add_argument('--tag',required=True);p.add_argument('--title',required=True);p.add_argument('--notes',required=True)
    p.add_argument('--no-music',action='store_true',help='Round 81 (Chastain Rush): no soundtrack in the ZIP (the game and the soundtrack together pass the 2 GiB limit); the launcher installs it on its first start')
    p.add_argument('--not-latest',action='store_true',help='publish without moving the Latest release (the update catalog stays where it is)')
    a=p.parse_args()
    if a.out.exists():raise ValueError('Output must be a new immutable draft folder')
    work=a.out/'work';starter=a.out/'starter/ChastainRush';assets=a.out/'assets'
    for d in (work,starter,assets):d.mkdir(parents=True)
    gm=verify(a.checks,work,a.game_manifest);mm=verify(a.checks,work,a.music_manifest)
    if gm['type']!='game' or mm['type']!='soundtrack':raise ValueError('Wrong manifest types')
    g=f"versions/{gm['build']}";m=f"shared/soundtracks/{mm['build']}"
    copy_exact(a.game,starter/g,gm['files'])
    if not a.no_music:copy_exact(a.music,starter/m,mm['files'])
    shutil.copy2(a.launcher,starter/'ChastainRushLauncher.exe');shutil.copy2(a.cover,starter/'ChastainRush-Cover.png')
    shutil.copy2(a.icon,starter/'ChastainRush-Icon.png');shutil.copy2(a.poster,starter/'ChastainRush-Poster.png')
    shutil.copy2(a.readme,starter/'README-LAUNCHER.md')   # (Round 81: Woodstock Rush's Install-Launcher.ps1, a migration from its old Racer.exe, is not needed: a fresh install)
    shutil.copy2(a.game_manifest,starter/'game-manifest.json');shutil.copy2(a.music_manifest,starter/'soundtrack-manifest.json')
    licenses=starter/'Launcher-Licenses';licenses.mkdir()
    for name in ('JSON-LICENSE.txt','MINIZ-LICENSE.txt'):shutil.copy2(ROOT/'Launcher/vendor'/name,licenses/name)
    if a.no_music:r.write(starter/'state.json',dict(schema=1,game=dict(directory=g,manifest=gm),highestGameBuild=gm['build']));(starter/'shared/PersonalMusic').mkdir(parents=True,exist_ok=True)
    else:r.write(starter/'state.json',dict(schema=1,game=dict(directory=g,manifest=gm),music=dict(directory=m,manifest=mm),highestGameBuild=gm['build'],highestMusicBuild=mm['build']))
    files=r.inventory(starter);archive=assets/a.zip_name
    with zipfile.ZipFile(archive,'w',zipfile.ZIP_DEFLATED,compresslevel=5) as z:
        for f in files:z.write(starter/f['path'],'ChastainRush/'+f['path'])
    if archive.stat().st_size>=2*1024**3:raise ValueError('Starter exceeds the 2 GiB release asset limit')
    extracted=work/'verified-extraction'
    with zipfile.ZipFile(archive) as z:z.extractall(extracted)
    if r.inventory(extracted/'ChastainRush')!=files:raise ValueError('Extracted starter differs')
    # The launcher's own store opens the extracted starter and finds its game.
    s=subprocess.run([str(a.checks),str(extracted/'ChastainRush'),str(work/'status-saves'),'status'],capture_output=True,text=True)
    if s.returncode or json.loads(s.stdout)['game']['manifest']['build']!=gm['build']:raise ValueError('Launcher cannot open the extracted starter: '+s.stderr)
    shutil.rmtree(extracted);shutil.rmtree(starter.parent)
    inventory=dict(tag=a.tag,title=a.title,latest=not a.not_latest,game=dict(version=gm['version'],build=gm['build'],notes=a.notes),assets=r.inventory(assets),starterFiles=len(files),publication='LOCAL DRAFT ONLY')
    r.write(a.out/'INVENTORY.json',inventory)
    print(json.dumps(dict(archive=str(archive),files=len(files),bytes=archive.stat().st_size,sha256=r.digest(archive),game=gm['version'],soundtrack=mm['build'])))
if __name__=='__main__':main()
