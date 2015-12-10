using System;
using Android.App;
using Android.Bluetooth;
using Android.Content;
using Android.OS;
using Android.Util;
using Android.Views;
using Android.Views.InputMethods;
using Android.Widget;
using Java.Lang;
using System.Collections;
using System.Runtime.InteropServices;
using System.IO;
using ProtoBuf;

namespace BlueNet
{
	/// <summary>
	/// This activity handles all bluetooth aspects
	/// </summary>
	[Activity (Theme = "@style/Theme.Main")]
	public class BlueHandle : Activity
	{
		//Variables
		public int maxDevices = 0;
		private ArrayList messages = new ArrayList ();
		private int devices = 0;
		private int directDevices = 0;
		private int randomCount;
		private int randomTotal;
		private ArrayList randomDevices = new ArrayList ();
		public string DeviceName;
		public bool turn = false;
		public ArrayList messageContents = new ArrayList ();
		public ArrayAdapter<string> messagesViewAdapter;
		private MyHandler handle;




		// Debugging
		private const string TAG = "BluetoothChat";
		private bool activeReturn = false;




		// Message types sent from the BluetoothChatService Handler
		// TODO: Make into Enums
		public const int MESSAGE_STATE_CHANGE = 1;
		public const int MESSAGE_READ = 2;
		public const int MESSAGE_WRITE = 3;
		public const int MESSAGE_DEVICE_NAME = 4;
		public const int MESSAGE_TOAST = 5;




		// Key names received from the BluetoothChatService Handler
		public const string DEVICE_NAME = "device_name";
		public const string INDEX = "index";
		public const string TOAST = "toast";




		// Intent request codes
		private const int REQUEST_CONNECT_DEVICE = 1;
		private const int REQUEST_ENABLE_BT = 2;




		// Names of connected devices
		protected ArrayList DeviceNames = new ArrayList();
		private ArrayList playersNotPlayed = new ArrayList ();




		// Bluetooth Adapter
		private BluetoothAdapter bluetoothAdapter = null;
		// bluetooth service
		private BluetoothChatService service = null;


		protected override void OnCreate (Bundle bundle)
		{
			base.OnCreate (bundle);
			//Initialization
			// Get local Bluetooth adapter
			bluetoothAdapter = BluetoothAdapter.DefaultAdapter;
			DeviceName = bluetoothAdapter.Name;
			handle = new MyHandler (this);

			messagesViewAdapter = new ArrayAdapter<string>(this, Resource.Layout.message);

			// If the adapter is null, then Bluetooth is not supported
			if (bluetoothAdapter == null) {
				Toast.MakeText (this, "Bluetooth is not available", ToastLength.Long).Show ();
				Finish ();
				return;
			}

			BluetoothAdapter adapter = BluetoothAdapter.DefaultAdapter;

			#region Home Screen UI stuff

			//set the content view to the home scree ui
			SetContentView (Resource.Layout.Home);

			//button handler for the only button - Math and Science Extravaganze with friends
			var butt = FindViewById<Button> (Resource.Id.gameButton);
			butt.Click += (object sender, EventArgs e) => {

				//build a dialog to ask for a new game or an exsiting game
				AlertDialog.Builder builder = new AlertDialog.Builder(this, 5);
				builder.SetTitle("Start Game?");
				builder.SetMessage("Do you want to start a new game or connect to an exsiting one?");

				//if new game launch the new game view which will get the number of players and the name of the device
				builder.SetPositiveButton("New Game", (s, ev) => {

					SetContentView(Resource.Layout.TextInput);

					var number = FindViewById<EditText>(Resource.Id.numPlayers);

					var name = FindViewById<EditText>(Resource.Id.editText1);


					var button = FindViewById<Button>(Resource.Id.createButton);

					button.Click += (object sender1, EventArgs e1) => {

						Console.WriteLine(number.Text.ToString());
						Console.WriteLine(name.Text.ToString());

						adapter.SetName(name.Text.ToString());

						maxDevices = Integer.ParseInt(number.Text.ToString());

						//enable discoverability for ever
						EnsureDiscoverable();

						SetContentView(Resource.Layout.WaitView);
					};

				});

				//if exsiting game then start the device finding dialog and initiate a connection
				builder.SetNegativeButton("Existing Game", (s, ev) => {

					activeReturn = true;

					Intent serverIntent = new Intent(this, typeof(DeviceListActivity));

					StartActivityForResult(serverIntent, REQUEST_CONNECT_DEVICE);
				});


				//show the dialog
				Dialog dialog = builder.Create();
				dialog.Show();
			};
			#endregion
				
		}





