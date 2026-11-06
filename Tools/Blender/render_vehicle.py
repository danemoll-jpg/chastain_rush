"""0.75 Part A: render a vehicle source model with the default rider (Eevee, soft studio light).

Run:  blender --background SourceArt/Blender/<Vehicle>.blend --python Tools/Blender/render_vehicle.py -- <out_dir> <tag> <pose> [ox oy oz]
The rider parts of <pose> (Moto / Atv / Car) are appended from SourceArt/Blender/Rider.blend with the default look
(man, short hair, flat cap, T-shirt, jeans) and moved by the Unity offset (ox, oy, oz) (the car's seat point H).
Views: front, side, three-quarter, rear three-quarter, top, cabin close-up.
"""
import bpy, math, sys, os
from mathutils import Vector, Matrix
sys.path.insert(0, os.path.dirname(__file__))
import kit

a = sys.argv[sys.argv.index('--') + 1:]
out, tag, pose = a[0], a[1], a[2]; off = Vector((float(a[3]), float(a[4]), float(a[5]))) if len(a) > 5 else Vector((0, 0, 0))
os.makedirs(out, exist_ok=True)
L = dict(body='Man', hair='Short', hat='FlatCap', shirt='Tee', pants='Jeans')
with bpy.data.libraries.load(os.path.join(kit.ROOT, 'SourceArt', 'Blender', 'Rider.blend')) as (src, dst):
    dst.objects = [n for n in src.objects if n.startswith(pose + '_')]
for ob in dst.objects:
    head, slot = ob.name.split('__'); p, cat, opt, body = head.split('_')[:4]
    on = (body in ('Any', L['body'])) and (cat == 'Base' or (cat == 'Shirt' and opt == L['shirt']) or (cat == 'Pants' and opt == L['pants'])
                                            or (cat == 'Hair' and opt == L['hair']) or (cat == 'Hat' and opt == L['hat']))
    if on:
        bpy.context.scene.collection.objects.link(ob); ob.matrix_world = Matrix.Translation(kit.B(*off)) @ ob.matrix_world
        ob.data.materials[0] = bpy.data.materials.get(slot) or ob.data.materials[0]
sc = bpy.context.scene
for ob in list(sc.objects):  # the Needle 600's own 0.73 rider is replaced in the game by the parametric rider
    if ob.name.startswith('Rider__'): ob.hide_render = True
sc.render.engine = 'BLENDER_EEVEE'; sc.eevee.taa_render_samples = 32; sc.eevee.use_gtao = True; sc.eevee.use_soft_shadows = True
sc.render.resolution_x, sc.render.resolution_y = 1000, 700
world = bpy.data.worlds.new('studio'); sc.world = world; world.use_nodes = True
world.node_tree.nodes['Background'].inputs['Color'].default_value = (.62, .68, .76, 1); world.node_tree.nodes['Background'].inputs['Strength'].default_value = .8
for name, rot, energy in (('key', (50, 0, 35), 3.2), ('fill', (65, 0, -140), 1.0)):
    l = bpy.data.lights.new(name, 'SUN'); l.energy = energy; l.angle = math.radians(8)
    o = bpy.data.objects.new(name, l); sc.collection.objects.link(o); o.rotation_euler = [math.radians(x) for x in rot]
bpy.ops.mesh.primitive_plane_add(size=14, location=(0, 0, -.53)); g = bpy.context.object
gm = bpy.data.materials.new('ground'); gm.use_nodes = True; gm.node_tree.nodes['Principled BSDF'].inputs['Base Color'].default_value = (.55, .58, .52, 1); g.data.materials.append(gm)
cam = bpy.data.cameras.new('cam'); co = bpy.data.objects.new('cam', cam); sc.collection.objects.link(co); sc.camera = co
meshes = [o for o in sc.objects if o.type == 'MESH' and o != g and not o.hide_render]
lo = Vector([min(min((o.matrix_world @ v.co)[i] for v in o.data.vertices) for o in meshes) for i in range(3)])
hi = Vector([max(max((o.matrix_world @ v.co)[i] for v in o.data.vertices) for o in meshes) for i in range(3)])
c = (lo + hi) / 2; size = (hi - lo).length
d = size * 1.45
H = kit.B(*(off + Vector((0, .74, -.045)))) if pose == 'Car' else Vector((0, 0, 0))
views = {'front': (c + Vector((0, d, .1 * d)), c), 'side': (c + Vector((d, 0, .05 * d)), c), 'three-quarter': (c + Vector((.7 * d, .7 * d, .3 * d)), c),
         'rear-three-quarter': (c + Vector((-.65 * d, -.7 * d, .25 * d)), c), 'top': (c + Vector((0, -.001, 1.4 * d)), c)}
if pose == 'Car': views['cabin'] = (H + Vector((-1.6, .9, .25)), H + Vector((0, .05, -.25)))
else: views['rider-close'] = (c + Vector((.55 * d, .45 * d, .45 * d)), c + Vector((0, 0, .45)))
for name, (eye, t) in views.items():
    co.location = eye; co.rotation_euler = (t - eye).to_track_quat('-Z', 'Y').to_euler()
    cam.lens = 70 if name in ('cabin', 'rider-close') else 50
    if name == 'top': cam.type = 'ORTHO'; cam.ortho_scale = max(hi.y - lo.y, hi.x - lo.x) * 1.15
    else: cam.type = 'PERSP'
    sc.render.filepath = os.path.join(out, f'{tag}-{name}.png'); bpy.ops.render.render(write_still=True)
print('RENDERED', out)
