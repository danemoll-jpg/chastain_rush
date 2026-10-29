"""Deliberate publisher operation. Default is dry run; never called by game builds."""
import argparse, hashlib, json, os, pathlib, subprocess, urllib.request
REPO='danemoll-jpg/chastain-rush-releases'
LATEST_CATALOG='https://github.com/'+REPO+'/releases/latest/download/update-catalog.json'
# Round 81 (J): the GitHub CLI: $GH, else Woodstock Rush's bundled one (Builds/PublisherTools, used as a tool, read only), else 'gh' on the path
GH=os.environ.get('GH') or next((str(p) for p in [pathlib.Path(r'C:/Users/danmo/Racer/Builds/PublisherTools/gh-2.101.0/bin/gh.exe')] if p.is_file()),'gh')
def gh(*args):
    return subprocess.run([GH,*args],check=True,capture_output=True,text=True).stdout
def main():
    p=argparse.ArgumentParser(description=__doc__);p.add_argument('draft',type=pathlib.Path);p.add_argument('--publish',action='store_true');p.add_argument('--confirm-repository');p.add_argument('--resume-draft',action='store_true');p.add_argument('--replace-assets',action='store_true',help='replace changed assets of an existing published, not-Latest release (same tag and file names)');a=p.parse_args()
    inventory=json.loads((a.draft/'INVENTORY.json').read_text());assets=a.draft/'assets';tag=inventory.get('tag','build-'+str(inventory['game']['build']))
    for item in inventory['assets']:
        file=assets/item['path']
        if not file.is_file() or file.stat().st_size!=item['bytes']: raise ValueError('Incomplete staged asset')
        h=hashlib.sha256()
        with file.open('rb') as f:
            for block in iter(lambda:f.read(1024*1024),b''): h.update(block)
        if h.hexdigest()!=item['sha256']: raise ValueError('Staged asset changed after inventory')
    print(json.dumps(dict(repository=REPO,tag=tag,assets=len(inventory['assets']),bytes=sum(x['bytes'] for x in inventory['assets']),publish=a.publish)))
    if not a.publish:return
    if a.confirm_repository!=REPO:raise ValueError('Explicit repository confirmation required')
    # A starter-only release (inventory latest=false) leaves Latest, and so every launcher's update catalog, where it is.
    latest=inventory.get('latest',True)
    if not latest:
        with urllib.request.urlopen(LATEST_CATALOG,timeout=20) as response: catalog_before=response.read()
    # No credential extraction. gh uses the publisher\'s own deliberately configured session.
    repo=json.loads(gh('api','repos/'+REPO));
    if repo['private'] or not repo.get('permissions',{}).get('push'):raise ValueError('Public repository with publishing permission required')
    releases=json.loads(gh('api','repos/'+REPO+'/releases?per_page=100'))
    matches=[r for r in releases if r['tag_name']==tag]
    if a.replace_assets:return replace(a,inventory,assets,tag,matches,catalog_before if not latest else None)
    if matches:
        if not a.resume_draft or len(matches)!=1 or not matches[0]['draft']:raise ValueError('Release exists; only explicit resume of an unpublished draft is allowed')
        remote=matches[0]
    else:
        if a.resume_draft:raise ValueError('No matching draft exists')
        gh('release','create',tag,'--repo',REPO,'--draft','--title',inventory.get('title','Chastain Rush '+inventory['game']['version']),'--notes',inventory['game']['notes'],*([] if latest else ['--latest=false']))
        releases=json.loads(gh('api','repos/'+REPO+'/releases?per_page=100'))
        remote=next(r for r in releases if r['tag_name']==tag and r['draft'])
    release_id=remote['id'];existing={x['name']:x for x in remote['assets']}
    expected={x['path']:x for x in inventory['assets']}
    for name,asset in existing.items():
        item=expected.get(name)
        if not item or asset['size']!=item['bytes'] or asset.get('digest')!='sha256:'+item['sha256']:raise ValueError('Existing draft differs from approved inventory; nothing overwritten')
    # Upload all immutable content into the draft. The catalog is uploaded last.
    for index,item in enumerate(sorted(inventory['assets'],key=lambda x:x['path']=='update-catalog.json'),1):
        if item['path'] in existing:continue
        gh('release','upload',tag,str(assets/item['path']),'--repo',REPO)
        print(f"Uploaded {index}/{len(inventory['assets'])}: {item['path']} ({item['bytes']} bytes)",flush=True)
    # Draft tags may not exist until publication; their release ID is stable.
    remote=json.loads(gh('api','repos/'+REPO+'/releases/'+str(release_id)));by_name={x['name']:x for x in remote['assets']}
    for item in inventory['assets']:
        r=by_name.get(item['path'])
        if not r or r['size']!=item['bytes'] or r.get('digest')!='sha256:'+item['sha256']:raise ValueError('Uploaded asset verification failed; draft remains unpublished')
    gh('release','edit',tag,'--repo',REPO,'--draft=false','--latest' if latest else '--latest=false')
    with urllib.request.urlopen(LATEST_CATALOG,timeout=20) as response: catalog=response.read()
    if not latest:
        if catalog!=catalog_before:raise ValueError('Latest update catalog changed; investigate before announcing')
        print('Published without moving Latest; the signed update catalog is unchanged. Previous releases retained.');return
    if hashlib.sha256(catalog).hexdigest()!=hashlib.sha256((assets/'update-catalog.json').read_bytes()).hexdigest():raise ValueError('Latest catalog fetch mismatch; investigate before announcing rollout')
    print('Published and fetched latest signed catalog. Previous releases retained.')
