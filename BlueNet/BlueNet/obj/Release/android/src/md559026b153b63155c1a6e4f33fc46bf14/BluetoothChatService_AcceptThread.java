package md559026b153b63155c1a6e4f33fc46bf14;


public class BluetoothChatService_AcceptThread
	extends java.lang.Thread
	implements
		mono.android.IGCUserPeer
{
	static final String __md_methods;
	static {
		__md_methods = 
			"n_run:()V:GetRunHandler\n" +
			"";
		mono.android.Runtime.register ("BlueNet.BluetoothChatService+AcceptThread, BlueNet, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null", BluetoothChatService_AcceptThread.class, __md_methods);
	}


	public BluetoothChatService_AcceptThread () throws java.lang.Throwable
	{
		super ();
		if (getClass () == BluetoothChatService_AcceptThread.class)
			mono.android.TypeManager.Activate ("BlueNet.BluetoothChatService+AcceptThread, BlueNet, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null", "", this, new java.lang.Object[] {  });
	}


	public void run ()
	{
		n_run ();
	}

	private native void n_run ();

	java.util.ArrayList refList;
	public void monodroidAddReference (java.lang.Object obj)
	{
		if (refList == null)
			refList = new java.util.ArrayList ();
		refList.add (obj);
	}

	public void monodroidClearReferences ()
	{
		if (refList != null)
			refList.clear ();
	}
}