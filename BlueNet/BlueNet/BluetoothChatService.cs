using System.IO;
using System.Runtime.CompilerServices;
using Android.Bluetooth;
using Android.Content;
using Android.OS;
using Android.Util;
using Java.Lang;
using Java.Util;

namespace BlueNet
{
	/// <summary>
	/// This class does all the work for setting up and managing Bluetooth
	/// connections with other devices. It has a thread that listens for
	/// incoming connections, a thread for connecting with a device, and a
	/// thread for performing data transmissions when connected.
	/// </summary>
	class BluetoothChatService
	{
		// Debugging
		private const string TAG = "BluetoothChatService";
		private const bool Debug = true;
	
		// Name for the SDP record when creating server socket
		private const string NAME = "BluetoothChat";
	
		// Unique UUID for this application
		private static UUID MY_UUID = UUID.FromString ("fa87c0d0-afac-11de-8a39-0800200c9a66");

		// Number of Connections
		public const int SIZE = 5;

		// Member fields
		protected BluetoothAdapter _adapter;
		protected Handler _handler;
		private AcceptThread[] acceptThread = new AcceptThread[SIZE];
		protected ConnectThread[] connectThread = new ConnectThread[SIZE];
		private ConnectedThread[] connectedThread = new ConnectedThread[SIZE];
		protected int[] _state = new int[SIZE];
	
		// Constants that indicate the current connection state
		// TODO: Convert to Enums
		public const int STATE_NONE = 0;       // we're doing nothing
		public const int STATE_LISTEN = 1;     // now listening for incoming connections
		public const int STATE_CONNECTING = 2; // now initiating an outgoing connection
		public const int STATE_CONNECTED = 3;  // now connected to a remote device

		/// <summary>
		/// Constructor. Prepares a new BluetoothChat session.
		/// </summary>
		/// <param name='context'>
		/// The UI Activity Context.
		/// </param>
		/// <param name='handler'>
		/// A Handler to send messages back to the UI Activity.
		/// </param>
		public BluetoothChatService (Context context, Handler handler)
		{
			_adapter = BluetoothAdapter.DefaultAdapter;
			_handler = handler;
			for(int i =0; i<SIZE; i++) {
				_state[i] = STATE_NONE;
			}

			for (int i = 0; i < SIZE; i++) {

				connectThread [i] = null;
				acceptThread [i] = null;
				connectedThread [i] = null;
			}
		}
		
		/// <summary>
		/// Set the current state of the chat connection.
		/// </summary>
		/// <param name='state'>
		/// An integer defining the current connection state.
		/// </param>
		[MethodImpl(MethodImplOptions.Synchronized)]
		private void SetState (int state, int index)
		{
			if (Debug)
				Log.Debug (TAG, "setState() " + _state + " -> " + state);
			
			_state[index] = state;
	
			// Give the new state to the Handler so the UI Activity can update
			_handler.ObtainMessage (BlueHandle.MESSAGE_STATE_CHANGE, state, -1).SendToTarget ();
		}
		
		/// <summary>
		/// Return the current connection state.
		/// </summary>
		[MethodImpl(MethodImplOptions.Synchronized)]
		public int GetState (int index)
		{
			return _state[index];
		}
		
		// Start the chat service. Specifically start AcceptThread to begin a
		// session in listening (server) mode. Called by the Activity onResume()
		[MethodImpl(MethodImplOptions.Synchronized)]
		public void Start (int index)
		{	
				
			if (Debug)
				Log.Debug (TAG, "start");

			// Cancel any thread attempting to make a connection
			if (connectThread[index] != null) {
				connectThread[index].Cancel ();
				connectThread[index] = null;
			}

			// Cancel any thread currently running a connection
			if (connectedThread[index] != null) {
				connectedThread[index].Cancel ();
				connectedThread[index] = null;
			}


			// Start the thread to listen on a BluetoothServerSocket
			// TODO listening multiple times, may break
			if (acceptThread[index] == null) {
				acceptThread[index] = new AcceptThread (this, index);
				acceptThread[index].Start ();
			}
		
			SetState (STATE_LISTEN, index);

		}
		
