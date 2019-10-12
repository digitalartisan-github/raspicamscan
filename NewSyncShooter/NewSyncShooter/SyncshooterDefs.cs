using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using Newtonsoft.Json;

namespace NewSyncShooter
{
    public class SyncshooterDefs
    {
        // front/back/right/left カメラのIPアドレスの第4オクテット
        public int front_camera { get; set; } = -1;
        public int back_camera { get; set; } = -1;
        public int right_camera { get; set; } = -1;
        public int left_camera { get; set; } = -1;
        // IPアドレスのフォーマット (ex. 192.168.55.%d)
        public string ip_template { get; set; }
        public int camera_group_num { get; set; }
        public Dictionary<string, int[]> camera_group { get; set; }

        public string FrontCameraAddress
        {
            get
            {
                return Adrs4th_to_string( this.front_camera );
            }
        }
        public string BackCameraAddress
        {
            get
            {
                return Adrs4th_to_string( this.back_camera );
            }
        }
        public string RightCameraAddress
        {
            get
            {
                return Adrs4th_to_string( this.right_camera );
            }
        }
        public string LeftCameraAddress
        {
            get
            {
                return Adrs4th_to_string( this.left_camera );
            }
        }

        private string Adrs4th_to_string( int adrs4th )
        {
            string sFormat = ip_template;
            if ( string.IsNullOrEmpty( sFormat ) ) {
                return string.Empty;
            } else {
                int index = sFormat.LastIndexOf(".%d");
                sFormat = sFormat.Substring( 0, index );
                return sFormat + string.Format( ".{0}", adrs4th );
            }
        }

        // IP Address の一覧を列挙する
        public IEnumerable<string> GetAllCameraIPAddress()
        {
            return GetAllCameraIPAddressRaw().OrderBy( adrs =>
            {
                // IP Address の第4オクテットの昇順でソートする
                int idx = adrs.LastIndexOf('.');
                int adrs4th = int.Parse(adrs.Substring( idx  + 1 ));
                return adrs4th;
            } );
        }

        private IEnumerable<string> GetAllCameraIPAddressRaw()
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
