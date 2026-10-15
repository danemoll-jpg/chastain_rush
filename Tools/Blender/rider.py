"""0.75 Part B: the parametric rider, generated in Blender from this script and assembled in Unity from parts.

Run:  blender --background --factory-startup --python Tools/Blender/rider.py

One FBX (Assets/Resources/VehicleModels/Rider.fbx, source SourceArt/Blender/Rider.blend) holds every part in three poses.
Objects are named "<Pose>_<Category>_<Option>_<Body>__<slot>":
  Pose      Moto (Needle 600 vehicle space), Atv (Trail Four vehicle space), Car (relative to the driver's seat point H,
            which each car places; the steering-wheel grips are at H + (+/-0.147, 0.437, 0.396))
  Category  Base (head, face, neck, hands, shoes), Shirt (Tee / Long / Jacket / Leather), Pants (Jeans / Shorts),
            Hair (Short / Medium / Long / Ponytail: everything BELOW the hat band), HairTop (the same styles ABOVE the band;
            hidden under a hat, so hair and hat never pass through each other), Hat (FlatCap / Baseball / Beanie / Cowboy)
  Body      Man / Woman, or Any (hair and hats fit both: the head is the same size)
  Arm       0.78: arm parts carry a fifth key, "<Pose>_<Category>_<Option>_<Body>_<Arm>__<slot>", Arm = U / L / H (upper
            arm, forearm, hand) + R / L (side). Their origin is the joint they turn about (U: shoulder, L: elbow, H: wrist),
            so the game can move the arms (fist wave, victory) with a simple shoulder / elbow / wrist rig (RiderArms).
            0.83: Emblem (Tee / Jacket, Man): a chest print, a plain white ring with a simple generic bird inside (no
            lettering, not any real logo), on the T-shirt / long sleeve (centred) or the jacket (left chest, clear of the zip)
  slot      skin, hair, hat, trim, shirt, pants, shoes, eyes, pupil, mouth (the game colours them per rider); 0.83 also
            inner (the T-shirt under the open leather jacket) and emblem
The head is always upright and faces forward, so hair and hats are the same shapes in every pose. The hat band is a plane
through the head 5.5 cm above its centre, 12 degrees higher at the front; every hat covers the whole head above it.
"""
import bpy, bmesh, math, sys, os
from mathutils import Vector, Matrix
sys.path.insert(0, os.path.dirname(__file__))
import kit
from kit import B, blob, cyl, limb, box, hull, loft, ring

BAND_Y, BAND_TILT = .055, 12.0  # hat band: height above the head centre, tilt (front higher), degrees

# ---------------------------------------------------------------- poses (right side, x >= 0; mirrored for the left)
POSES = {
    # Needle 600 (0.73 rider): seated on the seat, leaning toward the bars, feet on the pegs.
    'Moto': dict(hip=(0, .53, -.30), S=(0, .975, -.10), head=(0, 1.225, -.005), elbow=(.29, .82, .17), hand=(.395, .735, .575),
                 hipj=(.10, .52, -.27), knee=(.205, .42, .07), ankle=(.205, .10, -.13), foot=(0, 0, 1), grip=(1, 0, 0)),
    # Trail Four: more upright, wide bars, feet on the footboards.
    'Atv': dict(hip=(0, .56, -.32), S=(0, 1.00, -.15), head=(0, 1.255, -.08), elbow=(.33, .86, .07), hand=(.42, .755, .36),
                hipj=(.10, .55, -.29), knee=(.27, .52, .03), ankle=(.31, .16, -.10), foot=(0, 0, 1), grip=(1, 0, 0)),
    # Cars (relative to the seat point H): leaning back a little, hands at ten to two, feet on the pedals.
    'Car': dict(hip=(0, 0, 0), S=(0, .50, -.10), head=(0, .74, -.045), elbow=(.25, .30, .13), hand=(.147, .437, .396),
                hipj=(.10, -.01, .02), knee=(.14, .12, .44), ankle=(.13, -.22, .62), foot=(0, .34, .94), grip=(.5, .1, .86)),
    # 0.81 Part B: the dirt bike (tall seat, upright, high wide bars) and the cruiser (low seat, leaning back a little,
    # wide pulled-back bars, feet forward on forward controls), in their own vehicle spaces (Tools/Blender/bikes.py).
    'Dirt': dict(hip=(0, .70, -.25), S=(0, 1.18, -.19), head=(0, 1.43, -.13), elbow=(.33, 1.00, .09), hand=(.42, .905, .35),
                 hipj=(.10, .69, -.22), knee=(.22, .55, .18), ankle=(.21, .17, .02), foot=(0, 0, 1), grip=(1, 0, 0)),
    'Cruiser': dict(hip=(0, .42, -.35), S=(0, .90, -.43), head=(0, 1.14, -.38), elbow=(.31, .73, -.13), hand=(.43, .76, .17),
                    hipj=(.11, .41, -.32), knee=(.20, .50, .08), ankle=(.24, .17, .32), foot=(0, 0, 1), grip=(1, 0, 0)),
    # 0.81 Part D: the people in the scripted scenes (ground at y = 0, facing +z): standing (arms at the sides; legs=True
    # exports each leg as one part turning about its hip, for walking), seated on a log by the fire, seated on a sled.
    'Stand': dict(hip=(0, .925, 0), S=(0, 1.425, -.01), head=(0, 1.67, .02), elbow=(.235, 1.115, -.005), hand=(.24, .80, .06),
                  hipj=(.10, .915, 0), knee=(.105, .475, .03), ankle=(.11, .09, -.01), foot=(0, 0, 1), grip=(1, 0, 0), legs=True),
    'Sit': dict(hip=(0, .64, 0), S=(0, 1.13, -.04), head=(0, 1.37, 0), elbow=(.22, .86, .15), hand=(.17, .62, .37),
                hipj=(.10, .61, .02), knee=(.13, .48, .44), ankle=(.13, .09, .47), foot=(0, 0, 1), grip=(1, 0, 0)),
    'Sled': dict(hip=(0, .30, 0), S=(0, .79, -.06), head=(0, 1.03, -.02), elbow=(.24, .58, .18), hand=(.20, .50, .45),
                 hipj=(.10, .29, .02), knee=(.13, .42, .40), ankle=(.13, .17, .70), foot=(0, .25, 1), grip=(1, 0, 0)),
}
BODY = {  # Man / Woman: same height class, different build
    'Man': dict(sw=.195, hem=(.172, .118), waist=(.165, .11), chest=(.19, .122), upper=(.198, .11), top=(.178, .088), pelvis=(.17, .12, .15),
                arm=(.056, .047, .044, .034), thigh=(.075, .058), shin=(.056, .044), hand=1.0, shoe=1.0, neck=.048),
    'Woman': dict(sw=.17, hem=(.172, .112), waist=(.138, .097), chest=(.158, .107), upper=(.166, .097), top=(.152, .080), pelvis=(.182, .125, .155),
                  arm=(.050, .042, .039, .031), thigh=(.077, .056), shin=(.053, .041), hand=.9, shoe=.92, neck=.042),
}
SHIRTS, PANTS = ('Tee', 'Long', 'Jacket', 'Leather'), ('Jeans', 'Shorts')
# Chastain Rush, Round 28 (C3): Short and Medium remodeled; Mohawk, CrewCut, Spiky, Buzz and Curly added (after the old ones: RiderLook's
# saved indices keep their meaning)
HAIRS, HATS = ('Short', 'Medium', 'Long', 'Ponytail', 'Mohawk', 'CrewCut', 'Spiky', 'Buzz', 'Curly'), ('FlatCap', 'Baseball', 'Beanie', 'Cowboy')


