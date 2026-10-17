"""0.73 Part D pilot: the Needle 600 motorcycle and its rider, generated in Blender from this script.

Run:  blender --background --factory-startup --python Tools/Blender/needle600.py -- [--revision N]

Everything is written in the game's vehicle space (Unity: x right, y up, z forward; metres; origin = vehicle body
origin) and converted to Blender space (x right, y forward, z up). The model is fitted to the existing vehicle, not the
other way round: wheel centres (0, -0.20, +/-0.825), wheel radius 0.33, rider hands on the grips (+/-0.40, 0.72, 0.58),
feet on the pegs. Output:
  SourceArt/Blender/Needle600.blend                (editable source)
  Assets/Resources/VehicleModels/Needle600.fbx     (Unity; FBX, Z forward / Y up, transforms baked)
Objects are named "<Group>__<slot>": groups Body (fixed), Front (steers about the fork axis), Rider (rides with the
steering pose), WheelFront / WheelRear (origin on the axle, axle along x). Slots are the game's shared vehicle materials
(paint, metal, engine, rubber, lamp, tail) and the rider's identity materials (skin, shirt, trousers, shoes, hair, eyes,
pupil, mouth); the game assigns its own materials by slot, so the player's colour reaches only the paint.
"""
import bpy, bmesh, math, sys, os
from mathutils import Vector, Matrix

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), '..', '..'))
REV = int(sys.argv[sys.argv.index('--revision') + 1]) if '--revision' in sys.argv else 3

def B(x, y, z):  # Unity vehicle space -> Blender space
    return Vector((x, z, y))

COLORS = {  # preview colours only (the game uses its own materials)
    'paint': (.12, .36, .95), 'metal': (.48, .56, .60), 'engine': (.20, .21, .23), 'rubber': (.035, .043, .054),
    'lamp': (.95, .91, .7), 'tail': (.65, .03, .03), 'skin': (.78, .52, .36), 'shirt': (.14, .36, .70),
    'trousers': (.12, .20, .32), 'shoes': (.17, .095, .05), 'hair': (.18, .065, .028), 'eyes': (.96, .95, .90),
    'pupil': (.035, .043, .054), 'mouth': (.24, .075, .065)}

def mat(slot):
    m = bpy.data.materials.get(slot)
    if not m:
        m = bpy.data.materials.new(slot); m.use_nodes = True
        bsdf = m.node_tree.nodes['Principled BSDF']; c = COLORS[slot]
        bsdf.inputs['Base Color'].default_value = (c[0], c[1], c[2], 1)
        bsdf.inputs['Roughness'].default_value = .35 if slot in ('paint', 'metal', 'lamp') else .6
        if slot == 'metal': bsdf.inputs['Metallic'].default_value = .6
        m.diffuse_color = (c[0], c[1], c[2], 1)
    return m

PARTS = []  # (object, group, slot)

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

def hull(name, pts8, group, slot, bevel=.02, segs=2):
    """8 corner points in Unity space: bottom (4, counter-clockwise from left-rear seen from above), top (4, same order)."""
    v = [B(*p) for p in pts8]
    f = [(0, 3, 2, 1), (4, 5, 6, 7), (0, 1, 5, 4), (1, 2, 6, 5), (2, 3, 7, 6), (3, 0, 4, 7)]
    ob = mesh_ob(name, v, f)
    # outward normals
    bm = bmesh.new(); bm.from_mesh(ob.data); bmesh.ops.recalc_face_normals(bm, faces=bm.faces); bm.to_mesh(ob.data); bm.free()
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
    bm = bmesh.new(); bmesh.ops.create_uvsphere(bm, u_segments=segs, v_segments=rings, radius=1)
    me = bpy.data.meshes.new(name); bm.to_mesh(me); bm.free()
    ob = bpy.data.objects.new(name, me); bpy.context.collection.objects.link(ob)
    R = Matrix.Rotation(math.radians(rot[0]), 4, 'X') @ Matrix.Rotation(math.radians(rot[1]), 4, 'Z') @ Matrix.Rotation(math.radians(rot[2]), 4, 'Y')
    ob.matrix_world = Matrix.Translation(B(*c)) @ R @ Matrix.Diagonal((r[0], r[2], r[1], 1))
    return finish(ob, group, slot)

def limb(name, p0, p1, r0, r1, group, slot, verts=10):
    """A tapered capsule (cylinder with ball ends)."""
    cyl(name, p0, p1, r0, group, slot, verts, r1=r1, cap=False)
    blob(name + ' joint', p0, (r0, r0, r0), group, slot, verts, 6)
    blob(name + ' end', p1, (r1, r1, r1), group, slot, verts, 6)

