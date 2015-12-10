﻿using System;
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
		public string DeviceName;
		public bool turn = false;

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

			if(!bluetoothAdapter.IsEnabled){

				Intent enableIntent = new Intent (BluetoothAdapter.ActionRequestEnable);
				StartActivityForResult (enableIntent, 1);
			}

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
						Intent discoverIntent = new Intent(BluetoothAdapter.ActionRequestDiscoverable);
						discoverIntent.PutExtra(BluetoothAdapter.ExtraDiscoverableDuration, 0);
						StartActivity(discoverIntent);

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
			// setupChat() will then be called during onActivityResult
			if (!bluetoothAdapter.IsEnabled) {
				Intent enableIntent = new Intent (BluetoothAdapter.ActionRequestEnable);
				StartActivityForResult (enableIntent, REQUEST_ENABLE_BT);
				// Otherwise, setup the chat session
			} else {
				if (service == null)
					// Initialize the BluetoothChatService to perform bluetooth connections
					service = new BluetoothChatService (this, new MyHandler (this));			}
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
				discoverableIntent.PutExtra (BluetoothAdapter.ExtraDiscoverableDuration, 300);
				StartActivity (discoverableIntent);
			}
		}

		/// <summary>
		/// Determines whether this instance has messages the specified readBuf.
		/// </summary>
		/// <returns><c>true</c> if this instance has messages the specified readBuf; otherwise, <c>false</c>.</returns>
		/// <param name="readBuf">Read buffer.</param>
		private bool HasMessages(MessageStruct str){
			foreach (MessageStruct temp in messages) {
				if (temp.Equals(str)){
					return true;
				}
			}
			return false;
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


			if (!turn) {

				subButt.Enabled = false;
			}
		}

		public void makeMove(){

			if (messages.Count % 2 == 0) {


			} else {

				Button doneButt = FindViewById<Button> (Resource.Id.subButton);
				EditText text = FindViewById<EditText> (Resource.Id.messageEntry);

				doneButt.Click+= (object sender, EventArgs e) => {


				};
			}
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
						//bluetoothChat.title.SetText (Resource.String.title_connected_to);
						//bluetoothChat.title.Append (bluetoothChat.connectedDeviceName);
						//bluetoothChat.conversationArrayAdapter.Clear ();
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
					byte[] writeBuf = (byte[])msg.Obj;

					// put the message in the messageList
					if (!bluetooth.HasMessages ((MessageStruct)RawDeserialize(writeBuf, 0, typeof(MessageStruct)))) {
						bluetooth.messages.Add ((MessageStruct)RawDeserialize(writeBuf, 0, typeof(MessageStruct)));
					}

					break;
					// reads the message, if it is a device list then updates device list, else updates the message list
				case MESSAGE_READ:
					byte[] readBuf = (byte[])msg.Obj;

					MessageStruct message = (MessageStruct)RawDeserialize (readBuf, 0, typeof(MessageStruct));
					if (message.Pass && !message.Type) {
						//get devices
						// decode byte[] for device names
						if (message.Number != 0) {
							bluetooth.maxDevices = message.Number;
						}

						string[] devices = ByteArrayToString(message.Data).Split(' ');

						foreach (string device in devices) {
							// add unique devices to the list
							AddDevice(device);
						}

					} else if (!message.Pass) {
						//add message to the messageList
						if (ProcessMessage (message)) {
							//send the message to all- flooding :)
							bluetooth.SendMessages (readBuf);
							// remove player from list of people who haven't played


							string[] players = (string[])bluetooth.playersNotPlayed.ToArray();
							string player = players [message.Number];
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
						// calculate if this is the starting device
						// add numbers to the count
						bluetooth.randomCount++;
						bluetooth.randomTotal += message.Number;

						// if all numbers have been received then find average
						if(bluetooth.maxDevices == bluetooth.randomCount){
							int average = bluetooth.randomCount / bluetooth.maxDevices;
							//TODO SORT DEVICES ? how is this sorting
							bluetooth.DeviceNames.Sort();
							string[] temp = (string[]) bluetooth.DeviceNames.ToArray ();

							// if you match, you are the first player
							if (temp [average] == bluetooth.DeviceName) {
								// execute turn if it is you TODO
								// START THE GAME HERE ######################## TODO
								// CHOOSE A RANDOM PROMPT

								bluetooth.startGame();


							} else {
							// you are not the first player
							}
						}
					}
					break;
					// saves the device to the list of devices
				case MESSAGE_DEVICE_NAME:
					if(AddDevice(msg.Data.GetString (DEVICE_NAME))){
						bluetooth.directDevices++;

						// send updated device list to all
						MessageStruct newMessage = new MessageStruct();
						newMessage.Pass = true;
						newMessage.Type = false;
						string temp = "";
						// put the devices into a string
						foreach (string device in bluetooth.DeviceNames) {
							temp += device;
							temp += " ";
						}
						newMessage.Data = StringToByteArray(temp);
						if (bluetooth.maxDevices != 0) {
							newMessage.Number = bluetooth.maxDevices;
						} else {
							newMessage.Number = 0;
						}

						// sends the devices out to all devices
						byte[] byteMessage = RawSerialize (newMessage);
						bluetooth.SendMessages (byteMessage);

					}
					break;
				case MESSAGE_TOAST:
					Toast.MakeText (Application.Context, msg.Data.GetString (TOAST), ToastLength.Short).Show ();
					break;					
				}
			}

			/// <summary>
			/// Processes the message.
			/// </summary>
			/// <returns><c>true</c>, if message was new, <c>false</c> otherwise.</returns>
			/// <param name="message">Message.</param>
			public bool ProcessMessage(MessageStruct message){
				if (!bluetooth.HasMessages (message)) {
					bluetooth.messages.Add (message);

					return true;
				}
				return false;
			}
			/// <summary>
			/// Adds the device.and updates text for devices connected to
			/// </summary>
			/// <returns><c>true</c>, if device was added, <c>false</c> otherwise.</returns>
			/// <param name="device">Device.</param>
			public bool AddDevice(string device){
				if (!bluetooth.DeviceFound (device)) {
					bluetooth.DeviceNames.Add (device);
					bluetooth.devices++;
					Toast.MakeText (Application.Context, "Connected to " + device, ToastLength.Short).Show ();


					if(bluetooth.devices == bluetooth.maxDevices && bluetooth.maxDevices != 0){
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
				MessageStruct newMessage = new MessageStruct ();
				Random rand = new Random ();

				newMessage.Number = rand.Next (1, bluetooth.maxDevices);
				newMessage.Type = true;
				newMessage.Pass = true;
				byte[] temp = RawSerialize (newMessage);
				bluetooth.SendMessages (temp);

				bluetooth.playersNotPlayed = bluetooth.DeviceNames;
			}

			//found online
			public static byte[] RawSerialize( object anything )
			{
				int rawSize = Marshal.SizeOf( anything );
				IntPtr buffer = Marshal.AllocHGlobal( rawSize );
				Marshal.StructureToPtr( anything, buffer, false );
				byte[] rawDatas = new byte[ rawSize ];
				Marshal.Copy( buffer, rawDatas, 0, rawSize );
				Marshal.FreeHGlobal( buffer );
				return rawDatas;
			}

			// found online
			public static object RawDeserialize( byte[] rawData, int position, Type
				anyType )
			{
				int rawsize = Marshal.SizeOf( anyType );
				if( rawsize > rawData.Length )
					return null;
				IntPtr buffer = Marshal.AllocHGlobal( rawsize );
				Marshal.Copy( rawData, position, buffer, rawsize );
				object retobj = Marshal.PtrToStructure( buffer, anyType );
				Marshal.FreeHGlobal( buffer );
				return retobj;
			}

			/// <summary>
			/// Decode the specified message.
			/// </summary>
			/// <param name="message">Message.</param>
			public MessageStruct Decode(byte[] message){
				MessageStruct messages = new MessageStruct ();
				int size = Marshal.SizeOf (messages);
				IntPtr pointer = Marshal.AllocHGlobal (size);
				Marshal.Copy (message, 0, pointer, size);
				messages = (MessageStruct)Marshal.PtrToStructure (pointer, messages.GetType ());
				Marshal.FreeHGlobal (pointer);
				return messages;

			}
			/// <summary>
			/// Encode the specified message.
			/// </summary>
			/// <param name="message">Message.</param>
			public byte[] Encode (MessageStruct message){
//				int size = Marshal.SizeOf (message);
//				byte[] array = new byte[size];
//				IntPtr pointer = Marshal.AllocHGlobal (size);
//				Marshal.StructureToPtr (message, pointer, false);
//				Marshal.Copy (pointer, array, 0, size);
//				Marshal.FreeHGlobal (pointer);
//				return array;
//				System.Collections.ArrayList temp;
				byte[] temp;
				using (var ms = new MemoryStream ()) {

					Serializer.Serialize (ms, message);

					temp = ms.ToArray ();
				}

				foreach (byte b in temp) {

					Console.WriteLine (b);
				}

				return temp;
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
					service = new BluetoothChatService (this, new MyHandler (this));
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

