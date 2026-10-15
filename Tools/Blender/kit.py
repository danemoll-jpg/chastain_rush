"""0.75 shared Blender modelling kit (the 0.73 needle600.py helpers, made reusable; needle600.py itself is unchanged).

Everything is written in the game's vehicle space (Unity: x right, y up, z forward; metres) and converted to Blender space
(x right, y forward, z up) by B(). Parts are collected as (object, group, slot); export() joins them into one object per
"<group>__<slot>" and writes the .blend source and the Unity FBX exactly as the 0.73 pipeline did. Slots are the game's
shared vehicle materials (paint, metal, engine, rubber, lamp, tail, glass, chrome, interior) and the rider materials
(skin, hair, hat, trim, shirt, pants, shoes, eyes, pupil, mouth); the game assigns its own materials by slot.
"""
import bpy, bmesh, math, os
from mathutils import Vector, Matrix

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), '..', '..'))


def B(x, y, z):  # Unity vehicle space -> Blender space
    return Vector((x, z, y))


COLORS = {  # preview colours only (the game uses its own materials)
    'paint': (.12, .36, .95), 'metal': (.48, .56, .60), 'engine': (.20, .21, .23), 'rubber': (.035, .043, .054),
    'lamp': (.95, .91, .7), 'tail': (.65, .03, .03), 'glass': (.10, .17, .22), 'chrome': (.75, .78, .80), 'interior': (.10, .10, .11),
    'skin': (.78, .52, .36), 'hair': (.18, .065, .028), 'hat': (.03, .03, .035), 'trim': (.10, .08, .06), 'shirt': (.12, .36, .95),
    'pants': (.10, .21, .48), 'shoes': (.17, .095, .05), 'eyes': (.96, .95, .90), 'pupil': (.035, .043, .054), 'mouth': (.45, .16, .14),
    'inner': (.92, .92, .90), 'emblem': (.95, .95, .93)}


def mat(slot):
    m = bpy.data.materials.get(slot)
    if not m:
        m = bpy.data.materials.new(slot); m.use_nodes = True
        bsdf = m.node_tree.nodes['Principled BSDF']; c = COLORS[slot]
        bsdf.inputs['Base Color'].default_value = (c[0], c[1], c[2], 1)
        bsdf.inputs['Roughness'].default_value = .35 if slot in ('paint', 'metal', 'lamp', 'chrome', 'glass') else .6
        if slot in ('metal', 'chrome'): bsdf.inputs['Metallic'].default_value = .6
        if slot == 'glass':
            bsdf.inputs['Alpha'].default_value = .5; m.blend_method = 'BLEND'; m.show_transparent_back = False
        m.diffuse_color = (c[0], c[1], c[2], 1)
    return m


PARTS = []  # (object, group, slot)
GROUP = ['Body']  # current default group (set by modelling code)


def finish(ob, group, slot, bevel=0.0, segs=2, smooth=True):
    ob.data.materials.clear(); ob.data.materials.append(mat(slot))
    if bevel > 0:
        md = ob.modifiers.new('bevel', 'BEVEL'); md.width = bevel; md.segments = segs; md.limit_method = 'ANGLE'; md.angle_limit = math.radians(30)
    if smooth:
        for p in ob.data.polygons: p.use_smooth = True
        ob.data.use_auto_smooth = True; ob.data.auto_smooth_angle = math.radians(38)
    PARTS.append((ob, group, slot)); return ob


def mesh_ob(name, verts, faces):
    me = bpy.data.meshes.new(name); me.from_pydata([tuple(v) for v in verts], [], faces); me.update()
    ob = bpy.data.objects.new(name, me); bpy.context.collection.objects.link(ob); return ob


def outward(ob):
    bm = bmesh.new(); bm.from_mesh(ob.data); bmesh.ops.recalc_face_normals(bm, faces=bm.faces); bm.to_mesh(ob.data); bm.free()


