using System.Collections;

// ReSharper disable once CheckNamespace
namespace NBitcoin
{

	class BitWriter
	{
		List<bool> values = new List<bool>();
		public void Write(bool value)
		{
			values.Insert(Position, value);
			_Position++;
		}

		internal void Write(byte[] bytes)
		{
			Write(bytes, bytes.Length * 8);
		}

		public void Write(byte[] bytes, int bitCount)
		{
			bytes = SwapEndianBytes(bytes);
			BitArray array = new BitArray(bytes);
			values.InsertRange(Position, array.OfType<bool>().Take(bitCount));
			_Position += bitCount;
		}

		public byte[] ToBytes()
		{
			var array = ToBitArray();
			var bytes = ToByteArray(array);
			bytes = SwapEndianBytes(bytes);
			return bytes;
		}

		//BitArray.CopyTo do not exist in portable lib
		static byte[] ToByteArray(BitArray bits)
		{
			int arrayLength = bits.Length / 8;
			if (bits.Length % 8 != 0)
				arrayLength++;
			byte[] array = new byte[arrayLength];

			for (int i = 0; i < bits.Length; i++)
			{
				int b = i / 8;
				int offset = i % 8;
				array[b] |= bits.Get(i) ? (byte)(1 << offset) : (byte)0;
			}
			return array;
		}


		public BitArray ToBitArray()
		{
			return new BitArray(values.ToArray());
		}

		public int[] ToIntegers()
		{
			var array = new BitArray(values.ToArray());
			return Wordlist.ToIntegers(array);
		}


		static byte[] SwapEndianBytes(byte[] bytes)
		{
			byte[] output = new byte[bytes.Length];
			for (int i = 0; i < output.Length; i++)
			{
				byte newByte = 0;
				for (int ib = 0; ib < 8; ib++)
				{
					newByte += (byte)(((bytes[i] >> ib) & 1) << (7 - ib));
				}
				output[i] = newByte;
			}
			return output;
		}


		int _Position;
		public int Position
		{
			get => _Position;
			set => _Position = value;
		}
		public void Write(BitArray bitArray, int bitCount)
		{
			for (int i = 0; i < bitCount; i++)
			{
				Write(bitArray.Get(i));
			}
		}
	}

}