def replace(a,inventory,assets,tag,matches,catalog_before):
    # Each changed asset is uploaded under a temporary name and checked before the old one is removed and the new one renamed,
    # so the download link never serves a partial upload; the gap without the file is the two API calls of the swap.
    if inventory.get('latest',True) or catalog_before is None:raise ValueError('Asset replacement is only for not-Latest releases')
    if len(matches)!=1 or matches[0]['draft']:raise ValueError('Replacement needs exactly one published release with this tag')
    release_id=matches[0]['id'];latest_tag=json.loads(gh('api','repos/'+REPO+'/releases/latest'))['tag_name']
    if latest_tag==tag:raise ValueError('Release is Latest; refusing to replace its assets')
    for item in inventory['assets']:
        existing={x['name']:x for x in json.loads(gh('api','repos/'+REPO+'/releases/'+str(release_id)))['assets']}
        old=existing.get(item['path'])
        if old and old['size']==item['bytes'] and old.get('digest')=='sha256:'+item['sha256']:print('Unchanged: '+item['path']);continue
        source=assets/item['path'];temporary=assets/('replacement-'+item['path'])
        if ('replacement-'+item['path']) in existing:raise ValueError('A replacement upload is already on the release; inspect it first')
        os.link(source,temporary)
        try:gh('release','upload',tag,str(temporary),'--repo',REPO)
        finally:temporary.unlink()
        uploaded={x['name']:x for x in json.loads(gh('api','repos/'+REPO+'/releases/'+str(release_id)))['assets']}.get(temporary.name)
        if not uploaded or uploaded['size']!=item['bytes'] or uploaded.get('digest')!='sha256:'+item['sha256']:raise ValueError('Replacement upload verification failed; the old asset is still in place')
        if old:gh('api','-X','DELETE','repos/'+REPO+'/releases/assets/'+str(old['id']))
        gh('api','-X','PATCH','repos/'+REPO+'/releases/assets/'+str(uploaded['id']),'-f','name='+item['path'])
        final={x['name']:x for x in json.loads(gh('api','repos/'+REPO+'/releases/'+str(release_id)))['assets']}.get(item['path'])
        if not final or final['size']!=item['bytes'] or final.get('digest')!='sha256:'+item['sha256']:raise ValueError('Replaced asset verification failed')
        print(f"Replaced {item['path']} ({item['bytes']} bytes)",flush=True)
    gh('release','edit',tag,'--repo',REPO,'--title',inventory['title'],'--notes',inventory['game']['notes'],'--latest=false')
    with urllib.request.urlopen(LATEST_CATALOG,timeout=20) as response: catalog=response.read()
    if catalog!=catalog_before:raise ValueError('Latest update catalog changed; investigate before announcing')
    print('Replaced assets without moving Latest; the signed update catalog is unchanged.')
if __name__=='__main__':main()