def hull(name, pts8, group, slot, bevel=.02, segs=2):
    """8 corner points in Unity space: bottom (4, counter-clockwise from left-rear seen from above), top (4, same order)."""
    v = [B(*p) for p in pts8]
    f = [(0, 3, 2, 1), (4, 5, 6, 7), (0, 1, 5, 4), (1, 2, 6, 5), (2, 3, 7, 6), (3, 0, 4, 7)]
    ob = mesh_ob(name, v, f); outward(ob)
    return finish(ob, group, slot, bevel, segs)


def box(name, c, s, group, slot, bevel=.01, rot=(0, 0, 0)):
    x, y, z = c; a, b, d = s[0] / 2, s[1] / 2, s[2] / 2
    pts = [(-a, -b, -d), (a, -b, -d), (a, -b, d), (-a, -b, d), (-a, b, -d), (a, b, -d), (a, b, d), (-a, b, d)]
    R = Matrix.Rotation(math.radians(rot[0]), 3, 'X') @ Matrix.Rotation(math.radians(rot[1]), 3, 'Y') @ Matrix.Rotation(math.radians(rot[2]), 3, 'Z')
    pts = [tuple(R @ Vector(p) + Vector((x, y, z))) for p in pts]
    return hull(name, pts, group, slot, bevel, 1 if bevel < .015 else 2)


def cyl(name, p0, p1, r, group, slot, verts=10, r1=None, cap=True, smooth=True):
    a, b = B(*p0), B(*p1); d = b - a; L = d.length
    bm = bmesh.new()
    bmesh.ops.create_cone(bm, cap_ends=cap, cap_tris=False, segments=verts, radius1=r, radius2=r if r1 is None else r1, depth=L)
    me = bpy.data.meshes.new(name); bm.to_mesh(me); bm.free()
    ob = bpy.data.objects.new(name, me); bpy.context.collection.objects.link(ob)
    ob.matrix_world = Matrix.Translation((a + b) / 2) @ d.normalized().to_track_quat('Z', 'Y').to_matrix().to_4x4()
    return finish(ob, group, slot, smooth=smooth)


def blob(name, c, r, group, slot, segs=14, rings=9, rot=(0, 0, 0)):
    """Ellipsoid at c (Unity) with radii r (Unity x, y, z); rot = degrees about Unity x, y, z (applied x, then y, then z)."""
    bm = bmesh.new(); bmesh.ops.create_uvsphere(bm, u_segments=segs, v_segments=rings, radius=1)
    me = bpy.data.meshes.new(name); bm.to_mesh(me); bm.free()
    ob = bpy.data.objects.new(name, me); bpy.context.collection.objects.link(ob)
    R = Matrix.Rotation(math.radians(rot[0]), 4, 'X') @ Matrix.Rotation(math.radians(rot[1]), 4, 'Z') @ Matrix.Rotation(math.radians(rot[2]), 4, 'Y')
    ob.matrix_world = Matrix.Translation(B(*c)) @ R @ Matrix.Diagonal((r[0], r[2], r[1], 1))
    return finish(ob, group, slot)


def limb(name, p0, p1, r0, r1, group, slot, verts=10, ends=True):
    """A tapered capsule (cylinder with ball ends)."""
    cyl(name, p0, p1, r0, group, slot, verts, r1=r1, cap=False)
    if ends:
        blob(name + ' joint', p0, (r0, r0, r0), group, slot, verts, 6)
        blob(name + ' end', p1, (r1, r1, r1), group, slot, verts, 6)


