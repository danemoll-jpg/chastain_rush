"""Prepare one independently versioned component; preserve the other catalog pointer."""
import argparse,base64,hashlib,importlib.util,json,pathlib,shutil,zipfile
from cryptography.hazmat.primitives import hashes,serialization
from cryptography.hazmat.primitives.asymmetric import padding
ROOT=pathlib.Path(__file__).resolve().parents[1]
spec=importlib.util.spec_from_file_location('release',ROOT/'Tools/Prepare-LauncherRelease.py');r=importlib.util.module_from_spec(spec);spec.loader.exec_module(r)
def verified(path,key):
    e=json.loads(path.read_text());raw=base64.b64decode(e['payload'],validate=True)
    key.public_key().verify(base64.b64decode(e['signature'],validate=True),raw,padding.PKCS1v15(),hashes.SHA256())
    if e['keyId']!=json.loads((ROOT/'Launcher/pinned-key.json').read_text())['keyId']:raise ValueError('Unknown publisher key')
    return json.loads(raw)
def main():
    p=argparse.ArgumentParser(description=__doc__);p.add_argument('component',choices=['game','soundtrack']);p.add_argument('--source',type=pathlib.Path,required=True,help='Explicit runtime or designated BundleMusic root')
    p.add_argument('--previous-catalog',type=pathlib.Path,required=True);p.add_argument('--previous-manifest',type=pathlib.Path,required=True)
    p.add_argument('--out',type=pathlib.Path,required=True);p.add_argument('--build',type=int,required=True);p.add_argument('--version',required=True);p.add_argument('--notes',required=True)
    p.add_argument('--key',type=pathlib.Path,default=ROOT/'Builds/PublisherPrivate/launcher-signing.pem');p.add_argument('--base-url',help='Round 81: where the assets will be (a local check: http://127.0.0.1:port/...)');a=p.parse_args()
    key=serialization.load_pem_private_key(a.key.read_bytes(),None);catalog=verified(a.previous_catalog,key);previous=verified(a.previous_manifest,key)
    if catalog['type']!='catalog' or previous['type']!=a.component or a.build<=previous['build']:raise ValueError('Correct current component manifest and increasing build required')
    if a.out.exists():raise ValueError('Use a fresh immutable output directory')
    assets=a.out/'assets';assets.mkdir(parents=True);tag=f'{a.component}-{a.build}';base=a.base_url or f'https://github.com/danemoll-jpg/chastain-rush-releases/releases/download/{tag}'
    manifest=dict(schema=1,type=a.component,platform='windows-x64',minLauncher=1,build=a.build,version=a.version,notes=a.notes)
    if a.component=='game':
        files=[f for f in r.inventory(a.source) if f['path'].split('/')[0] not in {'Music','ChastainRush_BurstDebugInformation_DoNotShip','ChastainRush_BackUpThisFolder_ButDontShipItWithYourGame'}]
        if not any(f['path']=='ChastainRush.exe' for f in files):raise ValueError('Complete runtime required')
        archive=assets/'game.zip'
        with zipfile.ZipFile(archive,'w',zipfile.ZIP_DEFLATED,compresslevel=5) as z:
            for f in files:z.write(a.source/f['path'],f['path'])
        manifest.update(files=files,saveSchema=1,package=dict(url=base+'/game.zip',bytes=archive.stat().st_size,sha256=r.digest(archive)))
    else:
        files=r.inventory(a.source);old={f['path']:f for f in previous['files']}
        for f in files:
            prior=old.get(f['path'])
            if prior and prior['sha256']==f['sha256'] and prior['bytes']==f['bytes']:f['url']=prior['url'];continue
            source=a.source/f['path'];name='music-'+f['sha256']+source.suffix.lower();f['url']=base+'/'+name
            if not (assets/name).exists():shutil.copy2(source,assets/name)
        manifest['files']=files
    filename=a.component+'-manifest.json';r.write(assets/filename,r.sign(manifest,key));catalog[a.component]=base+'/'+filename
    r.write(assets/'update-catalog.json',r.sign(catalog,key));inventory=r.inventory(assets)
    if any(x['bytes']>=2*1024**3 for x in inventory):raise ValueError('Asset exceeds GitHub size limit')
    r.write(a.out/'INVENTORY.json',dict(tag=tag,game=dict(build=a.build,version=a.version,notes=a.notes),component=a.component,assets=inventory,publication='LOCAL DRAFT ONLY'))
    print(json.dumps(dict(component=a.component,tag=tag,files=len(files),assets=len(inventory),otherPointerPreserved=True,uploaded=False)))
if __name__=='__main__':main()
