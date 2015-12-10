using System;
using System.Runtime.InteropServices;
using ProtoBuf;

namespace BlueNet
{
	//[Serializable]
	[ProtoContract]
	public struct MessageStruct
	{
		//const int ARRAY_SIZE = 100000000;

		[ProtoMemberAttribute(1)]
		short destination;
		[ProtoMemberAttribute(2)]
		short source;
		[ProtoMemberAttribute(3)]
		bool pass;
		[ProtoMemberAttribute(4)]
		bool type;
		[ProtoMemberAttribute(5)]
		int number;
		//[MarshalAs(UnmanagedType.ByValArray, SizeConst = ARRAY_SIZE)]
		[ProtoMemberAttribute(6)]
		byte[] data;

		public int Number {
			get {
				return number;
			}
			set{
				number = value;
			}
		}

		public bool Pass {
			get {
				return pass;
			}
			set{ 
				pass = value;
			}
		}

		public byte[] Data {
			get {
				return data;
			}
			set{ data = value;}
		}

		public bool Type {
			get {
				return type;
			}
			set {
				type = value;
			}
				
		}
	}
}