def V(p): return Vector(p)
def lerp(a, b, t): return tuple(V(a) + (V(b) - V(a)) * t)
def M(p, s): return (p[0] * s, p[1], p[2])  # mirror to side s
def add(a, b): return tuple(V(a) + V(b))


# ---------------------------------------------------------------- body parts
def torso(G, P, body, slot, grow=0.0):
    """Shirt torso: horizontal elliptical rings along the spine from the hem to the collar."""
    hip, S = V(P['hip']), V(P['S'])
    secs = []
    for t, (w, d), fwd in ((.12, body['hem'], 0), (.35, body['waist'], 0), (.70, body['chest'], .012), (.92, body['upper'], .006)):
        c = hip + (S - hip) * t + Vector((0, 0, fwd)); secs.append(ring(tuple(c), w + grow, d + grow, 16, 'y'))
    top = S + Vector((0, .035, .0)); secs.append(ring(tuple(top), body['top'][0] + grow, body['top'][1] + grow, 16, 'y'))
    neck = S + Vector((0, .07, .012)); secs.append(ring(tuple(neck), .066 + grow * .6, .058 + grow * .6, 16, 'y'))
    loft(G + ' torso', secs, G, slot)
    return hip + (S - hip) * .70 + Vector((0, 0, .012))


def bust(G, P, body, slot, chest, grow=0.0):
    d = body['chest'][1] + grow
    for s in (-1, 1): blob(G + ' bust', (s * .055, chest.y - .015, chest.z + d * .62), (.064 + grow * .5, .058 + grow * .3, .055 + grow * .5), G, slot, 12, 8)


def leg(G, P, s):
    """0.81: group of a leg part (thigh, shin, hem, shoe) for poses with legs=True: <group>_G<side>, else the group."""
    return f'{G}_G{"R" if s > 0 else "L"}' if P.get('legs') else G


def arm(G, seg, s):
    """Group of an arm part (0.78): seg U (upper arm), L (forearm) or H (hand), on side s (+1 right, -1 left)."""
    return f'{G}_{seg}{"R" if s > 0 else "L"}'


def arm_points(P, body, s):
    sh = add(M(P['S'], s), (s * body['sw'], -.02, 0)); el = M(P['elbow'], s); ha = M(P['hand'], s)
    wr = tuple(V(ha) - (V(ha) - V(el)).normalized() * .06)
    return sh, el, wr, ha


