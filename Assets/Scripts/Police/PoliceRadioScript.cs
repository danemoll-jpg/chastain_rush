using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ChastainRush;
using UnityEngine;

namespace Racer
{
    // Chastain Rush, Round 39 (Part D): the radio's script in the scene (MapBuilder.Police.cs), in its own file (a scene MonoBehaviour must live in a file named after it)
    // the script, embedded in the scene by the map build (Tools/Audio/police-radio-lines.csv)
    public sealed class PoliceRadioScript : MonoBehaviour
    {
        public static PoliceRadioScript Instance { get; private set; }
        [TextArea] public string csv = "";
        void Awake() { Instance = this; PoliceRadio.Load(csv); }
    }
}
