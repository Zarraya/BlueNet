using System;
using System.Runtime.InteropServices;
using ProtoBuf;

namespace BlueNet
{
	[Serializable]
	[StructLayout(LayoutKind.Sequential)]
	public struct MessageStruct
	{

		bool setBit { get; set; }
		bool pass{ get; set; }
		bool type{ get; set; }
		int number{ get; set; }
		string data{ get; set; }


	}
}

