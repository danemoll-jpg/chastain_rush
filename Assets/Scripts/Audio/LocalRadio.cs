using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Networking;

namespace Racer
{
    // Local files only. One streaming clip, one cancellable request, bounded path/history lists.
    // Chastain Rush, Round 27 (B1): Racer's radio, ported. Edits (commented "Chastain Rush" in place): no RaceFlow; the settings are
    // GameSettings (Racer's Options, settings.json beside the race records); it starts itself once, with the first scene, and is
    // never started in a check run unless -radio is given; the controls work while driving (free roam or a race) when no menu,
    // map or debug menu has the input; [ and ] are this game's debug keys (debug panel, bug capture), so the keyboard's next /
    // previous song are . and , (the controller is Racer's D-pad); no weather / police ducking or title speech (none here); the
    // station / song line is drawn by its own small canvas at the bottom center, as Racer's songBanner.
    public sealed class LocalRadio : MonoBehaviour
    {
        static LocalRadio persistent;
        public float PlaybackSeconds=>source&&source.clip?source.time:0;
        public static AudioSource Source=>persistent?persistent.source:null;
        public static LocalRadio Current=>persistent;
        // Chastain Rush: one radio for the whole run, made with the first scene (Racer: LocalRadio.Attach from RaceFlow)
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            if(persistent)return;
            if(ChastainRush.CheckRun.Active&&Array.IndexOf(Environment.GetCommandLineArgs(),"-radio")<0)return;   // checks: no radio unless asked
            var go=new GameObject("Continuous local radio");DontDestroyOnLoad(go);
            persistent=go.AddComponent<LocalRadio>();persistent.Initialize();
        }
        static GameSettings.Options Settings=>GameSettings.Settings;   // Chastain Rush: Racer's flow.Save.Settings
        UnityEngine.UI.Text songBanner;
        AudioSource source;
        AudioClip clip;
        UnityWebRequest request;
        Coroutine loading;
        Task<MusicCollection.Result> scan;
        CancellationTokenSource scanCancellation;
        MusicCollection.Progress scanProgress;
        int scanRevision, activeScanRevision;
        bool rescanPending;
        string scanSummary="Not scanned";
        Task<string> metadata, picker;
        readonly List<string> library=new(), bag=new(), history=new();
        readonly HashSet<string> failed=new(StringComparer.OrdinalIgnoreCase);
        readonly System.Random random=new();
        MusicCollection.Channel[] channels=Array.Empty<MusicCollection.Channel>();
        readonly Dictionary<string,List<string>> channelHistory=new(StringComparer.OrdinalIgnoreCase);
        readonly Dictionary<string,string> channelLast=new(StringComparer.OrdinalIgnoreCase);
        int channelIndex=-1;
        public string ChannelName=>Settings.radioOn&&channelIndex>=0?channels[channelIndex].Name:"Off";
        public string[] ChannelNames=>channels.Where(c=>c.Paths.Any(p=>!failed.Contains(p))).Select(c=>c.Name).ToArray();
        public string CurrentPath=>current;
        string metadataPath;
        string current, lastPlayed, song="Radio: add MP3, WAV or Ogg files in Settings / Music", scanFolder;
        float toastUntil, retryAt;
        float stationUntil;
        string stationToast;
        int metadataRevision;
        int revision;
        public string Status { get; private set; }="Empty library";
        public string Song=>ChannelName=="Off"?"Radio Off":song;
        public string Toast=>Time.unscaledTime<stationUntil?stationToast:Time.unscaledTime<toastUntil?Song:null;
        void SongToast(float seconds=5) { toastUntil=Mathf.Max(Time.unscaledTime,stationUntil)+seconds; }
        public static string BundledFolder=>Path.GetFullPath(Path.Combine(Application.dataPath,"..","Music"));   // Chastain Rush: no launcher; Music beside the exe (in the Editor, beside Assets)
        public bool Bundled=>Settings.musicSource=="bundled";
        public bool IncludeSubfolders=>Settings.musicRecursive;
        public string Folder=>Bundled?BundledFolder:Settings.musicFolder;
        public bool Scanning=>scan!=null;
        public string ScanStatus=>Scanning?$"Scanning… {Volatile.Read(ref scanProgress.Tracks):N0} tracks / {Volatile.Read(ref scanProgress.Entries):N0} entries (Cancel available)":scanSummary+(failed.Count>0?$" · {failed.Count} playback skips":"");
        public int Count=>library.Count;
        public bool Loading=>loading!=null;
        public bool Playing=>source && source.isPlaying;
        public void Initialize()
        {
            source=gameObject.AddComponent<AudioSource>(); source.playOnAwake=false;source.volume=0;
            source.spatialBlend=0; source.ignoreListenerPause=true; source.priority=0;   // Chastain Rush, Round 35 (Part B): the most important voice (Racer: 180): with 32 real voices, the pipes' streams, waterfalls, AI engines and traffic (all 128 to 160) took the radio's voice
            source.bypassListenerEffects=true;   // Chastain Rush, Round 35 (Part B): not through the listener's low-pass (the glass stretches' underwater muffle, PipeAquarium)
            if(string.IsNullOrEmpty(Settings.musicSource))Settings.musicSource=string.IsNullOrEmpty(Settings.musicFolder)?"bundled":"custom";
            if(string.IsNullOrWhiteSpace(Settings.musicFolder)) Settings.musicFolder=Path.Combine(Application.persistentDataPath,"Music");
            GameSettings.SaveSettings();
            EnsureDefault(); Rescan();
            MakeBanner();
            // Chastain Rush: Racer's RaceFlow.Start puts AudioCeiling (a soft ceiling for full-scale music over loud impacts) on the listener
            var listener=FindAnyObjectByType<AudioListener>(); if(listener&&!listener.GetComponent<AudioCeiling>())listener.gameObject.AddComponent<AudioCeiling>();
        }
        // Chastain Rush: Racer's songBanner (RaceMenus.Initialize), on a canvas of its own under the menus
        void MakeBanner()
        {
            var canvasGo=new GameObject("Radio banner",typeof(RectTransform),typeof(Canvas),typeof(UnityEngine.UI.CanvasScaler));canvasGo.transform.SetParent(transform,false);
            var canvas=canvasGo.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.sortingOrder=40;
            var scaler=canvasGo.GetComponent<UnityEngine.UI.CanvasScaler>();scaler.uiScaleMode=UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1280,720);scaler.matchWidthOrHeight=.5f;
            var go=new GameObject("Current song",typeof(RectTransform));go.transform.SetParent(canvasGo.transform,false);
            songBanner=go.AddComponent<UnityEngine.UI.Text>();songBanner.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");songBanner.fontSize=18;songBanner.color=Color.white;songBanner.raycastTarget=false;
            songBanner.alignment=TextAnchor.MiddleCenter;
            songBanner.rectTransform.anchorMin=new Vector2(.12f,.035f);songBanner.rectTransform.anchorMax=new Vector2(.80f,.10f);
            songBanner.horizontalOverflow=HorizontalWrapMode.Overflow;
            songBanner.resizeTextForBestFit=true; songBanner.resizeTextMinSize=14; songBanner.resizeTextMaxSize=18;
            songBanner.rectTransform.offsetMin=songBanner.rectTransform.offsetMax=Vector2.zero;
            songBanner.gameObject.AddComponent<UnityEngine.UI.Outline>();
        }
        void LateUpdate()
        {
            if(!songBanner)return;
            songBanner.gameObject.SetActive(!ChastainRush.GameMenus.Open&&!ChastainRush.WorldMap.OwnsInput);songBanner.text=Toast??"";
        }
        void EnsureDefault()
        {
            try
            {
                if(!Bundled&&Folder!=Path.Combine(Application.persistentDataPath,"Music"))return;
                Directory.CreateDirectory(Folder);
                var readme=Path.Combine(Folder,"README.txt");   // Chastain Rush: README.txt
                if(!File.Exists(readme))File.WriteAllText(readme,"Add DRM-free MP3, PCM WAV or Ogg Vorbis files, including artist/album subfolders, then Rescan. Files stay local and are never modified. See RADIO.md beside the game. Limits: 100,000 tracks / 250,000 entries / 20,000 folders / 30 seconds per scan; MP3/Ogg 256 MiB, WAV 64 MiB. Limits and skipped entries are shown in Music settings.");
            }
            catch(Exception e) when(e is IOException||e is UnauthorizedAccessException){Status="Music folder unavailable; choose another folder";}
        }
        public static bool LocalFolder(string path)=>!string.IsNullOrWhiteSpace(path)&&Path.IsPathRooted(path)&&!path.StartsWith(@"\\")&&!path.Contains("://");
        public void Rescan()
        {
            StopLoad();source.Stop();if(clip)Destroy(clip);clip=null;current=null;
            scanRevision++;scanCancellation?.Cancel();rescanPending=true;
            if(scan!=null)return;
            BeginScan();
        }
        void BeginScan()
        {
            rescanPending=false;
            var folder=Folder; scanFolder=folder;
            if(!LocalFolder(folder)){Status="Choose a local folder";return;}
            Status="Scanning…";
            activeScanRevision=scanRevision;scanCancellation=new CancellationTokenSource();
            scanProgress=new MusicCollection.Progress();var token=scanCancellation.Token;var progress=scanProgress;bool recursive=true;
            scan=Task.Run(()=>MusicCollection.Scan(folder,recursive,token,progress));   // Chastain Rush: no launcher's personal folder
        }
        public void CancelScan(){scanRevision++;rescanPending=false;scanCancellation?.Cancel();scanSummary="Scan canceled; previous collection retained";}
        public void SetRecursive(bool recursive){Settings.musicRecursive=recursive;GameSettings.SaveSettings();Rescan();}
        public void SetSource(bool bundled)
        {
            Settings.musicSource=bundled?"bundled":"custom";GameSettings.SaveSettings();
            ClearCollection();EnsureDefault();Rescan();
        }
        void ClearCollection()
        {
            stationUntil=toastUntil=0;
            StopLoad(); source.Stop(); if(clip)Destroy(clip);clip=null;current=null;
            history.Clear(); library.Clear();bag.Clear();failed.Clear();lastPlayed=null;
            channels=Array.Empty<MusicCollection.Channel>();channelIndex=-1;channelHistory.Clear();channelLast.Clear();
            song="Radio: no track playing";
        }
        public void SetFolder(string path)
        {
            if(!LocalFolder(path)){Status="Choose a local folder";return;}
            ClearCollection();Settings.musicSource="custom";
            Settings.musicFolder=Path.GetFullPath(path);GameSettings.SaveSettings();
            Rescan();
        }
        public void OpenFolder()
        {
            try { if(Directory.Exists(Folder))System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(Folder){UseShellExecute=true}); else Status="Folder missing; choose another folder"; }
            catch(Exception){Status="Could not open Music folder";}
        }
        public void ChooseFolder()
        {
            if(picker!=null)return;
            var promise=new TaskCompletionSource<string>();picker=promise.Task;
            var thread=new Thread(()=>{try{promise.SetResult(WindowsFolder.Pick());}catch{promise.SetResult(null);}});
            thread.IsBackground=true;thread.SetApartmentState(ApartmentState.STA);thread.Start();
        }
        public void Toggle()
        {
            int next=Settings.radioOn?channelIndex+1:0;
            while(next<channels.Length&&!channels[next].Paths.Any(p=>!failed.Contains(p)))next++;
            SelectChannel(next<channels.Length?next:-1);
        }
        void SelectChannel(int index,bool announce=true)
        {
            if(channelIndex>=0)
            {
                channelHistory[channels[channelIndex].Id]=new List<string>(history);
                channelLast[channels[channelIndex].Id]=lastPlayed;
            }
            StopLoad();source.Stop();source.clip=null;if(clip)Destroy(clip);clip=null;current=null;
            library.Clear();bag.Clear();history.Clear();lastPlayed=null;channelIndex=index;
            Settings.radioOn=index>=0;
            if(index>=0)
            {
                var channel=channels[index];library.AddRange(channel.Paths);
                if(channelHistory.TryGetValue(channel.Id,out var prior))history.AddRange(prior.Where(p=>library.Contains(p)));
                channelLast.TryGetValue(channel.Id,out lastPlayed);
                Settings.radioChannel=channel.Id;retryAt=0;Next();
            }
            else {song=Status=channels.Length==0?"Off / no playable channels. Add music, then Rescan.":"Radio Off";Settings.radioChannel="off:";}
            GameSettings.SaveSettings();
            if(announce)
            {
                stationToast=index<0?"Radio Off":Plain(channels[index].Name,48)+" Radio";
                stationUntil=Time.unscaledTime+2.5f; toastUntil=stationUntil;
            }
        }
        public void ShowSong(){SongToast(); if(!clip&&loading==null)song=Status;}
        public void Next()
        {
            if(!Settings.radioOn||Scanning)return;
            pendingSeek=0f;   // Chastain Rush, Round 34: a new song starts at its start
            if(library.Count==0){Status="No playable files. Settings / Music / Open folder, then Rescan";ShowSong();return;}
            if(bag.Count==0)
            {
                bag.AddRange(library.Where(p=>!failed.Contains(p)));
                for(int i=bag.Count-1;i>0;i--){int j=random.Next(i+1);(bag[i],bag[j])=(bag[j],bag[i]);}
                if(bag.Count>1&&bag[^1]==lastPlayed)(bag[0],bag[^1])=(bag[^1],bag[0]);
            }
            if(bag.Count==0){Status="Channel has no readable audio; skipping";Toggle();return;}
            if(bag[^1]==lastPlayed&&library.Count(p=>!failed.Contains(p))>1)
            {bag.RemoveAt(bag.Count-1);if(bag.Count==0){Next();return;}}
            var path=bag[^1];bag.RemoveAt(bag.Count-1);Play(path,true);
        }
        public void Previous()
        {
            if(!Settings.radioOn||Scanning)return;
            pendingSeek=0f;   // Chastain Rush, Round 34
            while(history.Count>0){var path=history[^1];history.RemoveAt(history.Count-1);if(library.Contains(path)&&!failed.Contains(path)){Play(path,false);return;}}
            ShowSong();
        }
        void Play(string path,bool remember)
        {
            if(remember&&current!=null&&clip){if(history.Count==32)history.RemoveAt(0);history.Add(current);}
            StopLoad();source.Stop();if(clip)Destroy(clip);clip=null;
            current=path;Status="Loading…";song=FormatSong(null,null,path);
            if(metadata==null)metadataPath=null;

            loading=StartCoroutine(Load(path,revision));
        }
        IEnumerator Load(string path,int token)
        {
            yield return null;
            bool valid=false;
            try
            {
                var info=new FileInfo(path);valid=info.Exists&&info.Length>=12&&info.Length<=(Type(path)==AudioType.WAV?64L:256L)*1024*1024;
                if(valid&&Type(path)==AudioType.WAV)
                {
                    using var stream=File.OpenRead(path);var header=new byte[12];valid=stream.Read(header,0,12)==12&&System.Text.Encoding.ASCII.GetString(header,0,4)=="RIFF"&&System.Text.Encoding.ASCII.GetString(header,8,4)=="WAVE";
                }
            }catch{}
            if(!valid){Fail(path);yield break;}
            request=UnityWebRequestMultimedia.GetAudioClip(new Uri(path).AbsoluteUri,Type(path));request.timeout=15;
            ((DownloadHandlerAudioClip)request.downloadHandler).streamAudio=true;
            yield return request.SendWebRequest();
            if(token!=revision)yield break;
            if(request.result!=UnityWebRequest.Result.Success){request.Dispose();request=null;Fail(path);yield break;}
            try{clip=DownloadHandlerAudioClip.GetContent(request);}catch{clip=null;}
            request.Dispose();request=null;
            if(!clip||clip.length<=0){Fail(path);yield break;}
            StartClip();
        }
        void StartClip()
        {
            lastPlayed=current;held=false;songLength=clip.length;   // Chastain Rush, Round 34
            source.clip=clip;source.volume=Settings.music*.32f;
            if(Settings.radioOn)source.Play();
            if(pendingSeek>0f){try{source.time=Mathf.Min(pendingSeek,clip.length-.1f);}catch{}}   // Chastain Rush, Round 34: the same song again, where it was
            else resumeTries=0;
            lastTime=pendingSeek;pendingSeek=0f;
            retryAt=Time.unscaledTime+1.5f; // Give a newly opened streaming voice time to start.
            loading=null;Status="Ready / "+library.Count+" tracks";SongToast();
            Debug.Log("[Radio] playing "+ChannelName+": "+Path.GetFileName(current)+$" ({clip.length:0} s)");   // Chastain Rush: for checks
        }
        void Fail(string path){pendingSeek=0f;failed.Add(path);loading=null;Status="Skipped unreadable or oversized audio";song=Status;SongToast(4);retryAt=Time.unscaledTime+.5f;}
        void StopLoad(){revision++;if(request!=null){request.Abort();request.Dispose();request=null;}if(loading!=null){StopCoroutine(loading);loading=null;}}
        
        // Chastain Rush, Round 34 (Part B, Dan: "every time click back to the game it skips to the next song. Let it stay on the same
        // song"): losing focus pauses the song where it is, getting it back goes on from there (only the player's controls and a
        // song's real end change the song). The game's own menus never touch it (ignoreListenerPause).
        float lastTime,resumeFrom,pendingSeek,songLength;int resumeTries;bool held;   // songLength: kept apart, a reset audio device destroys the clip
        public bool Held=>held;
        void OnApplicationFocus(bool focus)=>Hold(!focus);
        void OnApplicationPause(bool paused)=>Hold(paused);
        public void Hold(bool hold)   // public for the check (Checks/Round20Runner.R34.cs)
        {
            if(hold){if(!held&&source.isPlaying&&clip){lastTime=source.time;source.Pause();held=true;}return;}
            if(!held)return;
            held=false;retryAt=Time.unscaledTime+1.5f;
            if(clip&&source.clip==clip&&Settings.radioOn){source.UnPause();if(!source.isPlaying)Resume();}
        }
        void Resume()
        {
            resumeTries++;resumeFrom=lastTime;
            Debug.Log($"[Radio] going on with {Path.GetFileName(current)} at {lastTime:0.0} s"+(resumeTries>1?" (the voice is gone: the same file again)":""));
            if(resumeTries==1&&clip&&source.clip==clip){try{source.time=Mathf.Min(lastTime,clip.length-.1f);}catch{}source.Play();return;}
            pendingSeek=lastTime;Play(current,false);   // a reset audio device leaves a streamed clip dead: the same song again from the file, at the same place
        }
        void Update()
        {
            source.volume=ChastainRush.StartupTitle.Quiet?0:Settings.music*.32f*(1-.4f*WeatherEffects.Duck)*(1-.45f*PoliceRadioVoice.Duck);   // Round 51: silent under the title's voice and theme (Racer: StartupTitle.SpeechPending)   // Round 39: and under the police radio   // Chastain Rush: no police radio or title speech to dip under (Round 35: the thunder's dip back);// 0.76: dips under thunder; 0.99: and a little while the police radio speaks
            if(scan!=null&&scan.IsCompleted)
            {
                var result=scan.IsCompletedSuccessfully?scan.Result:new MusicCollection.Result{Error="Scan failed; try another folder"};scan=null;
                scanCancellation.Dispose();scanCancellation=null;
                if(activeScanRevision!=scanRevision||scanFolder!=Folder){if(rescanPending)BeginScan();return;}
                scanSummary=result.Summary;
                bool on=Settings.radioOn;string selected=Settings.radioChannel;string previousChannel=ChannelName;
                if(channelIndex>=0){channelHistory[channels[channelIndex].Id]=new List<string>(history);channelLast[channels[channelIndex].Id]=lastPlayed;}
                channels=result.Channels;channelIndex=-1;failed.Clear();
                int index=Array.FindIndex(channels,c=>string.Equals(c.Id,selected,StringComparison.OrdinalIgnoreCase));
                if(index<0&&channels.Length>0)index=0;
                SelectChannel(on?index:-1,previousChannel!=(on&&index>=0?channels[index].Name:"Off"));
                Status=channels.Length==0?"No playable channels. Add music, then Rescan":channels.Length+" channels / "+result.Paths.Count+" tracks";
                Debug.Log("[Radio] "+Status+" in "+Folder+": "+string.Join(", ",channels.Select(c=>c.Name+" ("+c.Paths.Length+")")));   // Chastain Rush: for checks
            }
            if(metadata!=null&&metadata.IsCompleted){if(metadata.IsCompletedSuccessfully&&clip&&metadataPath==current&&metadataRevision==revision){song=metadata.Result;SongToast();}metadata=null;}
            if(metadata==null&&clip&&(metadataPath!=current||metadataRevision!=revision)){metadataPath=current;metadataRevision=revision;var path=current;metadata=Task.Run(()=>ReadTitle(path));}
            if(picker!=null&&picker.IsCompleted){var path=picker.Result;picker=null;if(path!=null)SetFolder(path);}
            // Chastain Rush, Round 34 (Part B): a song ends only at its end. Racer read any stopped voice as the song's end, so a voice
            // stopped by the window losing focus (or the audio device being reset) skipped to the next song; now it goes on from where it was.
            if(source.isPlaying&&clip){if(source.time>.05f){lastTime=source.time;if(source.time>resumeFrom+1f)resumeTries=0;}}
            else if(!held&&!Scanning&&Settings.radioOn&&loading==null&&library.Count>0&&Time.unscaledTime>=retryAt)
            {
                retryAt=Time.unscaledTime+1;
                if(current!=null&&lastTime>0f&&lastTime<songLength-2f&&resumeTries<3)Resume();else{lastTime=0f;resumeTries=0;Next();}
            }
            // Chastain Rush: while driving (free roam or a race), not while a menu, the map or the debug menu has the input
            if(ChastainRush.GameMenus.Open||MenuInput.Blocked||ChastainRush.WorldMap.OwnsInput||(ChastainRush.DeveloperHud.Instance&&ChastainRush.DeveloperHud.Instance.MenuOpen))return;
            var k=Keyboard.current;var g=ChastainRush.TrailerMode.Active?null:Gamepad.current;// 0.77: the D-pad works the Trailer Mode cameras
            if(ChastainRush.TrailerMode.Active&&(k?.periodKey.wasPressedThisFrame==true||k?.commaKey.wasPressedThisFrame==true))k=null;// Round 43: , and . are Trailer Mode's clock
            // Chastain Rush: . / , for next / previous ([ and ] are the debug panel and bug capture here)
            if(k?.periodKey.wasPressedThisFrame==true||g?.dpad.right.wasPressedThisFrame==true)Next();
            else if(k?.commaKey.wasPressedThisFrame==true||g?.dpad.left.wasPressedThisFrame==true)Previous();
            if(k?.iKey.wasPressedThisFrame==true||g?.dpad.up.wasPressedThisFrame==true)ShowSong();
            if(k?.nKey.wasPressedThisFrame==true||g?.dpad.down.wasPressedThisFrame==true)Toggle();
        }
        static AudioType Type(string p)=>Path.GetExtension(p).ToLowerInvariant() switch {".mp3"=>AudioType.MPEG,".wav"=>AudioType.WAV,".ogg"=>AudioType.OGGVORBIS,_=>AudioType.UNKNOWN};
        static string Plain(string value,int limit=48)
        {
            var clean=new string((value??"").Where(c=>!char.IsControl(c)).ToArray());
            var elements=System.Globalization.StringInfo.GetTextElementEnumerator(clean);var result=new System.Text.StringBuilder();int count=0;
            while(elements.MoveNext()){if(count++==limit){result.Append('…');break;}result.Append(elements.GetTextElement());}
            return result.ToString();
        }
        public static string FormatSong(string artist,string title,string path) => "Artist: "+Plain(string.IsNullOrWhiteSpace(artist)?"Unknown Artist":artist)+"\nSong: "+Plain(string.IsNullOrWhiteSpace(title)?Path.GetFileNameWithoutExtension(path):title);
        static string ReadTitle(string path)
        {
            try
            {
                using var stream=new MetadataStream(path);
                ATL.Settings.ReadAllMetaFrames=false;
                var track=new ATL.Track(stream,Path.GetExtension(path));
                string title=string.IsNullOrWhiteSpace(track.Title)?Path.GetFileNameWithoutExtension(path):track.Title;
                return FormatSong(track.Artist,title,path);
            }
            catch {return FormatSong(null,null,path);}
        }
        sealed class MetadataStream:FileStream
        {
            int remaining=4*1024*1024;
            public override int ReadByte(){if(--remaining<0)throw new IOException("Metadata read budget");return base.ReadByte();}
            public MetadataStream(string path):base(path,FileMode.Open,FileAccess.Read,FileShare.ReadWrite|FileShare.Delete){}
            public override int Read(byte[] b,int o,int c){if((remaining-=c)<0)throw new IOException("Metadata read budget");return base.Read(b,o,c);}
            public override int Read(Span<byte> b){if((remaining-=b.Length)<0)throw new IOException("Metadata read budget");return base.Read(b);}
        }
        void OnDestroy(){if(persistent==this)persistent=null;scanCancellation?.Cancel();StopLoad();if(clip)Destroy(clip);}
    }
    static class WindowsFolder
    {
        [StructLayout(LayoutKind.Sequential,CharSet=CharSet.Unicode)]
        struct BrowseInfo {public IntPtr owner,root,display;[MarshalAs(UnmanagedType.LPWStr)]public string title;public uint flags;public IntPtr callback,param;public int image;}
        [DllImport("shell32.dll",CharSet=CharSet.Unicode)]static extern IntPtr SHBrowseForFolderW(ref BrowseInfo info);
        [DllImport("shell32.dll",CharSet=CharSet.Unicode)]static extern bool SHGetPathFromIDListW(IntPtr id,System.Text.StringBuilder path);
        public static string Pick()
        {
            var display=Marshal.AllocHGlobal(520);
            try {var info=new BrowseInfo{display=display,title="Choose your local Chastain Rush music folder",flags=0x41};var id=SHBrowseForFolderW(ref info);if(id==IntPtr.Zero)return null;
                try{var path=new System.Text.StringBuilder(260);return SHGetPathFromIDListW(id,path)?path.ToString():null;}finally{Marshal.FreeCoTaskMem(id);}}
            finally{Marshal.FreeHGlobal(display);}
        }
    }
}