		protected override void OnStart ()
		{
			base.OnStart ();

			// If BT is not on, request that it be enabled.
			// will then be called during onActivityResult
			// TODO maybe put in onCreate()
			if (!bluetoothAdapter.IsEnabled) {
				Intent enableIntent = new Intent (BluetoothAdapter.ActionRequestEnable);
				StartActivityForResult (enableIntent, REQUEST_ENABLE_BT);

				// Otherwise, setup the chat session
			} else 
				if (service == null){
					// Initialize the BluetoothChatService to perform bluetooth connections
					service = new BluetoothChatService (this, handle);
					//TODO MAYBE?
					for(int index = 0; index < BluetoothChatService.SIZE; index++){
						if (service.GetState (index) == BluetoothChatService.STATE_NONE) {
							// Start the Bluetooth chat services
							service.Start (index);
						}
					}
			}
		}




		protected override void OnResume ()
		{
			base.OnResume ();

			// Performing this check in onResume() covers the case in which BT was
			// not enabled during onStart(), so we were paused to enable it...
			// onResume() will be called when ACTION_REQUEST_ENABLE activity returns.
			if (service != null) {
				// Only if the state is STATE_NONE, do we know that we haven't started already
				for(int index = 0; index < BluetoothChatService.SIZE; index++){
					if (service.GetState (index) == BluetoothChatService.STATE_NONE) {
						// Start the Bluetooth chat services
						service.Start (index);
					}
				}
			}
		}




		protected override void OnDestroy ()
		{
			base.OnDestroy ();

			// Stop the Bluetooth chat services
			// sorts through all devices
			for (int index = 0; index < BluetoothChatService.SIZE; index++) {
				if (service != null)
					service.Stop (index);
			}
		}



		/// <summary>
		/// Ensures the device is discoverable.
		/// </summary>
		private void EnsureDiscoverable ()
		{
			if (bluetoothAdapter.ScanMode != ScanMode.ConnectableDiscoverable) {
				Intent discoverableIntent = new Intent (BluetoothAdapter.ActionRequestDiscoverable);
				discoverableIntent.PutExtra (BluetoothAdapter.ExtraDiscoverableDuration, 0);
				StartActivity (discoverableIntent);
			}
		}




		/// <summary>
		/// Devices the found.
		/// </summary>
		/// <returns><c>true</c>, if device found, <c>false</c> otherwise.</returns>
		/// <param name="devices">Devices.</param>
		private bool DeviceFound (string devices){
			foreach(string device in DeviceNames){
				if(devices.Equals(device)){
					return true;
				}
			}
			return false;
		}




		public void startGame(){

			SetContentView(Resource.Layout.GameView);

			Button subButt = FindViewById<Button> (Resource.Id.subButton);

			Random rnd = new Random();

			string[] prompts = new string[] {" dna","n atom"," beaker"," magnet"," planet"," space"," microscope"," telescope"," lightbulb"," math compas"," gravity"," pi"," solar system"," cylinder"," safety goggles"," chemistry"," protein","n orbit"," rocket","n acid"," cell"," fungus"," battery"," sunlight"," testtube"," brain"," frequenycy"," sound waves"," cephalopod"};

			int randInt = rnd.Next(0, prompts.Length);

			messagesViewAdapter.Add (prompts [randInt]);

//			if (!turn) {
//
//				subButt.Enabled = false;
//			}

			makeMove ();
		}