def strip(name, prof, width, thick, x, group, slot, bevel=.006):
    """A curved panel: profile points (z, y) in Unity space, extruded across +/- width/2 at x, thickness along the normal."""
    v = []; n = len(prof)
    for i, (z, y) in enumerate(prof):
        z0, y0 = prof[max(i - 1, 0)]; z1, y1 = prof[min(i + 1, n - 1)]; tz, ty = z1 - z0, y1 - y0; L = math.hypot(tz, ty); nz, ny = -ty / L, tz / L
        for s in (-1, 1):
            for t in (0, 1): v.append(B(x + s * width / 2, y + ny * thick * t, z + nz * thick * t))
    f = []
    for i in range(n - 1):
        a, b = i * 4, (i + 1) * 4
        f += [(a + 0, a + 2, b + 2, b + 0), (a + 1, b + 1, b + 3, a + 3), (a + 0, b + 0, b + 1, a + 1), (a + 2, a + 3, b + 3, b + 2)]
    f += [(0, 1, 3, 2), ((n - 1) * 4 + 0, (n - 1) * 4 + 2, (n - 1) * 4 + 3, (n - 1) * 4 + 1)]
    ob = mesh_ob(name, v, f); outward(ob)
    return finish(ob, group, slot, bevel, 1)


def tube(name, pts, r, group, slot, res=4):
    """A bent pipe through Unity-space points."""
    cu = bpy.data.curves.new(name, 'CURVE'); cu.dimensions = '3D'; cu.bevel_depth = r; cu.bevel_resolution = 1; cu.resolution_u = res
    sp = cu.splines.new('BEZIER'); sp.bezier_points.add(len(pts) - 1)
    for bp, p in zip(sp.bezier_points, pts): bp.co = B(*p); bp.handle_left_type = bp.handle_right_type = 'AUTO'
    ob = bpy.data.objects.new(name, cu); bpy.context.collection.objects.link(ob)
    bpy.context.view_layer.objects.active = ob; ob.select_set(True); bpy.ops.object.convert(target='MESH'); ob.select_set(False)
    return finish(ob, group, slot)


def loft(name, sections, group, slot, cap=True, bevel=0.0, smooth=True):
    """A lofted body through cross-sections. Each section is a list of Unity-space points (same count, same winding);
    cap = True (both ends), 'top', 'bottom' or False."""
    n = len(sections[0]); v = [B(*p) for s in sections for p in s]; f = []
    for i in range(len(sections) - 1):
        for j in range(n): f.append((i * n + j, i * n + (j + 1) % n, (i + 1) * n + (j + 1) % n, (i + 1) * n + j))
    if cap in (True, 'bottom'): f.append(tuple(range(n))[::-1])
    if cap in (True, 'top'): f.append(tuple(range((len(sections) - 1) * n, len(sections) * n)))
    ob = mesh_ob(name, v, f); outward(ob)
    return finish(ob, group, slot, bevel, 2, smooth)


def ring(c, rx, ry, n=12, axis='z', phase=0.0):
    """Points of an ellipse around centre c (Unity) in the plane normal to the given Unity axis."""
    out = []
    for i in range(n):
        a = phase + i * 2 * math.pi / n; u, w = math.cos(a) * rx, math.sin(a) * ry
        if axis == 'z': out.append((c[0] + u, c[1] + w, c[2]))
        elif axis == 'y': out.append((c[0] + u, c[1], c[2] + w))
        else: out.append((c[0], c[1] + w, c[2] + u))
    return out


