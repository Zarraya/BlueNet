using System;
using System.Runtime.InteropServices;
using ProtoBuf;

namespace BlueNet
{
	[Serializable]
	[StructLayout(LayoutKind.Sequential)]
	public struct MessageStruct
	{
		//const int ARRAY_SIZE = 100000000;

		bool pass;

		bool type;

		int number;

		[MarshalAs(UnmanagedType.LPStr, SizeConst = 50)]
		byte[] data;


	}
}