		public void makeMove(){

			if (messages.Count % 2 == 1) {

				Android.Graphics.Bitmap image;

				LinearLayout layout = new LinearLayout (this);
				layout.Orientation = Orientation.Vertical;
				layout.SetBackgroundColor(Android.Graphics.Color.White);
				LinearLayout horiLayout = new LinearLayout (this);
				horiLayout.SetBackgroundColor(Android.Graphics.Color.SlateGray);
				horiLayout.Orientation = Orientation.Horizontal;

				Button drawButt = new Button (this);
				drawButt.Text = "Draw";
				horiLayout.AddView(drawButt);

				Button eraseButt = new Button (this);
				eraseButt.Text = "Erase";
				horiLayout.AddView(eraseButt);

				Button clearButt = new Button (this);
				clearButt.Text = "Clear";
				horiLayout.AddView(clearButt);

				Button doneButt = new Button (this);
				doneButt.Text = "Done";
				horiLayout.AddView(doneButt);

				DrawTest dt = new DrawTest(this);

				layout.AddView(horiLayout);
				layout.AddView(dt);

				drawButt.Click += (object sender, EventArgs e) => {

					dt.setColor(false);
				};

				eraseButt.Click += (object sender, EventArgs e) => {

					dt.setColor(true);
				};

				clearButt.Click += (object sender, EventArgs e) => {

					dt.clear();
				};

				doneButt.Click += (object sender, EventArgs e) => {

					image = dt.done();

					//MessageStruct message = new MessageStruct();
					//message.Data = "Image Sent";
					//message.Number = 0;
					//message.Pass = false;

					//messages.Add(message);

					//byte[] temp = MyHandler.RawSerialize(message);

					//SendMessages(temp);

					//messagesViewAdapter.Add(message.Data);

					SetContentView(Resource.Layout.GameView);
				};



				SetContentView(layout);


			} else {

				Button doneButt = FindViewById<Button> (Resource.Id.subButton);
				EditText text = FindViewById<EditText> (Resource.Id.messageEntry);

				doneButt.Click+= (object sender, EventArgs e) => {

					//MessageStruct message = new MessageStruct();
					//message.Data = text.Text;
					//message.Number = 0;
					//message.Pass = false;

					//messages.Add(message);
					//messagesViewAdapter.Add(message.Data);

					//byte[] temp = MyHandler.RawSerialize(message);

					//SendMessages(temp);
				};
			}


		}




		public void messageRecived(){
		}








		// The Handler that gets information back from the BluetoothChatService
		private class MyHandler : Handler
		{
			BlueHandle bluetooth;

			public MyHandler (BlueHandle blue)
			{
				bluetooth = blue;	
			}




