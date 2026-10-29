"""Local-only release preparation. Never uploads or reads GitHub credentials."""
import argparse, base64, hashlib, json, pathlib, shutil, zipfile
from cryptography.hazmat.primitives import hashes, serialization
from cryptography.hazmat.primitives.asymmetric import padding

ROOT = pathlib.Path(__file__).resolve().parents[1]
def digest(path):
    h=hashlib.sha256()
    with path.open('rb') as f:
        for b in iter(lambda:f.read(1024*1024), b''): h.update(b)
    return h.hexdigest()
def inventory(root):
    result=[]
    for p in sorted(root.rglob('*')):
        if p.is_symlink() or p.is_junction(): raise ValueError(f'Linked input forbidden: {p}')
        if p.is_file(): result.append(dict(path=p.relative_to(root).as_posix(),bytes=p.stat().st_size,sha256=digest(p)))
    return result
def sign(payload,key):
    raw=json.dumps(payload,sort_keys=True,separators=(',',':'),ensure_ascii=False).encode()
    public=json.loads((ROOT/'Launcher/pinned-key.json').read_text())
    return dict(keyId=public['keyId'],payload=base64.b64encode(raw).decode(),signature=base64.b64encode(key.sign(raw,padding.PKCS1v15(),hashes.SHA256())).decode())
def write(path,obj): path.write_text(json.dumps(obj,indent=2),encoding='utf-8')
def main():
    p=argparse.ArgumentParser(description=__doc__)
    p.add_argument('--game',type=pathlib.Path,required=True);p.add_argument('--bundle-music',type=pathlib.Path,required=True)
    p.add_argument('--out',type=pathlib.Path,required=True);p.add_argument('--version',required=True);p.add_argument('--build',type=int,required=True)
    p.add_argument('--music-build',type=int,required=True);p.add_argument('--notes',required=True)
    p.add_argument('--key',type=pathlib.Path,default=ROOT/'Builds/PublisherPrivate/launcher-signing.pem')
    p.add_argument('--base-url');a=p.parse_args()
    if a.out.exists(): raise ValueError('Output must be a new immutable release directory')
    if not (a.game/'ChastainRush.exe').is_file(): raise ValueError('Complete game runtime required')
    a.out.mkdir(parents=True); assets=a.out/'assets';assets.mkdir()
    key=serialization.load_pem_private_key(a.key.read_bytes(),password=None)
    base=a.base_url or f'https://github.com/danemoll-jpg/chastain-rush-releases/releases/download/build-{a.build}'
    runtime=a.out/'starter/versions'/str(a.build);runtime.mkdir(parents=True)
    # Explicit runtime input only. Music is separately inventoried from the designated root.
    excluded={'Music','ChastainRush_BurstDebugInformation_DoNotShip','ChastainRush_BackUpThisFolder_ButDontShipItWithYourGame'}
    for item in a.game.iterdir():
        if item.name in excluded: continue
        if item.is_symlink() or item.is_junction(): raise ValueError('Linked runtime input')
        if item.is_dir(): shutil.copytree(item,runtime/item.name)
        else: shutil.copy2(item,runtime/item.name)
    files=inventory(runtime); archive=assets/'game.zip'
    with zipfile.ZipFile(archive,'w',zipfile.ZIP_DEFLATED,compresslevel=5) as z:
        for f in files: z.write(runtime/f['path'],f['path'])
    if archive.stat().st_size>=2*1024**3: raise ValueError('Game asset exceeds GitHub release limit')
    gm=dict(schema=1,type='game',platform='windows-x64',minLauncher=1,build=a.build,version=a.version,notes=a.notes,saveSchema=1,files=files,package=dict(url=base+'/game.zip',bytes=archive.stat().st_size,sha256=digest(archive)))
    music=inventory(a.bundle_music)
    shared=a.out/'starter/shared/soundtracks'/str(a.music_build);shared.mkdir(parents=True)
    for f in music:
        source=a.bundle_music/f['path'];target=shared/f['path'];target.parent.mkdir(parents=True,exist_ok=True);shutil.copy2(source,target)
        asset='music-'+f['sha256']+source.suffix.lower();f['url']=base+'/'+asset
        if not (assets/asset).exists(): shutil.copy2(source,assets/asset)
    mm=dict(schema=1,type='soundtrack',platform='windows-x64',minLauncher=1,build=a.music_build,version=str(a.music_build),notes='Designated bundled soundtrack',files=music)
    write(assets/'game-manifest.json',sign(gm,key));write(assets/'soundtrack-manifest.json',sign(mm,key))
    catalog=dict(schema=1,type='catalog',game=base+'/game-manifest.json',soundtrack=base+'/soundtrack-manifest.json')
    write(assets/'update-catalog.json',sign(catalog,key))
    write(a.out/'starter/state.json',dict(schema=1,game=dict(directory=f'versions/{a.build}',manifest=gm),music=dict(directory=f'shared/soundtracks/{a.music_build}',manifest=mm),highestGameBuild=a.build,highestMusicBuild=a.music_build))
    (a.out/'starter/shared/PersonalMusic').mkdir(parents=True)
    for name in ('game-manifest.json','soundtrack-manifest.json'): shutil.copy2(assets/name,a.out/'starter'/name)
    write(a.out/'INVENTORY.json',dict(game=gm,music=mm,assets=inventory(assets),publication='LOCAL DRAFT ONLY; no upload authorized by this command'))
    print(json.dumps(dict(output=str(a.out),runtimeFiles=len(files),soundtrackFiles=len(music),gameZipBytes=archive.stat().st_size,uploaded=False)))
if __name__=='__main__': main()
