
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;

namespace BlueNet
{
	[Activity (Label = "GameActivity")]			
	public class GameActivity : Activity
	{
		public bool turn = false;

		protected override void OnCreate (Bundle savedInstanceState)
		{
			base.OnCreate (savedInstanceState);

			SetContentView (Resource.Layout.GameView);

			Button subButt = FindViewById<Button> (Resource.Id.subButton);


			if (!turn) {

				subButt.Enabled = false;
			}
		}
	}
}

