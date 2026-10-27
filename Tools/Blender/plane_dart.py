"""Chastain Rush, Round 74 (H, Dan: "Ideally there will be other planes to select eventually"; 04:40: a faster, more agile plane next to the Sky
Hopper): the Sky Dart, a low-wing single-seat racing plane, generated in Blender from this script (the kit and standard of plane.py).

Run:  F:\\blender\\blender.exe --background --factory-startup --python Tools/Blender/plane_dart.py

Our own design, no make, badge or logo: a slim fuselage with a long cowl, a two-blade propeller (1.8 m across) at the nose, a low tapered wing 7.8 m
across, a bubble canopy over the one seat (the pilot seen from the chase camera: Dan, "planes should have windows so that we can see the avatar"), a
swept fin, fixed gear with wheel pants. Unity vehicle space as plane.py: the origin at the physics box's middle (VehicleProfile.PlaneDart: box 1.3 x
1.5 x 7.6 m centred at y 0.05), the wheels' bottom at y = GROUND (the same as the Sky Hopper: PlaneFlight.WheelDrop). Groups: Body, Prop (origin on
the hub, turns about z), Disc. Slots: paint, white, glass, metal, engine, rubber, lamp, tail, interior. The wing tips: WING_TIP.
Output: SourceArt/Blender/PlaneDart.blend, Assets/Resources/VehicleModels/PlaneDart.fbx
"""
import bpy, math, sys, os
from mathutils import Vector
sys.path.insert(0, os.path.dirname(__file__))
import kit
from kit import B, box, hull, cyl, blob, tube, loft, ring

kit.COLORS['white'] = (.94, .94, .92)
GROUND = -1.25
H = Vector((0, -.22, -.35))                      # the pilot's seat point (the Car pose's hip); VehicleProfile.PlaneDart's Seat
HUB = (0, .02, 3.55)
R_PROP = .90
WING_Y, SPAN = -.42, 3.9                         # the wing's underside at the root, its half span
WING_TIP = (SPAN, WING_Y + .15, .0)


