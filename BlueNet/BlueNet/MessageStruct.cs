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

		[MarshalAs(UnmanagedType.ByValArray, SizeConst = 100000000)]
		string data;

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

		public string Data {
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