			public override void HandleMessage (Message msg)
			{
				switch (msg.What) {
				case MESSAGE_STATE_CHANGE:
					switch (msg.Arg1) {
					case BluetoothChatService.STATE_CONNECTED:
						break;
					case BluetoothChatService.STATE_CONNECTING:
						break;
					case BluetoothChatService.STATE_LISTEN:
					case BluetoothChatService.STATE_NONE:
						//TODO what if someone disconnects
						// which one disconnected?
						// .Remove();
						//bluetoothChat.title.SetText (Resource.String.title_not_connected);
						break;
					}
					break;
				case MESSAGE_WRITE:

					break;
					// reads the message, if it is a device list then updates device list, else updates the message list

					//IF pass is true AND message is False, then we are passing device info
					//IF pass is False, then we are processing a message
					//IF PASS is True AND message is True, then we are starting the game
				case MESSAGE_READ:
					byte[] readBuf = (byte[])msg.Obj;

					bool pass = true;
					bool type = true;
					int number = 0;
					string data;


					data = decode(pass, type, number, readBuf);

					if (pass && !type) {
						//get devices
						// decode byte[] for device names
						if (number != 0) {
							bluetooth.maxDevices = number;
						}
						//ByteArrayToString(
						string[] devices = data.Split(' ');
					
						foreach (string device in devices) {
							// add unique devices to the list
							bool ans = false;
							if(AddDevice(device)){
								//forward devices
								ans = true;

								Console.WriteLine (device + "\n\t" + bluetooth.devices);

								Console.Write (device + " ");
							}
							if (ans) {
								bluetooth.SendMessages (readBuf);
							}
						}

					} else if (!pass) {
						//add message to the messageList
						if (!bluetooth.messages.Contains (data)) {
								bluetooth.messages.Add (data);

							//send the message to all- flooding :)
							bluetooth.SendMessages (readBuf);
							// remove player from list of people who haven't played


							string[] players = Array.ConvertAll<object, string>( bluetooth.playersNotPlayed.ToArray(), x => x.ToString());
							string player = players [number];
							bluetooth.playersNotPlayed.Remove(player);
							if (player == bluetooth.DeviceName) {
								// TODO MAKE MOVE HERE
								bluetooth.makeMove();

							} else {
							// NOT YOUR TURN
							// IF YOU HAVE GONE, LOAD DATA


							}
						}

					} else {

						if (!bluetooth.randomDevices.Contains (data)) {
							// calculate if this is the starting device
							// add numbers to the count
							bluetooth.randomCount++;
							Console.WriteLine ("Random Count = " + bluetooth.randomCount);
							bluetooth.randomTotal += number;

							bluetooth.randomDevices.Add (data);

							//forward the message
							bluetooth.SendMessages (readBuf);

							// if all numbers have been received then find average
							if (bluetooth.maxDevices == bluetooth.randomCount) {
								int average = bluetooth.randomCount / bluetooth.maxDevices;
								//TODO SORT DEVICES ? how is this sorting
								bluetooth.DeviceNames.Sort ();
								string[] temp = Array.ConvertAll (bluetooth.DeviceNames.ToArray (), x => x.ToString ());

								// if you match, you are the first player
								if (temp [average].Equals (bluetooth.DeviceName)) {
									// execute turn if it is you TODO
									// START THE GAME HERE ######################## TODO
									// CHOOSE A RANDOM PROMPT

									bluetooth.startGame ();


								} else {
									// you are not the first player
								}
							}
						}
					}
					break;

					// saves the device to the list of devices
				case MESSAGE_DEVICE_NAME:

					bool newPass;
					bool newType;
					int newNumber;
					string newData;


					string deviceName = msg.Data.GetString (DEVICE_NAME);
					if(AddDevice(deviceName)){
						bluetooth.directDevices++;

						// send updated device list to all
						newPass = true;
						newType = false;
						newData = "";
						// put the devices into a string
						foreach (string device in bluetooth.DeviceNames) {
							newData += device;
							newData += " ";
						}
						if (bluetooth.maxDevices != 0) {
							newNumber = bluetooth.maxDevices;
						} else {
							newNumber = 0;
						}

						// sends the devices out to all devices
						byte[] byteMessage = encode(newPass, newType, newNumber, newData);
						bluetooth.SendMessages (byteMessage);

					}
					break;
				case MESSAGE_TOAST:
					Toast.MakeText (Application.Context, msg.Data.GetString (TOAST), ToastLength.Short).Show ();
					break;					
				}
			}
			/// <summary>
			/// Decode the specified pass, type, number and data.
			/// </summary>
			/// <param name="pass">If set to <c>true</c> pass.</param>
			/// <param name="type">If set to <c>true</c> type.</param>
			/// <param name="number">Number.</param>
			/// <param name="data">Data.</param>
			public string decode(bool pass, bool type, int number, byte[] data){
				string temp = System.Text.Encoding.UTF8.GetString(data);
				string[] bools = temp.Split ('*');
				pass = System.Convert.ToBoolean(bools [0]);
				type = System.Convert.ToBoolean(bools [1]);
				number = Integer.ParseInt(bools [2]);
				return bools [3];

			}
			/// <summary>
			/// Encode the specified pass, type, number and data.
			/// </summary>
			/// <param name="pass">If set to <c>true</c> pass.</param>
			/// <param name="type">If set to <c>true</c> type.</param>
			/// <param name="number">Number.</param>
			/// <param name="data">Data.</param>
			public byte[] encode(bool pass, bool type, int number, string data){
				string temp = pass.ToString () + "*" + type.ToString () + "*" + number + "*" + data;
				return System.Text.Encoding.UTF8.GetBytes (temp);
			}