def shirt(pose, P, bodyname, kind):
    body = BODY[bodyname]; G = f'{pose}_Shirt_{kind}_{bodyname}'
    jacket = kind in ('Jacket', 'Leather')
    grow = .02 if jacket else .006
    chest = torso(G, P, body, 'shirt', grow)
    if bodyname == 'Woman': bust(G, P, body, 'shirt', chest, grow)
    ua0, ua1, fa0, fa1 = body['arm']
    for s in (-1, 1):
        sh, el, wr, ha = arm_points(P, body, s); U, L = arm(G, 'U', s), arm(G, 'L', s)
        if kind == 'Tee':
            mid = lerp(sh, el, .48)
            blob(G + ' shoulder', sh, (ua0 + .012,) * 3, U, 'shirt', 12, 8)
            cyl(G + ' sleeve', sh, mid, ua0 + .012, U, 'shirt', 12, r1=ua0 + .014, cap=False)
            cyl(G + ' hem', lerp(sh, el, .44), mid, ua0 + .017, U, 'shirt', 12)
            limb(G + ' upper arm', lerp(sh, el, .4), el, ua0 * .95, ua1, U, 'skin', 10)
            limb(G + ' forearm', el, wr, fa0, fa1, L, 'skin', 10)
        else:
            g = .012 if jacket else .004
            blob(G + ' shoulder', sh, (ua0 + g + .004,) * 3, U, 'shirt', 12, 8)
            limb(G + ' sleeve', sh, el, ua0 + g, ua1 + g, U, 'shirt', 12)
            # the elbow ball rides with the forearm so the bend stays closed
            limb(G + ' sleeve lower', el, wr, fa0 + g, fa1 + g, L, 'shirt', 12, ends=False)
            blob(G + ' elbow', el, (fa0 + g,) * 3, L, 'shirt', 12, 6)
            cuff = tuple(V(wr) - (V(wr) - V(el)).normalized() * .03)
            cyl(G + ' cuff', cuff, wr, fa1 + g + .006, L, 'trim' if jacket else 'shirt', 12)
    hip, S = V(P['hip']), V(P['S'])
    if kind == 'Jacket':
        hem = hip + (S - hip) * .12
        loft(G + ' hem band', [ring(tuple(hem), body['hem'][0] + grow + .006, body['hem'][1] + grow + .006, 16, 'y'),
                               ring(tuple(hem + Vector((0, .045, 0))), body['hem'][0] + grow + .004, body['hem'][1] + grow + .004, 16, 'y')], G, 'trim')
        neck = S + Vector((0, .07, .012))
        loft(G + ' collar', [ring(tuple(neck + Vector((0, -.01, 0))), .084, .074, 16, 'y'), ring(tuple(neck + Vector((0, .05, -.006))), .078, .068, 16, 'y')], G, 'shirt')
        # zip down the front, on the surface of each ring
        pts = []
        for t, (w, d), fwd in ((.12, body['hem'], 0), (.35, body['waist'], 0), (.70, body['chest'], .012), (.92, body['upper'], .006)):
            c = hip + (S - hip) * t; dd = d + grow + (.03 if (bodyname == 'Woman' and t == .70) else 0)
            pts.append((0, c.y, c.z + fwd + dd + .002))
        pts.append((0, neck.y + .03, neck.z + .072))
        for a, b in zip(pts, pts[1:]): cyl(G + ' zip', a, b, .006, G, 'trim', 6)
    elif kind == 'Leather':
        # 0.83: a leather jacket worn open over a T-shirt: waistband, the T-shirt showing down the front between two
        # lapel edges, and a fold-over collar with pointed wings (the game gives the shell a slight sheen).
        hem = hip + (S - hip) * .12
        loft(G + ' hem band', [ring(tuple(hem), body['hem'][0] + grow + .006, body['hem'][1] + grow + .006, 16, 'y'),
                               ring(tuple(hem + Vector((0, .045, 0))), body['hem'][0] + grow + .004, body['hem'][1] + grow + .004, 16, 'y')], G, 'trim')
        neck = S + Vector((0, .07, .012))
        rows = [(.20, body['hem'], 0, .040), (.35, body['waist'], 0, .046), (.70, body['chest'], .012, .060), (.92, body['upper'], .006, .062)]
        edges = {-1: [], 1: []}; verts, faces, k = [], [], 5
        for t, (w, d), fwd, half in rows:
            c = hip + (S - hip) * t
            dd = d + grow + (.03 if (bodyname == 'Woman' and t == .70) else 0)
            for j in range(k):
                x = -half + 2 * half * j / (k - 1); z = c.z + fwd + (dd + .003) * math.sqrt(max(0.0, 1 - (x / (w + grow)) ** 2))
                verts.append(B(x, c.y, z))
            for sd in (-1, 1):
                x = sd * (half + .004); edges[sd].append((x, c.y, c.z + fwd + (dd + .004) * math.sqrt(max(0.0, 1 - (x / (w + grow)) ** 2))))
        verts.append(B(0, neck.y - .012, neck.z + .060))
        for r in range(len(rows) - 1):
            for j in range(k - 1): faces.append((r * k + j, (r + 1) * k + j, (r + 1) * k + j + 1, r * k + j + 1))
        last = (len(rows) - 1) * k
        for j in range(k - 1): faces.append((last + j, len(verts) - 1, last + j + 1))
        ob = kit.mesh_ob(G + ' undershirt', verts, faces)
        for poly in ob.data.polygons:
            if poly.normal.y < 0: poly.flip()
        kit.finish(ob, G, 'inner')
        for sd in (-1, 1):
            pts = edges[sd] + [(sd * .05, neck.y - .005, neck.z + .055)]
            for a, b in zip(pts, pts[1:]): cyl(G + ' lapel', a, b, .011, G, 'shirt', 8)
            # collar wing: from the side of the neck, folded down and out, pointed at the chest
            root, outer, tip = (sd * .062, neck.y + .035, neck.z - .02), (sd * .115, neck.y - .025, neck.z + .02), (sd * .062, neck.y - .055, neck.z + .07)
            o = Vector((0, 0, .012)); n = Vector((0, .006, 0)); mid = V(lerp(root, tip, .5))
            kit.hull(G + ' collar wing', [tuple(V(root) - n), tuple(V(outer) - n), tuple(V(tip) - n), tuple(mid - n),
                                         tuple(V(root) + n + o), tuple(V(outer) + n + o), tuple(V(tip) + n + o), tuple(mid + n + o)], G, 'shirt', .004, 1)
        loft(G + ' collar', [ring(tuple(neck + Vector((0, -.01, 0))), .084, .074, 16, 'y'), ring(tuple(neck + Vector((0, .04, -.006))), .08, .07, 16, 'y')], G, 'shirt')
    else:
        neck = S + Vector((0, .07, .012))
        loft(G + ' neckline', [ring(tuple(neck + Vector((0, -.012, 0))), .072, .064, 16, 'y'), ring(tuple(neck + Vector((0, .006, 0))), .07, .062, 16, 'y')], G, 'shirt')


def pants(pose, P, bodyname, kind):
    body = BODY[bodyname]; G = f'{pose}_Pants_{kind}_{bodyname}'
    px, py, pz = body['pelvis']; blob(G + ' seat', P['hip'], (px, py, pz), G, 'pants', 16, 10)
    t0, t1 = body['thigh']; s0, s1 = body['shin']
    for s in (-1, 1):
        hj, kn, an = M(P['hipj'], s), M(P['knee'], s), M(P['ankle'], s)
        L = leg(G, P, s)
        if kind == 'Jeans':
            limb(G + ' thigh', hj, kn, t0, t1 + .002, L, 'pants', 12)
            limb(G + ' shin', kn, an, s0 + .004, s1 + .008, L, 'pants', 12, ends=False)
            hem = tuple(V(an) + (V(an) - V(kn)).normalized() * .01)
            cyl(G + ' hem', lerp(kn, an, .9), hem, s1 + .012, L, 'pants', 12)
        else:
            end = lerp(hj, kn, .66)
            limb(G + ' leg', hj, end, t0 + .004, t1 + .014, L, 'pants', 12, ends=False)
            blob(G + ' hip', hj, (t0 + .004,) * 3, L, 'pants', 12, 8)
            cyl(G + ' hem', lerp(hj, kn, .6), end, t1 + .018, L, 'pants', 12)
            limb(G + ' thigh', lerp(hj, kn, .55), kn, t1 + .006, t1, L, 'skin', 10)
            limb(G + ' shin', kn, an, s0, s1 - .002, L, 'skin', 10)
            cyl(G + ' sock', lerp(kn, an, .88), an, s1 + .002, L, 'eyes', 10)


