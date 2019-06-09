using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using Newtonsoft.Json;

namespace NewSyncShooter
{
	public class SyncshooterDefs
	{
		public int front_camera { get; set; }
		public int back_camera { get; set; }
		public int right_camera { get; set; }
		public int left_camera { get; set; }
		public string ip_template { get; set; }
		public int camera_group_num { get; set; }
		public Dictionary<string, int[]> camera_group { get; set; }

		// IP Address の一覧を列挙する
		public IEnumerable<string> GetAllCameraIPAddress()
		{
			string sFormat = ip_template;
			if ( string.IsNullOrEmpty( sFormat ) == false ) {
				int index = sFormat.LastIndexOf(".%d");
				sFormat = sFormat.Substring( 0, index );
				foreach ( var pair in camera_group ) {
					int[] addreses = pair.Value;
					foreach ( var adrs in addreses ) {
						string text = sFormat + string.Format(".{0}", adrs);
						yield return text;
					}
				}
			}
		}

		public static SyncshooterDefs Deserialize( string path )
		{
			var jsonStr = File.ReadAllText( path );
			var defs = JsonConvert.DeserializeObject<SyncshooterDefs>( jsonStr );
			return defs;
		}

		public void Serialize( string path )
		{
			var text = JsonConvert.SerializeObject( this, Formatting.Indented );
			File.WriteAllText( path, text );
		}
	}
}
