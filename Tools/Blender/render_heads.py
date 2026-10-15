"""Round 28 (C1, C3): pictures of the rider's hair styles and hands from SourceArt/Blender/Rider.blend (Eevee, soft studio light).

Run:  blender --background SourceArt/Blender/Rider.blend --python Tools/Blender/render_heads.py -- <out_dir> [hair|hands|all] [pose]
      [hair styles,...|-] [hats,...] [bodies,...]   (the last three only for 'hair': just those)
  hair   every hair style, bare-headed and under every hat, from the front, the side and the back (one sheet per style, and every
         file on its own), on the Man and the Woman
  hands  the hands on the grips / the wheel from the rider's eyes (as the first-person camera sees them) and from outside, in the
         Moto, Cruiser, Dirt, Atv and Car poses
Names are "<Pose>_<Category>_<Option>_<Body>[_<Arm>]__<slot>" (rider.py); the same activation rules as the game (RiderLook).
"""
import bpy, math, sys, os
from mathutils import Vector

argv = sys.argv[sys.argv.index('--') + 1:] if '--' in sys.argv else []
out = argv[0] if argv else 'renders'; WHAT = argv[1] if len(argv) > 1 else 'all'; POSE = argv[2] if len(argv) > 2 else 'Moto'
ONLY_HAIR = argv[3].split(',') if len(argv) > 3 and argv[3] != '-' else None; ONLY_HAT = argv[4].split(',') if len(argv) > 4 else None
ONLY_BODY = argv[5].split(',') if len(argv) > 5 else None
os.makedirs(out, exist_ok=True)
sys.path.insert(0, os.path.dirname(__file__))
import rider
HAIRS, HATS = list(rider.HAIRS) + ['Bald'], ['None'] + list(rider.HATS)

DEFAULT = dict(body='Man', hair='Short', hat='None', shirt='Tee', pants='Jeans', skin=(.78, .52, .36), haircol=(.18, .065, .028),
               hatcol=(.85, .14, .1), shirtcol=(.12, .36, .95), pantscol=(.10, .21, .48), trimcol=(.05, .05, .05), shoecol=(.2, .2, .22))


def active(name, L, pose):
    head, slot = name.split('__'); parts = head.split('_'); p, cat, opt, body = parts[0], parts[1], parts[2], parts[3]
    if p != pose or (body not in ('Any', 'All') and body != L['body']): return False
    if cat == 'Base': return True
    if cat == 'Shirt': return opt == L['shirt']
    if cat == 'Pants': return opt == L['pants']
    if cat == 'Hair': return opt == L['hair']
    if cat == 'HairTop': return opt == L['hair'] and L['hat'] == 'None'
    if cat == 'Hat': return opt == L['hat']
    return False


def colour(slot, c):
    if slot not in bpy.data.materials: return
    m = bpy.data.materials[slot]
    if not m.node_tree: return
    b = m.node_tree.nodes.get('Principled BSDF')
    if b: b.inputs['Base Color'].default_value = (c[0], c[1], c[2], 1)


sc = bpy.context.scene
sc.render.engine = 'BLENDER_EEVEE'; sc.eevee.taa_render_samples = 32; sc.eevee.use_gtao = True; sc.eevee.use_soft_shadows = True
sc.render.resolution_x, sc.render.resolution_y = 420, 420
world = bpy.data.worlds.new('studio'); sc.world = world; world.use_nodes = True
world.node_tree.nodes['Background'].inputs['Color'].default_value = (.62, .68, .76, 1); world.node_tree.nodes['Background'].inputs['Strength'].default_value = .8
for name, rot, energy in (('key', (50, 0, 35), 3.2), ('fill', (65, 0, -140), 1.0), ('back', (60, 0, 180), 1.2)):
    l = bpy.data.lights.new(name, 'SUN'); l.energy = energy; l.angle = math.radians(8)
    o = bpy.data.objects.new(name, l); sc.collection.objects.link(o); o.rotation_euler = [math.radians(a) for a in rot]
cam = bpy.data.cameras.new('cam'); co = bpy.data.objects.new('cam', cam); sc.collection.objects.link(co); sc.camera = co; cam.lens = 60; cam.clip_start = .01
meshes = [o for o in bpy.data.objects if o.type == 'MESH']


def show(L, pose):
    for o in meshes: o.hide_render = not active(o.name, L, pose)
    colour('skin', L['skin']); colour('hair', L['haircol']); colour('hat', L['hatcol']); colour('shirt', L['shirtcol']); colour('pants', L['pantscol'])
    colour('trim', L['trimcol']); colour('shoes', L['shoecol'])


def shoot(eye, target, path, lens=60):
    cam.lens = lens
    co.location = Vector(eye); co.rotation_euler = (Vector(target) - Vector(eye)).to_track_quat('-Z', 'Y').to_euler()
    sc.render.filepath = path; bpy.ops.render.render(write_still=True)


def head_of(pose):
    h = rider.POSES[pose]['head']; return Vector(rider.B(*h))


if WHAT in ('hair', 'all'):
    H = head_of(POSE)
    # front (the face: Unity +z, Blender +y, is the rider's front; +z up), the side (+x), the back, three-quarter front
    views = {'front': H + Vector((0, .62, .05)), 'side': H + Vector((.62, 0, .05)), 'back': H + Vector((0, -.62, .10)), 'threeq': H + Vector((.42, .45, .16))}
    for body in ONLY_BODY or ('Man', 'Woman'):
        for hair in ONLY_HAIR or HAIRS:
            for hat in ONLY_HAT or HATS:
                L = dict(DEFAULT); L.update(body=body, hair=hair, hat=hat)
                show(L, POSE)
                for vn, eye in views.items():
                    shoot(eye, H + Vector((0, 0, .01)), os.path.join(out, f'hair-{POSE}-{body}-{hair}-{hat}-{vn}.png'))
if WHAT in ('hands', 'all'):
    for pose in ('Moto', 'Cruiser', 'Dirt', 'Atv', 'Car'):
        if pose not in rider.POSES: continue
        P = rider.POSES[pose]; L = dict(DEFAULT); L.update(hat='Baseball'); show(L, pose)
        H = Vector(rider.B(*P['head'])); hr, hl = Vector(rider.B(*P['hand'])), Vector(rider.B(*rider.M(P['hand'], -1)))
        mid = (hr + hl) / 2
        eye = H + Vector((0, .135, .0))                       # just in front of the face, as the first-person camera
        shoot(eye, mid, os.path.join(out, f'hands-{pose}-eyes.png'), 30)
        shoot(hr + Vector((.28, .22, .12)), hr, os.path.join(out, f'hands-{pose}-right.png'), 60)
        shoot(hl + Vector((-.25, -.25, .16)), hl, os.path.join(out, f'hands-{pose}-left-back.png'), 60)
print('RENDERED', out)