def shoe(G, an, fwd, scale, LG=None):
    f = V(fwd).normalized(); up = Vector((0, 1, 0)); up = (up - f * up.dot(f)).normalized(); side = f.cross(up)
    heel = V(an) - f * .065 * scale - up * .085 * scale; L, Wd, H = .27 * scale, .052 * scale, .115 * scale
    def P(a, b, c): return tuple(heel + f * a + side * b + up * c)
    G0, G = G, (LG or G)
    hull(G0 + ' shoe', [P(0, -Wd, 0), P(0, Wd, 0), P(L, Wd * 1.05, 0), P(L, -Wd * 1.05, 0),
                       P(.01, -Wd * .95, H), P(.01, Wd * .95, H), P(L * .93, Wd, H * .48), P(L * .93, -Wd, H * .48)], G, 'shoes', .022, 2)
    hull(G0 + ' sole', [P(-.004, -Wd - .003, -.012), P(-.004, Wd + .003, -.012), P(L + .006, Wd * 1.05 + .003, -.012), P(L + .006, -Wd * 1.05 - .003, -.012),
                       P(-.004, -Wd - .003, .014), P(-.004, Wd + .003, .014), P(L + .006, Wd * 1.05 + .003, .014), P(L + .006, -Wd * 1.05 - .003, .014)], G, 'trim', .006, 1)


def base(pose, P, bodyname):
    body = BODY[bodyname]; G = f'{pose}_Base_All_{bodyname}'; H = V(P['head']); woman = bodyname == 'Woman'
    def h(x, y, z): return tuple(H + Vector((x, y, z)))
    blob(G + ' head', tuple(H), (.100, .118, .110), G, 'skin', 16, 10)
    blob(G + ' jaw', h(0, -.055, .025), (.072, .05, .07) if woman else (.082, .056, .078), G, 'skin', 14, 8)
    blob(G + ' nose', h(0, -.006, .104), (.013, .019, .017) if woman else (.015, .022, .019), G, 'skin', 10, 6)
    for s in (-1, 1):
        blob(G + ' ear', h(s * .101, -.005, -.01), (.017, .032, .025), G, 'skin', 10, 6)
        blob(G + ' eye', h(s * .038, .028, .092), (.022, .016, .011) if woman else (.021, .015, .011), G, 'eyes', 10, 6)
        blob(G + ' pupil', h(s * .038, .028, .101), (.009, .010, .005), G, 'pupil', 8, 5)
        if woman:
            box(G + ' brow', h(s * .040, .062, .099), (.044, .007, .010), G, 'hair', .003, (0, 0, s * -12))
            box(G + ' lashes', h(s * .058, .036, .095), (.014, .004, .005), G, 'pupil', .0015, (0, 0, s * 30))
        else:
            box(G + ' brow', h(s * .040, .062, .099), (.048, .012, .012), G, 'hair', .004, (0, 0, s * -8))
    blob(G + ' mouth', h(0, -.05, .097), (.027, .012, .013) if woman else (.030, .008, .012), G, 'mouth', 10, 6)
    S = V(P['S']); neck0 = S + Vector((0, .04, .005)); neck1 = H + Vector((0, -.09, -.03))
    cyl(G + ' neck', tuple(neck0), tuple(neck1), body['neck'], G, 'skin', 10)
    k = body['hand']
    for s in (-1, 1):
        sh, el, wr, ha = arm_points(P, body, s); Hd = arm(G, 'H', s)
        hand(G, Hd, P, s, k, wr, ha)
        shoe(G, M(P['ankle'], s), P['foot'], body['shoe'], leg(G, P, s))


# Chastain Rush, Round 28 (C1, Dan: "can we fix the drivers' hands so they don't look so bad?"): the hand was a capsule, a ball for the
# fist and a ball for the thumb (lumpy blocks, seen big in first person on the bike's grips). Now a closed hand round the grip: a palm
# block from the wrist to the knuckles, four fingers curled round the grip's own axis (over the top first, round the front and under),
# a thumb wrapped under from the inside, fingertips, the wrist joining the forearm. The grip's axis is the pose's 'grip' (a bike's bar,
# the car's rim at ten to two); the grip (radius 16 mm) is centred on the pose's hand point. All in the hand's arm group (it turns at the
# wrist: the fist wave and the celebrations shake this same closed hand)
GRIP_R = .016


def hand(G, Hd, P, s, k, wr, ha):
    g = V(M(P['grip'], s)).normalized(); c = V(ha); W = V(wr)
    f = (c - W).normalized(); n = (f - g * f.dot(g)).normalized()     # from the wrist toward the grip, square to it
    u = g.cross(n)
    if u.y < 0: u = -u                                                    # round the grip: over the top first
    rf = .0098 * k; R = GRIP_R + rf; KN = 70                           # the knuckles over the top-front of the grip

    def at(theta, along, r=R):
        t = math.radians(theta); return tuple(c - n * (r * math.cos(t)) + u * (r * math.sin(t)) + g * along)
    # fingers, index (inside: toward the vehicle's middle, -g) to little
    for i, (end, rr) in enumerate(((235, 1.0), (245, 1.0), (238, .96), (222, .88))):
        o = (-.0285 + .019 * i) * k; r = rf * rr
        pts = [at(th, o, GRIP_R + r) for th in range(KN, end + 1, 30)] + [at(end, o, GRIP_R + r)]
        kit.tube(G + f' finger{i}', pts, r, Hd, 'skin', 3)
        blob(G + f' tip{i}', pts[-1], (r, r, r), Hd, 'skin', 8, 5)
        blob(G + f' knuckle{i}', pts[0], (r * 1.12, r * 1.12, r * 1.12), Hd, 'skin', 8, 5)
    # the palm: from the wrist to the knuckle line, its width along the grip
    K = V(at(KN, -.0, R - .002)); t = (K - W).cross(g).normalized()
    if t.dot(u) < 0: t = -t
    hw, hk, th = .029 * k, .040 * k, .015 * k
    W2 = W + f * .012
    pts8 = [tuple(W2 - g * hw - t * th), tuple(W2 + g * hw - t * th), tuple(K + g * hk - t * th), tuple(K - g * hk - t * th),
            tuple(W2 - g * hw + t * th), tuple(W2 + g * hw + t * th), tuple(K + g * hk + t * th), tuple(K - g * hk + t * th)]
    kit.hull(G + ' palm', pts8, Hd, 'skin', .009, 2)
    # the thumb: from the palm's inside edge near the wrist, round under the grip toward the fingers
    o = -.041 * k
    tpts = [tuple(W2 + f * .018 - g * .028 * k), at(-25, o, GRIP_R + .011 * k), at(-75, o + .006 * k, GRIP_R + .0095 * k), at(-118, o + .012 * k, GRIP_R + .009 * k)]
    kit.tube(G + ' thumb', tpts, .0098 * k, Hd, 'skin', 3)
    blob(G + ' thumb tip', tpts[-1], (.0098 * k,) * 3, Hd, 'skin', 8, 5)
    # the wrist, from inside the sleeve into the palm
    limb(G + ' wrist', tuple(W - f * .03), tuple(W + f * .016), .029 * k, .031 * k, Hd, 'skin', 10)