		/// <summary>
		/// Start the ConnectThread to initiate a connection to a remote device.
		/// </summary>
		/// <param name='device'>
		/// The BluetoothDevice to connect.
		/// </param>
		[MethodImpl(MethodImplOptions.Synchronized)]
		public void Connect (BluetoothDevice device)
		{
			int index;
			for (index = 0; index < SIZE;index++) {
	
				if (connectedThread [index] == null) {

					break;
				}

				// Cancel any thread attempting to make a connection
				if (_state [index] == STATE_CONNECTING) {
					if (connectThread[index] != null) {
						connectThread[index].Cancel ();
						connectThread[index] = null;
						break;
					}
				}
	
				// Cancel any thread currently running a connection, if no open slots are available
				if (connectedThread[index] != null && index == SIZE-1) {
					// TODO SAFETY CHECK
					// change index to the safe index value
					connectedThread [index].Cancel ();
					connectedThread = null;

					break;
				}
			}
		

			// Start the thread to connect with the given device
			connectThread [index] = new ConnectThread (device, this, index);
			connectThread [index].Start ();
			
			SetState (STATE_CONNECTING, index);
		}
		

		
		/// <summary>
		/// Start the ConnectedThread to begin managing a Bluetooth connection
		/// </summary>
		/// <param name='socket'>
		/// The BluetoothSocket on which the connection was made.
		/// </param>
		/// <param name='device'>
		/// The BluetoothDevice that has been connected.
		/// </param>
		[MethodImpl(MethodImplOptions.Synchronized)]
		public void Connected (BluetoothSocket socket, BluetoothDevice device, int index)
		{
				
			if (Debug)
				Log.Debug (TAG, "connected");

			// Cancel the thread that completed the connection
			if (connectThread [index] != null) {
				connectThread [index].Cancel ();
				connectThread [index] = null;
			}

			// Cancel any thread currently running a connection
			if (connectedThread [index] != null) {
				connectedThread [index].Cancel ();
				connectedThread [index] = null;
			}

			// Cancel the accept thread because we only want to connect to one device
			if (acceptThread [index] != null) {
				acceptThread [index].Cancel ();
				acceptThread [index] = null;
			}
		
			// Start the thread to manage the connection and perform transmissions
			connectedThread [index] = new ConnectedThread (socket, this, index);
			connectedThread [index].Start ();

			// Send the name of the connected device back to the UI Activity
			var msg = _handler.ObtainMessage (BlueHandle.MESSAGE_DEVICE_NAME );
			Bundle bundle = new Bundle ();
			bundle.PutString (BlueHandle.DEVICE_NAME , device.Name);
			msg.Data = bundle;
			_handler.SendMessage (msg);

			SetState (STATE_CONNECTED, index);

		}
		
		/// <summary>
		/// Stop all threads.
		/// </summary>
		[MethodImpl(MethodImplOptions.Synchronized)]
		public void Stop (int index)
		{

			if (Debug)
				Log.Debug (TAG, "stop");

			if (connectThread[index] != null) {
				connectThread[index].Cancel ();
				connectThread[index] = null;
			}

			if (connectedThread[index] != null) {
				connectedThread[index].Cancel ();
				connectedThread[index] = null;
			}

			if (acceptThread[index] != null) {
				acceptThread[index].Cancel ();
				acceptThread[index] = null;
			}
		
			SetState (STATE_NONE, index);

		}
		
		/// <summary>
		/// Write to the ConnectedThread in an unsynchronized manner
		/// </summary>
		/// <param name='out'>
		/// The bytes to write.
		/// when we write, we are not writing to a specific thread
		/// no routing, we are using a flooding technique
		/// so we write to all
		/// we are writing while synchronized
		/// </param>
		public void Write (byte[] @out, int index)
		{
			// Create temporary object
			ConnectedThread r;

			lock (this) {
				if (_state[index] != STATE_CONNECTED) 
					return;
			r = connectedThread[index];
				// Perform the write synchronized
			}
			// Perform the write unsynchronized
			r.Write (@out);
		}
	