def tyre(group, R, rx, ry, seg=28, side=8, knobs=0, squared=.92, slot='rubber', knob=(.8, .024, .05)):
    """At the origin, axle along Unity x: a torus with a squared section (outer radius R + ry), optional tread blocks."""
    bm = bmesh.new(); verts = []
    for i in range(seg):
        a = i * 2 * math.pi / seg; row = []
        for j in range(side):
            b = j * 2 * math.pi / side; sq = 1.0 if abs(math.cos(b)) < .75 else squared
            rr = R + ry * math.cos(b) * sq; x = rx * math.sin(b)
            row.append(bm.verts.new((x, math.cos(a) * rr, math.sin(a) * rr)))
        verts.append(row)
    for i in range(seg):
        for j in range(side): bm.faces.new((verts[i][j], verts[(i + 1) % seg][j], verts[(i + 1) % seg][(j + 1) % side], verts[i][(j + 1) % side]))
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    me = bpy.data.meshes.new(group + ' tyre'); bm.to_mesh(me); bm.free(); ob = bpy.data.objects.new(group + ' tyre', me); bpy.context.collection.objects.link(ob); finish(ob, group, slot)
    for i in range(knobs):
        a = (i + .5) * 2 * math.pi / knobs
        for s in (-1, 1):
            kr = R + ry * squared + knob[1] * .15
            c = (s * rx * .5, math.cos(a + s * .05) * kr, math.sin(a + s * .05) * kr)
            k = box(group + ' tread', (0, 0, 0), (rx * knob[0], knob[1], knob[2]), group, slot, 0)
            k.matrix_world = Matrix.Translation(Vector(c)) @ Matrix.Rotation(a - math.pi / 2, 4, 'X')
            k.location = Vector(c)


def join_all():
    """Join parts into one object per (group, slot), named '<group>__<slot>'; modifiers and transforms applied."""
    groups = {}
    for ob, g, s in PARTS: groups.setdefault((g, s), []).append(ob)
    out = []
    for (g, s), obs in groups.items():
        for ob in obs:
            bpy.context.view_layer.objects.active = ob
            for md in list(ob.modifiers): bpy.ops.object.modifier_apply(modifier=md.name)
        bpy.ops.object.select_all(action='DESELECT')
        for ob in obs: ob.select_set(True)
        bpy.context.view_layer.objects.active = obs[0]
        if len(obs) > 1: bpy.ops.object.join()
        ob = bpy.context.view_layer.objects.active; ob.name = ob.data.name = f'{g}__{s}'
        bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
        out.append(ob)
    PARTS.clear()
    return out


def set_origin(ob, unity_point):
    """Move an object's origin to a Unity-space point without moving its geometry."""
    p = B(*unity_point); ob.data.transform(Matrix.Translation(-p)); ob.location = p


def tris(objs=None):
    return sum(sum(len(p.vertices) - 2 for p in ob.data.polygons) for ob in (objs or bpy.data.objects) if ob.type == 'MESH')


def export(name):
    """Save SourceArt/Blender/<name>.blend and Assets/Resources/VehicleModels/<name>.fbx (the 0.73 export settings)."""
    os.makedirs(os.path.join(ROOT, 'SourceArt', 'Blender'), exist_ok=True)
    os.makedirs(os.path.join(ROOT, 'Assets', 'Resources', 'VehicleModels'), exist_ok=True)
    bpy.ops.wm.save_as_mainfile(filepath=os.path.join(ROOT, 'SourceArt', 'Blender', name + '.blend'))
    bpy.ops.export_scene.fbx(filepath=os.path.join(ROOT, 'Assets', 'Resources', 'VehicleModels', name + '.fbx'), use_selection=False, object_types={'MESH'},
                             axis_forward='-Z', axis_up='Y', bake_space_transform=True, apply_unit_scale=True, apply_scale_options='FBX_SCALE_UNITS',
                             mesh_smooth_type='OFF', use_mesh_modifiers=True, add_leaf_bones=False, path_mode='STRIP', use_custom_props=False)
    counts = {ob.name: tris([ob]) for ob in bpy.data.objects if ob.type == 'MESH'}
    print(name.upper(), 'triangles', tris(), counts)


def made(start):
    """Objects added to PARTS since index start."""
    return [ob for ob, g, s in PARTS[start:]]


def place(objs, M4):
    for ob in objs: ob.matrix_world = M4 @ ob.matrix_world


def at(unity_point, turn=False):
    """Blender matrix placing a part built at the origin at a Unity point (turn = half a turn about the vertical, so the
    outer face of a wheel built facing +x faces -x)."""
    M4 = Matrix.Translation(B(*unity_point))
    return M4 @ Matrix.Rotation(math.pi, 4, 'Z') if turn else M4