# ---------------------------------------------------------------- hair and hats (head frame: origin at the head centre)
def band_plane():
    t = math.radians(BAND_TILT); n = Vector((0, -math.sin(t), math.cos(t)))  # Blender space: (x, forward, up)
    return Vector((0, 0, BAND_Y)), n


def shell(name, c, r, G, cuts, solid=.008, flare=0.0):
    """An ellipsoid hair shell around the head, with cut planes removing the face and below the hairline; flare widens it
    below the head centre (a bob / long hair falling away from the face)."""
    bm = bmesh.new(); bmesh.ops.create_uvsphere(bm, u_segments=22, v_segments=14, radius=1)
    for v in bm.verts:
        if flare and v.co.z < 0: k = 1 + flare * (-v.co.z); v.co.x *= k; v.co.y *= k
    bmesh.ops.transform(bm, matrix=Matrix.Translation(B(*c)) @ Matrix.Diagonal((r[0], r[2], r[1], 1)), verts=bm.verts)
    for co, no in cuts:
        bmesh.ops.bisect_plane(bm, geom=bm.verts[:] + bm.edges[:] + bm.faces[:], plane_co=co, plane_no=no, clear_outer=True)
    me = bpy.data.meshes.new(name); bm.to_mesh(me); bm.free(); ob = bpy.data.objects.new(name, me); bpy.context.collection.objects.link(ob)
    md = ob.modifiers.new('solid', 'SOLIDIFY'); md.thickness = solid; md.offset = -1
    return kit.finish(ob, G, 'hair')


def face_cut(y=.06, z=.075, ang=30, part=0.0):
    """Removes the face side of a hair shell; part > 0 tips the hairline so the hair sweeps lower on the left (a side part)."""
    a = math.radians(ang); return B(0, y, z), Vector((part, math.cos(a), -math.sin(a))).normalized()


def low_cut(a, b):
    """Keep y > a + b z (Unity, head frame): remove everything below a plane that dips toward the back."""
    n = Vector((0, b, -1)).normalized()  # Unity normal (0, -1, b) -> Blender (0, b, -1)
    return B(0, a, 0), n


def on_head(x, y, z, lift=0.0):
    """the point on an ellipsoid round the head (radii a little over the head's own) in the direction of (x, y, z), lifted along its normal"""
    d = Vector((x, y, z)).normalized(); r = (.104, .124, .115)
    t = 1 / math.sqrt((d.x / r[0]) ** 2 + (d.y / r[1]) ** 2 + (d.z / r[2]) ** 2); p = d * t
    nrm = Vector((p.x / r[0] ** 2, p.y / r[1] ** 2, p.z / r[2] ** 2)).normalized()
    return p + nrm * lift, nrm


def tufts(G, n, ymin, seed, size=(.028, .006, .026), zmax=.07):
    """small flat locks lying on the hair, over the crown (break up the smooth shell)"""
    import random
    rnd = random.Random(seed); done = 0; tries = 0
    while done < n and tries < 400:
        tries += 1; x, y, z = rnd.uniform(-1, 1), rnd.uniform(0, 1), rnd.uniform(-1, 1)
        p, nrm = on_head(x, y, z, .0)
        if p.y < ymin or p.z > zmax: continue
        pitch = math.degrees(math.atan2(nrm.z, nrm.y)); roll = -math.degrees(math.atan2(nrm.x, nrm.y))
        blob(G + f' tuft{done}', tuple(p), size, G, 'hair', 10, 6, (pitch, rnd.uniform(-40, 40), roll)); done += 1