		/// <summary>
		/// Indicate that the connection attempt failed and notify the UI Activity.
		/// </summary>
		private void ConnectionFailed (int index)
		{
			SetState (STATE_LISTEN, index);
			
			// Send a failure message back to the Activity
			var msg = _handler.ObtainMessage (BlueHandle.MESSAGE_TOAST);
			Bundle bundle = new Bundle ();
			bundle.PutString (BlueHandle.TOAST, "Unable to connect device");
			msg.Data = bundle;
			_handler.SendMessage (msg);
		}
	
		/// <summary>
		/// Indicate that the connection was lost and notify the UI Activity.
		/// </summary>
		public void ConnectionLost (int index)
		{
			SetState (STATE_LISTEN, index);
			
			// Send a failure message back to the Activity
			var msg = _handler.ObtainMessage (BlueHandle.MESSAGE_TOAST);
			Bundle bundle = new Bundle ();
			bundle.PutString (BlueHandle.TOAST, "Device connection was lost");
			//bundle.PutString (BluetoothChat.DEVICE_NAME, Device.name);
			msg.Data = bundle;
			_handler.SendMessage (msg);
		}

		/// <summary>
		/// This thread runs while listening for incoming connections. It behaves
		/// like a server-side client. It runs until a it is full of connections
		/// (or until cancelled).
		/// </summary>
		private class AcceptThread : Thread
		{
			// The local server socket
			private BluetoothServerSocket mmServerSocket;
			private BluetoothChatService _service;
			private int mmIndex;
			
			public AcceptThread (BluetoothChatService service, int index)
			{
				_service = service;
				BluetoothServerSocket tmp = null;
				mmIndex = index;
	
				// Create a new listening server socket TODO potential problem with names?
				try {
					tmp = _service._adapter.ListenUsingRfcommWithServiceRecord (NAME, MY_UUID);
	
				} catch (Java.IO.IOException e) {
					Log.Error (TAG, "listen() failed", e);
				}
				mmServerSocket = tmp;
			}
	
			public override void Run ()
			{
				if (Debug)
					Log.Debug (TAG, "BEGIN mAcceptThread " + this.ToString ());
				
				Name = "AcceptThread";
				BluetoothSocket socket = null;
	
				// Listen to the server socket if we're not connected
				while (_service._state[mmIndex] != BluetoothChatService.STATE_CONNECTED) {
					try {
						// This is a blocking call and will only return on a
						// successful connection or an exception
						socket = mmServerSocket.Accept ();
					} catch (Java.IO.IOException e) {
						Log.Error (TAG, "accept() failed", e);
						break;
					}
					
					// If a connection was accepted
					if (socket != null) {
						lock (this) {
							switch (_service._state[mmIndex]) {
							case STATE_LISTEN:
							case STATE_CONNECTING:
								// Situation normal. Start the connected thread.
								_service.Connected (socket, socket.RemoteDevice, mmIndex);
								break;
							case STATE_NONE:
							case STATE_CONNECTED:
								// Either not ready or already connected. Terminate new socket.
								try {
									socket.Close ();
								} catch (Java.IO.IOException e) {
									Log.Error (TAG, "Could not close unwanted socket", e);
								}
								break;
							}
						}
					}
				}
				
				if (Debug)
					Log.Info (TAG, "END mAcceptThread");
			}
	
			public void Cancel ()
			{
				if (Debug)
					Log.Debug (TAG, "cancel " + this.ToString ());
				
				try {
					mmServerSocket.Close ();
				} catch (Java.IO.IOException e) {
					Log.Error (TAG, "close() of server failed", e);
				}
			}
		}
		
		/// <summary>
		/// This thread runs while attempting to make an outgoing connection
		/// with a device. It runs straight through; the connection either
		/// succeeds or fails.
		/// </summary>
		// TODO: Convert to a .NET thread
		protected class ConnectThread : Thread
		{
			private BluetoothSocket mmSocket;
			private BluetoothDevice mmDevice;
			private BluetoothChatService _service;
			private int mmIndex;
			