def body():
    G = 'Body'
    # (the cockpit's keys lowered to a tub under the canopy, its top about 0.25 m: the pilot is seen through the canopy, not hidden in the fuselage)
    keys = [(3.40, .26, .26, .02), (3.05, .42, .42, .02), (2.30, .50, .52, .04), (1.20, .52, .58, .06), (.70, .52, .50, -.04), (.30, .52, .42, -.17), (-.40, .52, .42, -.17),
            (-1.05, .46, .44, -.04), (-1.80, .38, .44, .18), (-2.80, .26, .32, .24), (-3.60, .14, .20, .28), (-4.10, .08, .12, .30)]
    loft('Fuselage', [ring((0, cy, z), rx, ry, 18, 'z') for z, rx, ry, cy in keys], G, 'paint', cap=True)
    # the racing stripe along the top, in front of the canopy and behind it (not through the cockpit)
    loft('Racing stripe', [ring((0, cy + ry * .95, z), .12, .04, 10, 'z') for z, rx, ry, cy in keys[1:4]], G, 'white', cap=True)
    loft('Racing stripe', [ring((0, cy + ry * .95, z), .10, .04, 10, 'z') for z, rx, ry, cy in keys[8:-1]], G, 'white', cap=True)
    # ---- the bubble canopy over the seat and the turtle deck behind it
    blob('Canopy', (0, .52, -.25), (.44, .46, 1.05), G, 'glass', 20, 12)
    hull('Turtle deck', [(-.30, .45, -1.15), (.30, .45, -1.15), (-.10, .32, -3.4), (.10, .32, -3.4), (-.22, .70, -1.15), (.22, .70, -1.15), (-.06, .42, -3.4), (.06, .42, -3.4)], G, 'paint', .03, 2)
    # ---- the cowl's exhausts and the spinner
    for s in (-1, 1):
        for k in range(3): cyl('Exhaust', (s * .48, -.05, 2.6 - k * .22), (s * .58, -.10, 2.55 - k * .22), .045, G, 'engine', 8)
    blob('Spinner', HUB, (.17, .17, .30), G, 'white', 14, 8)
    # ---- the low wing: tapered, rounded tips, a little dihedral
    hull('Wing', [(-SPAN, WING_Y + .20, .55), (SPAN, WING_Y + .20, .55), (SPAN, WING_Y + .20, -.35), (-SPAN, WING_Y + .20, -.35),
                  (-.6, WING_Y, 1.25), (.6, WING_Y, 1.25), (.6, WING_Y, -.75), (-.6, WING_Y, -.75),
                  (-SPAN, WING_Y + .30, .50), (SPAN, WING_Y + .30, .50), (SPAN, WING_Y + .28, -.30), (-SPAN, WING_Y + .28, -.30),
                  (-.6, WING_Y + .28, 1.15), (.6, WING_Y + .28, 1.15), (.6, WING_Y + .20, -.70), (-.6, WING_Y + .20, -.70)], G, 'paint', .04, 2)
    for s in (-1, 1):
        blob('Wing tip', (s * SPAN, WING_Y + .25, .10), (.10, .08, .48), G, 'white', 12, 6)
        blob('Nav light', (s * (SPAN + .06), WING_Y + .25, .40), (.04, .04, .06), G, 'lamp' if s > 0 else 'tail', 8, 6)
        box('Wing stripe', (s * (SPAN * .72), WING_Y + .31, .10), (.40, .01, .78), G, 'white', .004)
    # ---- the tail
    hull('Stabilizer', [(-1.45, .28, -3.35), (1.45, .28, -3.35), (1.25, .28, -4.05), (-1.25, .28, -4.05), (-1.45, .34, -3.40), (1.45, .34, -3.40), (1.25, .33, -4.00), (-1.25, .33, -4.00)], G, 'paint', .02, 1)
    hull('Fin', [(-.05, .30, -2.95), (.05, .30, -2.95), (.05, .30, -4.15), (-.05, .30, -4.15), (-.03, 1.45, -3.90), (.03, 1.45, -3.90), (.03, 1.45, -4.35), (-.03, 1.45, -4.35)], G, 'paint', .02, 1)
    box('Fin stripe', (0, 1.05, -4.05), (.11, .14, .55), G, 'white', .01, (-22, 0, 0))
    blob('Beacon', (0, 1.48, -4.15), (.05, .05, .06), G, 'tail', 8, 6)
    # ---- the gear: the nose leg and wheel, the mains under the wing with wheel pants
    cyl('Nose leg', (0, -.40, 2.30), (0, GROUND + .24, 2.40), .05, G, 'metal', 8)
    cyl('Nose wheel', (-.07, GROUND + .24, 2.40), (.07, GROUND + .24, 2.40), .24, G, 'rubber', 16)
    for s in (-1, 1):
        cyl('Main leg', (s * 1.05, WING_Y, .10), (s * 1.15, GROUND + .30, .05), .05, G, 'metal', 8)
        cyl('Main wheel', (s * 1.08, GROUND + .30, .05), (s * 1.24, GROUND + .30, .05), .30, G, 'rubber', 18)
        blob('Wheel pant', (s * 1.16, GROUND + .38, .05), (.14, .24, .52), G, 'paint', 12, 8)
    # ---- inside: the seat, the panel, the stick
    hull('Seat', [(-.22, H.y - .14, H.z - .25), (.22, H.y - .14, H.z - .25), (.22, H.y - .14, H.z + .22), (-.22, H.y - .14, H.z + .22),
                  (-.22, H.y + .02, H.z - .25), (.22, H.y + .02, H.z - .25), (.22, H.y, H.z + .22), (-.22, H.y, H.z + .22)], G, 'interior', .04, 2)
    hull('Seat back', [(-.23, H.y - .06, H.z - .34), (.23, H.y - .06, H.z - .34), (.23, H.y - .06, H.z - .20), (-.23, H.y - .06, H.z - .20),
                       (-.21, H.y + .70, H.z - .48), (.21, H.y + .70, H.z - .48), (.21, H.y + .70, H.z - .34), (-.21, H.y + .70, H.z - .34)], G, 'interior', .04, 2)
    box('Panel', (0, .28, .62), (.70, .26, .10), G, 'engine', .02, (-15, 0, 0))
    cyl('Stick', (0, -.30, .25), (0, .05, .18), .025, G, 'engine', 8)


def prop():
    G = 'Prop'
    for s in (-1, 1):
        box('Blade', (0, s * (.12 + (R_PROP - .12) / 2), HUB[2] + .05), (.15, R_PROP - .12, .05), G, 'engine', .01, (0, 0, 8 * s))
        box('Blade tip', (0, s * (R_PROP - .06), HUB[2] + .05), (.151, .12, .051), G, 'tail', .004, (0, 0, 8 * s))


def disc():
    cyl('Prop disc', (HUB[0], HUB[1], HUB[2] + .04), (HUB[0], HUB[1], HUB[2] + .05), R_PROP, 'Disc', 'glass', 40, smooth=False)


def build():
    bpy.ops.wm.read_factory_settings(use_empty=True)
    body(); prop(); disc()
    out = kit.join_all()
    for ob in out:
        g = ob.name.split('__')[0]
        if g in ('Prop', 'Disc'): kit.set_origin(ob, HUB)
    print('PlaneDart: hub', HUB, 'wheels at', GROUND, 'wing tips at', WING_TIP)
    kit.export('PlaneDart')


if __name__ == '__main__':
    build()