def hair_parts(style, G):
    # Chastain Rush, Round 28 (C3, Dan: "can we fix some of the hair styles? Especially short and medium don't really work well"): both
    # were one smooth shell with a straight cut (a helmet). Now the shell sits closer to the head and the edges and crown are broken up:
    # Short: brushed forward, a ragged fringe, sideburns, a tapered nape. Medium: fuller, swept fringe, locks over the tops of the ears,
    # layered at the back
    if style == 'Short':
        shell(G + ' shell', (0, .014, -.006), (.107, .127, .118), G, [face_cut(.058, .082, 32, .15), low_cut(-.032, .32)], .008)
        for i, x in enumerate((-.058, -.04, -.021, -.003, .016, .034, .052)):
            blob(G + f' fringe{i}', (x, .072 + .003 * math.sin(i * 2.1), .087 - abs(x) * .3), (.014, .008, .016), G, 'hair', 8, 6, (30, 0, x * 150))
        for sd in (-1, 1): blob(G + ' sideburn', (sd * .099, -.012, .038), (.008, .026, .014), G, 'hair', 8, 6)
        tufts(G, 7, .08, 11)
    elif style == 'Medium':
        shell(G + ' shell', (0, .012, -.012), (.113, .131, .123), G, [face_cut(.047, .079, 36, .3), low_cut(-.078, .16)], .011, .32)
        for i, x in enumerate((-.062, -.04, -.018, .004, .026, .048)):
            blob(G + f' fringe{i}', (x, .076, .08 - abs(x) * .3), (.02, .01, .036), G, 'hair', 10, 6, (42, 0, 22 + x * 100))
        for i, x in enumerate((-.055, -.02, .02, .055)): blob(G + f' back{i}', (x, -.06, -.105), (.026, .05, .014), G, 'hair', 10, 7, (-12, 0, x * 60))
        tufts(G, 8, .07, 12, (.032, .007, .03))
    elif style == 'CrewCut':
        # short on the sides and back, a little longer and flat on top, the front brushed up
        shell(G + ' shell', (0, .012, -.004), (.104, .124, .114), G, [face_cut(.062, .082, 30, 0), low_cut(-.02, .42)], .006)
        # longer on top: a second shell over the crown only, 6 mm out, its edge a soft step all round
        shell(G + ' top', (0, .014, -.004), (.11, .13, .12), G, [face_cut(.07, .086, 30, 0), low_cut(.06, .0)], .006)
    elif style == 'Buzz':
        shell(G + ' shell', (0, .008, -.004), (.103, .121, .113), G, [face_cut(.06, .082, 30, 0), low_cut(-.03, .3)], .004)
    elif style == 'Mohawk':
        # shaved sides (a thin buzz) and a crest from the forehead over the crown to the nape
        shell(G + ' shell', (0, .006, -.004), (.102, .12, .112), G, [face_cut(.062, .082, 30, 0), low_cut(-.02, .4)], .003)
        # a fin: three ridges along the midline, front to back, each a little higher and narrower (one smooth crest, not beads)
        for j, (lift, r) in enumerate(((.006, .015), (.017, .013), (.027, .009))):
            line = []
            for i in range(13):
                a = math.radians(-60 + i * 14)                               # over the head, front (+z) to back
                p, nrm = on_head(0, math.cos(a), -math.sin(a), 0.0)
                hump = 0.6 + 0.4 * math.sin(math.pi * i / 12)
                line.append(tuple(p + nrm * lift * hump))
            kit.tube(G + f' crest{j}', line, r, G, 'hair', 4)
    elif style == 'Spiky':
        shell(G + ' shell', (0, .012, -.006), (.106, .126, .117), G, [face_cut(.058, .082, 32, 0), low_cut(-.03, .32)], .007)
        n = 0
        for i in range(60):
            if n >= 18: break
            gy = 1 - (i + .5) / 60; rad = math.sqrt(max(0, 1 - gy * gy)); ang = i * 2.39996
            x, y, z = rad * math.cos(ang), gy, rad * math.sin(ang)
            p, nrm = on_head(x, y, z, 0)
            if p.y < .075 or p.z > .085: continue                            # above the hat band (a hat hides them), off the face
            limb(G + f' spike{n}', tuple(p - nrm * .004), tuple(p + nrm * .045 + Vector((0, 0, .008))), .013, .002, G, 'hair', 6, ends=False); n += 1
    elif style == 'Curly':
        shell(G + ' shell', (0, .014, -.008), (.111, .13, .12), G, [face_cut(.05, .08, 34, 0), low_cut(-.05, .25)], .01)
        n = 0
        for i in range(140):
            if n >= 42: break
            gy = 1 - 2 * (i + .5) / 140; rad = math.sqrt(max(0, 1 - gy * gy)); ang = i * 2.39996
            p, nrm = on_head(rad * math.cos(ang), gy, rad * math.sin(ang), .012)
            if p.y < -.04 or (p.z > .06 and p.y < .075) or (p.y < -.01 and p.z > -.02): continue
            blob(G + f' curl{n}', tuple(p), (.021, .021, .021), G, 'hair', 8, 6); n += 1
    elif style == 'Long':
        shell(G + ' shell', (0, .012, -.012), (.117, .124, .125), G, [face_cut(.045, .075, 36, -.3), low_cut(-.075, .2)], .012, .3)
        blob(G + ' fall', (0, -.15, -.072), (.108, .17, .055), G, 'hair', 14, 10, (-8, 0, 0))
        for s in (-1, 1): blob(G + ' lock', (s * .1, -.105, -.02), (.032, .13, .05), G, 'hair', 10, 8, (0, 0, s * 4))
    elif style == 'Ponytail':
        shell(G + ' shell', (0, .02, -.008), (.109, .113, .118), G, [face_cut(.06, .078, 28), low_cut(-.03, .35)])
        cyl(G + ' tie', (0, .0, -.112), (0, -.012, -.128), .026, G, 'trim', 10)
        limb(G + ' tail', (0, -.012, -.128), (0, -.10, -.165), .028, .034, G, 'hair', 10)
        limb(G + ' tail end', (0, -.10, -.165), (0, -.20, -.16), .034, .012, G, 'hair', 10)


def place(objs, M4):
    for ob in objs: ob.matrix_world = M4 @ ob.matrix_world


def hair(pose, P):
    H = V(P['head']); co, no = band_plane()
    for style in HAIRS:
        start = len(kit.PARTS); G = f'{pose}_Hair_{style}_Any'
        hair_parts(style, G)
        made = kit.PARTS[start:]; del kit.PARTS[start:]
        # join this style (modifiers applied), then split it at the hat band: Hair = below, HairTop = above
        bpy.ops.object.select_all(action='DESELECT')
        for ob, g, s in made:
            bpy.context.view_layer.objects.active = ob
            for md in list(ob.modifiers): bpy.ops.object.modifier_apply(modifier=md.name)
            ob.select_set(True)
        for slot in sorted({s for _, _, s in made}):
            obs = [ob for ob, g, s in made if s == slot]
            bpy.ops.object.select_all(action='DESELECT')
            for ob in obs: ob.select_set(True)
            bpy.context.view_layer.objects.active = obs[0]
            if len(obs) > 1: bpy.ops.object.join()
            low = bpy.context.view_layer.objects.active; bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
            top = low.copy(); top.data = low.data.copy(); bpy.context.collection.objects.link(top)
            for ob, outer in ((low, True), (top, False)):
                bm = bmesh.new(); bm.from_mesh(ob.data)
                bmesh.ops.bisect_plane(bm, geom=bm.verts[:] + bm.edges[:] + bm.faces[:], plane_co=co, plane_no=no, clear_outer=outer, clear_inner=not outer)
                bm.to_mesh(ob.data); bm.free()
            place([low, top], Matrix.Translation(B(*H)))
            kit.PARTS.append((low, G, slot))
            if len(top.data.polygons): kit.PARTS.append((top, f'{pose}_HairTop_{style}_Any', slot))
            else: bpy.data.objects.remove(top)


