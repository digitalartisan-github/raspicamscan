using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;

namespace NewSyncShooter
{
	public class CameraParam
	{
		public int Orientation { get; set; }
		public string Shutter_speed_mode { get; set; }
		public string awb_mode { get; set; }
		public int brightness { get; set; }
		public string drc_strength { get; set; }
		public int jpeg_quality { get; set; }
		public int[] max_resol { get; set; }
		public int[] preview_resol { get; set; }
		public int shutter_speed { get; set; }
		public double wb_gb { get; set; }
		public double wb_rg { get; set; }

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