def strip(name, prof, width, thick, x, group, slot):
    """A curved panel: profile points (z, y) in Unity space, extruded across +/- width/2 at x, thickness along the normal."""
    v = []; n = len(prof)
    for i, (z, y) in enumerate(prof):
        z0, y0 = prof[max(i - 1, 0)]; z1, y1 = prof[min(i + 1, n - 1)]; tz, ty = z1 - z0, y1 - y0; L = math.hypot(tz, ty); nz, ny = -ty / L, tz / L
        for s in (-1, 1):
            for t in (0, 1): v.append(B(x + s * width / 2, y + ny * thick * t, z + nz * thick * t))
    f = []
    for i in range(n - 1):
        a, b = i * 4, (i + 1) * 4  # verts per section: (-,0) (-,1) (+,0) (+,1)
        f += [(a + 0, a + 2, b + 2, b + 0), (a + 1, b + 1, b + 3, a + 3), (a + 0, b + 0, b + 1, a + 1), (a + 2, a + 3, b + 3, b + 2)]
    f += [(0, 1, 3, 2), ((n - 1) * 4 + 0, (n - 1) * 4 + 2, (n - 1) * 4 + 3, (n - 1) * 4 + 1)]
    ob = mesh_ob(name, v, f)
    bm = bmesh.new(); bm.from_mesh(ob.data); bmesh.ops.recalc_face_normals(bm, faces=bm.faces); bm.to_mesh(ob.data); bm.free()
    return finish(ob, group, slot, .006, 1)

def tube(name, pts, r, group, slot, verts=8):
    """A bent pipe through Unity-space points (exhaust)."""
    cu = bpy.data.curves.new(name, 'CURVE'); cu.dimensions = '3D'; cu.bevel_depth = r; cu.bevel_resolution = 1; cu.resolution_u = 4
    sp = cu.splines.new('BEZIER'); sp.bezier_points.add(len(pts) - 1)
    for bp, p in zip(sp.bezier_points, pts): bp.co = B(*p); bp.handle_left_type = bp.handle_right_type = 'AUTO'
    ob = bpy.data.objects.new(name, cu); bpy.context.collection.objects.link(ob)
    bpy.context.view_layer.objects.active = ob; ob.select_set(True); bpy.ops.object.convert(target='MESH'); ob.select_set(False)
    return finish(ob, group, slot)

# ---------------------------------------------------------------- the motorcycle
AX_F = (0, -.20, .825); AX_R = (0, -.20, -.825)
RAKE = Vector((0, .76, -.305))  # steering axis (Unity space) through the front axle; t = 1 at the steering head
def axis(t): p = Vector(AX_F) + RAKE * t; return (p.x, p.y, p.z)

def wheel(group, front):
    """At the origin, axle along x. Knobbly tyre, alloy rim, spokes, hub, brake disc (front) or sprocket (rear)."""
    # tyre: a torus with a squared section, plus tread knobs
    bm = bmesh.new(); seg, side = 28, 8; R, rx, ry = .266, (.058 if front else .066), .058
    verts = []
    for i in range(seg):
        a = i * 2 * math.pi / seg; row = []
        for j in range(side):
            b = j * 2 * math.pi / side; sq = 1.0 if abs(math.cos(b)) < .75 else .92
            rr = R + ry * math.cos(b) * sq; x = rx * math.sin(b)
            row.append(bm.verts.new((x, math.cos(a) * rr, math.sin(a) * rr)))
        verts.append(row)
    for i in range(seg):
        for j in range(side): bm.faces.new((verts[i][j], verts[(i + 1) % seg][j], verts[(i + 1) % seg][(j + 1) % side], verts[i][(j + 1) % side]))
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    me = bpy.data.meshes.new(group + ' tyre'); bm.to_mesh(me); bm.free(); ob = bpy.data.objects.new(group + ' tyre', me); bpy.context.collection.objects.link(ob); finish(ob, group, 'rubber')
    knobs = 24 if front else 22
    for i in range(knobs):
        a = (i + .5) * 2 * math.pi / knobs
        for s in (-1, 1):
            kr = R + ry * .92 + .005  # knob centre just inside the tread so it is seated on it; outer edge at 0.33
            c = (s * rx * .55, math.cos(a + s * .06) * kr, math.sin(a + s * .06) * kr)
            k = box(group + ' knob', (0, 0, 0), (rx * .7, .022, .035), group, 'rubber', 0)
            k.matrix_world = Matrix.Translation(Vector(c)) @ Matrix.Rotation(a - math.pi / 2, 4, 'X')
            k.location = Vector(c)
    # rim and hub
    cyl(group + ' rim', (-.034, 0, 0), (.034, 0, 0), .218, group, 'metal', 28, cap=False)
    cyl(group + ' rim inner', (-.03, 0, 0), (.03, 0, 0), .196, group, 'metal', 28, cap=False)
    cyl(group + ' hub', (-.07, 0, 0), (.07, 0, 0), .045, group, 'metal', 12)
    for i in range(16):
        a = i * 2 * math.pi / 16; s = -1 if i % 2 else 1
        cyl(group + ' spoke', (s * .05, math.cos(a + .25) * .04, math.sin(a + .25) * .04), (s * .018, math.cos(a) * .2, math.sin(a) * .2), .0045, group, 'metal', 4, smooth=False)
    if front: cyl(group + ' brake disc', (.075, 0, 0), (.083, 0, 0), .115, group, 'metal', 20)
    else: cyl(group + ' sprocket', (-.085, 0, 0), (-.075, 0, 0), .10, group, 'engine', 20)