			/// <summary>
			/// Adds the device.and updates text for devices connected to
			/// </summary>
			/// <returns><c>true</c>, if device was added, <c>false</c> otherwise.</returns>
			/// <param name="device">Device.</param>
			public bool AddDevice(string device){
				if (!bluetooth.DeviceNames.Contains(device)) {
					bluetooth.DeviceNames.Add (device);
					bluetooth.devices++;
					Toast.MakeText (Application.Context, "Connected to " + device, ToastLength.Short).Show ();

					Console.WriteLine (device);
					if((bluetooth.devices == bluetooth.maxDevices) && (bluetooth.maxDevices != 0)){
						// send random number to decide who goes first
						sendStart();
					}
					return true;
				}
				return false;
			}




			/// <summary>
			/// Sends the start. message with a random int, for determining who the first player is.
			/// </summary>
			public void sendStart(){
				bool pass;
				bool type;
				int number;
				string data;

				Random rand = new Random ();

				number = rand.Next (1, bluetooth.maxDevices);
				type = true;
				pass = true;
				data = bluetooth.DeviceName;

				byte[] temp = encode(pass,type,number,data);
				bluetooth.SendMessages (temp);

				bluetooth.playersNotPlayed = bluetooth.DeviceNames;
			}



			/// <summary>
			/// Strings to byte array.
			/// </summary>
			/// <returns>The to byte array.</returns>
			/// <param name="temp">Temp.</param>
			public byte[] StringToByteArray(string temp){
				byte[] bytes = new byte[temp.Length*sizeof(char)];
				System.Buffer.BlockCopy(temp.ToCharArray(),0,bytes,0,bytes.Length);
				return bytes;
			}




			/// <summary>
			/// Bytes the array to string.
			/// </summary>
			/// <returns>The array to string.</returns>
			/// <param name="temp">Temp.</param>
			public string ByteArrayToString(byte[] temp){
				char[] chars = new char[temp.Length / sizeof(char)];
				System.Buffer.BlockCopy (temp, 0, chars, 0, temp.Length);
				return new string (chars);
			}

//			public byte[] BitmapToByteArray(Android.Graphics.Bitmap temp){
//
//			
//			}

		}

		/// <summary>
		/// Raises the activity result event.
		/// Connects to a new device from deviceListActivity
		/// </summary>
		/// <param name="requestCode">Request code.</param>
		/// <param name="resultCode">Result code.</param>
		/// <param name="data">Data.</param>
		protected override void OnActivityResult(int requestCode, Result resultCode, Intent data)
		{

			switch (requestCode) {
			case REQUEST_CONNECT_DEVICE:
				// When DeviceListActivity returns with a device to connect
				if (resultCode == Result.Ok) {
					// Get the device MAC address
					var address = data.Extras.GetString (DeviceListActivity.EXTRA_DEVICE_ADDRESS);
					// Get the BLuetoothDevice object
					BluetoothDevice device = bluetoothAdapter.GetRemoteDevice (address);
					// Attempt to connect to the device
					service.Connect (device);
				}
				break;
			case REQUEST_ENABLE_BT:
				// When the request to enable Bluetooth returns
				if (resultCode == Result.Ok) {
					// Bluetooth is now enabled, so set up service
					service = new BluetoothChatService (this, handle);
				} else {
				
					// User did not enable Bluetooth or an error occured
					Toast.MakeText (this, Resource.String.bt_not_enabled_leaving, ToastLength.Short).Show ();
					Finish ();
				}
				break;
			}

			SetContentView (Resource.Layout.WaitView);
		}

		/// <summary>
		/// Sends a message.
		/// </summary>
		/// <param name='message'>
		/// A array of bytes to send.
		/// </param>
		private void SendMessages (byte[] message)
		{
			// Get the message bytes and tell the BluetoothChatService to write
			// Floods the message to all devices
			for (int index = 0; index < BluetoothChatService.SIZE; index++) {
				service.Write (message, index);
			}
		}

	}

}

