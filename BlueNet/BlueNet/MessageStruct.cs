using System;
using System.Runtime.InteropServices;
using ProtoBuf;

namespace BlueNet
{
	[Serializable]
	[StructLayout(LayoutKind.Sequential)]
	public struct MessageStruct
	{

		bool setBit;
		bool pass;
		bool type;
		int number;
		string data;


		public bool SetBit {
			get {
				return setBit;
			}
			set{
				setBit = value;
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

		public bool Type {
			get {
				return type;
			}
			set{
				type = value;
			}
		}

		public int Number {
			get {
				return number;
			}
			set{
				number = value;
			}
		}

		public string Data {
			get {
				return data;
			}
			set{
				data = value;
			}
		}
	}
}