def bike():
    # --- frame (fixed) ---
    head0, head1 = axis(.80), axis(1.06)
    cyl('Steering head', head0, head1, .036, 'Body', 'metal', 12)
    cyl('Main spine', axis(1.0), (0, .37, -.20), .032, 'Body', 'metal', 10)
    cyl('Down tube', axis(.84), (0, -.06, .20), .03, 'Body', 'metal', 10)
    cyl('Cradle', (0, -.06, .20), (0, -.13, -.06), .028, 'Body', 'metal', 10)
    cyl('Cradle rear', (0, -.13, -.06), (0, .03, -.17), .028, 'Body', 'metal', 10)
    for s in (-1, 1):
        cyl('Seat rail', (s * .07, .37, -.20), (s * .075, .41, -.80), .018, 'Body', 'metal', 8)
        cyl('Rear strut', (s * .07, .05, -.17), (s * .075, .37, -.70), .016, 'Body', 'metal', 8)
        cyl('Pivot plate', (s * .07, .03, -.12), (s * .07, .37, -.22), .022, 'Body', 'metal', 8)
        # swing arm, box section tapering to the axle
        hull('Swing arm', [(s * .055 - .02, -.015, -.17), (s * .055 + .02, -.015, -.17), (s * .078 + .014, -.215, -.86), (s * .078 - .014, -.215, -.86),
                           (s * .055 - .02, .055, -.17), (s * .055 + .02, .055, -.17), (s * .078 + .014, -.17, -.86), (s * .078 - .014, -.17, -.86)], 'Body', 'metal', .008, 1)
        box('Foot peg', (s * .205, .0, -.19), (.10, .025, .055), 'Body', 'metal', .004)
        box('Peg mount', (s * .12, .02, -.19), (.08, .05, .05), 'Body', 'metal', .006)
    cyl('Swing arm brace', (-.06, .03, -.42), (.06, .03, -.42), .02, 'Body', 'metal', 8)
    cyl('Axle rear', (-.10, -.20, -.825), (.10, -.20, -.825), .014, 'Body', 'metal', 8)
    # rear shock: spring and body
    cyl('Shock spring', (0, .33, -.24), (0, .08, -.40), .034, 'Body', 'metal', 12)
    cyl('Shock body', (0, .36, -.22), (0, .22, -.31), .026, 'Body', 'engine', 10)
    # --- engine ---
    hull('Crankcase', [(-.12, -.10, -.12), (.12, -.10, -.12), (.12, -.08, .16), (-.12, -.08, .16), (-.13, .12, -.10), (.13, .12, -.10), (.13, .14, .18), (-.13, .14, .18)], 'Body', 'engine', .03)
    blob('Clutch cover', (.135, .02, .03), (.035, .1, .1), 'Body', 'engine', 14, 8)
    blob('Stator cover', (-.135, .03, -.02), (.03, .085, .085), 'Body', 'metal', 14, 8)
    hull('Cylinder', [(-.09, .12, .02), (.09, .12, .02), (.09, .12, .20), (-.09, .12, .20), (-.08, .32, .08), (.08, .32, .08), (.08, .32, .25), (-.08, .32, .25)], 'Body', 'engine', .015, 1)
    for i in range(4):
        y = .17 + i * .04
        hull('Cooling fin', [(-.115, y, .0 + i * .02), (.115, y, .0 + i * .02), (.115, y, .225 + i * .02), (-.115, y, .225 + i * .02),
                             (-.115, y + .012, .0 + i * .02), (.115, y + .012, .0 + i * .02), (.115, y + .012, .225 + i * .02), (-.115, y + .012, .225 + i * .02)], 'Body', 'metal', 0, 1)
    box('Carburettor', (0, .25, -.06), (.08, .07, .08), 'Body', 'metal', .01)
    box('Air box', (0, .30, -.30), (.17, .15, .2), 'Body', 'engine', .025)
    # radiator behind the shrouds
    for s in (-1, 1): box('Radiator', (s * .11, .30, .30), (.035, .22, .12), 'Body', 'engine', .008, (0, 0, 0))
    # chain (left side) from the front sprocket to the rear sprocket
    # 0.89: the chain 7 mm outboard (a side of it lay on the rear sprocket's face)
    cyl('Chain upper', (-.092, .06, -.07), (-.092, -.10, -.825), .011, 'Body', 'engine', 6)
    cyl('Chain lower', (-.092, -.02, -.07), (-.092, -.30, -.825), .011, 'Body', 'engine', 6)
    # exhaust (right side): header forward and down, back along the side, up into the silencer under the seat
    tube('Exhaust header', [(.05, .27, .25), (.10, .21, .34), (.16, .09, .30), (.19, .08, .10), (.20, .17, -.18), (.20, .26, -.38)], .026, 'Body', 'metal', 8)
    cyl('Silencer', (.20, .26, -.38), (.20, .37, -.80), .055, 'Body', 'metal', 14)
    cyl('Silencer cap', (.20, .37, -.80), (.20, .38, -.83), .04, 'Body', 'engine', 12)
    # --- bodywork (paint) ---
    hull('Fuel tank', [(-.13, .30, .02), (.13, .30, .02), (.14, .30, .44), (-.14, .30, .44), (-.085, .50, -.02), (.085, .50, -.02), (.10, .54, .38), (-.10, .54, .38)], 'Body', 'paint', .045, 3)
    box('Fuel cap', (0, .545, .24), (.06, .02, .06), 'Body', 'metal', .008)
    for s in (-1, 1):
        # radiator shrouds: the big coloured side panels beside the tank
        hull('Shroud', [(s * .15, .17, .44), (s * .17, .17, .44), (s * .165, .24, .10), (s * .145, .24, .10),
                        (s * .155, .55, .52), (s * .175, .55, .52), (s * .16, .50, .10), (s * .14, .50, .10)], 'Body', 'paint', .012, 1)
        # rear side number panels under the seat
        hull('Side panel', [(s * .12, .22, -.24), (s * .14, .22, -.24), (s * .14, .30, -.66), (s * .12, .30, -.66),
                            (s * .12, .40, -.20), (s * .14, .40, -.20), (s * .14, .44, -.68), (s * .12, .44, -.68)], 'Body', 'paint', .012, 1)
    strip('Rear fender', [(-.30, .41), (-.50, .43), (-.72, .46), (-.92, .49), (-1.06, .51)], .20, .016, 0, 'Body', 'paint')
    box('Tail lamp', (0, .475, -1.045), (.11, .045, .03), 'Body', 'tail', .008)
    box('Tail lamp housing', (0, .475, -1.025), (.13, .06, .03), 'Body', 'engine', .008)
    # seat: long, flat dirt-bike seat, narrow at the tank
    hull('Seat', [(-.075, .40, .20), (.075, .40, .20), (.12, .39, -.76), (-.12, .39, -.76), (-.07, .49, .14), (.07, .49, .14), (.115, .485, -.74), (-.115, .485, -.74)], 'Body', 'rubber', .03, 2)
    # --- front end (steers about the fork axis) ---
    for s in (-1, 1):
        x = s * .088
        cyl('Fork slider', (x, -.20, .825), (x, axis(.42)[1], axis(.42)[2]), .029, 'Front', 'engine', 12)
        cyl('Fork tube', (x, axis(.40)[1], axis(.40)[2]), (x, axis(1.075)[1], axis(1.075)[2]), .022, 'Front', 'metal', 12)  # 0.89: its top 5 mm inside the upper clamp (was on the clamp's top face)
        box('Axle lug', (x, -.20, .835), (.04, .06, .06), 'Front', 'engine', .008)
    cyl('Axle front', (-.115, -.20, .825), (.115, -.20, .825), .014, 'Front', 'metal', 8)  # 0.89: ends 7 mm past the lugs (were flush)
    for t, h in ((.84, .035), (1.06, .04)):
        p = axis(t); box('Triple clamp', p, (.26, h, .09), 'Front', 'metal', .01, (-22, 0, 0))
    # bar risers and handlebars (slight rise and sweep back to the grips at +/-0.40, 0.72, 0.58)
    top = axis(1.07)
    for s in (-1, 1): cyl('Bar riser', (s * .035, top[1], top[2]), (s * .035, top[1] + .06, top[2] + .02), .016, 'Front', 'metal', 8)
    cyl('Handlebar', (-.24, .685, .555), (.24, .685, .555), .013, 'Front', 'metal', 10)
    for s in (-1, 1):
        cyl('Handlebar bend', (s * .24, .685, .555), (s * .33, .715, .575), .013, 'Front', 'metal', 10)
        cyl('Grip', (s * .33, .715, .575), (s * .45, .725, .583), .021, 'Front', 'rubber', 10)
        box('Lever', (s * .34, .72, .62), (.13, .012, .02), 'Front', 'metal', .004, (0, s * 18, 0))
    cyl('Bar pad', (-.09, .69, .556), (.09, .69, .556), .026, 'Front', 'engine', 10)  # 0.89: wraps the bar (was flush below it)
    # number plate with the headlight, square to the fork
    plate_c = Vector(axis(1.0)) + Vector((0, -.05, .085))
    hull('Number plate', [(-.13, plate_c.y - .15, plate_c.z + .03), (.13, plate_c.y - .15, plate_c.z + .03), (.13, plate_c.y - .15, plate_c.z + .06), (-.13, plate_c.y - .15, plate_c.z + .06),
                          (-.12, plate_c.y + .10, plate_c.z - .07), (.12, plate_c.y + .10, plate_c.z - .07), (.12, plate_c.y + .10, plate_c.z - .04), (-.12, plate_c.y + .10, plate_c.z - .04)], 'Front', 'paint', .02, 2)
    lamp_c = plate_c + Vector((0, -.05, .05))
    cyl('Headlamp', (0, lamp_c.y + .02, lamp_c.z - .005), (0, lamp_c.y - .006, lamp_c.z + .03), .062, 'Front', 'lamp', 16)
    # front fender: high, motocross style, under the lower clamp
    lo = axis(.80)
    strip('Front fender', [(lo[2] - .26, lo[1] - .045), (lo[2] - .10, lo[1] - .005), (lo[2] + .06, lo[1] + .0), (lo[2] + .22, lo[1] - .03), (lo[2] + .36, lo[1] - .09), (lo[2] + .44, lo[1] - .15)], .15, .014, 0, 'Front', 'paint')

