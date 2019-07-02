using Prism.Interactivity.InteractionRequest;
using System.Collections.Generic;

namespace TestHostApp2.Notifications
{
	public class ThreeDBuildingNotification : Confirmation
	{
		public string ImageFolderPath { get; set; } = string.Empty;
		public string ThreeDDataFolderPath { get; set; } = string.Empty;
		public bool IsCutPetTable { get; set; } = true;
		public bool IsSkipAlreadyBuilt { get; set; } = true;
		public bool IsEnableSkipAlreadyBuilt { get; set; } = false;

		public ThreeDBuildingNotification()
		{

		}
	}
}
