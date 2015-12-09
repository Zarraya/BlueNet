using System;

namespace BlueNet
{
	public struct MessageStruct
	{
		short destination;
		short source;
		bool pass;
		bool type;
		int number;
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