# ---------------------------------------------------------------- the rider (helmet-free, flat cap, visible face and hair)
def rider():
    G = 'Rider'
    # hips and torso: seated on the seat, leaning forward toward the bars
    blob('Hips', (0, .53, -.30), (.17, .11, .15), G, 'trousers', 14, 8)
    hull('Torso', [(-.15, .56, -.38), (.15, .56, -.38), (.15, .58, -.18), (-.15, .58, -.18), (-.20, .99, -.20), (.20, .99, -.20), (.19, 1.02, .0), (-.19, 1.02, .0)], G, 'shirt', .07, 3)
    blob('Chest', (0, .88, -.08), (.16, .12, .08), G, 'shirt', 12, 8)
    blob('Shoulders', (0, 1.0, -.10), (.21, .06, .11), G, 'shirt', 12, 6)
    cyl('Neck', (0, 1.02, -.08), (0, 1.12, -.035), .05, G, 'skin', 10)
    # head: looking ahead
    hc = (0, 1.225, -.005)
    blob('Head', hc, (.105, .125, .115), G, 'skin', 18, 12)
    blob('Jaw', (0, 1.165, .025), (.085, .055, .08), G, 'skin', 14, 8)
    blob('Nose', (0, 1.215, .113), (.02, .03, .025), G, 'skin', 10, 6)
    for s in (-1, 1):
        blob('Ear', (s * .106, 1.22, -.01), (.018, .034, .026), G, 'skin', 10, 6)
        blob('Eye', (s * .040, 1.255, .095), (.021, .015, .011), G, 'eyes', 10, 6)
        blob('Pupil', (s * .040, 1.255, .104), (.009, .010, .005), G, 'pupil', 8, 5)
        box('Brow', (s * .042, 1.289, .101), (.048, .011, .012), G, 'hair', .004, (0, 0, s * -8))
    blob('Mouth', (0, 1.175, .098), (.032, .008, .012), G, 'mouth', 10, 6)
    # hair: back and sides below the cap, a fringe at the front
    blob('Hair', (0, 1.255, -.035), (.116, .11, .118), G, 'hair', 16, 10)
    blob('Fringe', (-.02, 1.315, .075), (.075, .022, .03), G, 'hair', 12, 6, (0, 0, -8))
    # flat cap: low crown leaning forward, short brim
    blob('Flat cap', (0, 1.335, -.01), (.125, .05, .13), G, 'hair', 18, 8, (8, 0, 0))
    hull('Cap brim', [(-.085, 1.318, .07), (.085, 1.318, .07), (.07, 1.296, .16), (-.07, 1.296, .16), (-.085, 1.33, .07), (.085, 1.33, .07), (.07, 1.308, .16), (-.07, 1.308, .16)], G, 'hair', .01, 1)
    for s in (-1, 1):
        sh, el, ha = (s * .19, .97, -.10), (s * .29, .82, .17), (s * .395, .728, .565)
        # short sleeve ending in a clean cuff just above the elbow; bare forearm from the elbow joint to the hand
        cuff = tuple(Vector(sh) + (Vector(el) - Vector(sh)) * .86)
        cyl('Upper arm', sh, cuff, .056, G, 'shirt', 12, r1=.05, cap=False)
        blob('Shoulder', sh, (.058, .058, .058), G, 'shirt', 12, 8)
        cyl('Cuff', tuple(Vector(cuff) - (Vector(el) - Vector(sh)).normalized() * .025), cuff, .053, G, 'shirt', 12)
        cyl('Elbow', cuff, el, .042, G, 'skin', 10, cap=False)
        limb('Forearm', el, ha, .043, .036, G, 'skin')
        blob('Hand', (s * .395, .735, .575), (.04, .038, .05), G, 'skin', 10, 6)
        hip, knee, ankle = (s * .10, .52, -.27), (s * .205, .42, .07), (s * .205, .10, -.13)
        limb('Thigh', hip, knee, .072, .058, G, 'trousers')
        limb('Shin', knee, ankle, .055, .045, G, 'trousers')
        hull('Boot', [(s * .205 - .05, -.002, -.23), (s * .205 + .05, -.002, -.23), (s * .205 + .05, -.002, -.02), (s * .205 - .05, -.002, -.02),
                      (s * .205 - .048, .13, -.21), (s * .205 + .048, .13, -.21), (s * .205 + .045, .07, -.03), (s * .205 - .045, .07, -.03)], G, 'shoes', .02, 2)

