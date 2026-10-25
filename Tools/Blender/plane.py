"""Chastain Rush, Round 69: the player's plane (Sky Hopper), generated in Blender from this script (the kit and standard of Tools/Blender/helicopter.py).

Run:  F:\\blender\\blender.exe --background --factory-startup --python Tools/Blender/plane.py

Our own design, no make, badge or logo: a small single-engine high-wing plane. A cabin with side windows and a windshield under the wing, the
engine cowl and a two-blade propeller (1.9 m across) at the nose, a strut-braced wing 11 m across on top of the cabin (a little dihedral), a
tapered tail cone with the stabilizer and a swept fin, fixed tricycle gear (wheel pants on the mains). Two seats; the pilot on the left, the game's
rider in the cars' seated pose at Seat H. Unity vehicle space as the other vehicles (x right, y up, z forward), the origin at the physics box's
middle (VehicleProfile.Plane: box 1.3 x 1.5 x 7.6 m centred at y 0.05): the wheels' bottom at y = GROUND. Groups: Body, Prop (origin on the
propeller's hub, turns about z), Disc (the propeller's blur disc, shown at speed). Slots: paint, white, glass, metal, engine, rubber, lamp, tail,
interior. The wing tips: WING_TIP (PlaneFlight's wing strike points).
Output: SourceArt/Blender/Plane.blend, Assets/Resources/VehicleModels/Plane.fbx
"""
import bpy, math, sys, os
from mathutils import Vector
sys.path.insert(0, os.path.dirname(__file__))
import kit
from kit import B, box, hull, cyl, blob, tube, loft, ring

kit.COLORS['white'] = (.94, .94, .92)
GROUND = -1.25
H = Vector((-.30, -.30, .55))                    # the pilot's seat point (left seat; the Car pose's hip); VehicleProfile.Plane.Seat
HUB = (0, .12, 3.62)                             # the propeller's hub
R_PROP = .95
WING_Y, SPAN = 1.02, 5.5                         # the wing's underside over the origin, its half span
WING_TIP = (SPAN, WING_Y + .2, .15)