			public ConnectThread (BluetoothDevice device, BluetoothChatService service, int index)
			{
				mmDevice = device;
				_service = service;
				BluetoothSocket tmp = null;
				mmIndex = index;
				
				// Get a BluetoothSocket for a connection with the
				// given BluetoothDevice
				try {
					tmp = device.CreateRfcommSocketToServiceRecord (MY_UUID);
				} catch (Java.IO.IOException e) {
					Log.Error (TAG, "create() failed", e);
				}
				mmSocket = tmp;
			}
			
			public override void Run ()
			{
				Log.Info (TAG, "BEGIN mConnectThread");
				Name = "ConnectThread";
	
				// Always cancel discovery because it will slow down a connection
				_service._adapter.CancelDiscovery ();
	
				// Make a connection to the BluetoothSocket
				try {
					// This is a blocking call and will only return on a
					// successful connection or an exception
					mmSocket.Connect ();
				} catch (Java.IO.IOException e) {
					_service.ConnectionFailed (mmIndex);
					// Close the socket
					try {
						mmSocket.Close ();
					} catch (Java.IO.IOException e2) {
						Log.Error (TAG, "unable to close() socket during connection failure", e2);
					}

					// Start the service over to restart listening mode
					_service.Start (mmIndex);
					return;
				}
	
				// Reset the ConnectThread because we're done
				lock (this) {
					_service.connectThread[mmIndex] = null;
				}
	
				// Start the connected thread
				_service.Connected (mmSocket, mmDevice, mmIndex);

				//continuing discovery service
				for (int i = 0; i < SIZE; i++) {

					if (_service._state [i] == BluetoothChatService.STATE_NONE || _service._state [i] == BluetoothChatService.STATE_LISTEN) {

						_service._adapter.StartDiscovery ();
						break;
					}
				}
			}
	
			public void Cancel ()
			{
				try {
					mmSocket.Close ();
				} catch (Java.IO.IOException e) {
					Log.Error (TAG, "close() of connect socket failed", e);
				}
			}
		}
	
		/// <summary>
		/// This thread runs during a connection with a remote device.
		/// It handles all incoming and outgoing transmissions.
		/// </summary>
		// TODO: Convert to a .NET thread
		private class ConnectedThread : Thread
		{
			private BluetoothSocket mmSocket;
			private Stream mmInStream;
			private Stream mmOutStream;
			private BluetoothChatService _service;
			private int mmIndex;
	
			public ConnectedThread (BluetoothSocket socket, BluetoothChatService service, int index)
			{
				Log.Debug (TAG, "create ConnectedThread: ");
				mmSocket = socket;
				_service = service;
				mmIndex = index;
				Stream tmpIn = null;
				Stream tmpOut = null;
	
				// Get the BluetoothSocket input and output streams
				try {
					tmpIn = socket.InputStream;
					tmpOut = socket.OutputStream;
				} catch (Java.IO.IOException e) {
					Log.Error (TAG, "temp sockets not created", e);
				}
	
				mmInStream = tmpIn;
				mmOutStream = tmpOut;
			}
	
			public override void Run ()
			{
				Log.Info (TAG, "BEGIN mConnectedThread");
				byte[] buffer = new byte[1024];
				int bytes;
	
				// Keep listening to the InputStream while connected
				while (true) {
					try {
						// Read from the InputStream
						bytes = mmInStream.Read (buffer, 0, buffer.Length);
	
						// Send the obtained bytes to the UI Activity
						_service._handler.ObtainMessage (BlueHandle.MESSAGE_READ, bytes, -1, buffer)
							.SendToTarget ();
					} catch (Java.IO.IOException e) {
						Log.Error (TAG, "disconnected", e);
						_service.ConnectionLost (mmIndex);
						break;
					}
				}
			}
	
			/// <summary>
			/// Write to the connected OutStream.
			/// </summary>
			/// <param name='buffer'>
			/// The bytes to write
			/// </param>
			public void Write (byte[] buffer)
			{
				try {
					mmOutStream.Write (buffer, 0, buffer.Length);
				} catch (Java.IO.IOException e) {
					Log.Error (TAG, "Exception during write", e);
				}
			}
	
			public void Cancel ()
			{
				try {
					mmSocket.Close ();
				} catch (Java.IO.IOException e) {
					Log.Error (TAG, "close() of connect socket failed", e);
				}
			}
		}
	}
}

