using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

namespace Racer
{
    // Chastain Rush (Round 16): copied from Racer unchanged.
    // Vector graphics remain crisp at each menu resolution; text is a separate readable layer.
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class MenuGlyph : MaskableGraphic
    {
        public string Path="";
        string Shape=>Path.Substring(Path.LastIndexOf('/')+1);
        static bool Xbox => MenuInput.Pad==null||MenuInput.Pad.layout=="Gamepad"||MenuInput.Pad.layout.Contains("XInput")||MenuInput.Pad.layout.Contains("Xbox");
        public static string Label(string path)
        {
            var key=path.Substring(path.LastIndexOf('/')+1);
            if(path.Contains("Gamepad")&&!Xbox){var control=MenuInput.Pad.TryGetChildControl<UnityEngine.InputSystem.InputControl>(path.Substring(path.IndexOf('/')+1));if(control!=null)return control.displayName;}
            if(path.Contains("leftStick"))key=path.EndsWith("Press")?"leftStickPress":"leftStick";
            if(path.Contains("rightStick"))key=path.EndsWith("Press")?"rightStickPress":"rightStick";
            return key switch {"buttonSouth"=>"A","buttonEast"=>"B","buttonWest"=>"X","buttonNorth"=>"Y",
                "leftShoulder"=>"LB","rightShoulder"=>"RB","leftTrigger"=>"LT","rightTrigger"=>"RT",
                "leftStick"=>"L","rightStick"=>"R","leftStickPress"=>"L3","rightStickPress"=>"R3",
                "start"=>"","select"=>"","dpad"=>"","arrows"=>"↑↓","leftRight"=>"← →","escape"=>"Esc","space"=>"Space","leftBracket"=>"[","rightBracket"=>"]","leftButton"=>"Click","rightButton"=>"RMB","scroll"=>"Wheel","backspace"=>"⌫",_=>key.Replace("Arrow","").ToUpperInvariant()};
        }
        public void SetPath(string path){if(Path==path)return;Path=path;SetVerticesDirty();}
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();var r=GetPixelAdjustedRect();string s=Path.Contains("leftStick/")?"leftStick":Path.Contains("rightStick/")?"rightStick":Shape;bool pad=Path.Contains("Gamepad");
            var c=new Color(.19f,.28f,.32f);
            if(pad&&Xbox)c=s switch {"buttonSouth"=>new Color(.12f,.65f,.28f),"buttonEast"=>new Color(.85f,.15f,.19f),"buttonWest"=>new Color(.10f,.40f,.88f),"buttonNorth"=>new Color(.83f,.64f,.05f),_=>c};
            void Polygon(Vector2[] points,Color tint){int start=vh.currentVertCount;foreach(var p in points)vh.AddVert(p,tint,Vector2.zero);for(int i=1;i<points.Length-1;i++)vh.AddTriangle(start,start+i,start+i+1);}
            void Box(float x,float y,float w,float h,Color tint){Polygon(new[]{new Vector2(x,y),new Vector2(x+w,y),new Vector2(x+w,y+h),new Vector2(x,y+h)},tint);}
            void Circle(float radius,Color tint){var p=new Vector2[40];for(int i=0;i<p.Length;i++){float a=i*Mathf.PI*2/p.Length;p[i]=r.center+new Vector2(Mathf.Cos(a),Mathf.Sin(a))*radius;}Polygon(p,tint);}
            if(pad&&(s.StartsWith("button")||s.Contains("Stick"))){Circle(r.height*.49f,Color.white);Circle(r.height*.44f,c);if(s.Contains("Stick"))Circle(r.height*.33f,new Color(.08f,.13f,.16f));}
            else if(s=="dpad") {Box(r.center.x-5,r.yMin,10,r.height,Color.white);Box(r.xMin,r.center.y-5,r.width,10,Color.white);}
            else if(s.EndsWith("Trigger")){Polygon(new[]{new Vector2(r.xMin,r.yMax),new Vector2(r.xMax,r.yMax),new Vector2(r.xMax-4,r.yMin+5),new Vector2(r.center.x,r.yMin),new Vector2(r.xMin+4,r.yMin+5)},c);}
            else {Box(r.xMin,r.yMin,r.width,r.height,Color.white);Box(r.xMin+2,r.yMin+2,r.width-4,r.height-4,c);}
            if(s=="start")for(int i=0;i<3;i++)Box(r.xMin+8,r.yMin+8+i*7,r.width-16,3,Color.white);
            if(s=="select"){Box(r.xMin+7,r.yMin+12,18,15,Color.white);Box(r.xMin+12,r.yMin+7,18,15,c);Box(r.xMin+14,r.yMin+9,14,11,Color.white);}
        }
    }
}