def dome(name, r, G, slot='hat', solid=.006, tip=0.0):
    """The upper half of an ellipsoid (hat frame: origin on the band, y up), open at the band."""
    bm = bmesh.new(); bmesh.ops.create_uvsphere(bm, u_segments=24, v_segments=16, radius=1)
    if tip:
        for v in bm.verts:
            if v.co.z > .6: v.co.z += tip * (v.co.z - .6) / .4
    bmesh.ops.transform(bm, matrix=Matrix.Diagonal((r[0], r[2], r[1], 1)), verts=bm.verts)
    bmesh.ops.bisect_plane(bm, geom=bm.verts[:] + bm.edges[:] + bm.faces[:], plane_co=(0, 0, 0), plane_no=(0, 0, 1), clear_inner=True)
    me = bpy.data.meshes.new(name); bm.to_mesh(me); bm.free(); ob = bpy.data.objects.new(name, me); bpy.context.collection.objects.link(ob)
    md = ob.modifiers.new('solid', 'SOLIDIFY'); md.thickness = solid; md.offset = 1
    return kit.finish(ob, G, slot)


def brim(name, G, slot, inner, outer, a0, a1, n, lift, thick=.008, y0=0.0):
    """A brim (hat frame): ring sector between the inner ellipse and outer(angle), lifted by lift(x, z)."""
    v, f = [], []
    for i in range(n + 1):
        a = math.radians(a0 + (a1 - a0) * i / n); ca, sa = math.sin(a), math.cos(a)  # angle 0 = straight ahead (+z)
        ox, oz = outer(a)
        for (x, z) in ((inner[0] * ca, inner[1] * sa), (ox * ca, oz * sa)):
            for t in (0, 1): v.append(B(x, y0 + lift(x, z) + t * thick, z))
    for i in range(n):
        a, b = i * 4, (i + 1) * 4
        f += [(a + 0, a + 2, b + 2, b + 0), (a + 1, b + 1, b + 3, a + 3), (a + 2, a + 3, b + 3, b + 2), (a + 0, b + 0, b + 1, a + 1)]
    if a1 - a0 < 360: f += [(0, 1, 3, 2), (n * 4, n * 4 + 2, n * 4 + 3, n * 4 + 1)]
    ob = kit.mesh_ob(name, v, f); kit.outward(ob)
    return kit.finish(ob, G, slot, .003, 1)


def hats(pose, P):
    H = V(P['head'])
    hatframe = Matrix.Translation(B(*H) + B(0, BAND_Y, -.004)) @ Matrix.Rotation(math.radians(BAND_TILT), 4, 'X')
    for kind in HATS:
        start = len(kit.PARTS); G = f'{pose}_Hat_{kind}_Any'
        if kind == 'FlatCap':
            # one smooth lofted shape: snug at the band, swelling forward over the brim, flat top
            secs = [ring((0, -.002, 0), .127, .135, 24, 'y'), ring((0, .026, .012), .136, .152, 24, 'y'), ring((0, .052, .022), .134, .156, 24, 'y'),
                    ring((0, .068, .02), .112, .132, 24, 'y'), ring((0, .074, .016), .06, .07, 24, 'y')]
            loft(G + ' crown', secs, G, 'hat', cap='top')
            brim(G + ' brim', G, 'hat', (.10, .135), lambda a: (.125, .19), -60, 60, 12, lambda x, z: .004 - .3 * x * x, .009)
            blob(G + ' button', (0, .074, .016), (.014, .007, .014), G, 'hat', 8, 5)
        elif kind == 'Baseball':
            dome(G + ' crown', (.126, .098, .135), G)
            blob(G + ' button', (0, .097, .0), (.016, .009, .016), G, 'hat', 8, 5)
            brim(G + ' brim', G, 'hat', (.108, .13), lambda a: (.14, .235), -68, 68, 14, lambda x, z: .004 - .9 * x * x - .05 * max(z - .13, 0), .009)
            loft(G + ' band', [ring((0, -.002, 0), .128, .137, 24, 'y'), ring((0, .016, 0), .128, .137, 24, 'y')], G, 'hat', cap=False)
        elif kind == 'Beanie':
            dome(G + ' crown', (.127, .108, .136), G, tip=.08)
            loft(G + ' cuff', [ring((0, -.004, 0), .133, .142, 24, 'y'), ring((0, .044, 0), .132, .141, 24, 'y')], G, 'hat', cap=False)
        elif kind == 'Cowboy':
            secs = []
            for y, k in ((-.002, 1.0), (.04, .99), (.08, .965), (.115, .93), (.128, .86)):
                secs.append(ring((0, y, 0), .127 * k, .137 * k, 20, 'y'))
            secs.append(ring((0, .118, .004), .07, .09, 20, 'y'))  # the pinched crease on top
            loft(G + ' crown', secs, G, 'hat', cap='top')
            loft(G + ' hatband', [ring((0, .0, 0), .131, .141, 20, 'y'), ring((0, .03, 0), .13, .14, 20, 'y')], G, 'trim', cap=False)
            brim(G + ' brim', G, 'hat', (.125, .135), lambda a: (.205, .222), 0, 360, 32, lambda x, z: 1.7 * x * x - .012 * (abs(z) / .22), .01)
        place([ob for ob, g, s in kit.PARTS[start:]], hatframe)


# ---------------------------------------------------------------- build, check, export
# 0.83: the chest emblem - a plain ring with a simple bird (spread wings, small head, fanned tail) inside, one generic
# outline of our own; right half, from the top of the head round to the bottom of the tail, in units of the inner radius.
BIRD = [(0, .66), (.09, .60), (.10, .47), (.26, .43), (.50, .55), (.76, .66), (.90, .60), (.80, .46), (.88, .36), (.70, .28),
        (.76, .16), (.56, .11), (.58, .00), (.34, .02), (.20, -.16), (.26, -.52), (.10, -.42), (0, -.58)]