# ---------------------------------------------------------------- build, join by group and slot, export
def build():
    bpy.ops.wm.read_factory_settings(use_empty=True)
    bike(); rider()
    wheel('WheelFront', True); wheel('WheelRear', False)
    # join
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
    # wheels: origin on the axle
    for ob in out:
        if ob.name.startswith('Wheel'):
            z = .825 if ob.name.startswith('WheelFront') else -.825
            ob.location = B(0, -.20, z)
    return out

def tris():
    n = 0
    for ob in bpy.data.objects:
        if ob.type == 'MESH': n += sum(len(p.vertices) - 2 for p in ob.data.polygons)
    return n

if __name__ == '__main__':
    objs = build()
    os.makedirs(os.path.join(ROOT, 'SourceArt', 'Blender'), exist_ok=True)
    os.makedirs(os.path.join(ROOT, 'Assets', 'Resources', 'VehicleModels'), exist_ok=True)
    bpy.ops.wm.save_as_mainfile(filepath=os.path.join(ROOT, 'SourceArt', 'Blender', 'Needle600.blend'))
    bpy.ops.export_scene.fbx(filepath=os.path.join(ROOT, 'Assets', 'Resources', 'VehicleModels', 'Needle600.fbx'), use_selection=False, object_types={'MESH'},
                             axis_forward='-Z', axis_up='Y', bake_space_transform=True, apply_unit_scale=True, apply_scale_options='FBX_SCALE_UNITS',
                             mesh_smooth_type='OFF', use_mesh_modifiers=True, add_leaf_bones=False, path_mode='STRIP', use_custom_props=False)
    counts = {ob.name: sum(len(p.vertices) - 2 for p in ob.data.polygons) for ob in bpy.data.objects if ob.type == 'MESH'}
    print('NEEDLE600 triangles', tris(), counts)
