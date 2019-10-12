using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;

namespace NewSyncShooter
{
    public class CameraParam
    {
        public int Orientation { get; set; } = 1;
        public string Shutter_speed_mode { get; set; } = "auto";    // "auto","manual"?
        public string awb_mode { get; set; } = "off";               // "off","auto","fluorescent","sunlight",...
        public int brightness { get; set; } = 50;
        public string drc_strength { get; set; } = "high";          // "high","low"?
        public int jpeg_quality { get; set; } = 100;
        public int[] max_resol { get; set; } = new int[] { 3280, 2464 };
        public int[] preview_resol { get; set; } = new int[] { 1640, 1232 };
        public int shutter_speed { get; set; } = 1;
        public double wb_gb { get; set; } = 1.5;
        public double wb_rg { get; set; } = 1.5;

        public static double WbOffsetMin { get; } = 1.0;
        public static double WbOffsetMax { get; } = 2.0;
        public static int BrightnessMin { get; } = 0;
        public static int BrightnessMax { get; } = 100;
        public static int ShutterSpeedMin { get; } = 1;
        public static int ShutterSpeedMax { get; } = 200;

        public static CameraParam DecodeFromJsonText( string jsonStr )
        {
            return JsonConvert.DeserializeObject<CameraParam>( jsonStr );
        }

        public string EncodeToJsonText()
        {
            return JsonConvert.SerializeObject( this );
        }

        public static CameraParam Deserialize( string path )
        {
            var jsonText = File.ReadAllText( path );
            return DecodeFromJsonText( jsonText );
        }

        public void Serialize( string path )
        {
            var text = JsonConvert.SerializeObject( this, Formatting.Indented );
            File.WriteAllText( path, text );
        }

    }
}