def emblem(pose, P, kind):
    body = BODY['Man']; G = f'{pose}_Emblem_{kind}_Man'
    grow = .02 if kind == 'Jacket' else .006
    hip, S = V(P['hip']), V(P['S'])
    secs = [(.35, body['waist'], 0), (.70, body['chest'], .012), (.92, body['upper'], .006)]

    def surface(x, t):  # the front of the shirt / jacket at height t along the spine, x across
        (t0, (w0, d0), f0), (t1, (w1, d1), f1) = (secs[0], secs[1]) if t <= secs[1][0] else (secs[1], secs[2])
        u = min(1.0, max(0.0, (t - t0) / (t1 - t0))); w = w0 + (w1 - w0) * u + grow; d = d0 + (d1 - d0) * u + grow; f = f0 + (f1 - f0) * u
        c = hip + (S - hip) * t
        return c.z + f + (d + .004) * math.sqrt(max(0.0, 1 - (x / w) ** 2))
    span = (S - hip).length
    cx, ct, R = (0.0, .66, .062) if kind == 'Tee' else (-.085, .74, .042)

    def place2(x, y):  # emblem plane (x right, y up, metres from its centre) -> Unity point on the chest
        t = ct + y / span; return (cx + x, (hip + (S - hip) * t).y, surface(cx + x, t))
    ri, n = R * .79, 32
    verts, faces = [], []
    for i in range(n):
        a = i * 2 * math.pi / n
        verts += [B(*place2(math.cos(a) * R, math.sin(a) * R)), B(*place2(math.cos(a) * ri, math.sin(a) * ri))]
    for i in range(n): faces.append((2 * i, 2 * ((i + 1) % n), 2 * ((i + 1) % n) + 1, 2 * i + 1))
    ob = kit.mesh_ob(G + ' ring', verts, faces)
    outline = BIRD[:-1] + [(-x, y) for x, y in reversed(BIRD[1:])]
    sc = ri * .9
    bm = bmesh.new(); vs = [bm.verts.new(B(*place2(x * sc, y * sc))) for x, y in outline]
    f = bm.faces.new(vs); bmesh.ops.triangulate(bm, faces=[f])
    me = bpy.data.meshes.new(G + ' bird'); bm.to_mesh(me); bm.free()
    ob2 = bpy.data.objects.new(G + ' bird', me); bpy.context.collection.objects.link(ob2)
    for o in (ob, ob2):  # face forward (Blender +y)
        for poly in o.data.polygons:
            if poly.normal.y < 0: poly.flip()
        kit.finish(o, G, 'emblem', smooth=False)


def build():
    bpy.ops.wm.read_factory_settings(use_empty=True)
    for pose, P in POSES.items():
        for bodyname in BODY:
            base(pose, P, bodyname)
            for k in SHIRTS: shirt(pose, P, bodyname, k)
            for k in PANTS: pants(pose, P, bodyname, k)
        for k in ('Tee', 'Jacket'): emblem(pose, P, k)
        hair(pose, P); hats(pose, P)
    objs = kit.join_all()
    # arm parts turn about their joint: origin on the shoulder (U), elbow (L) or wrist (H)
    for ob in objs:
        key = ob.name.split('__')[0].split('_')
        if len(key) != 5: continue
        s = 1 if key[4][1] == 'R' else -1
        if key[4][0] == 'G': kit.set_origin(ob, M(POSES[key[0]]['hipj'], s)); continue
        sh, el, wr, ha = arm_points(POSES[key[0]], BODY[key[3]], s)
        kit.set_origin(ob, {'U': sh, 'L': el, 'H': wr}[key[4][0]])
    return objs


def covered(H, p, bvh):
    """The hat covers p: a hat surface lies on the ray from the head centre through p, beyond p."""
    d = p - H; u = d.normalized(); o, travelled = H.copy(), 0.0
    for _ in range(12):
        hit = bvh.ray_cast(o, u)
        if hit[0] is None: return False
        travelled += hit[3]
        if travelled > d.length + .001: return True
        o = hit[0] + u * 1e-5; travelled += 1e-5
    return False


def check(objs):
    """Hair and hats never pass through each other: (1) every HairTop vertex is above the band and every Hair vertex
    below it; (2) with each hat, every head-skin point above the band lies under the hat (nothing pokes through)."""
    from mathutils.bvhtree import BVHTree
    co, no = band_plane(); out = []
    by = {ob.name: ob for ob in objs}
    for pose, P in POSES.items():
        H = B(*P['head'])
        def side(v): return (v - H - co).dot(no)
        for st in HAIRS:
            lo, hi = by.get(f'{pose}_Hair_{st}_Any__hair'), by.get(f'{pose}_HairTop_{st}_Any__hair')
            bad = sum(1 for v in lo.data.vertices if side(v.co) > 1e-4) + (sum(1 for v in hi.data.vertices if side(v.co) < -1e-4) if hi else 0)
            out.append(f'{pose} hair {st}: split errors {bad}')
        for kind in HATS:
            hat = [o for n, o in by.items() if n.startswith(f'{pose}_Hat_{kind}_Any__') and not n.endswith('trim')]
            bm = bmesh.new()
            for o in hat: bm.from_mesh(o.data)
            bvh = BVHTree.FromBMesh(bm); bm.free()
            # sample the head skin above the band (head shells of both bodies) and the below-band hair near the band
            pts = []
            for b in BODY:
                head = by[f'{pose}_Base_All_{b}__skin']
                pts += [v.co for v in head.data.vertices if side(v.co) > .002 and (v.co - H).length < .14]
            outside = sum(1 for p in pts if not covered(H, p, bvh))
            out.append(f'{pose} hat {kind}: head points above band {len(pts)}, outside the hat {outside}')
    return out


def counts(objs):
    """Triangles of each combination's worst case per pose (base + heaviest shirt/pants/hair top+rest/hat)."""
    t = {ob.name: kit.tris([ob]) for ob in objs}
    def cat(pose, c, o, b): return sum(v for n, v in t.items() if n.startswith(f'{pose}_{c}_{o}_{b}_'))  # includes the arm parts
    out = []
    for pose in POSES:
        for b in BODY:
            worst = cat(pose, 'Base', 'All', b) + max(cat(pose, 'Shirt', k, b) for k in SHIRTS) + max(cat(pose, 'Pants', k, b) for k in PANTS) \
                + max(cat(pose, 'Hair', k, 'Any') + cat(pose, 'HairTop', k, 'Any') for k in HAIRS) + max(cat(pose, 'Hat', k, 'Any') for k in HATS)
            out.append(f'{pose} {b}: worst-case rider triangles {worst}')
    return out


if __name__ == '__main__':
    objs = build()
    report = check(objs) + counts(objs)
    print('RIDER CHECK\n' + '\n'.join(report))
    with open(os.path.join(kit.ROOT, 'SourceArt', 'Blender', 'Rider-check.txt'), 'w') as f: f.write('\n'.join(report) + '\n')
    kit.export('Rider')