def body():
    G = 'Body'
    # ---- the fuselage: rings from the spinner back to the tail cone (the cabin deepest under the wing)
    keys = [(3.45, .30, .30, .12), (3.15, .52, .50, .10), (2.55, .60, .62, .08), (1.80, .64, .80, .10), (.90, .66, .88, .14), (-.20, .64, .86, .14),
            (-1.20, .52, .66, .18), (-2.40, .36, .44, .26), (-3.50, .22, .28, .34), (-4.30, .12, .18, .38)]
    secs = [ring((0, cy, z), rx, ry, 18, 'z') for z, rx, ry, cy in keys]
    loft('Fuselage', secs, G, 'paint', cap=True)
    loft('Cheat line', [ring((0, cy - .02, z), rx * 1.012, .06, 18, 'z') for z, rx, ry, cy in keys[1:-1]], G, 'white', cap=True)
    # ---- the cabin's glass: the windshield sloping up to the wing, side windows, the rear windows
    hull('Windshield', [(-.58, .45, 1.95), (.58, .45, 1.95), (.56, .45, 1.80), (-.56, .45, 1.80), (-.52, .98, 1.05), (.52, .98, 1.05), (.50, .98, .95), (-.50, .98, .95)], G, 'glass', .02, 1)
    for s in (-1, 1):
        hull('Side window', [(s * .64, .05, 1.55), (s * .70, .05, 1.55), (s * .70, .05, -.15), (s * .64, .05, -.15), (s * .60, .80, 1.10), (s * .66, .80, 1.10), (s * .64, .80, -.10), (s * .58, .80, -.10)], G, 'glass', .01, 1)
        hull('Rear window', [(s * .55, .12, -.45), (s * .61, .12, -.45), (s * .52, .12, -1.10), (s * .46, .12, -1.10), (s * .50, .70, -.45), (s * .56, .70, -.45), (s * .40, .55, -1.05), (s * .34, .55, -1.05)], G, 'glass', .01, 1)
        box('Door line', (s * .665, .25, .20), (.015, .80, .02), G, 'engine', .002)
    # ---- the engine cowl, its air inlets, exhaust and the spinner
    blob('Cowl', (0, .10, 2.95), (.58, .56, .78), G, 'paint', 20, 12)
    for s in (-1, 1): box('Inlet', (s * .22, .06, 3.58), (.18, .14, .06), G, 'engine', .02)
    cyl('Exhaust', (.30, -.40, 2.45), (.30, -.48, 2.10), .05, G, 'engine', 8)
    blob('Spinner', HUB, (.17, .17, .26), G, 'white', 14, 8)
    # ---- the wing: a thick-ish slab across the cabin's top, rounded tips, a flap line; the struts from the lower fuselage
    hull('Wing', [(-SPAN, WING_Y, .95), (SPAN, WING_Y, .95), (SPAN, WING_Y, -.60), (-SPAN, WING_Y, -.60),
                  (-SPAN, WING_Y + .20, .85), (SPAN, WING_Y + .20, .85), (SPAN, WING_Y + .14, -.55), (-SPAN, WING_Y + .14, -.55)], G, 'paint', .05, 2)
    for s in (-1, 1):
        blob('Wing tip', (s * SPAN, WING_Y + .10, .18), (.12, .11, .76), G, 'white', 12, 6)
        box('Flap line', (s * (SPAN * .45), WING_Y - .005, -.32), (SPAN * .7, .01, .02), G, 'engine', .002)
        tube('Strut', [(s * .62, -.40, .55), (s * 2.60, WING_Y - .02, .45)], .045, G, 'metal', 8)
        tube('Strut', [(s * .62, -.40, -.05), (s * 2.60, WING_Y - .02, -.15)], .04, G, 'metal', 8)
        blob('Nav light', (s * (SPAN + .08), WING_Y + .10, .55), (.04, .04, .06), G, 'lamp' if s > 0 else 'tail', 8, 6)
    # ---- the tail: the stabilizer, the fin with a swept leading edge, the rudder line
    hull('Stabilizer', [(-1.75, .30, -3.55), (1.75, .30, -3.55), (1.60, .30, -4.35), (-1.60, .30, -4.35), (-1.75, .38, -3.60), (1.75, .38, -3.60), (1.60, .36, -4.30), (-1.60, .36, -4.30)], G, 'paint', .02, 1)
    hull('Fin', [(-.06, .40, -3.10), (.06, .40, -3.10), (.06, .40, -4.40), (-.06, .40, -4.40), (-.04, 1.75, -4.05), (.04, 1.75, -4.05), (.04, 1.75, -4.55), (-.04, 1.75, -4.55)], G, 'paint', .02, 1)
    box('Fin stripe', (0, 1.30, -4.20), (.13, .16, .60), G, 'white', .01, (-18, 0, 0))
    blob('Beacon', (0, 1.78, -4.30), (.05, .05, .06), G, 'tail', 8, 6)
    # ---- the gear: the nose leg and wheel, the mains on spring legs with wheel pants
    cyl('Nose leg', (0, -.45, 2.45), (0, GROUND + .26, 2.55), .05, G, 'metal', 8)
    cyl('Nose wheel', (-.07, GROUND + .26, 2.55), (.07, GROUND + .26, 2.55), .26, G, 'rubber', 16)
    for s in (-1, 1):
        tube('Main leg', [(s * .55, -.55, .05), (s * 1.10, GROUND + .55, .00), (s * 1.20, GROUND + .32, -.02)], .05, G, 'metal', 8)
        cyl('Main wheel', (s * 1.13, GROUND + .32, -.02), (s * 1.31, GROUND + .32, -.02), .32, G, 'rubber', 18)
        blob('Wheel pant', (s * 1.22, GROUND + .40, -.02), (.16, .26, .55), G, 'paint', 12, 8)
        box('Step', (s * .70, -.60, .30), (.20, .03, .18), G, 'metal', .005)
    # ---- inside: two seats, the panel under the windshield, the yokes; the landing light in the wing's leading edge
    for x in (H.x, -H.x):
        hull('Seat', [(x - .22, H.y - .14, H.z - .25), (x + .22, H.y - .14, H.z - .25), (x + .22, H.y - .14, H.z + .22), (x - .22, H.y - .14, H.z + .22),
                      (x - .22, H.y + .02, H.z - .25), (x + .22, H.y + .02, H.z - .25), (x + .22, H.y, H.z + .22), (x - .22, H.y, H.z + .22)], G, 'interior', .04, 2)
        hull('Seat back', [(x - .23, H.y - .06, H.z - .34), (x + .23, H.y - .06, H.z - .34), (x + .23, H.y - .06, H.z - .20), (x - .23, H.y - .06, H.z - .20),
                           (x - .21, H.y + .70, H.z - .44), (x + .21, H.y + .70, H.z - .44), (x + .21, H.y + .70, H.z - .30), (x - .21, H.y + .70, H.z - .30)], G, 'interior', .04, 2)
        cyl('Yoke column', (x, .02, 1.55), (x, .02, 1.22), .025, G, 'engine', 8)
        box('Yoke', (x, .06, 1.20), (.32, .08, .04), G, 'rubber', .01)
    hull('Panel', [(-.62, -.10, 1.75), (.62, -.10, 1.75), (.62, -.10, 1.55), (-.62, -.10, 1.55), (-.60, .30, 1.62), (.60, .30, 1.62), (.60, .26, 1.45), (-.60, .26, 1.45)], G, 'interior', .03, 2)
    box('Panel face', (0, .12, 1.44), (1.08, .26, .02), G, 'engine', .004, (-12, 0, 0))
    box('Landing light', (-2.0, WING_Y + .08, .96), (.30, .08, .03), G, 'lamp', .01)


def prop():
    G = 'Prop'
    for s in (-1, 1):
        box('Blade', (0, s * (.12 + (R_PROP - .12) / 2), HUB[2] + .05), (.16, R_PROP - .12, .05), G, 'engine', .01, (0, 0, 8 * s))
        box('Blade tip', (0, s * (R_PROP - .06), HUB[2] + .05), (.161, .12, .051), G, 'tail', .004, (0, 0, 8 * s))


def disc():
    cyl('Prop disc', (HUB[0], HUB[1], HUB[2] + .04), (HUB[0], HUB[1], HUB[2] + .05), R_PROP, 'Disc', 'glass', 40, smooth=False)


def build():
    bpy.ops.wm.read_factory_settings(use_empty=True)
    body(); prop(); disc()
    out = kit.join_all()
    for ob in out:
        g = ob.name.split('__')[0]
        if g in ('Prop', 'Disc'): kit.set_origin(ob, HUB)
    print('Plane: hub', HUB, 'wheels at', GROUND, 'wing tips at', WING_TIP)
    kit.export('Plane')


if __name__ == '__main__':
    build()
